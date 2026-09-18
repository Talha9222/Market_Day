using System;
using UnityEngine;

// Authored scenario content. Nothing here is simulated or random (GDD 2.4):
// every price movement is a fixed percentage written into the event below.
// All companies, tickers and headlines are fictional (GDD 10).

public enum MDObjectiveKind
{
    FinalValueAbove,    // total portfolio value at resolution >= Amount
    PickStrongest,      // largest position is company index Index
    NoDrawdownBelow,    // total value never dipped below Amount at any resolution point
    RiskUnder,          // fraction of total value held in positions <= Amount (0..1)
    SectorAllocation    // fraction of *invested* value in Sector >= Amount (0..1)
}

[Serializable]
public class MDClue
{
    public string Title;
    public string Body;
    public MDClue(string title, string body) { Title = title; Body = body; }
}

[Serializable]
public class MDCompany
{
    public string Name;
    public string Tagline;
    public string Sector;
    public float StartPrice;
    public MDClue[] Clues;      // Clues[0] is free; each further clue costs one research token.
}

[Serializable]
public class MDMarketEvent
{
    public string Headline;
    public string Body;
    public float[] Move;        // percent change per company, index-matched to Scenario.Companies
}

[Serializable]
public class MDObjective
{
    public MDObjectiveKind Kind;
    public float Amount;
    public int Index;
    public string Sector;
    public string Description;  // shown on the objective card
}

[Serializable]
public class MDScenario
{
    public string Name;
    public string Blurb;
    public int Difficulty;      // 1-3, drives the stars on the carousel card
    public int Tokens;          // research token budget
    public int ParTokens;       // tokens a correct read actually needs; the 3rd star gate
    public MDCompany[] Companies;
    public MDMarketEvent[] Events;
    public MDObjective[] Objectives;

    /// <summary>Cumulative percent change a company sees across every event in this scenario.</summary>
    public float TotalMove(int company)
    {
        float mult = 1f;
        foreach (var e in Events) mult *= 1f + e.Move[company] * 0.01f;
        return (mult - 1f) * 100f;
    }

    public int BestCompany()
    {
        int best = 0;
        for (int i = 1; i < Companies.Length; i++)
            if (TotalMove(i) > TotalMove(best)) best = i;
        return best;
    }
}

public static class MDContent
{
    public const float StartingCapital = 10000f;
    public const int LotCount = 4;                                  // BUY commits 1/4 of starting capital
    public const float LotSize = StartingCapital / LotCount;        // $2,500 per BUY press
    public const int LivesPerScenario = 3;                          // GDD 8

    // ---- terse builders, so the table below stays readable ----

    static MDClue[] C(params string[] titleBodyPairs)
    {
        var clues = new MDClue[titleBodyPairs.Length / 2];
        for (int i = 0; i < clues.Length; i++)
            clues[i] = new MDClue(titleBodyPairs[i * 2], titleBodyPairs[i * 2 + 1]);
        return clues;
    }

    static MDCompany Co(string name, string tagline, string sector, float price, MDClue[] clues)
        => new MDCompany { Name = name, Tagline = tagline, Sector = sector, StartPrice = price, Clues = clues };

    static MDMarketEvent Ev(string headline, string body, float m0, float m1, float m2)
        => new MDMarketEvent { Headline = headline, Body = body, Move = new[] { m0, m1, m2 } };

    static MDObjective Value(float amount) => new MDObjective
    {
        Kind = MDObjectiveKind.FinalValueAbove,
        Amount = amount,
        Description = "Finish above " + MDUIKit.Money(amount)
    };

    static MDObjective Strongest(int index, string name) => new MDObjective
    {
        Kind = MDObjectiveKind.PickStrongest,
        Index = index,
        Description = "Hold your largest position in the strongest company"
    };

    static MDObjective Floor(float amount) => new MDObjective
    {
        Kind = MDObjectiveKind.NoDrawdownBelow,
        Amount = amount,
        Description = "Never let the portfolio fall below " + MDUIKit.Money(amount)
    };

    static MDObjective RiskUnder(float frac) => new MDObjective
    {
        Kind = MDObjectiveKind.RiskUnder,
        Amount = frac,
        Description = "Keep no more than " + Mathf.RoundToInt(frac * 100f) + "% of the portfolio invested"
    };

    static MDObjective InSector(string sector, float frac) => new MDObjective
    {
        Kind = MDObjectiveKind.SectorAllocation,
        Amount = frac,
        Sector = sector,
        Description = "Put at least " + Mathf.RoundToInt(frac * 100f) + "% of invested capital into " + sector
    };

    static MDScenario S(string name, string blurb, int difficulty, int tokens, int par,
                        MDCompany[] companies, MDMarketEvent[] events, params MDObjective[] objectives)
        => new MDScenario
        {
            Name = name, Blurb = blurb, Difficulty = difficulty,
            Tokens = tokens, ParTokens = par,
            Companies = companies, Events = events, Objectives = objectives
        };

    static MDCompany[] Trio(MDCompany a, MDCompany b, MDCompany c) => new[] { a, b, c };
    static MDMarketEvent[] Evs(params MDMarketEvent[] e) => e;

    static MDScenario[] _all;
    public static MDScenario[] All => _all ??= BuildAndShuffle();
    public static int Count => All.Length;
    public static MDScenario Get(int index) => All[Mathf.Clamp(index, 0, All.Length - 1)];

    /// <summary>
    /// The table below is authored with the strongest company written first, which is easy
    /// to read but makes "always pick the left one" a winning strategy. Each scenario's
    /// three companies are therefore reordered on load.
    ///
    /// The shuffle is seeded by the scenario index, not by the clock: a scenario always
    /// looks the same, on every device and on every retry, so nothing about the game
    /// becomes random (GDD 2.4) - the tell is just removed.
    /// </summary>
    static MDScenario[] BuildAndShuffle()
    {
        var scenarios = Build();
        for (int i = 0; i < scenarios.Length; i++) Reorder(scenarios[i], i);
        return scenarios;
    }

    static void Reorder(MDScenario s, int seed)
    {
        int n = s.Companies.Length;

        // order[slot] = index this company used to sit at.
        var order = new int[n];
        for (int i = 0; i < n; i++) order[i] = i;

        uint rng = (uint)seed * 2654435761u + 2463534242u;
        for (int i = n - 1; i > 0; i--)
        {
            rng = rng * 1664525u + 1013904223u;
            int j = (int)((rng >> 16) % (uint)(i + 1));
            (order[i], order[j]) = (order[j], order[i]);
        }

        var companies = new MDCompany[n];
        for (int slot = 0; slot < n; slot++) companies[slot] = s.Companies[order[slot]];
        s.Companies = companies;

        foreach (var e in s.Events)
        {
            var move = new float[n];
            for (int slot = 0; slot < n; slot++) move[slot] = e.Move[order[slot]];
            e.Move = move;
        }

        // Objectives that name a company by index have to follow it to its new slot.
        foreach (var o in s.Objectives)
            if (o.Kind == MDObjectiveKind.PickStrongest)
                o.Index = System.Array.IndexOf(order, o.Index);
    }

    // Value targets below assume the player commits two lots ($5,000) to the correct read.
    // MDSelfCheck verifies every scenario is winnable under exactly that play.
    static MDScenario[] Build() => new[]
    {
        // ================= PHASE 1 (1-7): one event, clues point the same way, roomy budget =================
        S("Opening Bell", "A quiet morning. One clear story.", 1, 4, 1,
            Trio(
                Co("Nova Motors", "Drive a Cleaner Tomorrow", "Automotive", 42.80f, C(
                    "New Factory Contract", "Secured a major supply contract overseas, expected to lift production by 25%.",
                    "Strong Order Book", "Dealer pre-orders for the new model are running well ahead of plan.",
                    "Hiring Push", "Opening a second assembly line and hiring across two shifts.")),
                Co("Helio Energy", "Power From The Sky", "Energy", 28.00f, C(
                    "Quiet Quarter", "No new projects announced this season.",
                    "Flat Demand", "Regional power demand is unchanged year on year.")),
                Co("Orbit Foods", "Nourish Every Table", "Food", 37.00f, C(
                    "Rising Input Costs", "Grain and packaging costs are climbing.",
                    "Margin Pressure", "Management warned that margins will tighten this half."))),
            Evs(Ev("Auto Sector Surges", "Strong delivery numbers across the sector lift confidence in manufacturers with committed order books.", 24f, 1f, -6f)),
            Value(11000f)),

        S("Green Future", "The clean energy transition finds a gear.", 1, 4, 1,
            Trio(
                Co("Helio Energy", "Power From The Sky", "Energy", 28.00f, C(
                    "Policy Tailwind", "Lawmakers are drafting incentives for clean generation.",
                    "Grid Contracts", "Three regional utilities signed long-term supply deals.",
                    "Capacity Doubling", "A second panel plant comes online next quarter.")),
                Co("Quarry & Sons", "Built To Last", "Materials", 51.20f, C(
                    "Steady Book", "Order volumes are flat but reliable.",
                    "Fuel Costs", "Haulage costs are eating into the quarry margin.")),
                Co("Orbit Foods", "Nourish Every Table", "Food", 37.00f, C(
                    "Stable Demand", "Grocery volumes rarely move much either way.",
                    "No Catalyst", "Nothing scheduled that would change the picture this month."))),
            Evs(Ev("Government Announces Green Energy Incentives", "New incentives will support clean generation and sustainable transport, expected to boost the sector for years.", 26f, 2f, 0f)),
            Value(11100f)),

        S("Full Pantry", "Boring businesses have good weeks too.", 1, 4, 1,
            Trio(
                Co("Orbit Foods", "Nourish Every Table", "Food", 37.00f, C(
                    "Shelf Expansion", "Two national grocers are doubling the shelf space they give the brand.",
                    "Input Costs Falling", "Grain prices have eased for a third straight month.",
                    "Own-Label Win", "Signed to supply a large retailer's own-label range.")),
                Co("Stratus Air", "Lift For Everyone", "Aviation", 19.40f, C(
                    "Fuel Exposure", "Fuel is the largest single cost and it is rising.",
                    "Thin Margins", "Even a good quarter leaves very little headroom.")),
                Co("Lantern Labs", "Tools For Thinking", "Technology", 88.60f, C(
                    "Quiet Roadmap", "No product launches scheduled this season.",
                    "High Expectations", "The price already assumes a strong year."))),
            Evs(Ev("Grocery Demand Holds Firm", "Households keep spending on staples while cutting back elsewhere. Packaged food suppliers gain shelf space and pricing power.", 22f, -8f, -3f)),
            Value(11000f)),

        S("Dock Lights", "Freight moves before anything else does.", 1, 4, 1,
            Trio(
                Co("Meridian Shipping", "Moving The World", "Shipping", 63.50f, C(
                    "Port Backlog Clearing", "The congestion that held sailings up all year is finally easing.",
                    "Rate Contracts Renewed", "Locked in next year's freight rates above this year's.",
                    "Fleet Ready", "Two refitted vessels rejoin service this month.")),
                Co("Copperline Rail", "Tracks To Everywhere", "Transport", 44.10f, C(
                    "Maintenance Window", "A long scheduled track closure will cut capacity.",
                    "Diverted Freight", "Some customers have already moved volume to road.")),
                Co("Beacon Textiles", "Woven Well", "Consumer", 22.30f, C(
                    "Soft Season", "Apparel orders came in below plan.",
                    "Discounting", "Clearing old stock at reduced prices."))),
            Evs(Ev("Shipping Rates Climb As Backlog Clears", "Freight operators with available capacity capture the rebound in volumes first.", 23f, -4f, -7f)),
            Value(11050f)),

        S("Bright Ward", "Care providers with contracts in hand.", 1, 4, 2,
            Trio(
                Co("Verdant Health", "Care, Close To Home", "Health", 56.70f, C(
                    "Clinic Rollout", "Twelve new neighbourhood clinics open this year.",
                    "Public Contract", "Won a multi-year regional care contract.",
                    "Staffing Solved", "Filled the nursing vacancies that capped growth.")),
                Co("Harbor Retail", "Everything, Nearby", "Retail", 31.90f, C(
                    "Footfall Down", "Store visits fell for a second quarter.",
                    "Lease Costs", "Renewals are coming in above the old rates.")),
                Co("Quarry & Sons", "Built To Last", "Materials", 51.20f, C(
                    "Flat Volumes", "Construction demand is steady, not growing.",
                    "No Announcements", "Nothing new on the books."))),
            Evs(Ev("Health Budget Expanded", "A broadened care budget favours providers already holding regional contracts.", 25f, -5f, 1f)),
            Value(11100f), Strongest(0, "Verdant Health")),

        S("Reading The Room", "The objective is the read, not the return.", 1, 4, 2,
            Trio(
                Co("Lantern Labs", "Tools For Thinking", "Technology", 88.60f, C(
                    "Enterprise Renewal", "The largest customers all renewed early.",
                    "Margin Expansion", "Cloud costs fell while pricing held.",
                    "New Platform", "A long-awaited platform ships next month.")),
                Co("Stratus Air", "Lift For Everyone", "Aviation", 19.40f, C(
                    "Load Factors Up", "Planes are flying fuller than last year.",
                    "Fuel Hedges Expiring", "Cheap fuel contracts run out this quarter.")),
                Co("Beacon Textiles", "Woven Well", "Consumer", 22.30f, C(
                    "Cotton Costs", "Raw cotton is at a two-year high.",
                    "Order Book Thin", "Retail buyers are ordering later and smaller."))),
            Evs(Ev("Software Spending Proves Sticky", "Businesses trim travel and inventory before they trim the tools they run on.", 21f, 4f, -9f)),
            Value(10900f), Strongest(0, "Lantern Labs")),

        S("Careful Hands", "Conviction is fine. Over-committing is not.", 1, 4, 2,
            Trio(
                Co("Quarry & Sons", "Built To Last", "Materials", 51.20f, C(
                    "Infrastructure Pipeline", "A decade of public works has been funded.",
                    "Reserves Extended", "Survey added fifteen years to the main site.",
                    "Price Discipline", "Held prices while competitors discounted.")),
                Co("Copperline Rail", "Tracks To Everywhere", "Transport", 44.10f, C(
                    "Steady Freight", "Volumes are holding, nothing exceptional.",
                    "Capex Heavy", "Most of the cash is going into track renewal.")),
                Co("Harbor Retail", "Everything, Nearby", "Retail", 31.90f, C(
                    "Weak Season", "The key trading season disappointed.",
                    "Stock Overhang", "Warehouses are still full of last season's goods."))),
            Evs(Ev("Infrastructure Programme Confirmed", "Funded public works give materials suppliers years of visible demand.", 24f, 3f, -8f)),
            Value(10700f), RiskUnder(0.60f)),

        // ================= PHASE 2 (8-13): two events, clues mildly conflict, tighter budget =================
        S("Crosswinds", "Two stories, and they disagree.", 2, 3, 2,
            Trio(
                Co("Stratus Air", "Lift For Everyone", "Aviation", 19.40f, C(
                    "Record Bookings", "Forward bookings are the strongest in the airline's history.",
                    "Fuel Hedges Hold", "Fuel is locked in below market for another full year.",
                    "New Routes", "Six profitable regional routes open next season.")),
                Co("Meridian Shipping", "Moving The World", "Shipping", 63.50f, C(
                    "Rates Softening", "Spot freight rates have slipped from the peak.",
                    "Fleet Costs", "Two vessels are in dry dock and earning nothing.")),
                Co("Lantern Labs", "Tools For Thinking", "Technology", 88.60f, C(
                    "Growth Slowing", "New customer additions came in under plan.",
                    "Priced For Perfection", "Any disappointment would be punished."))),
            Evs(Ev("Fuel Prices Spike", "Energy costs jump across the transport sector. Operators without hedges feel it immediately.", -6f, -9f, 2f),
                Ev("Travel Demand Surges", "Record seasonal bookings favour carriers that locked their fuel costs in early.", 33f, 6f, -2f)),
            Value(11000f)),

        S("Supply Chain Disruption", "When the route closes, who still delivers?", 2, 3, 2,
            Trio(
                Co("Copperline Rail", "Tracks To Everywhere", "Transport", 44.10f, C(
                    "Inland Network", "The rail network does not touch the affected shipping lane.",
                    "Spare Capacity", "Running well below maximum tonnage.",
                    "Contract Wins", "Three shippers have already switched volume to rail.")),
                Co("Meridian Shipping", "Moving The World", "Shipping", 63.50f, C(
                    "Lane Exposure", "A third of revenue runs through one canal.",
                    "Long Way Round", "Rerouting adds eleven days and a great deal of fuel.")),
                Co("Harbor Retail", "Everything, Nearby", "Retail", 31.90f, C(
                    "Stock On Hand", "Six weeks of inventory already in the country.",
                    "Restock Risk", "Anything past six weeks depends on the lane reopening."))),
            Evs(Ev("Shipping Lane Closed", "A key canal closes indefinitely. Sea freight reroutes; inland transport picks up the slack.", 18f, -16f, -4f),
                Ev("Closure Extended", "The closure is extended by a further quarter, hardening the shift to overland routes.", 14f, -9f, -7f)),
            Value(11200f), Strongest(0, "Copperline Rail")),

        S("Global Tensions", "Protect the downside before chasing the upside.", 2, 4, 2,
            Trio(
                Co("Quarry & Sons", "Built To Last", "Materials", 51.20f, C(
                    "Domestic Only", "Every site and customer is inside one country.",
                    "Stockpiled Inputs", "A year of consumables already on site.",
                    "Defensive Demand", "Public works keep running through downturns.")),
                Co("Lantern Labs", "Tools For Thinking", "Technology", 88.60f, C(
                    "Global Revenue", "Two thirds of revenue is earned abroad.",
                    "Currency Exposure", "Unhedged against a strengthening home currency.")),
                Co("Stratus Air", "Lift For Everyone", "Aviation", 19.40f, C(
                    "International Routes", "The profitable routes all cross borders.",
                    "Airspace Risk", "Two key corridors are under review."))),
            Evs(Ev("Trade Tensions Escalate", "Cross-border commerce faces new friction. Domestically focused businesses are insulated.", 9f, -14f, -18f),
                Ev("Domestic Programme Accelerated", "Public spending is pulled forward to offset the external shock.", 19f, -3f, -6f)),
            Value(10800f), Floor(9400f)),

        S("The Quiet Winner", "The loudest story is not the best one.", 2, 3, 2,
            Trio(
                Co("Verdant Health", "Care, Close To Home", "Health", 56.70f, C(
                    "Modest Guidance", "Management guided to steady, unspectacular growth.",
                    "Contracts Locked", "Revenue is contracted three years out regardless of the cycle.",
                    "Cash Generative", "Funds its own expansion with no new borrowing.")),
                Co("Nova Motors", "Drive a Cleaner Tomorrow", "Automotive", 42.80f, C(
                    "Bold Announcement", "Announced a new model line with headline-grabbing targets.",
                    "Execution Risk", "The last two launches both slipped by a year.")),
                Co("Beacon Textiles", "Woven Well", "Consumer", 22.30f, C(
                    "Turnaround Talk", "New management is promising a rapid recovery.",
                    "Debt Load", "Borrowings are high and refinancing is due."))),
            Evs(Ev("Markets Turn Cautious", "Investors rotate away from promises and towards contracted, visible revenue.", 12f, -11f, -15f),
                Ev("Earnings Season Confirms It", "Results reward the businesses that guided conservatively and delivered.", 13f, -5f, -9f)),
            Value(11000f), Strongest(0, "Verdant Health")),

        S("Energy In Focus", "A sector call, not a single company call.", 2, 4, 2,
            Trio(
                Co("Helio Energy", "Power From The Sky", "Energy", 28.00f, C(
                    "Storage Breakthrough", "A storage partner solved the overnight supply problem.",
                    "Utility Demand", "Utilities are bidding for every megawatt available.",
                    "Cost Curve", "Generation cost fell below the regional average.")),
                Co("Copperline Rail", "Tracks To Everywhere", "Transport", 44.10f, C(
                    "Coal Volumes Falling", "The largest freight category is in structural decline.",
                    "Diversifying Slowly", "Replacement freight is growing, but not fast enough.")),
                Co("Harbor Retail", "Everything, Nearby", "Retail", 31.90f, C(
                    "Energy Costs", "Store power bills are the second largest expense.",
                    "Passing It On", "Price rises are starting to hurt footfall."))),
            Evs(Ev("Energy Prices Rise", "Higher power costs squeeze energy buyers and reward generators.", 17f, -7f, -10f),
                Ev("Storage Milestone Confirmed", "Proven overnight storage removes the last objection to the clean generation case.", 15f, -2f, -3f)),
            Value(10900f), InSector("Energy", 0.60f)),

        S("Volatility Before Close", "Survive the shock, then take the upside.", 2, 3, 2,
            Trio(
                Co("Nova Motors", "Drive a Cleaner Tomorrow", "Automotive", 42.80f, C(
                    "Balance Sheet Strong", "Net cash, no refinancing due for five years.",
                    "Recall Contained", "The recall is real but the cost is already provisioned.",
                    "Order Book Intact", "No cancellations following the recall news.")),
                Co("Beacon Textiles", "Woven Well", "Consumer", 22.30f, C(
                    "Cheap On Paper", "Trades well below the sector average.",
                    "Cheap For A Reason", "Cash is running down faster than sales are recovering.")),
                Co("Stratus Air", "Lift For Everyone", "Aviation", 19.40f, C(
                    "Seasonal Peak", "Heading into the strongest quarter of the year.",
                    "Leverage", "Debt is high; a bad quarter would be dangerous."))),
            Evs(Ev("Recall Announced", "A sector-wide component recall hits confidence across manufacturers before the cost is understood.", -13f, -17f, -6f),
                Ev("Recall Cost Proves Small", "The provision covers it. Balance-sheet strength is rewarded and the discount unwinds.", 37f, -8f, 5f)),
            Value(10800f), Floor(9000f)),

        // ================= PHASE 3 (14-20): one clue misleads, multi-part objectives, tight margins =========
        S("Misdirection", "One of these clues is wrong.", 3, 3, 2,
            Trio(
                Co("Lantern Labs", "Tools For Thinking", "Technology", 88.60f, C(
                    "Analyst Downgrade", "A widely followed analyst cut the rating on valuation grounds.",
                    "Customers Disagree", "Renewal rates hit an all-time high the same week.",
                    "Buyback Started", "The board began buying its own shares at these prices.")),
                Co("Harbor Retail", "Everything, Nearby", "Retail", 31.90f, C(
                    "Upgrade Cycle", "Three brokers raised targets this month.",
                    "Same-Store Sales", "Underlying sales per store are still falling.")),
                Co("Beacon Textiles", "Woven Well", "Consumer", 22.30f, C(
                    "Positive Coverage", "Press attention has been unusually favourable.",
                    "Inventory Building", "Unsold stock has grown for four straight quarters."))),
            Evs(Ev("Downgrade Ignored", "Renewal data lands and contradicts the downgrade. The rating, not the business, was wrong.", 16f, -6f, -9f),
                Ev("Results Settle It", "Reported numbers confirm the customer data and bury the valuation argument.", 14f, -8f, -11f)),
            Value(11100f), Strongest(0, "Lantern Labs")),

        S("Return And Restraint", "Hit the number without betting the book.", 3, 3, 2,
            Trio(
                Co("Helio Energy", "Power From The Sky", "Energy", 28.00f, C(
                    "Subsidy Renewed", "The support scheme was extended for a further decade.",
                    "Backlog Full", "Installation slots are sold out for eighteen months.",
                    "Cash Funded", "The expansion needs no new equity.")),
                Co("Quarry & Sons", "Built To Last", "Materials", 51.20f, C(
                    "Record Quarter", "Reported the best quarter in the company's history.",
                    "One-Off Gain", "Most of the record came from selling a site, not from trading.")),
                Co("Meridian Shipping", "Moving The World", "Shipping", 63.50f, C(
                    "Dividend Raised", "Raised the payout by a fifth.",
                    "Paid From Reserves", "Earnings did not cover the dividend this year."))),
            Evs(Ev("Support Scheme Extended", "A decade of policy visibility re-rates generators with full order books.", 21f, -3f, -6f),
                Ev("One-Off Gains Unwound", "Investors strip out asset sales and re-price on trading performance alone.", 12f, -11f, -9f)),
            Value(10800f), RiskUnder(0.55f)),

        S("Sector Discipline", "Right sector, right size, right time.", 3, 3, 2,
            Trio(
                Co("Verdant Health", "Care, Close To Home", "Health", 56.70f, C(
                    "Reimbursement Cut", "A headline rate cut was announced for the sector.",
                    "Not Exposed", "The cut applies to a service line this provider exited last year.",
                    "Volume Growth", "Patient volumes are up a fifth year on year.")),
                Co("Nova Motors", "Drive a Cleaner Tomorrow", "Automotive", 42.80f, C(
                    "Strong Deliveries", "Delivery numbers beat every published estimate.",
                    "Price Cuts Behind It", "The volume came from discounting, and margins fell.")),
                Co("Lantern Labs", "Tools For Thinking", "Technology", 88.60f, C(
                    "Steady Quarter", "Nothing unexpected either way.",
                    "Fair Value", "Priced about right for the growth on offer."))),
            Evs(Ev("Reimbursement Cut Lands", "The sector sells off indiscriminately before anyone checks which providers are actually exposed.", -9f, 4f, 1f),
                Ev("Exposure Clarified", "Detail emerges and the unaffected providers recover hard.", 34f, -13f, 2f)),
            Value(10900f), InSector("Health", 0.60f), Floor(9200f)),

        S("The Expensive Lesson", "A good story at a bad price is still a bad price.", 3, 3, 2,
            Trio(
                Co("Meridian Shipping", "Moving The World", "Shipping", 63.50f, C(
                    "Unfashionable", "Nobody has written about this company in a year.",
                    "Fleet Renewed", "The capital spending cycle finished; cash now builds.",
                    "Rates Firming", "Long-term contract rates are quietly rising.")),
                Co("Stratus Air", "Lift For Everyone", "Aviation", 19.40f, C(
                    "Momentum", "The share price has doubled in six months.",
                    "Nothing New", "No change in earnings, routes, or costs explains it.")),
                Co("Beacon Textiles", "Woven Well", "Consumer", 22.30f, C(
                    "Recovery Story", "Management says the turnaround is working.",
                    "Cash Burn", "Cash left is enough for roughly three more quarters."))),
            Evs(Ev("Momentum Unwinds", "Positions built on price action alone unwind quickly when the flow stops.", 8f, -21f, -14f),
                Ev("Cash Returns Begin", "The finished capital cycle turns into buybacks and dividends.", 19f, -9f, -12f)),
            Value(11000f), Strongest(0, "Meridian Shipping"), RiskUnder(0.80f)),

        S("Three Shocks", "Hold your nerve twice, then collect.", 3, 3, 2,
            Trio(
                Co("Quarry & Sons", "Built To Last", "Materials", 51.20f, C(
                    "No Debt", "The balance sheet carries no borrowings at all.",
                    "Contracted Backlog", "Four years of work already signed.",
                    "Self Funded", "Every project is paid for from operating cash.")),
                Co("Harbor Retail", "Everything, Nearby", "Retail", 31.90f, C(
                    "Cheap Multiple", "Looks inexpensive on this year's earnings.",
                    "Floating Rate Debt", "Borrowing costs rise the moment rates do.")),
                Co("Copperline Rail", "Tracks To Everywhere", "Transport", 44.10f, C(
                    "Essential Network", "Freight has to move somehow.",
                    "Refinancing Due", "A large borrowing matures inside twelve months."))),
            Evs(Ev("Rates Rise Sharply", "Borrowing costs jump. Leverage stops being free and starts being the whole story.", -4f, -19f, -13f),
                Ev("Credit Tightens Further", "Lenders pull back. Refinancing anything becomes expensive.", -3f, -15f, -11f),
                Ev("Quality Re-Rates", "Capital rotates hard into businesses that never needed to borrow.", 41f, 3f, 6f)),
            Value(10900f), Floor(8800f)),

        S("Every Constraint", "Return target, risk cap, and the right read.", 3, 3, 2,
            Trio(
                Co("Helio Energy", "Power From The Sky", "Energy", 28.00f, C(
                    "Guidance Cut", "Management lowered this year's revenue guidance.",
                    "Timing Only", "The shortfall is two projects slipping a quarter, not lost work.",
                    "Backlog Grew", "Signed order backlog rose while guidance fell.")),
                Co("Nova Motors", "Drive a Cleaner Tomorrow", "Automotive", 42.80f, C(
                    "Guidance Raised", "Management raised the outlook for the year.",
                    "Channel Stuffing", "Dealer inventory rose faster than deliveries did.")),
                Co("Verdant Health", "Care, Close To Home", "Health", 56.70f, C(
                    "In Line", "Everything came in exactly as expected.",
                    "No Catalyst", "Nothing scheduled to change that."))),
            Evs(Ev("Guidance Taken At Face Value", "The market reacts to the headline numbers before reading the detail.", -11f, 9f, 1f),
                Ev("Backlog Data Published", "Order backlog is disclosed and the picture inverts completely.", 38f, -16f, 2f)),
            // Target sits low on purpose: the risk cap forbids the third lot that a higher
            // target would demand, so 3 stars has to be reachable on a two-lot commitment.
            Value(10600f), Strongest(0, "Helio Energy"), RiskUnder(0.70f)),

        S("Closing Bell", "Everything the desk has taught you.", 3, 3, 2,
            Trio(
                Co("Orbit Foods", "Nourish Every Table", "Food", 37.00f, C(
                    "Short Interest High", "A large number of investors are betting against it.",
                    "Thesis Is Stale", "The bear case rests on a contract that was renewed last month.",
                    "Pricing Power", "Passed through every input cost rise without losing volume.")),
                Co("Lantern Labs", "Tools For Thinking", "Technology", 88.60f, C(
                    "Consensus Favourite", "Almost every analyst rates it a buy.",
                    "Growth Decelerating", "Growth has slowed for three consecutive quarters.")),
                Co("Meridian Shipping", "Moving The World", "Shipping", 63.50f, C(
                    "Rate Peak Passed", "Freight rates have rolled over from the cycle high.",
                    "Costs Fixed", "The cost base cannot flex down as fast as rates are falling."))),
            Evs(Ev("Crowded Trades Unwind", "Positioning, not fundamentals, drives the first move in both directions.", -8f, -14f, -12f),
                Ev("Contract Renewal Disclosed", "The renewal invalidates the bear case and the short position covers in a hurry.", 29f, -9f, -8f),
                Ev("Quality Of Earnings", "Final results reward pricing power over headline growth.", 17f, -7f, -6f)),
            Value(11200f), Strongest(0, "Orbit Foods"), Floor(9000f)),
    };
}
