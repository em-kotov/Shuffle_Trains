using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelLoader
{
    public static void LoadNextLevel()
    {
        LevelProgress.Advance();
        YandexSdk.ShowInterstitial(ReloadActiveScene);
    }

    public static void RestartFromFirstLevel()
    {
        LevelProgress.Reset();
        YandexSdk.ShowInterstitial(ReloadActiveScene);
    }

    private static void ReloadActiveScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
