using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class YandexSdk
{
    private const float MinSecondsBetweenInterstitials = 60f;

#if UNITY_WEBGL && !UNITY_EDITOR
    // Use readonly to avoid compile-time unreachable branches in editor builds.
    private static readonly bool RunsOnYandex = true;
#else
    private static readonly bool RunsOnYandex = false;
#endif

    private static YandexBridge _bridge;
    private static bool _initializeRequested;
    private static bool _gameReadySent;
    private static bool _gameplayRunning;
    private static bool _gameplayRequested;
    private static bool _browserFocused = true;
    private static bool _sdkPaused;
    private static bool _applicationPaused;
    private static bool _advertisementOpen;
    private static bool _leaderboardOpen;
    private static bool _optionsOpen;
    private static bool _tutorialOpen;
    private static bool _shopOpen;
    private static bool _authDialogOpen;
    private static float _timeScaleBeforePause;
    private static bool _audioPausedBeforePause;
    private static float _lastInterstitialTime = float.NegativeInfinity;
    private static Action _interstitialCallback;
    private static Action<bool> _rewardedCallback;

    // Always false outside WebGL; boot must not wait for successful SDK initialization.
    public static bool IsInitialized { get; private set; }

    // Unlike IsInitialized, this also becomes true when SDK initialization fails.
    public static bool IsResolved { get; private set; }

    public static bool IsPaused { get; private set; }

    public static bool IsAudioPaused { get; private set; }

    public static bool IsOnlyOptionsPaused => IsPaused && _optionsOpen &&
        IsAudioPaused == false && _leaderboardOpen == false && _shopOpen == false && _tutorialOpen == false;

    public static bool IsOnlyTutorialPaused => IsPaused && _tutorialOpen &&
        IsAudioPaused == false && _leaderboardOpen == false && _shopOpen == false && _optionsOpen == false;

    public static string Language
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsInitialized)
            {
                return YG_GetLanguage();
            }

#endif
            return "ru";
        }
    }

    public static bool IsMobile
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsInitialized)
            {
                string type = YG_GetDeviceType();
                return type == "mobile" || type == "tablet";
            }

#endif
            return Application.isMobilePlatform;
        }
    }

    public static void Initialize()
    {
        if (_initializeRequested)
        {
            return;
        }

        _initializeRequested = true;
        _bridge = YandexBridge.Create();

        if (RunsOnYandex == false)
        {
            MarkResolved();
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        YG_Init();
#endif
    }

    public static void GameReady()
    {
        if (_gameReadySent)
        {
            return;
        }

        _gameReadySent = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        YG_GameReady();
#endif
    }

    public static void GameplayStart()
    {
        // Ignore scene/menu callbacks before Game Ready; Bootstrap starts the first gameplay section.
        if (_gameReadySent == false)
        {
            return;
        }

        // Keep the request if a level loads or a menu closes while suspended.
        _gameplayRequested = true;
        UpdateGameplayState();
    }

    public static void GameplayStop()
    {
        _gameplayRequested = false;
        UpdateGameplayState();
    }

    // Invoke onFinished even when no ad is shown; level transitions depend on the callback.
    public static void ShowInterstitial(Action onFinished)
    {
        bool throttled;

        if (Time.realtimeSinceStartup - _lastInterstitialTime < MinSecondsBetweenInterstitials)
        {
            throttled = true;
        }
        else
        {
            throttled = false;
        }

        if (CanShowAdvertisement() == false || throttled)
        {
            onFinished?.Invoke();
            return;
        }

        _lastInterstitialTime = Time.realtimeSinceStartup;
        _interstitialCallback = onFinished;

        OpenAdvertisement();

#if UNITY_WEBGL && !UNITY_EDITOR
        YG_ShowFullscreenAdv();
#endif
    }

    public static void ShowRewardedVideo(Action<bool> onFinished)
    {
        if (CanShowAdvertisement() == false)
        {
            onFinished?.Invoke(false);
            return;
        }

        _rewardedCallback = onFinished;

        OpenAdvertisement();

#if UNITY_WEBGL && !UNITY_EDITOR
        YG_ShowRewardedVideo();
#endif
    }

    public static void SaveData(string json)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (IsInitialized)
        {
            YG_SaveData(json);
        }
#endif
    }

    public static void LoadData()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (IsInitialized)
        {
            YG_LoadData();
            return;
        }

#endif
        YandexBridge.ReportDataLoaded(string.Empty);
    }

    public static void SetLeaderboardOpen(bool open)
    {
        _leaderboardOpen = open;
        ApplyPause();
    }

    public static void SetOptionsOpen(bool open)
    {
        _optionsOpen = open;
        ApplyPause();
    }

    public static void SetTutorialOpen(bool open)
    {
        _tutorialOpen = open;
        ApplyPause();
    }

    public static void SetShopOpen(bool open)
    {
        _shopOpen = open;
        ApplyPause();
    }

    internal static void HandleSdkInitialized()
    {
        IsInitialized = true;
        MarkResolved();
    }

    internal static void HandleSdkFailed(string reason)
    {
        IsInitialized = false;
        Debug.LogWarning($"YandexSdk - SDK unavailable, running with local saves only ({reason})");
        MarkResolved();
    }

    internal static void HandleInterstitialClosed()
    {
        Action callback = _interstitialCallback;
        _interstitialCallback = null;

        CloseAdvertisement();
        callback?.Invoke();
    }

    internal static void HandleRewardedClosed(bool rewarded)
    {
        Action<bool> callback = _rewardedCallback;
        _rewardedCallback = null;

        CloseAdvertisement();
        callback?.Invoke(rewarded);
    }

    internal static void HandleBrowserFocus(bool focused)
    {
        _browserFocused = focused;
        ApplyPause();
    }

    internal static void HandleSdkPause(bool paused)
    {
        _sdkPaused = paused;
        ApplyPause();
    }

    // WebGL canvas focus can be lost before the first click or during an ad.
    // Use browser/SDK signals there; Unity focus is only authoritative outside WebGL.
    internal static void HandleApplicationFocus(bool focused)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return;
#else
        HandleBrowserFocus(focused);
#endif
    }

    internal static void HandleApplicationPause(bool paused)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        _applicationPaused = paused;
        ApplyPause();
#endif
    }

    internal static void Shutdown()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        YG_Dispose();
#endif
        _gameplayRequested = false;
        _browserFocused = true;
        _sdkPaused = false;
        _applicationPaused = false;
        _advertisementOpen = false;
        _leaderboardOpen = false;
        _optionsOpen = false;
        _tutorialOpen = false;
        _shopOpen = false;
        _authDialogOpen = false;
        YandexLeaderboard.ResetSession();
        ApplyPause();
        _bridge = null;
        _initializeRequested = false;
        _gameReadySent = false;
        IsInitialized = false;
        IsResolved = false;
        _interstitialCallback = null;
        _rewardedCallback = null;
        _lastInterstitialTime = float.NegativeInfinity;
    }

    internal static void HandleAuthDialog(bool open)
    {
        _authDialogOpen = open;
        ApplyPause();
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void YG_Init();

    [DllImport("__Internal")]
    private static extern void YG_Dispose();

    [DllImport("__Internal")]
    private static extern void YG_GameReady();

    [DllImport("__Internal")]
    private static extern void YG_GameplayStart();

    [DllImport("__Internal")]
    private static extern void YG_GameplayStop();

    [DllImport("__Internal")]
    private static extern void YG_ShowFullscreenAdv();

    [DllImport("__Internal")]
    private static extern void YG_ShowRewardedVideo();

    [DllImport("__Internal")]
    private static extern void YG_SaveData(string json);

    [DllImport("__Internal")]
    private static extern void YG_LoadData();

    [DllImport("__Internal")]
    private static extern string YG_GetLanguage();

    [DllImport("__Internal")]
    private static extern string YG_GetDeviceType();
#endif

    private static bool CanShowAdvertisement()
    {
        return RunsOnYandex && IsInitialized && IsPaused == false && _bridge != null;
    }

    private static void OpenAdvertisement()
    {
        _advertisementOpen = true;

        ApplyPause();
    }

    private static void CloseAdvertisement()
    {
        if (_advertisementOpen == false)
        {
            return;
        }

        _advertisementOpen = false;

        ApplyPause();
    }

    // Each interruption releases only its own pause so closing an ad cannot resume a hidden tab.
    private static void ApplyPause()
    {
        bool audioPaused;

        if (_sdkPaused || _advertisementOpen || _applicationPaused ||
            _authDialogOpen || _browserFocused == false)
        {
            audioPaused = true;
        }
        else
        {
            audioPaused = false;
        }

        bool paused;

        if (audioPaused || _leaderboardOpen || _optionsOpen || _shopOpen || _tutorialOpen)
        {
            paused = true;
        }
        else
        {
            paused = false;
        }

        if (paused && IsPaused == false)
        {
            _timeScaleBeforePause = Time.timeScale;
        }

        if (paused)
        {
            Time.timeScale = 0f;
        }
        else if (IsPaused)
        {
            Time.timeScale = _timeScaleBeforePause;
        }

        if (audioPaused && IsAudioPaused == false)
            _audioPausedBeforePause = AudioListener.pause;

        if (audioPaused)
        {
            // AudioManager may not have awakened yet; silence all sources through the listener.
            AudioListener.pause = true;
        }
        else if (IsAudioPaused)
        {
            AudioListener.pause = _audioPausedBeforePause;
        }

        IsPaused = paused;
        IsAudioPaused = audioPaused;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMuted(audioPaused);
        }

        UpdateGameplayState();
    }

    private static void UpdateGameplayState()
    {
        bool running;

        if (_gameReadySent && _gameplayRequested && IsPaused == false)
        {
            running = true;
        }
        else
        {
            running = false;
        }

        if (_gameplayRunning == running)
        {
            return;
        }

        _gameplayRunning = running;

#if UNITY_WEBGL && !UNITY_EDITOR
        if (running)
            YG_GameplayStart();
        else
            YG_GameplayStop();
#endif
    }

    private static void MarkResolved()
    {
        IsResolved = true;
    }
}
