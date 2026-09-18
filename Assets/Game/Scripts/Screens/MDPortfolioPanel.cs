using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ledger-style portfolio screen with Summary / Holdings / Performance tabs (GDD 6).
/// Rebuilt from MDGame on every open, so there is no state to keep in sync.
/// </summary>
public class MDPortfolioPanel
{
    public RectTransform Root { get; private set; }

    RectTransform _body;
    Button[] _tabs;
    int _tab;
    MDGame _game;

    static readonly string[] TabNames = { "SUMMARY", "HOLDINGS", "PERFORMANCE" };

    public static MDPortfolioPanel Create(Transform parent, Action onBack)
    {
        var panel = new MDPortfolioPanel();

        var root = MDUIKit.Rect(parent, "Portfolio");
        MDUIKit.Fill(root);
        panel.Root = root;
        MDUIKit.VStack(root, 12f, 16, TextAnchor.UpperCenter);

        var bar = MDUIKit.SkinnedCard(root, "TopBar", MDSkin.Bar, MDUIKit.Leather, 10f);
        MDUIKit.Size(bar.parent as RectTransform, 126f);
        MDUIKit.HStack(bar, 10f, 34, TextAnchor.MiddleCenter, padV: 4);
        MDUIKit.Size(MDUIKit.Btn(bar, "BACK", MDUIKit.Brass, MDUIKit.Ink, 26, onBack), -1f, 150f, 0f, 1f);
        MDUIKit.Size(MDUIKit.Label(bar, "YOUR PORTFOLIO", 40, MDUIKit.Brass,
            TextAnchor.MiddleCenter, FontStyle.Bold), -1f, -1f, 1f, 1f);
        MDUIKit.Size(MDUIKit.Rect(bar, "Pad"), -1f, 150f, 0f, 1f);

        var tabRow = MDUIKit.Rect(root, "Tabs");
        MDUIKit.Size(tabRow, 92f);
        MDUIKit.HStack(tabRow, 8f, 0);

        panel._tabs = new Button[TabNames.Length];
        for (int i = 0; i < TabNames.Length; i++)
        {
            int index = i;
            panel._tabs[i] = MDUIKit.Btn(tabRow, TabNames[i], MDUIKit.BrassDark, MDUIKit.Paper, 26,
                () => panel.ShowTab(index));
        }

        var bodyHolder = MDUIKit.SkinnedCard(root, "Ledger", MDSkin.Ledger, MDUIKit.Paper, 34f);
        MDUIKit.Size(bodyHolder.parent as RectTransform, -1f, -1f, -1f, 1f);
        panel._body = MDUIKit.Scroll(bodyHolder, "LedgerScroll", horizontal: false, spacing: 8f, pad: 16);

        MDUIKit.Size(MDUIKit.Label(root, "\"Knowledge reduces risk.\"", 26, MDUIKit.OnDark,
            TextAnchor.MiddleCenter, FontStyle.Italic), 48f);

        return panel;
    }

    public void Refresh(MDGame game)
    {
        _game = game;
        ShowTab(_tab);
    }

    void ShowTab(int index)
    {
        _tab = index;
        for (int i = 0; i < _tabs.Length; i++)
            _tabs[i].GetComponent<Image>().color = i == index ? MDUIKit.Brass : MDUIKit.BrassDark;
        for (int i = 0; i < _tabs.Length; i++)
            MDUIKit.BtnLabel(_tabs[i]).color = i == index ? MDUIKit.Ink : MDUIKit.Paper;

        MDUIKit.ClearChildren(_body);
        if (_game == null) return;

        if (index == 0) BuildSummary();
        else if (index == 1) BuildHoldings();
        else BuildPerformance();
    }

    void BuildSummary()
    {
        Row("Starting Capital", MDUIKit.Money(MDContent.StartingCapital), MDUIKit.Ink);
        Row("Cash", MDUIKit.Money(_game.Cash), MDUIKit.Ink);
        Row("Invested", MDUIKit.Money(_game.Invested), MDUIKit.Ink);
        Divider();
        Row("Total Value", MDUIKit.Money(_game.TotalValue), MDUIKit.Ink, 36);
        Row("Total P/L", MDUIKit.Money(_game.Profit), MDUIKit.PnLColor(_game.Profit), 36);
        Row("Return", MDUIKit.Pct(_game.ReturnPct), MDUIKit.PnLColor(_game.Profit), 36);
        Divider();

        float risk = _game.TotalValue <= 0f ? 0f : _game.Invested / _game.TotalValue;
        Row("Risk Level", RiskWord(risk) + "  (" + Mathf.RoundToInt(risk * 100f) + "% invested)",
            risk > 0.75f ? MDUIKit.Bad : risk > 0.4f ? MDUIKit.BrassDark : MDUIKit.Good);
        RiskBar(risk);

        Divider();
        Row("Research Tokens Used", _game.TokensUsed + " of " + _game.Scenario.Tokens, MDUIKit.Ink);
        Row("Day", _game.Day + " of " + _game.TotalDays, MDUIKit.Ink);

        Divider();
        Header("OBJECTIVE");
        foreach (var o in _game.Scenario.Objectives)
        {
            bool met = _game.IsObjectiveMet(o);
            MDUIKit.Size(MDUIKit.MarkedRow(_body, met,
                o.Description + (met ? "   (on track)" : ""), 24,
                met ? MDUIKit.Good : MDUIKit.Muted, MDUIKit.Good, MDUIKit.Muted), 46f);
        }
    }

    void BuildHoldings()
    {
        Header("COMPANY / SHARES / ENTRY / NOW / P/L");

        bool any = false;
        for (int i = 0; i < _game.CompanyCount; i++)
        {
            var h = _game.Holdings[i];
            if (h.Shares == 0) continue;
            any = true;

            float pnl = _game.PnL(i);
            var row = NewRow(96f);
            MDUIKit.Label(row, _game.Scenario.Companies[i].Name, 26, MDUIKit.Ink, TextAnchor.MiddleLeft);
            MDUIKit.Size(MDUIKit.Label(row, h.Shares.ToString(), 26, MDUIKit.Ink), -1f, 110f, 0f, 1f);
            MDUIKit.Size(MDUIKit.Label(row, MDUIKit.Money2(h.AvgEntry), 24, MDUIKit.Muted), -1f, 150f, 0f, 1f);
            MDUIKit.Size(MDUIKit.Label(row, MDUIKit.Money2(_game.Price[i]), 24, MDUIKit.Ink), -1f, 150f, 0f, 1f);
            MDUIKit.Size(MDUIKit.Label(row, MDUIKit.Money(pnl), 26, MDUIKit.PnLColor(pnl),
                TextAnchor.MiddleRight, FontStyle.Bold), -1f, 160f, 0f, 1f);
        }

        if (!any)
            Row("No positions held.", "", MDUIKit.Muted, 28);

        Divider();
        Row("Cash", MDUIKit.Money(_game.Cash), MDUIKit.Ink, 30);
        Row("Total Value", MDUIKit.Money(_game.TotalValue), MDUIKit.Ink, 30);
    }

    void BuildPerformance()
    {
        Header("PRICE SINCE OPEN");
        for (int i = 0; i < _game.CompanyCount; i++)
        {
            float pct = _game.PriceChangePct(i);
            Row(_game.Scenario.Companies[i].Name,
                MDUIKit.Money2(_game.Price[i]) + "   " + MDUIKit.Pct(pct),
                MDUIKit.PnLColor(pct));
        }

        Divider();
        Header("THIS SCENARIO");
        Row("Events Resolved", _game.EventIndex + " of " + _game.Scenario.Events.Length, MDUIKit.Ink);
        Row("Research Spent", _game.TokensUsed + " tokens", MDUIKit.Ink);
        Row("Par For A Correct Read", _game.Scenario.ParTokens + " tokens", MDUIKit.Muted);
        Row("Attempts Left", _game.Lives.ToString(), MDUIKit.Ink);

        int largest = _game.LargestPosition();
        Row("Largest Position",
            largest < 0 ? "None" : _game.Scenario.Companies[largest].Name,
            largest < 0 ? MDUIKit.Muted : MDUIKit.BrassDark);
    }

    // ---- small ledger primitives ----

    RectTransform NewRow(float height)
    {
        var row = MDUIKit.Rect(_body, "Row");
        MDUIKit.Size(row, height);
        MDUIKit.HStack(row, 6f, 4);
        return row;
    }

    void Row(string label, string value, Color valueColor, int size = 28)
    {
        var row = NewRow(size + 34f);
        MDUIKit.Label(row, label, size, MDUIKit.Ink, TextAnchor.MiddleLeft);
        if (!string.IsNullOrEmpty(value))
            MDUIKit.Label(row, value, size, valueColor, TextAnchor.MiddleRight, FontStyle.Bold);
    }

    void Header(string text)
    {
        var t = MDUIKit.Label(_body, text, 24, MDUIKit.BrassDark, TextAnchor.MiddleLeft, FontStyle.Bold);
        MDUIKit.Size(t, 44f);
    }

    void Divider()
    {
        var line = MDUIKit.Panel(_body, "Divider", MDUIKit.PaperDark);
        MDUIKit.Size(line, 3f);
    }

    void RiskBar(float risk)
    {
        var track = MDUIKit.Panel(_body, "RiskTrack", MDUIKit.Wood);
        MDUIKit.Size(track, 26f);
        var fill = MDUIKit.Panel(track.transform, "Fill",
            risk > 0.75f ? MDUIKit.Bad : risk > 0.4f ? MDUIKit.Brass : MDUIKit.Good);
        var rt = fill.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(Mathf.Clamp01(risk), 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static string RiskWord(float risk) =>
        risk <= 0.01f ? "None" : risk < 0.35f ? "Low" : risk < 0.7f ? "Moderate" : "High";
}
