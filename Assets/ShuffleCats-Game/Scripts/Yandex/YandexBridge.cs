using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// The GameObject and callback names must match SendMessage calls in Plugins/WebGL/YandexGames.jslib.
public class YandexBridge : MonoBehaviour
{
    public const string ObjectName = "YandexGamesBridge";

    private static YandexBridge _instance;

    public static event Action<string> DataLoaded;

    public static YandexBridge Create()
    {
        if (_instance != null)
        {
            return _instance;
        }

        GameObject host = new GameObject(ObjectName);
        _instance = host.AddComponent<YandexBridge>();
        DontDestroyOnLoad(host);

        return _instance;
    }

    public static void ReportDataLoaded(string json)
    {
        DataLoaded?.Invoke(json);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        CloudSave.Tick();
        YandexLeaderboard.Tick();
    }

    // On WebGL, canvas focus can move to an ad; only browser/SDK signals should suspend gameplay.
    private void OnApplicationFocus(bool focused)
    {
        YandexSdk.HandleApplicationFocus(focused);
    }

    private void OnApplicationPause(bool paused)
    {
        YandexSdk.HandleApplicationPause(paused);
    }

    private void OnDestroy()
    {
        if (_instance != this)
        {
            return;
        }

        YandexSdk.Shutdown();
        _instance = null;
    }

    public void OnGameApiPause(string _)
    {
        YandexSdk.HandleSdkPause(true);
    }

    public void OnGameApiResume(string _)
    {
        YandexSdk.HandleSdkPause(false);
    }

    public void OnSdkInitialized(string _)
    {
        YandexSdk.HandleSdkInitialized();
    }

    public void OnSdkFailed(string reason)
    {
        YandexSdk.HandleSdkFailed(reason);
    }

    public void OnAdvOpened(string _)
    {
        // The JS bridge requires this callback; the pause is already applied when requesting the ad.
    }

    public void OnAdvClosed(string _)
    {
        YandexSdk.HandleInterstitialClosed();
    }

    public void OnRewardedClosed(string rewarded)
    {
        YandexSdk.HandleRewardedClosed(rewarded == "1");
    }

    public void OnDataLoaded(string json)
    {
        ReportDataLoaded(json);
    }

    public void OnLeaderboardResult(string json) => YandexLeaderboard.Receive(json);

    public void OnAuthDialogState(string open) => YandexSdk.HandleAuthDialog(open == "1");

    public void SetBrowserFocus(string focused)
    {
        YandexSdk.HandleBrowserFocus(focused == "1");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        YandexSdk.GameplayStart();
    }
}
