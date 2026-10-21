using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelLoader
{
    // public static LevelLoader Instance;

    // private void Awake()
    // {
    //     if (Instance == null)
    //     {
    //         Instance = this;
    //         DontDestroyOnLoad(this);
    //     }
    //     else
    //     {
    //         Destroy(gameObject);
    //     }
    // }

    // Single-scene progression: advance the level index, then reload the active
    // gameplay scene. LevelManager reads the new index and builds that layout.
    // The index is advanced before the ad so the progress is already banked if
    // the player leaves while it plays (Yandex requirement 4.2).
    public static void LoadNextLevel()
    {
        LevelProgress.Advance();
        YandexSdk.ShowInterstitial(ReloadActiveScene);
    }

    // Restart the campaign from level 1. Wallet money is kept (it lives in a
    // separate PlayerPrefs key and a DontDestroyOnLoad Wallet) — only the level
    // index is reset.
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
