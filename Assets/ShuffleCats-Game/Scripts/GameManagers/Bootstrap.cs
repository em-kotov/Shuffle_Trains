using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    // Neither wait should ever be hit: they only keep a silent SDK from holding
    // the game hostage on the boot screen.
    private const float SdkTimeout = 8f;
    private const float CloudSaveTimeout = 5f;

    [SerializeField] private int _firstLevelIndex = 1;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        YandexSdk.Initialize();
        StartCoroutine(LoadFirstLevel());
    }

    // The cloud snapshot has to land before the first level reads
    // LevelProgress, otherwise the player would start on the wrong level and
    // then have it swapped underneath them.
    private IEnumerator LoadFirstLevel()
    {
        yield return WaitUntilOrTimeout(() => YandexSdk.IsResolved, SdkTimeout);

        // Auto uses the SDK language; a saved manual choice takes precedence.
        // Load tables asynchronously before UI is created (safe for WebGL).
        yield return Localization.Initialize();
        LocalizationBinder.Create();

        bool cloudSaveReady = false;
        CloudSave.Load(() => cloudSaveReady = true);

        yield return WaitUntilOrTimeout(() => cloudSaveReady, CloudSaveTimeout);

        YandexLeaderboard.SyncScore();

        yield return SceneManager.LoadSceneAsync(_firstLevelIndex, LoadSceneMode.Single);

        // Requirement 1.19.2: the player can interact from here. Game Ready has
        // to precede the first gameplay section, so both are raised here rather
        // than from YandexBridge's scene hook, which also sees the boot scene.
        YandexSdk.GameReady();
        YandexSdk.GameplayStart();
    }

    private IEnumerator WaitUntilOrTimeout(Func<bool> condition, float timeout)
    {
        float deadline = Time.realtimeSinceStartup + timeout;

        while (condition() == false && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }
    }
}
