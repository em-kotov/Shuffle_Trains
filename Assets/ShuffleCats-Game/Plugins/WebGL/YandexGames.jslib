// Bridge between the Unity build and the Yandex Games SDK (/sdk.js, loaded by the
// WebGLTemplates/YandexGames template). Every callback is pushed back into the
// "YandexGamesBridge" GameObject that YandexSdk keeps alive across scenes.
mergeInto(LibraryManager.library, {

  YG_Init: function () {
    if (window.ygDispose) window.ygDispose();
    var disposed = false;
    var subscribedSdk = null;
    // YG_Init runs from Awake, before createUnityInstance() has resolved and
    // published window.unityInstance, so an early message has to wait for one
    // of the two entry points to exist rather than being dropped.
    var send = function (method, param) {
      var payload = param === undefined ? "" : param;

      var attempt = function (retriesLeft) {
        if (disposed) return;
        try {
          if (typeof SendMessage === "function") {
            SendMessage("YandexGamesBridge", method, payload);
            return;
          }
        } catch (e) { }

        if (window.unityInstance) {
          window.unityInstance.SendMessage("YandexGamesBridge", method, payload);
          return;
        }

        if (retriesLeft > 0) {
          setTimeout(function () { attempt(retriesLeft - 1); }, 100);
        }
      };

      attempt(50);
    };

    window.ygSend = send;
    window.ysdk = null;
    window.ygPlayer = null;
    window.ygLeaderboardState = null;

    var lastFocus = null;
    var reportFocus = function (focused) {
      focused = focused && !document.hidden;
      if (focused === lastFocus) return;
      lastFocus = focused;
      send("SetBrowserFocus", focused ? "1" : "0");
    };
    // Alt-Tab can leave the page visible. Check the document, not the canvas:
    // focus can move between elements without the player leaving the game.
    var syncFocus = function () { reportFocus(document.hasFocus()); };
    var blur = function () { reportFocus(false); };
    var pause = function () { send("OnGameApiPause"); };
    var resume = function () {
      // An ad/SDK resume must not briefly restart a still-unfocused game.
      syncFocus();
      send("OnGameApiResume");
    };

    // Register before SDK/player initialization, including the initial focus
    // state. These also work when /sdk.js is unavailable during local testing.
    document.addEventListener("visibilitychange", syncFocus);
    window.addEventListener("focus", syncFocus);
    window.addEventListener("blur", blur);
    syncFocus();
    // Recover missed focus events in embedded browsers/iframes. Only state
    // changes cross the Unity bridge; this timer is independent of timeScale.
    var focusTimer = setInterval(syncFocus, 250);

    window.ygDispose = function () {
      disposed = true;
      window.ygLeaderboardState = null;
      clearInterval(focusTimer);
      document.removeEventListener("visibilitychange", syncFocus);
      window.removeEventListener("focus", syncFocus);
      window.removeEventListener("blur", blur);
      if (subscribedSdk) {
        subscribedSdk.off("game_api_pause", pause);
        subscribedSdk.off("game_api_resume", resume);
      }
    };

    if (typeof YaGames === "undefined") {
      // Running outside Yandex Games (local file, itch, etc.). The game must
      // still boot, so report the failure and let C# fall back to local saves.
      send("OnSdkFailed", "YaGames is not defined");
      return;
    }

    YaGames.init().then(function (sdk) {
      if (disposed) return;
      window.ysdk = sdk;

      // Subscribe immediately: startup ads have no showFullscreenAdv callbacks.
      // Do not wait for getPlayer/cloud saves before listening for these events.
      subscribedSdk = sdk;
      sdk.on("game_api_pause", pause);
      sdk.on("game_api_resume", resume);

      // The player is needed for cloud saves. It resolves for guests too - the
      // SDK then keeps the data locally, which satisfies requirement 1.2.2.
      return sdk.getPlayer({ scopes: false }).then(function (player) {
        if (!disposed) window.ygPlayer = player;
      }).catch(function () {
        if (!disposed) window.ygPlayer = null;
      });
    }).then(function () {
      send("OnSdkInitialized", "");
    }).catch(function (error) {
      send("OnSdkFailed", String(error));
    });
  },

  YG_Dispose: function () {
    if (window.ygDispose) window.ygDispose();
  },

  YG_GameReady: function () {
    if (window.ysdk && window.ysdk.features && window.ysdk.features.LoadingAPI) {
      window.ysdk.features.LoadingAPI.ready();
    }
  },

  YG_GameplayStart: function () {
    if (window.ysdk && window.ysdk.features && window.ysdk.features.GameplayAPI) {
      window.ysdk.features.GameplayAPI.start();
    }
  },

  YG_GameplayStop: function () {
    if (window.ysdk && window.ysdk.features && window.ysdk.features.GameplayAPI) {
      window.ysdk.features.GameplayAPI.stop();
    }
  },

  YG_ShowFullscreenAdv: function () {
    var send = window.ygSend;
    var finished = false;
    var finish = function (wasShown) {
      if (finished) return;
      finished = true;
      send("OnAdvClosed", wasShown ? "1" : "0");
    };

    if (!window.ysdk) {
      finish(false);
      return;
    }

    // Never resume on a timer: ads can last longer than 30 seconds. Resume only
    // on a terminal callback (and C# still waits for SDK/focus pauses to clear).
    try {
      window.ysdk.adv.showFullscreenAdv({
        callbacks: {
          onOpen: function () { if (!finished) send("OnAdvOpened", ""); },
          onClose: finish,
          onError: function () { finish(false); },
          onOffline: function () { finish(false); }
        }
      });
    } catch (error) {
      finish(false);
    }
  },

  YG_ShowRewardedVideo: function () {
    var send = window.ygSend;
    var finished = false;
    var rewarded = false;
    var finish = function (earned) {
      if (finished) return;
      finished = true;
      send("OnRewardedClosed", earned ? "1" : "0");
    };

    if (!window.ysdk) {
      finish(false);
      return;
    }

    try {
      window.ysdk.adv.showRewardedVideo({
        callbacks: {
          onOpen: function () { if (!finished) send("OnAdvOpened", ""); },
          onRewarded: function () { if (!finished) rewarded = true; },
          onClose: function () { finish(rewarded); },
          onError: function () { finish(false); }
        }
      });
    } catch (error) {
      finish(false);
    }
  },

  YG_SaveData: function (jsonPtr) {
    var json = UTF8ToString(jsonPtr);

    if (!window.ygPlayer) {
      return;
    }

    try {
      window.ygPlayer.setData(JSON.parse(json), true);
    } catch (e) { }
  },

  YG_LoadData: function () {
    if (!window.ygPlayer) {
      window.ygSend("OnDataLoaded", "");
      return;
    }

    window.ygPlayer.getData().then(function (data) {
      window.ygSend("OnDataLoaded", data ? JSON.stringify(data) : "");
    }).catch(function () {
      window.ygSend("OnDataLoaded", "");
    });
  },

  YG_GetLanguage: function () {
    var lang = "ru";

    if (window.ysdk && window.ysdk.environment && window.ysdk.environment.i18n) {
      lang = window.ysdk.environment.i18n.lang || "ru";
    }

    var size = lengthBytesUTF8(lang) + 1;
    var buffer = _malloc(size);
    stringToUTF8(lang, buffer, size);
    return buffer;
  },

  YG_GetDeviceType: function () {
    var type = "desktop";

    if (window.ysdk && window.ysdk.deviceInfo) {
      type = window.ysdk.deviceInfo.type || "desktop";
    }

    var size = lengthBytesUTF8(type) + 1;
    var buffer = _malloc(size);
    stringToUTF8(type, buffer, size);
    return buffer;
  }
});
