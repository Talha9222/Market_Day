using System.Text;
using UnityEngine;

/// <summary>
/// One scenario run. Pure logic, no UI and no Unity scene dependencies, so
/// MDSelfCheck can play whole scenarios headlessly to prove they are winnable.
///
/// Flow (GDD 2): decision window -> event fires -> decision window -> ... -> resolve.
/// N authored events therefore give N+1 decision windows.
/// </summary>
public class MDHolding
{
    public int Shares;
    public float AvgEntry;
}

public class MDResult
{
    public bool Won;
    public int Stars;
    public float FinalValue;
    public float Profit;
    public float ReturnPct;
    public int TokensUsed;
    public string[] ObjectiveText;
    public bool[] ObjectiveMet;
}

public class MDGame
{
    public MDScenario Scenario { get; private set; }
    public int ScenarioIndex { get; private set; }

    public float Cash { get; private set; }
    public int TokensLeft { get; private set; }
    public int TokensUsed { get; private set; }
    public int Lives { get; private set; }

    public float[] Price { get; private set; }
    public int[] RevealedClues { get; private set; }   // per company; starts at 1 (GDD 2.1)
    public MDHolding[] Holdings { get; private set; }

    public int EventIndex { get; private set; }        // index of the NEXT event to fire
    public bool Resolved { get; private set; }

    float _minValueSeen;

    public MDGame(int scenarioIndex)
    {
        ScenarioIndex = scenarioIndex;
        Scenario = MDContent.Get(scenarioIndex);
        Lives = MDContent.LivesPerScenario;
        Restart();
    }

    /// <summary>Retry: full token budget, no revealed clues, no positions (GDD 8).</summary>
    public void Restart()
    {
        int n = Scenario.Companies.Length;
        Cash = MDContent.StartingCapital;
        TokensLeft = Scenario.Tokens;
        TokensUsed = 0;
        EventIndex = 0;
        Resolved = false;

        Price = new float[n];
        RevealedClues = new int[n];
        Holdings = new MDHolding[n];
        for (int i = 0; i < n; i++)
        {
            Price[i] = Scenario.Companies[i].StartPrice;
            RevealedClues[i] = 1;
            Holdings[i] = new MDHolding();
        }

        _minValueSeen = TotalValue;
    }

    // ---- derived state ----

    public int CompanyCount => Scenario.Companies.Length;
    public bool HasMoreEvents => EventIndex < Scenario.Events.Length;
    public int Day => EventIndex + 1;
    public int TotalDays => Scenario.Events.Length + 1;

    public float HoldingValue(int c) => Holdings[c].Shares * Price[c];

    public float Invested
    {
        get
        {
            float v = 0f;
            for (int i = 0; i < CompanyCount; i++) v += HoldingValue(i);
            return v;
        }
    }

    public float TotalValue => Cash + Invested;
    public float Profit => TotalValue - MDContent.StartingCapital;
    public float ReturnPct => Profit / MDContent.StartingCapital * 100f;

    /// <summary>Unrealised profit/loss on an open position.</summary>
    public float PnL(int c) => Holdings[c].Shares == 0 ? 0f
        : Holdings[c].Shares * (Price[c] - Holdings[c].AvgEntry);

    public float PriceChangePct(int c) =>
        (Price[c] / Scenario.Companies[c].StartPrice - 1f) * 100f;

    // ---- player actions ----

    public bool CanInvestigate(int c) =>
        !Resolved && TokensLeft > 0 && RevealedClues[c] < Scenario.Companies[c].Clues.Length;

    /// <summary>Spends one token to permanently reveal the next clue (GDD 3).</summary>
    public bool Investigate(int c)
    {
        if (!CanInvestigate(c)) return false;
        TokensLeft--;
        TokensUsed++;
        RevealedClues[c]++;
        return true;
    }

    public int SharesAffordable(int c)
    {
        float spend = Mathf.Min(MDContent.LotSize, Cash);
        return Mathf.FloorToInt(spend / Price[c]);
    }

    public bool CanBuy(int c) => !Resolved && SharesAffordable(c) >= 1;

    /// <summary>Commits one lot (a quarter of starting capital) at the current price.</summary>
    public bool Buy(int c)
    {
        if (!CanBuy(c)) return false;

        int shares = SharesAffordable(c);
        float cost = shares * Price[c];
        var h = Holdings[c];
        h.AvgEntry = (h.Shares * h.AvgEntry + cost) / (h.Shares + shares);
        h.Shares += shares;
        Cash -= cost;
        return true;
    }

    public bool CanSell(int c) => !Resolved && Holdings[c].Shares > 0;

    /// <summary>Closes the whole position at the current price.</summary>
    public bool Sell(int c)
    {
        if (!CanSell(c)) return false;

        var h = Holdings[c];
        Cash += h.Shares * Price[c];
        h.Shares = 0;
        h.AvgEntry = 0f;
        return true;
    }

    /// <summary>
    /// Fires the next authored event and applies its fixed price movements.
    /// Returns null once every event has fired.
    /// </summary>
    public MDMarketEvent AdvanceEvent()
    {
        if (!HasMoreEvents || Resolved) return null;

        var e = Scenario.Events[EventIndex++];
        for (int i = 0; i < CompanyCount; i++)
            Price[i] = Mathf.Max(0.01f, Price[i] * (1f + e.Move[i] * 0.01f));

        _minValueSeen = Mathf.Min(_minValueSeen, TotalValue);
        return e;
    }

    // ---- objectives ----

    /// <summary>Index of the largest position by market value, or -1 if nothing is held.</summary>
    public int LargestPosition()
    {
        int best = -1;
        float bestValue = 0f;
        for (int i = 0; i < CompanyCount; i++)
        {
            float v = HoldingValue(i);
            if (v > bestValue) { bestValue = v; best = i; }
        }
        return best;
    }

    float InvestedInSector(string sector)
    {
        float v = 0f;
        for (int i = 0; i < CompanyCount; i++)
            if (Scenario.Companies[i].Sector == sector) v += HoldingValue(i);
        return v;
    }

    public bool IsObjectiveMet(MDObjective o)
    {
        const float eps = 0.001f;
        switch (o.Kind)
        {
            case MDObjectiveKind.FinalValueAbove:
                return TotalValue >= o.Amount - eps;

            case MDObjectiveKind.PickStrongest:
                return LargestPosition() == o.Index;

            case MDObjectiveKind.NoDrawdownBelow:
                return Mathf.Min(_minValueSeen, TotalValue) >= o.Amount - eps;

            case MDObjectiveKind.RiskUnder:
                return TotalValue <= 0f || Invested / TotalValue <= o.Amount + eps;

            case MDObjectiveKind.SectorAllocation:
                float inv = Invested;
                return inv > eps && InvestedInSector(o.Sector) / inv >= o.Amount - eps;
        }
        return false;
    }

    /// <summary>
    /// Scores the run (GDD 8). One star for meeting the objective, a second for clearing it
    /// with room to spare, a third for doing that without over-spending research tokens.
    /// </summary>
    public MDResult Resolve()
    {
        _minValueSeen = Mathf.Min(_minValueSeen, TotalValue);

        var objectives = Scenario.Objectives;
        var met = new bool[objectives.Length];
        var text = new string[objectives.Length];
        bool won = true;

        for (int i = 0; i < objectives.Length; i++)
        {
            met[i] = IsObjectiveMet(objectives[i]);
            text[i] = objectives[i].Description;
            if (!met[i]) won = false;
        }

        int stars = 0;
        if (won)
        {
            stars = 1;
            if (TotalValue >= MarginBar()) stars++;
            if (TokensUsed <= Scenario.ParTokens) stars++;
        }
        else if (Lives > 0)
        {
            Lives--;
        }

        Resolved = true;

        return new MDResult
        {
            Won = won,
            Stars = stars,
            FinalValue = TotalValue,
            Profit = Profit,
            ReturnPct = ReturnPct,
            TokensUsed = TokensUsed,
            ObjectiveText = text,
            ObjectiveMet = met
        };
    }

    /// <summary>Value needed for the "cleared it comfortably" second star.</summary>
    float MarginBar()
    {
        float bar = MDContent.StartingCapital;
        foreach (var o in Scenario.Objectives)
            if (o.Kind == MDObjectiveKind.FinalValueAbove) bar = Mathf.Max(bar, o.Amount);
        return bar + MDContent.StartingCapital * 0.05f;
    }

    public string ObjectiveSummary()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < Scenario.Objectives.Length; i++)
        {
            if (i > 0) sb.Append('\n');
            sb.Append("- ").Append(Scenario.Objectives[i].Description);
        }
        return sb.ToString();
    }
}
