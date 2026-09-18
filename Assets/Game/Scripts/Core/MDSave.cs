using UnityEngine;

/// <summary>
/// Lifetime stats and scenario progression (GDD 8). PlayerPrefs only — there is no
/// account system and nothing here is worth a save file.
/// Lives are deliberately NOT stored: the GDD grants 3 lives *per scenario*, so they
/// live on MDGame and reset every time a scenario starts.
/// </summary>
public static class MDSave
{
    const string KeyStars = "MD_Stars_";      // per scenario, 0 = never cleared
    const string KeyWon = "MD_ScenariosWon";
    const string KeyPerfect = "MD_PerfectReads";
    const string KeyCleared = "MD_ScenariosCleared";

    public static int ScenariosWon => PlayerPrefs.GetInt(KeyWon, 0);
    public static int PerfectReads => PlayerPrefs.GetInt(KeyPerfect, 0);
    public static int ScenariosCleared => PlayerPrefs.GetInt(KeyCleared, 0);

    public static int Stars(int scenarioIndex) => PlayerPrefs.GetInt(KeyStars + scenarioIndex, 0);

    /// <summary>Scenario 0 is always open; the rest unlock by clearing the one before.</summary>
    public static bool IsUnlocked(int scenarioIndex) => scenarioIndex <= 0 || Stars(scenarioIndex - 1) > 0;

    public static int HighestUnlocked()
    {
        for (int i = 0; i < MDContent.Count; i++)
            if (!IsUnlocked(i)) return i - 1;
        return MDContent.Count - 1;
    }

    public static void RecordResult(int scenarioIndex, bool won, int stars)
    {
        if (!won)
        {
            PlayerPrefs.Save();
            return;
        }

        PlayerPrefs.SetInt(KeyWon, ScenariosWon + 1);
        if (stars >= 3) PlayerPrefs.SetInt(KeyPerfect, PerfectReads + 1);

        // "Cleared" counts distinct scenarios, so only the first clear of each increments it.
        if (Stars(scenarioIndex) == 0) PlayerPrefs.SetInt(KeyCleared, ScenariosCleared + 1);

        // Keep the best result, never downgrade it on a sloppier replay.
        if (stars > Stars(scenarioIndex)) PlayerPrefs.SetInt(KeyStars + scenarioIndex, stars);

        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        for (int i = 0; i < MDContent.Count; i++) PlayerPrefs.DeleteKey(KeyStars + i);
        PlayerPrefs.DeleteKey(KeyWon);
        PlayerPrefs.DeleteKey(KeyPerfect);
        PlayerPrefs.DeleteKey(KeyCleared);
        PlayerPrefs.Save();
    }
}
