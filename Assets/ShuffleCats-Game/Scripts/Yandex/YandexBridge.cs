using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Receiving end of the jslib bridge: the JS side reaches Unity through
// SendMessage("YandexGamesBridge", ...), so the object name and every public
// method name below are part of that contract - keep them in sync with
// Plugins/WebGL/YandexGames.jslib.
public class YandexBridge : MonoBehaviour
{
    public const string ObjectName = "YandexGamesBridge";

    private static YandexBridge _instance;

    // Raised with the raw JSON returned by player.getData(), or an empty string
    // when there is nothing stored (or no SDK at all).
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

    // Covers the editor and standalone players. On WebGL browser/SDK
    // signals are authoritative - canvas focus is also lost to
    // the ad overlay, which must not be treated as the player leaving.
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
        // The pause is applied when the ad is requested, so there is nothing to
        // do here - the callback exists so the JS side never calls into nothing.
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

    // Opens a gameplay section on every level (re)load. This also fires for the
    // boot scene, before the SDK has resolved and before Game Ready has gone
    // out - GameplayStart drops those, and Bootstrap opens the first section
    // itself once the gameplay scene is up.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        YandexSdk.GameplayStart();
    }
}
