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
    public static void LoadNextLevel()
    {
        LevelProgress.Advance();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Restart the campaign from level 1. Wallet money is kept (it lives in a
    // separate PlayerPrefs key and a DontDestroyOnLoad Wallet) — only the level
    // index is reset.
    public static void RestartFromFirstLevel()
    {
        LevelProgress.Reset();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
