using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneNav
{
    public const string HubSceneName = "Hub";
    public const string Level1SceneName = "level1";

    public static void LoadHub()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(HubSceneName);
    }

    public static void LoadLevel(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public static void LoadLevel1()
    {
        LoadLevel(Level1SceneName);
    }
}
