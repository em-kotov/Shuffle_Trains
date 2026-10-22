using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    private const float SdkTimeout = 8f;
    private const float CloudSaveTimeout = 5f;

    [SerializeField] private int _firstLevelIndex = 1;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        YandexSdk.Initialize();
        StartCoroutine(LoadFirstLevel());
    }

    // Apply cloud progress before loading the first level, which reads the saved index.
    private IEnumerator LoadFirstLevel()
    {
        yield return WaitUntilOrTimeout(() => YandexSdk.IsResolved, SdkTimeout);

        yield return Localization.Initialize();
        LocalizationBinder.Create();

        bool cloudSaveReady = false;
        CloudSave.Load(() => cloudSaveReady = true);

        yield return WaitUntilOrTimeout(() => cloudSaveReady, CloudSaveTimeout);

        YandexLeaderboard.SyncScore();

        yield return SceneManager.LoadSceneAsync(_firstLevelIndex, LoadSceneMode.Single);

        // Game Ready must precede GameplayStart; the bridge also sees the non-playable boot scene.
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
