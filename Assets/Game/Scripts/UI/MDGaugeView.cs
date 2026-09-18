using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Brass pressure-gauge price indicator (GDD 5). The needle sweeps towards its new
/// reading rather than snapping, which is the "gauge-needle sweep on price movement"
/// effect the GDD calls for.
///
/// ponytail: flat coloured rects stand in for the brass dial art. Give the dial and
/// needle Images their sprites when the art lands; the maths below does not change.
/// </summary>
public class MDGaugeView
{
    const float MaxReadingPct = 30f;    // +/-30% pins the needle at the end of its travel
    const float MaxAngle = 120f;
    const float SweepSpeed = 260f;      // degrees per second

    /// <summary>The paper surface holding the dial and labels.</summary>
    public RectTransform Root { get; private set; }

    /// <summary>The brass-edged frame around <see cref="Root"/>. This is what layout groups size.</summary>
    public RectTransform Outer => (RectTransform)Root.parent;

    Image _dial;
    RectTransform _needle;
    TMPro.TMP_Text _name, _price, _delta, _owned;

    float _angle, _targetAngle;
    bool _usesArt;
    float _punch;               // brief scale kick when the reading changes

    public static MDGaugeView Create(Transform parent, string name, int nameSize = 26)
    {
        var view = new MDGaugeView();

        var card = MDUIKit.Card(parent, "Gauge_" + name, MDUIKit.Paper);
        view.Root = card;
        MDUIKit.VStack(card, 4f, 10, TextAnchor.MiddleCenter);

        var dialHolder = MDUIKit.Rect(card, "DialHolder");
        MDUIKit.Size(dialHolder, 150f);

        view._dial = MDUIKit.Panel(dialHolder, "Dial", MDUIKit.PaperDark);
        var drt = view._dial.rectTransform;
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.sizeDelta = new Vector2(140f, 140f);

        // The brass gauge art paints its own needle into each face, so the dial swaps
        // sprite by price direction instead of rotating a separate needle part.
        view._usesArt = MDSkin.GaugeNeutral != null;
        if (view._usesArt)
        {
            MDUIKit.Skin(view._dial, MDSkin.GaugeNeutral, sliced: false);
            view._dial.preserveAspect = true;
            drt.sizeDelta = new Vector2(150f, 150f);
        }
        else
        {
            // Placeholder fallback: a flat dial with a needle we can actually rotate.
            view._needle = MDUIKit.Rect(drt, "Needle");
            view._needle.anchorMin = view._needle.anchorMax = new Vector2(0.5f, 0.5f);
            view._needle.pivot = new Vector2(0.5f, 0f);
            view._needle.sizeDelta = new Vector2(7f, 58f);
            view._needle.anchoredPosition = Vector2.zero;
            var needleImg = view._needle.gameObject.AddComponent<Image>();
            needleImg.color = MDUIKit.Bad;
            needleImg.raycastTarget = false;
        }

        view._name = MDUIKit.Size(MDUIKit.Label(card, name, nameSize, MDUIKit.Ink,
            TextAnchor.MiddleCenter, FontStyle.Bold), 34f);
        view._price = MDUIKit.Size(MDUIKit.Label(card, "$0.00", 32, MDUIKit.Ink,
            TextAnchor.MiddleCenter, FontStyle.Bold), 40f);
        view._delta = MDUIKit.Size(MDUIKit.Label(card, "+0.0%", 26, MDUIKit.Muted), 32f);
        view._owned = MDUIKit.Size(MDUIKit.Label(card, "", 22, MDUIKit.BrassDark,
            TextAnchor.MiddleCenter, FontStyle.Bold), 28f);

        return view;
    }

    public void Bind(MDGame game, int company)
    {
        var co = game.Scenario.Companies[company];
        float pct = game.PriceChangePct(company);

        _name.text = co.Name.ToUpperInvariant();
        _price.text = MDUIKit.Money2(game.Price[company]);
        _delta.text = MDUIKit.Pct(pct);
        _delta.color = MDUIKit.PnLColor(pct);

        if (_usesArt)
        {
            var face = MDSkin.GaugeFor(pct);
            if (_dial.sprite != face)
            {
                _dial.sprite = face;
                _punch = 1f;        // the face changed, so give it a kick
            }
        }
        else
        {
            _dial.color = Color.Lerp(MDUIKit.PaperDark, MDUIKit.PnLColor(pct), 0.22f);
        }

        int shares = game.Holdings[company].Shares;
        _owned.text = shares > 0 ? "OWNED " + shares + " sh" : "";

        _targetAngle = -Mathf.Clamp(pct / MaxReadingPct, -1f, 1f) * MaxAngle;
    }

    /// <summary>Driven from the owning MonoBehaviour's Update so gauges need no component.</summary>
    public void Tick(float deltaTime)
    {
        if (_usesArt)
        {
            // Stand-in for the needle sweep: the dial kicks when its face changes.
            if (_punch <= 0f) return;
            _punch = Mathf.Max(0f, _punch - deltaTime * 3.5f);
            float s = 1f + Mathf.Sin(_punch * Mathf.PI) * 0.12f;
            _dial.rectTransform.localScale = new Vector3(s, s, 1f);
            return;
        }

        if (_needle == null || Mathf.Approximately(_angle, _targetAngle)) return;
        _angle = Mathf.MoveTowards(_angle, _targetAngle, SweepSpeed * deltaTime);
        _needle.localRotation = Quaternion.Euler(0f, 0f, _angle);
    }

    /// <summary>Place the needle without a sweep, e.g. when a screen first opens.</summary>
    public void SnapNeedle()
    {
        _punch = 0f;
        if (_needle == null) return;
        _angle = _targetAngle;
        _needle.localRotation = Quaternion.Euler(0f, 0f, _angle);
    }

    /// <summary>
    /// Park the needle at an arbitrary reading. Used to seat it at the pre-event price so
    /// the following Bind() sweeps it across to the post-event one.
    /// </summary>
    public void SetReadingImmediate(float pct)
    {
        if (_usesArt)
        {
            _dial.sprite = MDSkin.GaugeFor(pct);   // seat the pre-event face, no kick
            _punch = 0f;
            return;
        }
        _angle = -Mathf.Clamp(pct / MaxReadingPct, -1f, 1f) * MaxAngle;
        _targetAngle = _angle;
        if (_needle != null) _needle.localRotation = Quaternion.Euler(0f, 0f, _angle);
    }
}
