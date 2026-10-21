using System;
using System.Runtime.InteropServices;
using UnityEngine;

// Facade over the Yandex Games SDK (see Plugins/WebGL/YandexGames.jslib).
// Outside a WebGL player SDK calls are no-ops and ad callbacks fire immediately.
// Unity focus/pause callbacks still suspend gameplay in editor/standalone builds.
public static class YandexSdk
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void YG_Init();
    [DllImport("__Internal")] private static extern void YG_Dispose();
    [DllImport("__Internal")] private static extern void YG_GameReady();
    [DllImport("__Internal")] private static extern void YG_GameplayStart();
    [DllImport("__Internal")] private static extern void YG_GameplayStop();
    [DllImport("__Internal")] private static extern void YG_ShowFullscreenAdv();
    [DllImport("__Internal")] private static extern void YG_ShowRewardedVideo();
    [DllImport("__Internal")] private static extern void YG_SaveData(string json);
    [DllImport("__Internal")] private static extern void YG_LoadData();
    [DllImport("__Internal")] private static extern string YG_GetLanguage();
    [DllImport("__Internal")] private static extern string YG_GetDeviceType();

    // static readonly, not const: a const false would make every guarded
    // branch below compile as unreachable code in the editor.
    private static readonly bool RunsOnYandex = true;
#else
    private static readonly bool RunsOnYandex = false;
#endif

    // Requirement 4.4: fullscreen ads only at logical pauses. Yandex throttles
    // them to one per minute anyway - asking sooner only shows the "ad is
    // coming" splash for nothing.
    private const float MinSecondsBetweenInterstitials = 60f;

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

    // True once YaGames.init() resolved. Always false in the editor, so nothing
    // that gates gameplay may depend on it.
    public static bool IsInitialized { get; private set; }

    // The SDK answered, one way or the other. Bootstrap waits for this (with a
    // timeout) before loading the first level.
    public static bool IsResolved { get; private set; }

    public static bool IsPaused { get; private set; }
    public static bool IsAudioPaused { get; private set; }

    // Guided menu interaction may continue during the settings pause, but
    // never during a platform interruption or a different blocking menu.
    public static bool IsOnlyOptionsPaused => IsPaused && _optionsOpen &&
        !IsAudioPaused && !_leaderboardOpen && !_shopOpen && !_tutorialOpen;
    public static bool IsOnlyTutorialPaused => IsPaused && _tutorialOpen &&
        !IsAudioPaused && !_leaderboardOpen && !_shopOpen && !_optionsOpen;

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

    // Requirement 1.19.2: called the moment the player can actually start playing.
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

    // Requirement 1.19.3: brackets around actual gameplay. Both calls are
    // idempotent so scene loads and panel toggles can fire them freely.
    public static void GameplayStart()
    {
        // The boot scene and panels closing from OnEnable both call in before
        // Game Ready has gone out. Those early calls are dropped; Bootstrap
        // opens the first section itself once the gameplay scene is up.
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

    // Shows an interstitial and then runs onFinished. When no ad can be shown
    // (throttled, SDK missing, editor) onFinished still runs - callers continue
    // the level flow from it, so it must never be dropped.
    public static void ShowInterstitial(Action onFinished)
    {
        bool throttled = Time.realtimeSinceStartup - _lastInterstitialTime < MinSecondsBetweenInterstitials;

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

    // onFinished receives true when the reward was earned. Nothing calls this
    // yet - it is the hook for a "watch an ad" button (requirement 4.5).
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

    // Requirement 1.3: no sound while the tab is in the background, and the
    // gameplay section is closed for the time the player is away.
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

    // Unity's own focus event, used by the editor and standalone players only.
    // On WebGL the canvas reports itself unfocused for reasons unrelated to the
    // player leaving - it has not been clicked yet, or an ad overlay took focus
    // - which would wrongly pause the game and suppress GameplayAPI.start().
    // There, browser events and the SDK are the authority.
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

    private static bool CanShowAdvertisement()
    {
        return RunsOnYandex && IsInitialized && IsPaused == false && _bridge != null;
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

    internal static void HandleAuthDialog(bool open)
    {
        _authDialogOpen = open;
        ApplyPause();
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

    // Requirement 4.7: pause sound and gameplay for the whole ad; 1.3 requires
    // silence while unfocused. A reason only releases its own pause, so an ad
    // close or SDK resume cannot restart a game that is still in the background.
    private static void ApplyPause()
    {
        // An in-game menu stops gameplay but keeps UI sounds and music audible,
        // just like shop/options. Platform interruptions still silence all audio.
        bool audioPaused = _sdkPaused || _advertisementOpen || _applicationPaused ||
            _authDialogOpen || _browserFocused == false;
        bool paused = audioPaused || _leaderboardOpen || _optionsOpen || _shopOpen || _tutorialOpen;

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

        if (audioPaused && !IsAudioPaused)
            _audioPausedBeforePause = AudioListener.pause;

        if (audioPaused)
        {
            // Silence every source, even if AudioManager has not awakened yet.
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
        bool running = _gameReadySent && _gameplayRequested && IsPaused == false;
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
