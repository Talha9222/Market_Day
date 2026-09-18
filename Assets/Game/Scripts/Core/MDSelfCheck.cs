using UnityEngine;

/// <summary>
/// Content + rules self-check. Runs from the splash screen in the Editor and in
/// development builds only, and is stripped from release.
///
/// It exists because every price move in this game is hand-authored: a typo in one
/// percentage silently makes a scenario unwinnable, and no compiler catches that.
/// This plays all 20 scenarios headlessly and fails loudly instead.
/// </summary>
public static class MDSelfCheck
{
    static int _failures;

    static void Require(bool condition, string scenario, string message)
    {
        if (condition) return;
        _failures++;
        Debug.LogError($"[MDSelfCheck] {scenario}: {message}");
    }

    public static void RunAll()
    {
        _failures = 0;
        var all = MDContent.All;

        Require(all.Length == 20, "content", $"expected 20 scenarios, found {all.Length}");

        for (int s = 0; s < all.Length; s++)
        {
            var sc = all[s];
            string id = $"#{s + 1} \"{sc.Name}\"";

            // --- structure ---
            Require(!string.IsNullOrEmpty(sc.Name), id, "has no name");
            Require(sc.Difficulty >= 1 && sc.Difficulty <= 3, id, $"difficulty {sc.Difficulty} outside 1-3");
            Require(sc.Companies.Length == 3, id, $"has {sc.Companies.Length} companies, expected 3");
            Require(sc.Events.Length >= 1, id, "has no market events");
            Require(sc.Objectives.Length >= 1, id, "has no objectives");
            Require(sc.ParTokens >= 0 && sc.ParTokens <= sc.Tokens, id,
                $"ParTokens {sc.ParTokens} is not within a budget of {sc.Tokens}");

            int hiddenClues = 0;
            foreach (var c in sc.Companies)
            {
                Require(c.Clues.Length >= 2 && c.Clues.Length <= 3, id,
                    $"{c.Name} has {c.Clues.Length} clues, GDD allows 2-3");
                Require(!string.IsNullOrEmpty(c.Sector), id, $"{c.Name} has no sector");
                Require(c.StartPrice > 0f, id, $"{c.Name} has a non-positive start price");
                hiddenClues += c.Clues.Length - 1;
            }
            Require(sc.Tokens <= hiddenClues, id,
                $"token budget {sc.Tokens} exceeds the {hiddenClues} clues there are to buy");

            foreach (var e in sc.Events)
            {
                Require(e.Move.Length == sc.Companies.Length, id,
                    $"event \"{e.Headline}\" has {e.Move.Length} moves for {sc.Companies.Length} companies");
                Require(!string.IsNullOrEmpty(e.Headline), id, "an event has no headline");
            }

            // --- objectives are actually satisfiable against the authored content ---
            foreach (var o in sc.Objectives)
            {
                if (o.Kind == MDObjectiveKind.PickStrongest)
                {
                    Require(o.Index >= 0 && o.Index < sc.Companies.Length, id,
                        $"PickStrongest index {o.Index} is out of range");
                    Require(o.Index == sc.BestCompany(), id,
                        $"PickStrongest points at {sc.Companies[o.Index].Name} but " +
                        $"{sc.Companies[sc.BestCompany()].Name} performs best " +
                        $"({sc.TotalMove(sc.BestCompany()):0.0}% vs {sc.TotalMove(o.Index):0.0}%)");
                }
                else if (o.Kind == MDObjectiveKind.SectorAllocation)
                {
                    bool sectorExists = false;
                    foreach (var c in sc.Companies)
                        if (c.Sector == o.Sector) sectorExists = true;
                    Require(sectorExists, id, $"SectorAllocation wants \"{o.Sector}\", which no company is in");
                }
            }

            // --- winnable: some sane play of the correct read has to clear it ---
            int bestStars = -1;
            string winningPlay = null;
            for (int lots = 1; lots <= MDContent.LotCount; lots++)
            {
                for (int sellPass = 0; sellPass < 2; sellPass++)
                {
                    bool sellAtEnd = sellPass == 1;
                    if (Play(s, lots, sellAtEnd, out int stars) && stars > bestStars)
                    {
                        bestStars = stars;
                        winningPlay = $"{lots} lot(s){(sellAtEnd ? ", sold before close" : ", held")}";
                    }
                }
            }

            Require(bestStars > 0, id,
                "is UNWINNABLE - no lot size into the strongest company meets its objectives. " +
                "Check the authored percentages or the target value.");

            if (bestStars > 0 && bestStars < 3)
                Debug.LogWarning($"[MDSelfCheck] {id}: best reachable result is {bestStars}/3 stars " +
                                 $"({winningPlay}). Three stars may be out of reach.");
        }

        // "Always buy the left-hand company" must not be a winning strategy.
        var winnerSlot = new int[3];
        for (int s = 0; s < all.Length; s++) winnerSlot[all[s].BestCompany()]++;

        for (int slot = 0; slot < winnerSlot.Length; slot++)
            Require(winnerSlot[slot] > 0, "content",
                $"no scenario has its strongest company in slot {slot} - the answer is positionally predictable");

        Require(winnerSlot[0] < all.Length * 0.6f, "content",
            $"{winnerSlot[0]} of {all.Length} scenarios put the strongest company first");

        if (_failures == 0)
            Debug.Log($"[MDSelfCheck] OK - {all.Length} scenarios validated and all winnable. " +
                      $"Strongest company sits left/middle/right in {winnerSlot[0]}/{winnerSlot[1]}/{winnerSlot[2]} scenarios.");
        else
            Debug.LogError($"[MDSelfCheck] {_failures} problem(s) found. See the errors above.");
    }

    /// <summary>
    /// Headless play of the intended correct read: research the strongest company up to par,
    /// commit <paramref name="lots"/> lots, hold through every event, optionally close before resolution.
    /// </summary>
    static bool Play(int scenarioIndex, int lots, bool sellAtEnd, out int stars)
    {
        var g = new MDGame(scenarioIndex);
        int target = g.Scenario.BestCompany();

        for (int i = 0; i < g.Scenario.ParTokens && g.CanInvestigate(target); i++)
            g.Investigate(target);

        for (int i = 0; i < lots; i++)
            g.Buy(target);

        while (g.HasMoreEvents)
            g.AdvanceEvent();

        if (sellAtEnd) g.Sell(target);

        var result = g.Resolve();
        stars = result.Stars;
        return result.Won;
    }
}
