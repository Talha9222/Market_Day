using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Splash screen. The progress bar tracks real work - building the scenario table and
/// streaming in the menu scene - rather than counting down a fake timer. It is held to a
/// minimum dwell so it never flashes past on a fast device.
/// </summary>
public class MDSplash : MonoBehaviour
{
    const float FadeInSeconds = 0.5f;
    const float FadeOutSeconds = 0.4f;
    const float MinimumSeconds = 2.4f;     // floor on how fast the bar may fill

    // Shown as the bar passes each threshold.
    static readonly (float at, string text)[] Status =
    {
        (0.00f, "UNLOCKING THE TRADING DESK"),
        (0.18f, "POLISHING THE BRASS"),
        (0.38f, "FILING THE DOSSIERS"),
        (0.58f, "PINNING THE EVENT CARDS"),
        (0.78f, "COUNTING RESEARCH TOKENS"),
        (0.94f, "RINGING THE OPENING BELL"),
    };

    CanvasGroup _group;
    RectTransform _bell, _barFill;
    TMPro.TMP_Text _status, _percent;

    void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.orientation = ScreenOrientation.Portrait;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        MDSelfCheck.RunAll();
#endif

        Build();
    }

    void Start()
    {
        MDAudio.PlayMusic();
        StartCoroutine(Run());
    }

    void Update()
    {
        // Slow breathing pulse on the bell so the screen is never completely static.
        if (_bell == null) return;
        float s = 1f + Mathf.Sin(Time.unscaledTime * 2.2f) * 0.035f;
        _bell.localScale = new Vector3(s, s, 1f);
    }

    void Build()
    {
        var canvas = MDUIKit.CreateCanvas("MDSplashCanvas");
        _group = canvas.gameObject.AddComponent<CanvasGroup>();
        _group.alpha = 0f;

        var root = MDUIKit.Screen_(canvas, "Splash");
        MDUIKit.VStack(root, 16f, 30, TextAnchor.MiddleCenter);

        var topSpacer = MDUIKit.Rect(root, "TopSpacer");
        MDUIKit.Size(topSpacer, 0f, -1f, -1f, 1f);

        var bell = MDUIKit.Panel(root, "Bell", MDUIKit.Brass, MDSkin.Bell, sliced: false);
        bell.preserveAspect = true;
        MDUIKit.Size(bell, 250f, 250f, 0f, 0f);
        _bell = bell.rectTransform;

        MDUIKit.Size(MDUIKit.Label(root, "MARKET DAY", 96, MDUIKit.Brass,
            TextAnchor.MiddleCenter, FontStyle.Bold), 124f);
        MDUIKit.Size(MDUIKit.Label(root, "A Fictional Market Strategy Game", 34, MDUIKit.OnDark), 50f);
        MDUIKit.Size(MDUIKit.Label(root, "\"Good decisions build better futures.\"", 28, MDUIKit.OnDark,
            TextAnchor.MiddleCenter, FontStyle.Italic), 46f);

        var midSpacer = MDUIKit.Rect(root, "MidSpacer");
        MDUIKit.Size(midSpacer, 0f, -1f, -1f, 1f);

        _status = MDUIKit.Size(MDUIKit.Label(root, Status[0].text, 30, MDUIKit.Brass,
            TextAnchor.MiddleCenter, FontStyle.Bold), 44f);

        // Brass-edged bar: dark wood track with a brass fill anchored from the left.
        var track = MDUIKit.SkinnedCard(root, "LoadBar", MDSkin.Bar, MDUIKit.Wood, 14f);
        MDUIKit.Size(track.parent as RectTransform, 46f);
        var fill = MDUIKit.Panel(track, "BarFill", MDUIKit.Brass);
        _barFill = fill.rectTransform;
        _barFill.anchorMin = Vector2.zero;
        _barFill.anchorMax = new Vector2(0f, 1f);
        _barFill.offsetMin = Vector2.zero;
        _barFill.offsetMax = Vector2.zero;

        _percent = MDUIKit.Size(MDUIKit.Label(root, "0%", 26, MDUIKit.OnDark), 38f);

        var bottomSpacer = MDUIKit.Rect(root, "BottomSpacer");
        MDUIKit.Size(bottomSpacer, 40f);
    }

    IEnumerator Run()
    {
        yield return Fade(0f, 1f, FadeInSeconds);

        float started = Time.unscaledTime;

        // Real work: force the scenario table to build (and reorder) before the menu opens.
        yield return null;
        var warm = MDContent.All;
        SetProgress(warm.Length > 0 ? 0.05f : 0f);
        yield return null;

        var load = SceneManager.LoadSceneAsync(MDApp.SceneMenu);
        load.allowSceneActivation = false;

        float shown = 0f;
        while (true)
        {
            // progress stalls at 0.9 while activation is held back, so rescale it.
            float real = Mathf.Clamp01(load.progress / 0.9f);
            float dwell = Mathf.Clamp01((Time.unscaledTime - started) / MinimumSeconds);
            float target = Mathf.Min(real, dwell);

            shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 1.4f);
            SetProgress(shown);

            if (shown >= 0.999f && real >= 0.999f) break;
            yield return null;
        }

        SetProgress(1f);
        MDAudio.Play(MDAudio.MarketEvent, 0.6f);
        yield return new WaitForSecondsRealtime(0.35f);

        yield return Fade(1f, 0f, FadeOutSeconds);
        load.allowSceneActivation = true;
    }

    void SetProgress(float t)
    {
        t = Mathf.Clamp01(t);
        _barFill.anchorMax = new Vector2(t, 1f);
        _percent.text = Mathf.RoundToInt(t * 100f) + "%";

        for (int i = Status.Length - 1; i >= 0; i--)
        {
            if (t >= Status[i].at)
            {
                if (_status.text != Status[i].text) _status.text = Status[i].text;
                return;
            }
        }
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            _group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        _group.alpha = to;
    }
}
