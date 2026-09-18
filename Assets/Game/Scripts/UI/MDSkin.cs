using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every art decision in the game, in one place. Sprites are loaded by filename from
/// Resources/Art, which is why the UI can stay code-built with no Inspector wiring.
///
/// The original filenames from the art drop are kept deliberately - so a name here can be
/// matched against the file on disk - and the comment says what each one was picked for.
/// To reskin something, change the string; nothing else needs to move.
///
/// A missing sprite returns null and the UI falls back to its flat placeholder colour, so
/// the game still runs if a file is renamed or removed.
/// </summary>
public static class MDSkin
{
    const string Folder = "Art/";

    static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

    public static Sprite Get(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;
        if (_cache.TryGetValue(fileName, out var cached)) return cached;   // caches misses too
        var sprite = Resources.Load<Sprite>(Folder + fileName);
        _cache[fileName] = sprite;
        return sprite;
    }

    // --- Backgrounds ---
    /// <summary>Warm study interior. Used behind every screen (GDD 5).</summary>
    public static Sprite Background => Get("bg_main");
    public static Sprite BackgroundAlt => Get("bg_alt");

    // --- Frames and plates ---
    /// <summary>Wide wood-and-brass plaque, 9-sliced. The generic button and bar.</summary>
    public static Sprite Plaque => Get("empty");
    /// <summary>Framed panel with a brass border and aged paper body, 9-sliced.</summary>
    public static Sprite Panel => Get("panel_framed");
    /// <summary>Thin brass bar, 9-sliced. Top bars and the ledger strip.</summary>
    public static Sprite Bar => Get("hud_panel");

    // --- Cards ---
    public static Sprite ClueCard => Get("company_clue_cards");
    public static Sprite ClueCardSealed => Get("company_clue_cards4");
    public static Sprite EventCard => Get("market_event_cards2");
    public static Sprite ObjectiveCard => Get("scenario_obj_card");
    public static Sprite Ledger => Get("portfolio_ledger");
    public static Sprite Dossier => Get("dosier_folder");
    public static Sprite ScenarioPlate => Get("level_icon");

    // --- Emblems ---
    public static Sprite Bell => Get("icon_brass_bell");
    public static Sprite CompanyPlate => Get("company");
    /// <summary>Magnifying glass. The INVESTIGATE action.</summary>
    public static Sprite Magnifier => Get("core_game_asset");
    /// <summary>Stack of coins. Research tokens and cash.</summary>
    public static Sprite Coins => Get("gameplay_elements4");
    public static Sprite ArrowUp => Get("asset_48");
    public static Sprite ArrowDown => Get("asset_49");
    /// <summary>Factory. Stands in for a company illustration on the dossier.</summary>
    public static Sprite CompanyArt => Get("asset_33");

    // The art drop has no star, so star ratings stay as brass diamond pips drawn in
    // MDUIKit.StarRow. Add a star sprite here and StarRow is the only place to change.
    public static Sprite Star => null;

    /// <summary>
    /// Brass gauge faces. The needle is painted into each image rather than being a
    /// separate part, so the gauge switches state instead of sweeping a needle.
    /// </summary>
    public static Sprite GaugeNeutral => Get("brass_gauge_states");    // full green-amber-red arc
    public static Sprite GaugeUp => Get("brass_gauge_states2");        // green
    public static Sprite GaugeFlat => Get("brass_gauge_states3");      // amber
    public static Sprite GaugeDown => Get("brass_gauge_states4");      // red

    /// <summary>Picks the gauge face for a price change, in percent.</summary>
    public static Sprite GaugeFor(float percent)
    {
        if (percent > 8f) return GaugeUp;
        if (percent > 0.05f) return GaugeFlat;
        if (percent < -0.05f) return GaugeDown;
        return GaugeNeutral;
    }
}
