using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gameplay scene. Drives the whole loop (GDD 2): Opening Bell decision window ->
/// Company Dossier -> Market Event -> further decision window -> Scenario Complete.
/// Portfolio, pause and result screens live in their own files.
/// </summary>
public class MDGameplay : MonoBehaviour
{
    enum Screen_ { Bell, Dossier, Event, Portfolio, Pause, Complete }

    MDGame _game;
    Screen_ _current;
    int _focus;                 // company shown in the dossier card / opened in the dossier screen

    RectTransform _root;
    RectTransform _bellPanel, _dossierPanel, _eventPanel;
    MDPortfolioPanel _portfolio;
    MDPausePanel _pause;
    MDCompletePanel _complete;

    // Opening Bell widgets
    TMP_Text _dayLabel, _tokenLabel, _objectiveLabel, _cashLabel, _valueLabel, _pnlLabel;
    TMP_Text _focusName, _focusTagline, _focusClueTitle, _focusClueBody, _focusHolding;
    MDGaugeView[] _bellGauges;
    Button _investigateBtn, _buyBtn, _sellBtn, _continueBtn;
    TMP_Text _toast;
    Coroutine _toastRoutine;

    // Dossier widgets
    TMP_Text _dossierName, _dossierTagline, _dossierProgress, _dossierBlurb;
    MDGaugeView _dossierGauge;
    RectTransform _clueList;

    // Market Event widgets
    TMP_Text _eventHeadline, _eventBody, _eventDay;
    MDGaugeView[] _eventGauges;

    void Awake()
    {
        Application.targetFrameRate = 60;
        UnityEngine.Screen.orientation = ScreenOrientation.Portrait;

        _game = new MDGame(MDApp.SelectedScenario);
        MDAudio.PlayMusic();

        var canvas = MDUIKit.CreateCanvas("MDGameplayCanvas");
        _root = MDUIKit.Screen_(canvas, "Gameplay");

        _bellPanel = BuildBell();
        _dossierPanel = BuildDossier();
        _eventPanel = BuildEvent();
        _portfolio = MDPortfolioPanel.Create(_root, () => Show(Screen_.Bell));
        _pause = MDPausePanel.Create(_root, () => Show(Screen_.Bell), Restart, ToMenu);
        _complete = MDCompletePanel.Create(_root, NextScenario, Restart, ToMenu);

        Show(Screen_.Bell);
        foreach (var g in _bellGauges) g.SnapNeedle();
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        foreach (var g in _bellGauges) g.Tick(dt);
        foreach (var g in _eventGauges) g.Tick(dt);
        _dossierGauge.Tick(dt);
    }

    // ---------------- navigation ----------------

    void Show(Screen_ screen)
    {
        _current = screen;
        _bellPanel.gameObject.SetActive(screen == Screen_.Bell);
        _dossierPanel.gameObject.SetActive(screen == Screen_.Dossier);
        _eventPanel.gameObject.SetActive(screen == Screen_.Event);
        _portfolio.Root.gameObject.SetActive(screen == Screen_.Portfolio);
        _pause.Root.gameObject.SetActive(screen == Screen_.Pause);
        _complete.Root.gameObject.SetActive(screen == Screen_.Complete);

        if (screen == Screen_.Bell) RefreshBell();
        else if (screen == Screen_.Dossier) RefreshDossier();
        else if (screen == Screen_.Portfolio) _portfolio.Refresh(_game);
    }

    void Restart()
    {
        _game.Restart();
        _focus = 0;
        Show(Screen_.Bell);
        foreach (var g in _bellGauges) g.SnapNeedle();
    }

    void ToMenu()
    {
        MDApp.ReturnToScenarioSelect = true;
        MDApp.Load(MDApp.SceneMenu);
    }

    void NextScenario()
    {
        int next = _game.ScenarioIndex + 1;
        if (next >= MDContent.Count || !MDSave.IsUnlocked(next)) { ToMenu(); return; }
        MDApp.SelectedScenario = next;
        MDApp.Load(MDApp.SceneGameplay);
    }

    /// <summary>End of a decision window: fire the next event, or resolve the scenario.</summary>
    void OnContinue()
    {
        if (_game.HasMoreEvents)
        {
            var before = (float[])_game.Price.Clone();
            var fired = _game.AdvanceEvent();
            ShowEvent(fired, before);
            return;
        }

        var result = _game.Resolve();
        MDSave.RecordResult(_game.ScenarioIndex, result.Won, result.Stars);
        MDAudio.Play(result.Won ? MDAudio.Win : MDAudio.Lose);
        bool hasNext = _game.ScenarioIndex + 1 < MDContent.Count;
        _complete.Show(_game, result, hasNext);
        Show(Screen_.Complete);
    }

    // ---------------- Opening Bell ----------------

    RectTransform BuildBell()
    {
        var panel = MDUIKit.Rect(_root, "OpeningBell");
        MDUIKit.Fill(panel);
        MDUIKit.VStack(panel, 10f, 16, TextAnchor.UpperCenter);

        // Top bar: day counter, scenario name, research tokens.
        var bar = MDUIKit.SkinnedCard(panel, "TopBar", MDSkin.Bar, MDUIKit.Leather, 10f);
        MDUIKit.Size(bar.parent as RectTransform, 126f);
        MDUIKit.HStack(bar, 10f, 34, TextAnchor.MiddleCenter, padV: 4);
        _dayLabel = MDUIKit.Label(bar, "", 28, MDUIKit.Paper, TextAnchor.MiddleLeft);

        var titleCol = MDUIKit.Rect(bar, "Title");
        MDUIKit.VStack(titleCol, 0f, 0, TextAnchor.MiddleCenter);
        MDUIKit.Size(MDUIKit.Label(titleCol, "OPENING BELL", 40, MDUIKit.Brass,
            TextAnchor.MiddleCenter, FontStyle.Bold), 48f);
        MDUIKit.Size(MDUIKit.Label(titleCol, _game.Scenario.Name, 24, MDUIKit.OnDark), 32f);

        _tokenLabel = MDUIKit.Label(bar, "", 28, MDUIKit.Brass, TextAnchor.MiddleRight, FontStyle.Bold);

        // Scenario objective card (GDD: must be a primary visual element, not chrome).
        // The framed panel is used rather than the illustrated objective card, whose
        // centre decoration sits straight under the text. Its wood plate carries the
        // heading, with the objectives themselves on clear paper below.
        var obj = MDUIKit.SkinnedCard(panel, "Objective", MDSkin.Panel, MDUIKit.Paper, 34f, padTop: 8f);
        MDUIKit.Size(obj.parent as RectTransform, 250f);
        MDUIKit.VStack(obj, 6f, 8, TextAnchor.UpperCenter);
        MDUIKit.Size(MDUIKit.Label(obj, "SCENARIO OBJECTIVE", 26, MDUIKit.Brass,
            TextAnchor.MiddleCenter, FontStyle.Bold), 78f);
        _objectiveLabel = MDUIKit.Size(MDUIKit.Label(obj, "", 26, MDUIKit.Ink, TextAnchor.UpperLeft),
            -1f, -1f, -1f, 1f);

        // Three gauges; tapping one focuses that company below.
        var gaugeRow = MDUIKit.Rect(panel, "Gauges");
        MDUIKit.Size(gaugeRow, 336f);
        MDUIKit.HStack(gaugeRow, 12f, 4);

        _bellGauges = new MDGaugeView[_game.CompanyCount];
        for (int i = 0; i < _game.CompanyCount; i++)
        {
            int index = i;
            _bellGauges[i] = MDGaugeView.Create(gaugeRow, _game.Scenario.Companies[i].Name, 22);
            var btn = _bellGauges[i].Outer.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() => { _focus = index; RefreshBell(); });
        }

        // Focused company: the dossier card the reviewer should read as "research".
        // padTop 8 puts the company name on the panel's wood plate, in brass, where it
        // reads as an engraved nameplate instead of dark-on-dark.
        var focus = MDUIKit.SkinnedCard(panel, "FocusCard", MDSkin.Panel, MDUIKit.Paper, 40f, padTop: 8f);
        MDUIKit.Size(focus.parent as RectTransform, -1f, -1f, -1f, 1f);
        MDUIKit.VStack(focus, 4f, 8, TextAnchor.UpperCenter);

        _focusName = MDUIKit.Size(MDUIKit.Label(focus, "", 40, MDUIKit.Brass,
            TextAnchor.MiddleCenter, FontStyle.Bold), 80f);
        _focusTagline = MDUIKit.Size(MDUIKit.Label(focus, "", 24, MDUIKit.Muted,
            TextAnchor.MiddleCenter, FontStyle.Italic), 36f);

        // A flat paper card, not the illustrated clue art: that art stretches under a
        // block of text and the drawing shows through behind the words.
        var clue = MDUIKit.Card(focus, "ClueCard", MDUIKit.Paper);
        MDUIKit.Size(clue.parent as RectTransform, -1f, -1f, -1f, 1f);
        // Tight padding: on the narrowest phone the headline wraps to two lines and the
        // card has the least room to give.
        MDUIKit.VStack(clue, 4f, 8, TextAnchor.UpperCenter);
        _focusClueTitle = MDUIKit.Size(MDUIKit.Label(clue, "", 30, MDUIKit.Bad,
            TextAnchor.MiddleCenter, FontStyle.Bold), 40f);
        _focusClueBody = MDUIKit.Size(MDUIKit.Label(clue, "", 26, MDUIKit.Ink,
            TextAnchor.UpperLeft), -1f, -1f, -1f, 1f);

        _focusHolding = MDUIKit.Size(MDUIKit.Label(focus, "", 24, MDUIKit.BrassDark,
            TextAnchor.MiddleCenter, FontStyle.Bold), 32f);

        var dossierBtn = MDUIKit.Btn(focus, "OPEN DOSSIER", MDUIKit.BrassDark, MDUIKit.Paper, 28,
            () => Show(Screen_.Dossier));
        MDUIKit.Size(dossierBtn, 68f);

        // Ledger strip.
        var money = MDUIKit.SkinnedCard(panel, "Ledger", MDSkin.Bar, MDUIKit.Leather, 8f);
        MDUIKit.Size(money.parent as RectTransform, 94f);
        MDUIKit.HStack(money, 6f, 34, TextAnchor.MiddleCenter, padV: 4);
        _cashLabel = MDUIKit.Label(money, "", 26, MDUIKit.Paper);
        _valueLabel = MDUIKit.Label(money, "", 26, MDUIKit.Paper);
        _pnlLabel = MDUIKit.Label(money, "", 26, MDUIKit.Paper);

        // Actions.
        var actions = MDUIKit.Rect(panel, "Actions");
        MDUIKit.Size(actions, 108f);
        MDUIKit.HStack(actions, 10f, 0);
        _investigateBtn = MDUIKit.Btn(actions, "INVESTIGATE", MDUIKit.Brass, MDUIKit.Ink, 30, DoInvestigate);
        _buyBtn = MDUIKit.Btn(actions, "BUY", MDUIKit.Good, MDUIKit.Paper, 34, DoBuy);
        _sellBtn = MDUIKit.Btn(actions, "SELL", MDUIKit.Bad, MDUIKit.Paper, 34, DoSell);

        var lower = MDUIKit.Rect(panel, "Lower");
        MDUIKit.Size(lower, 100f);
        MDUIKit.HStack(lower, 10f, 0);
        MDUIKit.Btn(lower, "PAUSE", MDUIKit.WoodLight, MDUIKit.Paper, 26, () => Show(Screen_.Pause));
        MDUIKit.Btn(lower, "PORTFOLIO", MDUIKit.WoodLight, MDUIKit.Paper, 26, () => Show(Screen_.Portfolio));
        _continueBtn = MDUIKit.Btn(lower, "PASS", MDUIKit.BrassDark, MDUIKit.Paper, 30, OnContinue);

        // Allocation feedback, standing in for the stamp animation (GDD 5).
        // Overlaid rather than stacked: it is only visible for a moment, and reserving a
        // permanent row for it was starving the focus card on tall narrow phones.
        _toast = MDUIKit.Label(panel, "", 34, MDUIKit.Brass, TextAnchor.MiddleCenter, FontStyle.Bold);
        var toastLayout = _toast.gameObject.AddComponent<LayoutElement>();
        toastLayout.ignoreLayout = true;
        var toastRect = _toast.rectTransform;
        toastRect.anchorMin = new Vector2(0f, 0f);
        toastRect.anchorMax = new Vector2(1f, 0f);
        toastRect.pivot = new Vector2(0.5f, 0f);
        toastRect.offsetMin = new Vector2(20f, 228f);
        toastRect.offsetMax = new Vector2(-20f, 276f);

        return panel;
    }

    void RefreshBell()
    {
        var scenario = _game.Scenario;

        _dayLabel.text = "Day " + _game.Day + "\nof " + _game.TotalDays;
        _tokenLabel.text = "Research\nTokens  " + _game.TokensLeft;
        _objectiveLabel.text = _game.ObjectiveSummary();

        for (int i = 0; i < _game.CompanyCount; i++)
        {
            _bellGauges[i].Bind(_game, i);
            // The focused company's frame is brass, the others stay dark.
            _bellGauges[i].Outer.GetComponent<Image>().color =
                i == _focus ? MDUIKit.Brass : MDUIKit.BrassDark;
        }

        var co = scenario.Companies[_focus];
        _focusName.text = co.Name.ToUpperInvariant();
        _focusTagline.text = co.Tagline + "   |   " + co.Sector;

        var latest = co.Clues[_game.RevealedClues[_focus] - 1];
        _focusClueTitle.text = latest.Title;
        _focusClueBody.text = latest.Body;

        int shares = _game.Holdings[_focus].Shares;
        _focusHolding.text = shares > 0
            ? $"HOLDING {shares} sh @ {MDUIKit.Money2(_game.Holdings[_focus].AvgEntry)}   " +
              $"P/L {MDUIKit.Money(_game.PnL(_focus))}"
            : "NO POSITION";

        _cashLabel.text = "Cash\n" + MDUIKit.Money(_game.Cash);
        _valueLabel.text = "Total Value\n" + MDUIKit.Money(_game.TotalValue);
        _pnlLabel.text = "P/L\n" + MDUIKit.Money(_game.Profit);
        _pnlLabel.color = MDUIKit.PnLColor(_game.Profit);

        int hidden = co.Clues.Length - _game.RevealedClues[_focus];
        _investigateBtn.interactable = _game.CanInvestigate(_focus);
        MDUIKit.BtnLabel(_investigateBtn).text = hidden > 0 ? "INVESTIGATE\n1 token" : "FULLY\nRESEARCHED";

        _buyBtn.interactable = _game.CanBuy(_focus);
        MDUIKit.BtnLabel(_buyBtn).text = "BUY\n" + MDUIKit.Money(Mathf.Min(MDContent.LotSize, _game.Cash));

        _sellBtn.interactable = _game.CanSell(_focus);

        MDUIKit.BtnLabel(_continueBtn).text = _game.HasMoreEvents ? "PASS" : "CLOSE\nMARKET";
    }

    void DoInvestigate()
    {
        if (!_game.Investigate(_focus)) return;
        var co = _game.Scenario.Companies[_focus];
        MDAudio.Play(MDAudio.Investigate);
        Toast("CLUE REVEALED: " + co.Clues[_game.RevealedClues[_focus] - 1].Title);
        RefreshBell();
    }

    void DoBuy()
    {
        int before = _game.Holdings[_focus].Shares;
        if (!_game.Buy(_focus)) return;
        MDAudio.Play(MDAudio.Buy);
        Toast("ALLOCATED: " + (_game.Holdings[_focus].Shares - before) + " shares");
        RefreshBell();
    }

    void DoSell()
    {
        int shares = _game.Holdings[_focus].Shares;
        if (!_game.Sell(_focus)) return;
        MDAudio.Play(MDAudio.Sell);
        Toast("CLOSED: " + shares + " shares");
        RefreshBell();
    }

    void Toast(string message)
    {
        if (_toastRoutine != null) StopCoroutine(_toastRoutine);
        _toastRoutine = StartCoroutine(ToastRoutine(message));
    }

    IEnumerator ToastRoutine(string message)
    {
        _toast.text = message;
        var c = _toast.color;
        for (float t = 0f; t < 1.6f; t += Time.unscaledDeltaTime)
        {
            c.a = t < 1.1f ? 1f : 1f - (t - 1.1f) / 0.5f;
            _toast.color = c;
            yield return null;
        }
        _toast.text = "";
        c.a = 1f;
        _toast.color = c;
    }

    // ---------------- Company Dossier ----------------

    RectTransform BuildDossier()
    {
        var panel = MDUIKit.Rect(_root, "Dossier");
        MDUIKit.Fill(panel);
        MDUIKit.VStack(panel, 10f, 16, TextAnchor.UpperCenter);

        var bar = MDUIKit.SkinnedCard(panel, "TopBar", MDSkin.Bar, MDUIKit.Leather, 10f);
        MDUIKit.Size(bar.parent as RectTransform, 126f);
        MDUIKit.HStack(bar, 10f, 34, TextAnchor.MiddleCenter, padV: 4);
        MDUIKit.Size(MDUIKit.Btn(bar, "BACK", MDUIKit.Brass, MDUIKit.Ink, 26,
            () => Show(Screen_.Bell)), -1f, 132f, 0f, 1f);
        MDUIKit.Size(MDUIKit.Label(bar, "COMPANY DOSSIER", 34, MDUIKit.Brass,
            TextAnchor.MiddleCenter, FontStyle.Bold), -1f, -1f, 1f, 1f);
        _dossierProgress = MDUIKit.Size(MDUIKit.Label(bar, "", 24, MDUIKit.OnDark,
            TextAnchor.MiddleRight), -1f, 158f, 0f, 1f);

        var head = MDUIKit.Rect(panel, "Head");
        MDUIKit.Size(head, 340f);
        MDUIKit.HStack(head, 12f, 0);

        var info = MDUIKit.SkinnedCard(head, "Info", MDSkin.Panel, MDUIKit.Paper, 30f, padTop: 8f);
        MDUIKit.VStack(info, 6f, 8, TextAnchor.MiddleCenter);
        _dossierName = MDUIKit.Size(MDUIKit.Label(info, "", 34, MDUIKit.Brass,
            TextAnchor.MiddleCenter, FontStyle.Bold), 76f);
        _dossierTagline = MDUIKit.Size(MDUIKit.Label(info, "", 24, MDUIKit.Muted,
            TextAnchor.MiddleCenter, FontStyle.Italic), 36f);
        // Explicit height: an Image reports its sprite's native size as its preferred
        // height, and the folio art is tall enough to burst the row if left to ask for it.
        var folio = MDUIKit.Panel(info, "Plate", MDUIKit.BrassDark, MDSkin.Dossier, false);
        folio.preserveAspect = true;
        MDUIKit.Size(folio, 130f, -1f, 1f, 1f);

        _dossierGauge = MDGaugeView.Create(head, "", 22);
        MDUIKit.Size(_dossierGauge.Outer, -1f, 300f, 0f, 1f);

        MDUIKit.Size(MDUIKit.Label(panel, "EVIDENCE", 28, MDUIKit.Brass,
            TextAnchor.MiddleCenter, FontStyle.Bold), 40f);

        var scrollHolder = MDUIKit.Rect(panel, "ClueScrollHolder");
        MDUIKit.Size(scrollHolder, -1f, -1f, -1f, 1f);
        _clueList = MDUIKit.Scroll(scrollHolder, "ClueScroll", horizontal: false, spacing: 12f, pad: 8);

        _dossierBlurb = MDUIKit.Size(MDUIKit.Label(panel, "", 24, MDUIKit.OnDark,
            TextAnchor.MiddleCenter, FontStyle.Italic), 56f);

        return panel;
    }

    void RefreshDossier()
    {
        var co = _game.Scenario.Companies[_focus];

        _dossierName.text = co.Name.ToUpperInvariant();
        _dossierTagline.text = co.Tagline;
        _dossierProgress.text = "Research\n" + _game.RevealedClues[_focus] + " / " + co.Clues.Length;
        _dossierGauge.Bind(_game, _focus);
        _dossierGauge.SnapNeedle();
        _dossierBlurb.text = "Tokens remaining: " + _game.TokensLeft + "   |   " + co.Sector;

        MDUIKit.ClearChildren(_clueList);

        for (int i = 0; i < co.Clues.Length; i++)
        {
            bool revealed = i < _game.RevealedClues[_focus];
            var card = MDUIKit.Card(_clueList, "Clue" + i,
                revealed ? MDUIKit.Paper : MDUIKit.WoodLight);
            MDUIKit.Size(card.parent as RectTransform, revealed ? 260f : 170f);
            MDUIKit.VStack(card, 4f, 14, TextAnchor.UpperCenter);

            if (revealed)
            {
                MDUIKit.Size(MDUIKit.MarkedRow(card, true, co.Clues[i].Title, 30,
                    MDUIKit.Good, MDUIKit.Good, MDUIKit.Muted), 40f);
                MDUIKit.Size(MDUIKit.Label(card, co.Clues[i].Body, 25, MDUIKit.Ink,
                    TextAnchor.UpperLeft), -1f, -1f, -1f, 1f);
            }
            else
            {
                MDUIKit.Size(MDUIKit.MarkedRow(card, false, "SEALED", 28,
                    MDUIKit.Muted, MDUIKit.Good, MDUIKit.Muted), 38f);

                bool first = i == _game.RevealedClues[_focus];
                var btn = MDUIKit.Btn(card, first && _game.TokensLeft > 0 ? "USE 1 TOKEN" : "NO TOKENS",
                    MDUIKit.Brass, MDUIKit.Ink, 26,
                    () => { if (_game.Investigate(_focus)) RefreshDossier(); });
                btn.interactable = first && _game.CanInvestigate(_focus);
                MDUIKit.Size(btn, 64f);
            }
        }
    }

    // ---------------- Market Event ----------------

    RectTransform BuildEvent()
    {
        var panel = MDUIKit.Rect(_root, "MarketEvent");
        MDUIKit.Fill(panel);
        MDUIKit.VStack(panel, 12f, 22, TextAnchor.UpperCenter);

        var bar = MDUIKit.SkinnedCard(panel, "TopBar", MDSkin.Bar, MDUIKit.Leather, 10f);
        MDUIKit.Size(bar.parent as RectTransform, 126f);
        MDUIKit.HStack(bar, 10f, 34, TextAnchor.MiddleCenter, padV: 4);
        _eventDay = MDUIKit.Label(bar, "", 26, MDUIKit.Paper, TextAnchor.MiddleLeft);
        MDUIKit.Size(MDUIKit.Label(bar, "MARKET EVENT", 42, MDUIKit.Brass,
            TextAnchor.MiddleCenter, FontStyle.Bold), -1f, -1f, 2f, 1f);
        MDUIKit.Label(bar, "Markets move\non information.", 20, MDUIKit.PaperDark, TextAnchor.MiddleRight);

        // Pinned event card — aged paper with an ink illustration plate (GDD 5).
        var card = MDUIKit.SkinnedCard(panel, "EventCard", MDSkin.Panel, MDUIKit.Paper, 44f, padTop: 8f);
        MDUIKit.Size(card.parent as RectTransform, -1f, -1f, -1f, 1f);
        MDUIKit.VStack(card, 8f, 10, TextAnchor.UpperCenter);
        MDUIKit.Size(MDUIKit.Label(card, "TODAY'S NEWS", 26, MDUIKit.Brass,
            TextAnchor.MiddleCenter, FontStyle.Bold), 78f);
        var eventPlate = MDUIKit.Panel(card, "Plate", MDUIKit.BrassDark, MDSkin.EventCard, false);
        eventPlate.preserveAspect = true;              // the illustration was being squashed
        MDUIKit.Size(eventPlate, 240f);
        _eventHeadline = MDUIKit.Size(MDUIKit.Label(card, "", 40, MDUIKit.Bad,
            TextAnchor.MiddleCenter, FontStyle.Bold), 110f);
        _eventBody = MDUIKit.Size(MDUIKit.Label(card, "", 28, MDUIKit.Ink,
            TextAnchor.UpperCenter), -1f, -1f, -1f, 1f);

        var gaugeRow = MDUIKit.Rect(panel, "EventGauges");
        MDUIKit.Size(gaugeRow, 336f);
        MDUIKit.HStack(gaugeRow, 12f, 0);
        _eventGauges = new MDGaugeView[_game.CompanyCount];
        for (int i = 0; i < _game.CompanyCount; i++)
            _eventGauges[i] = MDGaugeView.Create(gaugeRow, _game.Scenario.Companies[i].Name, 22);

        MDUIKit.Size(MDUIKit.Btn(panel, "CONTINUE", MDUIKit.Brass, MDUIKit.Ink, 40,
            () => Show(Screen_.Bell)), 110f);

        return panel;
    }

    /// <summary>Shows the fired event with the needles sweeping from their pre-event reading.</summary>
    void ShowEvent(MDMarketEvent fired, float[] pricesBefore)
    {
        MDAudio.Play(MDAudio.MarketEvent);
        _eventDay.text = "Day " + _game.Day + "\nof " + _game.TotalDays;
        _eventHeadline.text = fired.Headline;
        _eventBody.text = fired.Body;

        for (int i = 0; i < _game.CompanyCount; i++)
        {
            // Park the needle at the old reading, then bind the new one so it sweeps across.
            float startPct = (pricesBefore[i] / _game.Scenario.Companies[i].StartPrice - 1f) * 100f;
            _eventGauges[i].SetReadingImmediate(startPct);
            _eventGauges[i].Bind(_game, i);
        }

        Show(Screen_.Event);
    }
}
