using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu scene: main menu, scenario-select carousel, lifetime stats and settings.
/// Scenario select is a horizontal card carousel, never a grid (GDD 6 / 10).
/// </summary>
public class MDMainMenu : MonoBehaviour
{
    RectTransform _root;
    GameObject _menuPanel, _selectPanel, _statsPanel, _settingsPanel;
    Button _resetButton, _musicButton, _sfxButton;
    bool _resetArmed;

    void RefreshAudioLabels()
    {
        MDUIKit.BtnLabel(_musicButton).text = "MUSIC:  " + (MDAudio.MusicOn ? "ON" : "OFF");
        MDUIKit.BtnLabel(_sfxButton).text = "SOUND EFFECTS:  " + (MDAudio.SfxOn ? "ON" : "OFF");
    }

    void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.orientation = ScreenOrientation.Portrait;

        var canvas = MDUIKit.CreateCanvas("MDMenuCanvas");
        _root = MDUIKit.Screen_(canvas, "Menu");

        MDAudio.PlayMusic();

        _menuPanel = BuildMenu();
        _selectPanel = BuildScenarioSelect();
        _statsPanel = BuildStats();
        _settingsPanel = BuildSettings();

        Show(MDApp.ReturnToScenarioSelect ? _selectPanel : _menuPanel);
        MDApp.ReturnToScenarioSelect = false;
    }

    void Show(GameObject panel)
    {
        _menuPanel.SetActive(panel == _menuPanel);
        _selectPanel.SetActive(panel == _selectPanel);
        _statsPanel.SetActive(panel == _statsPanel);
        _settingsPanel.SetActive(panel == _settingsPanel);
        _resetArmed = false;
        if (_resetButton != null) MDUIKit.BtnLabel(_resetButton).text = "RESET PROGRESS";
    }

    RectTransform NewPanel(string name)
    {
        var rt = MDUIKit.Rect(_root, name);
        MDUIKit.Fill(rt);
        return rt;
    }

    /// <summary>Brass header bar with an optional back arrow.</summary>
    void Header(RectTransform parent, string title, string subtitle, System.Action onBack)
    {
        var bar = MDUIKit.SkinnedCard(parent, "Header", MDSkin.Bar, MDUIKit.Leather, 12f);
        MDUIKit.Size(bar.parent as RectTransform, 170f);

        var col = MDUIKit.Rect(bar, "Col");
        MDUIKit.Fill(col, 10f);
        var colStack = MDUIKit.VStack(col, 2f, 6, TextAnchor.MiddleCenter);
        // Keep the centred title clear of the back arrow on narrow screens.
        if (onBack != null) colStack.padding = new RectOffset(180, 180, 6, 6);
        MDUIKit.Size(MDUIKit.Label(col, title, 56, MDUIKit.Brass, TextAnchor.MiddleCenter, FontStyle.Bold), 70f);
        if (!string.IsNullOrEmpty(subtitle))
            MDUIKit.Size(MDUIKit.Label(col, subtitle, 28, MDUIKit.OnDark), 40f);

        if (onBack == null) return;

        var back = MDUIKit.Btn(bar, "BACK", MDUIKit.Brass, MDUIKit.Ink, 30, onBack);
        var brt = back.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 0.5f);
        brt.anchorMax = new Vector2(0f, 0.5f);
        brt.pivot = new Vector2(0f, 0.5f);
        brt.anchoredPosition = new Vector2(18f, 0f);
        brt.sizeDelta = new Vector2(150f, 78f);
    }

    // ---------------- main menu ----------------

    GameObject BuildMenu()
    {
        var panel = NewPanel("MainMenu");
        MDUIKit.VStack(panel, 26f, 40, TextAnchor.MiddleCenter);

        MDUIKit.Size(MDUIKit.Label(panel, "MARKET DAY", 92, MDUIKit.Brass,
            TextAnchor.MiddleCenter, FontStyle.Bold), 130f);
        MDUIKit.Size(MDUIKit.Label(panel, "A Fictional Market Strategy Game", 30, MDUIKit.OnDark), 46f);

        // The three brass gauges from the key art (GDD 5).
        var gauges = MDUIKit.Rect(panel, "Gauges");
        MDUIKit.Size(gauges, 230f);
        MDUIKit.HStack(gauges, 22f, 10);
        var faces = new[] { MDSkin.GaugeUp, MDSkin.GaugeFlat, MDSkin.GaugeNeutral };
        var labels = new[] { "ANALYSE", "DECIDE", "GROW" };
        for (int i = 0; i < labels.Length; i++)
        {
            var g = MDUIKit.Rect(gauges, "Gauge");
            MDUIKit.VStack(g, 4f, 4, TextAnchor.MiddleCenter);
            var dial = MDUIKit.Panel(g, "Dial", MDUIKit.BrassDark, faces[i], sliced: false);
            dial.preserveAspect = true;
            MDUIKit.Size(dial, 150f, 150f, 0f, 0f);
            MDUIKit.Size(MDUIKit.Label(g, labels[i], 24, MDUIKit.Brass,
                TextAnchor.MiddleCenter, FontStyle.Bold), 34f);
        }

        MDUIKit.Size(MDUIKit.Label(panel, "\"Good decisions build better futures.\"", 28, MDUIKit.OnDark,
            TextAnchor.MiddleCenter, FontStyle.Italic), 56f);

        MenuButton(panel, "PLAY", () =>
        {
            MDApp.SelectedScenario = Mathf.Max(0, MDSave.HighestUnlocked());
            MDApp.Load(MDApp.SceneGameplay);
        });
        MenuButton(panel, "SCENARIOS", () => Show(_selectPanel));
        MenuButton(panel, "STATS", () => Show(_statsPanel));
        MenuButton(panel, "SETTINGS", () => Show(_settingsPanel));

        return panel.gameObject;
    }

    void MenuButton(RectTransform parent, string label, System.Action onClick)
    {
        var b = MDUIKit.Btn(parent, label, MDUIKit.Brass, MDUIKit.Ink, 44, onClick);
        MDUIKit.Size(b, 112f, 620f, 0f, 0f);
    }

    // ---------------- scenario select (carousel) ----------------

    GameObject BuildScenarioSelect()
    {
        var panel = NewPanel("ScenarioSelect");
        MDUIKit.VStack(panel, 14f, 20, TextAnchor.UpperCenter);

        Header(panel, "SELECT SCENARIO", "Each scenario presents a unique market situation.",
            () => Show(_menuPanel));

        // GDD 10: horizontal scrolling card row. A grid here is an explicit reject.
        var track = MDUIKit.Rect(panel, "Carousel");
        MDUIKit.Size(track, 1080f, -1f, 1f, 1f);
        var content = MDUIKit.Scroll(track, "CarouselScroll", horizontal: true, spacing: 26f, pad: 40);

        for (int i = 0; i < MDContent.Count; i++)
            BuildScenarioCard(content, i);

        MDUIKit.Size(MDUIKit.Label(panel, "Swipe to browse", 26, MDUIKit.OnDark), 44f);
        return panel.gameObject;
    }

    void BuildScenarioCard(RectTransform parent, int index)
    {
        var scenario = MDContent.Get(index);
        bool unlocked = MDSave.IsUnlocked(index);
        int earned = MDSave.Stars(index);

        var outer = MDUIKit.SkinnedCard(parent, "Card" + index, MDSkin.Panel, unlocked ? MDUIKit.Paper : MDUIKit.WoodLight, 42f, padTop: 8f);
        MDUIKit.Size(outer.parent as RectTransform, -1f, 620f, 0f, 0f);

        MDUIKit.VStack(outer, 8f, 8, TextAnchor.UpperCenter);

        // The scenario number goes first so it lands on the card's wood title plate.
        MDUIKit.Size(MDUIKit.Label(outer, "SCENARIO " + (index + 1).ToString("00"), 28, MDUIKit.Brass,
            TextAnchor.MiddleCenter, FontStyle.Bold), 78f);

        // Brass medallion plate. Locked scenarios get it dimmed rather than hidden.
        var plate = MDUIKit.Panel(outer, "Plate", unlocked ? MDUIKit.BrassDark : MDUIKit.Wood,
            MDSkin.ScenarioPlate, sliced: false);
        plate.preserveAspect = true;
        if (MDSkin.ScenarioPlate != null && !unlocked) plate.color = new Color(0.45f, 0.40f, 0.34f, 1f);
        MDUIKit.Size(plate, 200f);

        MDUIKit.Size(MDUIKit.Label(outer, unlocked ? scenario.Name : "LOCKED", 40,
            unlocked ? MDUIKit.Ink : MDUIKit.Muted, TextAnchor.MiddleCenter, FontStyle.Bold), 62f);

        // Authored difficulty, then the player's best result.
        MDUIKit.Size(MDUIKit.Label(outer, "DIFFICULTY", 20, MDUIKit.Muted), 28f);
        MDUIKit.Size(MDUIKit.StarRow(outer, scenario.Difficulty, 3, 28f), 40f);

        MDUIKit.Size(MDUIKit.Label(outer,
            unlocked ? scenario.Blurb : "Complete the previous scenario to unlock.",
            26, unlocked ? MDUIKit.Ink : MDUIKit.Muted, TextAnchor.UpperCenter, FontStyle.Italic), 100f);

        if (unlocked && earned > 0)
        {
            MDUIKit.Size(MDUIKit.Label(outer, "YOUR BEST", 20, MDUIKit.Muted), 28f);
            MDUIKit.Size(MDUIKit.StarRow(outer, earned, 3, 28f), 40f);
        }
        else
        {
            MDUIKit.Size(MDUIKit.Label(outer, unlocked ? "Not yet cleared" : "", 24, MDUIKit.Muted), 68f);
        }

        var spacer = MDUIKit.Rect(outer, "Spacer");
        MDUIKit.Size(spacer, 0f, -1f, -1f, 1f);

        var play = MDUIKit.Btn(outer, unlocked ? "PLAY" : "LOCKED",
            unlocked ? MDUIKit.Brass : MDUIKit.Muted, MDUIKit.Ink, 38,
            () =>
            {
                MDApp.SelectedScenario = index;
                MDApp.Load(MDApp.SceneGameplay);
            });
        play.interactable = unlocked;
        MDUIKit.Size(play, 96f);
    }


    // ---------------- stats ----------------

    GameObject BuildStats()
    {
        var panel = NewPanel("Stats");
        MDUIKit.VStack(panel, 16f, 30, TextAnchor.UpperCenter);

        Header(panel, "YOUR RECORD", "Lifetime results across every scenario.", () => Show(_menuPanel));

        var ledger = MDUIKit.SkinnedCard(panel, "Ledger", MDSkin.Ledger, MDUIKit.Paper, 40f);
        MDUIKit.Size(ledger.parent as RectTransform, 620f);
        MDUIKit.VStack(ledger, 6f, 28, TextAnchor.UpperCenter);

        StatRow(ledger, "Scenarios Won", MDSave.ScenariosWon.ToString());
        StatRow(ledger, "Perfect Reads", MDSave.PerfectReads.ToString());
        StatRow(ledger, "Scenarios Cleared", MDSave.ScenariosCleared + " / " + MDContent.Count);

        int totalStars = 0;
        for (int i = 0; i < MDContent.Count; i++) totalStars += MDSave.Stars(i);
        StatRow(ledger, "Stars Earned", totalStars + " / " + (MDContent.Count * 3));

        MDUIKit.Size(MDUIKit.Label(panel, "\"Knowledge reduces risk.\"", 30, MDUIKit.OnDark,
            TextAnchor.MiddleCenter, FontStyle.Italic), 60f);

        return panel.gameObject;
    }

    static void StatRow(RectTransform parent, string label, string value)
    {
        var row = MDUIKit.Rect(parent, "Row");
        MDUIKit.Size(row, 86f);
        MDUIKit.HStack(row, 8f, 6);
        MDUIKit.Label(row, label, 34, MDUIKit.Ink, TextAnchor.MiddleLeft);
        MDUIKit.Label(row, value, 38, MDUIKit.BrassDark, TextAnchor.MiddleRight, FontStyle.Bold);
    }

    // ---------------- settings ----------------

    GameObject BuildSettings()
    {
        var panel = NewPanel("Settings");
        MDUIKit.VStack(panel, 22f, 40, TextAnchor.UpperCenter);

        Header(panel, "SETTINGS", null, () => Show(_menuPanel));

        _musicButton = MDUIKit.Btn(panel, "", MDUIKit.Brass, MDUIKit.Ink, 34, () =>
        {
            MDAudio.ToggleMusic();
            if (MDAudio.MusicOn) MDAudio.PlayMusic(); else MDAudio.StopMusic();
            RefreshAudioLabels();
        });
        MDUIKit.Size(_musicButton, 104f, 620f, 0f, 0f);

        _sfxButton = MDUIKit.Btn(panel, "", MDUIKit.Brass, MDUIKit.Ink, 34, () =>
        {
            MDAudio.ToggleSfx();
            RefreshAudioLabels();
        });
        MDUIKit.Size(_sfxButton, 104f, 620f, 0f, 0f);
        RefreshAudioLabels();

        MDUIKit.Size(MDUIKit.Label(panel,
            "Market Day is a fictional strategy game. All companies, headlines and prices " +
            "are invented for play and are not market data.",
            28, MDUIKit.OnDark, TextAnchor.UpperCenter), 170f);

        // Destructive, so it needs a second tap to confirm.
        _resetButton = MDUIKit.Btn(panel, "RESET PROGRESS", MDUIKit.Bad, MDUIKit.Paper, 36, OnResetPressed);
        MDUIKit.Size(_resetButton, 110f, 620f, 0f, 0f);

        var spacer = MDUIKit.Rect(panel, "Spacer");
        MDUIKit.Size(spacer, 0f, -1f, -1f, 1f);

        MDUIKit.Size(MDUIKit.Label(panel, "v0.1 placeholder build", 24, MDUIKit.OnDark), 40f);
        return panel.gameObject;
    }

    void OnResetPressed()
    {
        if (!_resetArmed)
        {
            _resetArmed = true;
            MDUIKit.BtnLabel(_resetButton).text = "TAP AGAIN TO ERASE ALL PROGRESS";
            return;
        }

        MDSave.ResetAll();
        _resetArmed = false;
        MDUIKit.BtnLabel(_resetButton).text = "PROGRESS RESET";
    }
}
