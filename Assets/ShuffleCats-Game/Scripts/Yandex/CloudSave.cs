using System;
using UnityEngine;

public static class CloudSave
{
    // Batch saves to stay within the SDK limit of 100 setData calls per five minutes.
    private const float SecondsBetweenPushes = 3f;

    private static bool _dirty;
    private static float _lastPushTime = float.NegativeInfinity;
    private static Action _loadCallback;
    private static bool _loading;

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

        // A restarted campaign can have a higher personal record but a lower current index.
        LevelProgress.MergeBestCompletedLevel(Mathf.Max(cloud.bestCompletedLevel, cloud.levelIndex));

        if (cloud.IsAheadOf(SaveSnapshot.FromLocal()) == false)
        {
            return;
        }

        cloud.ApplyToLocal();
    }
}
