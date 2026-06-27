using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneNav
{
    public const string HubSceneName = "Hub";

    public static void LoadHub()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(HubSceneName);
    }
}
