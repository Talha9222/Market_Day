using UnityEngine.SceneManagement;

/// <summary>Scene names and the one piece of state that has to survive a scene load.</summary>
public static class MDApp
{
    public const string SceneSplash = "MDSplash";
    public const string SceneMenu = "MDMainMenu";
    public const string SceneGameplay = "MDGameplay";

    /// <summary>Which scenario the gameplay scene should open. Set by the carousel.</summary>
    public static int SelectedScenario;

    /// <summary>Set when returning from gameplay so the menu reopens on the carousel.</summary>
    public static bool ReturnToScenarioSelect;

    public static void Load(string scene) => SceneManager.LoadScene(scene);
}
