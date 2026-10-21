using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

// Matches the technical name in the Yandex Console. Scores are highest completed
// level numbers, including the final level, rather than spendable coins.
public static class YandexLeaderboard
{
    public const string TechnicalName = "levelsCompleted";
    private const float RequestTimeout = 45f;
    private static readonly Dictionary<int, PendingRequest> Pending = new();
    private static int _nextId;
    private static bool _scoreDirty, _submitting;
    private static float _nextSubmitAt;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void YG_LeaderboardRequest(string json);
#endif

    [Serializable]
    private class Request
    {
        public int id;
        public string action;
        public string name = TechnicalName;
        public int score;
    }

    [Serializable]
    public class Entry
    {
        public int rank;
        public int score;
        public string name;
        public bool isUser;
    }

    [Serializable]
    public class Result
    {
        public int id;
        public string status;
        public bool authorized;
        public bool authorizationKnown;
        public int userRank;
        public int bestScore;
        public int retryAfter;
        public string scoreStatus;
        public Entry[] entries = Array.Empty<Entry>();
        public bool Succeeded => status == "ok";
    }

    private class PendingRequest
    {
        public float deadline;
        public Action<Result> callback;
    }

    public static void Read(Action<Result> callback) => Send("read", callback);

    // Only call from the explicit, localized "Sign in with Yandex" button.
    public static void Authorize(Action<Result> callback) => Send("authorize", callback);

    public static void SyncScore()
    {
        _scoreDirty = LevelProgress.BestCompletedLevel > 0;
    }

    public static void Tick()
    {
        if (Pending.Count > 0)
        {
            var expired = new List<int>();
            foreach (var pair in Pending)
                if (Time.realtimeSinceStartup >= pair.Value.deadline) expired.Add(pair.Key);
            foreach (int id in expired)
                Complete(new Result { id = id, status = "timeout" });
        }

        if (!_scoreDirty || _submitting || !YandexSdk.IsInitialized ||
            Time.realtimeSinceStartup < _nextSubmitAt) return;

        _submitting = true;
        int score = LevelProgress.BestCompletedLevel;
        Send("submit", result =>
        {
            _submitting = false;
            _nextSubmitAt = Time.realtimeSinceStartup + 30f;
            // Keep a failed submission for a later retry. Guests submit when
            // they explicitly sign in; do not poll or prompt them repeatedly.
            _scoreDirty = result.status != "unauthorized" &&
                (!result.Succeeded || LevelProgress.BestCompletedLevel > score);
        });
    }

    private static void Send(string action, Action<Result> callback)
    {
        if (!YandexSdk.IsInitialized)
        {
            callback?.Invoke(new Result { status = "unavailable" });
            return;
        }

        var request = new Request { id = ++_nextId, action = action, score = LevelProgress.BestCompletedLevel };
        Pending.Add(request.id, new PendingRequest
        {
            // Login is controlled by the user. Never resume an active SDK dialog
            // on a timer; the modal can still be closed to return to the game.
            deadline = action == "authorize" ? float.PositiveInfinity : Time.realtimeSinceStartup + RequestTimeout,
            callback = callback
        });
#if UNITY_WEBGL && !UNITY_EDITOR
        YG_LeaderboardRequest(JsonUtility.ToJson(request));
#else
        Complete(new Result { id = request.id, status = "unavailable" });
#endif
    }

    internal static void Receive(string json)
    {
        try
        {
            Result result = JsonUtility.FromJson<Result>(json);
            if (result != null) Complete(result);
        }
        catch (ArgumentException exception)
        {
            Debug.LogWarning($"Yandex leaderboard returned invalid data: {exception.Message}");
        }
    }

    private static void Complete(Result result)
    {
        if (!Pending.TryGetValue(result.id, out PendingRequest request)) return;
        Pending.Remove(result.id);
        result.entries ??= Array.Empty<Entry>();
        if (result.authorized && !string.IsNullOrEmpty(result.scoreStatus))
            SyncScore();
        request.callback?.Invoke(result);
    }

    internal static void ResetSession()
    {
        Pending.Clear();
        _scoreDirty = _submitting = false;
        _nextSubmitAt = 0;
    }
}
