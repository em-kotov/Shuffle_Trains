using System;
using UnityEngine;

// Mirrors the PlayerPrefs the game already writes into the Yandex player data
// (requirement 1.9 / 1.13.3: progress survives a reload and follows the player
// across devices). PlayerPrefs stays the single source of truth at runtime -
// this only copies it out and, once at boot, copies a further-along cloud state in.
public static class CloudSave
{
    // Yandex allows 100 setData calls per 5 minutes. Pushes are batched so a
    // burst of purchases or a level transition costs one request.
    private const float SecondsBetweenPushes = 3f;

    private static bool _dirty;
    private static float _lastPushTime = float.NegativeInfinity;
    private static Action _loadCallback;
    private static bool _loading;

    // Reads the cloud snapshot and applies it when it is further along than the
    // local one, then invokes onLoaded. onLoaded always runs, including when
    // there is no SDK - the boot flow waits on it.
    public static void Load(Action onLoaded)
    {
        if (_loading)
        {
            return;
        }

        _loading = true;
        _loadCallback = onLoaded;

        YandexBridge.DataLoaded += OnDataLoaded;
        YandexSdk.LoadData();
    }

    public static void Push()
    {
        _dirty = true;
    }

    // Driven by YandexBridge so the debounce keeps running across scene loads.
    public static void Tick()
    {
        if (_dirty == false || Time.unscaledTime - _lastPushTime < SecondsBetweenPushes)
        {
            return;
        }

        _dirty = false;
        _lastPushTime = Time.unscaledTime;

        YandexSdk.SaveData(JsonUtility.ToJson(SaveSnapshot.FromLocal()));
    }

    private static void OnDataLoaded(string json)
    {
        YandexBridge.DataLoaded -= OnDataLoaded;
        _loading = false;

        if (string.IsNullOrEmpty(json) == false)
        {
            ApplyIfNewer(json);
        }

        Action callback = _loadCallback;
        _loadCallback = null;
        callback?.Invoke();
    }

    private static void ApplyIfNewer(string json)
    {
        SaveSnapshot cloud;

        try
        {
            cloud = JsonUtility.FromJson<SaveSnapshot>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"CloudSave - unreadable cloud snapshot, keeping local progress ({exception.Message})");
            return;
        }

        if (cloud == null || cloud.version <= 0)
        {
            return;
        }

        // A restarted campaign may have a higher personal record but a lower index.
        LevelProgress.MergeBestCompletedLevel(Mathf.Max(cloud.bestCompletedLevel, cloud.levelIndex));
        if (cloud.IsAheadOf(SaveSnapshot.FromLocal()) == false)
        {
            return;
        }

        cloud.ApplyToLocal();
    }
}
