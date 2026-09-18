using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Pause menu (GDD 6). A leather folio over a dimmed desk.</summary>
public class MDPausePanel
{
    public RectTransform Root { get; private set; }

    public static MDPausePanel Create(Transform parent, Action onResume, Action onRestart, Action onMenu)
    {
        var panel = new MDPausePanel();

        var scrim = MDUIKit.Panel(parent, "PauseScrim", new Color(0f, 0f, 0f, 0.72f));
        MDUIKit.Fill(scrim.rectTransform);
        panel.Root = scrim.rectTransform;

        var folio = MDUIKit.SkinnedCard(scrim.transform, "Folio", MDSkin.Panel, MDUIKit.Leather, 52f, padTop: 8f);
        var folioOuter = (RectTransform)folio.parent;
        folioOuter.anchorMin = folioOuter.anchorMax = new Vector2(0.5f, 0.5f);
        folioOuter.sizeDelta = new Vector2(760f, 900f);

        MDUIKit.VStack(folio, 16f, 8, TextAnchor.UpperCenter);
        MDUIKit.Size(MDUIKit.Label(folio, "PAUSED", 56, MDUIKit.Brass,
            TextAnchor.MiddleCenter, FontStyle.Bold), 80f);
        var bell = MDUIKit.Panel(folio, "Bell", MDUIKit.Brass, MDSkin.Bell, sliced: false);
        bell.preserveAspect = true;
        MDUIKit.Size(bell, 150f, 150f, 0f, 0f);

        MDUIKit.Size(MDUIKit.Btn(folio, "RESUME", MDUIKit.Brass, MDUIKit.Ink, 40, onResume), 110f);
        MDUIKit.Size(MDUIKit.Btn(folio, "RESTART", MDUIKit.BrassDark, MDUIKit.Paper, 34, onRestart), 96f);
        MDUIKit.Size(MDUIKit.Btn(folio, "MAIN MENU", MDUIKit.BrassDark, MDUIKit.Paper, 34, onMenu), 96f);

        MDUIKit.Size(MDUIKit.Label(folio, "\"The market can wait.\"", 26, MDUIKit.Muted,
            TextAnchor.MiddleCenter, FontStyle.Italic), 48f);

        return panel;
    }
}

/// <summary>
/// Scenario Complete (GDD 6/8): objective result, star rating and key stats.
/// Also covers failure, which is only ever "the objective was not met by resolution".
/// </summary>
public class MDCompletePanel
{
    public RectTransform Root { get; private set; }

    TMPro.TMP_Text _title, _subtitle, _quote, _livesLabel;
    RectTransform _statList, _objectiveList, _starHolder;
    Button _nextBtn, _retryBtn;

    public static MDCompletePanel Create(Transform parent, Action onNext, Action onRetry, Action onMenu)
    {
        var panel = new MDCompletePanel();

        var scrim = MDUIKit.Panel(parent, "CompleteScrim", new Color(0f, 0f, 0f, 0.82f));
        MDUIKit.Fill(scrim.rectTransform);
        panel.Root = scrim.rectTransform;

        var book = MDUIKit.SkinnedCard(scrim.transform, "Book", MDSkin.Panel, MDUIKit.Paper, 52f, padTop: 8f);
        MDUIKit.Fill((RectTransform)book.parent, 28f);
        MDUIKit.VStack(book, 8f, 8, TextAnchor.UpperCenter);

        panel._title = MDUIKit.Size(MDUIKit.Label(book, "", 50, MDUIKit.Brass,
            TextAnchor.MiddleCenter, FontStyle.Bold), 80f);
        panel._subtitle = MDUIKit.Size(MDUIKit.Label(book, "", 28, MDUIKit.Muted,
            TextAnchor.MiddleCenter, FontStyle.Italic), 42f);
        panel._starHolder = MDUIKit.Rect(book, "StarHolder");
        MDUIKit.Size(panel._starHolder, 96f);
        MDUIKit.HStack(panel._starHolder, 0f, 0);

        panel._objectiveList = MDUIKit.Rect(book, "Objectives");
        MDUIKit.Size(panel._objectiveList, -1f, -1f, -1f, 1f);
        MDUIKit.VStack(panel._objectiveList, 6f, 4, TextAnchor.UpperCenter);

        var divider = MDUIKit.Panel(book, "Divider", MDUIKit.PaperDark);
        MDUIKit.Size(divider, 3f);

        panel._statList = MDUIKit.Rect(book, "Stats");
        MDUIKit.Size(panel._statList, -1f, -1f, -1f, 1f);
        MDUIKit.VStack(panel._statList, 4f, 4, TextAnchor.UpperCenter);

        panel._livesLabel = MDUIKit.Size(MDUIKit.Label(book, "", 26, MDUIKit.Bad,
            TextAnchor.MiddleCenter, FontStyle.Bold), 38f);
        panel._quote = MDUIKit.Size(MDUIKit.Label(book, "", 26, MDUIKit.Muted,
            TextAnchor.MiddleCenter, FontStyle.Italic), 46f);

        var buttons = MDUIKit.Rect(book, "Buttons");
        MDUIKit.Size(buttons, 110f);
        MDUIKit.HStack(buttons, 10f, 0);
        panel._retryBtn = MDUIKit.Btn(buttons, "RETRY", MDUIKit.BrassDark, MDUIKit.Paper, 30, onRetry);
        panel._nextBtn = MDUIKit.Btn(buttons, "NEXT SCENARIO", MDUIKit.Good, MDUIKit.Paper, 28, onNext);
        MDUIKit.Btn(buttons, "MAIN MENU", MDUIKit.BrassDark, MDUIKit.Paper, 28, onMenu);

        return panel;
    }

    public void Show(MDGame game, MDResult result, bool hasNextScenario)
    {
        _title.text = result.Won ? "MARKET CLOSED" : "OBJECTIVE MISSED";
        _title.color = result.Won ? MDUIKit.Brass : new Color(1f, 0.55f, 0.45f);
        _subtitle.text = result.Won
            ? "Another day, another step forward."
            : "The read did not hold. The evidence is still on the desk.";

        MDUIKit.ClearChildren(_starHolder);
        MDUIKit.StarRow(_starHolder, result.Stars, 3, 64f);

        MDUIKit.ClearChildren(_objectiveList);
        for (int i = 0; i < result.ObjectiveText.Length; i++)
        {
            bool met = result.ObjectiveMet[i];
            MDUIKit.Size(MDUIKit.MarkedRow(_objectiveList, met, result.ObjectiveText[i], 25,
                met ? MDUIKit.Good : MDUIKit.Bad, MDUIKit.Good, MDUIKit.Bad), 44f);
        }

        MDUIKit.ClearChildren(_statList);
        Stat("Starting Capital", MDUIKit.Money(MDContent.StartingCapital), MDUIKit.Ink);
        Stat("Final Portfolio Value", MDUIKit.Money(result.FinalValue), MDUIKit.Ink);
        Stat("Profit", MDUIKit.Money(result.Profit), MDUIKit.PnLColor(result.Profit));
        Stat("Return", MDUIKit.Pct(result.ReturnPct), MDUIKit.PnLColor(result.Profit));
        Stat("Research Tokens Used", result.TokensUsed + " (par " + game.Scenario.ParTokens + ")",
            result.TokensUsed <= game.Scenario.ParTokens ? MDUIKit.Good : MDUIKit.Muted);
        Stat("Perfect Read", result.Stars >= 3 ? "Yes" : "No",
            result.Stars >= 3 ? MDUIKit.Good : MDUIKit.Muted);

        _livesLabel.text = result.Won ? "" : "Attempts remaining: " + game.Lives;

        _quote.text = result.Won
            ? "\"Insight today, a brighter tomorrow.\""
            : "\"Every miss is evidence for the next read.\"";

        // Only offer the next scenario once this one has actually been cleared.
        _nextBtn.gameObject.SetActive(result.Won && hasNextScenario);
        _retryBtn.gameObject.SetActive(!result.Won || result.Stars < 3);
    }

    void Stat(string label, string value, Color valueColor)
    {
        var row = MDUIKit.Rect(_statList, "Stat");
        MDUIKit.Size(row, 52f);
        MDUIKit.HStack(row, 6f, 4);
        MDUIKit.Label(row, label, 26, MDUIKit.Ink, TextAnchor.MiddleLeft);
        MDUIKit.Label(row, value, 28, valueColor, TextAnchor.MiddleRight, FontStyle.Bold);
    }

}
