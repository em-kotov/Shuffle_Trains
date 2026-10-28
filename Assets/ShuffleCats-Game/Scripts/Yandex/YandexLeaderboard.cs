using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class YandexLeaderboard
{
    public const string TechnicalName = "levelsCompleted";
    private const float RequestTimeout = 45f;
    private static readonly Dictionary<int, PendingRequest> Pending = new();
    private static int _nextId;
    private static bool _scoreDirty;
    private static bool _submitting;
    private static float _nextSubmitAt;

    public static void Read(Action<Result> callback) => Send("read", callback);

    public static void Authorize(Action<Result> callback) => Send("authorize", callback);

    public static void SyncScore()
    {
        if (LevelProgress.BestCompletedLevel > 0)
        {
            _scoreDirty = true;
        }
        else
        {
            _scoreDirty = false;
        }
    }

    public static void Tick()
    {
        const float submitRetryDelaySeconds = 30f;

        if (Pending.Count > 0)
        {
            var expired = new List<int>();

            foreach (var pair in Pending)
            {
                if (Time.realtimeSinceStartup >= pair.Value.Deadline)
                    expired.Add(pair.Key);
            }

            foreach (int id in expired)
                Complete(new Result { Id = id, Status = "timeout" });
        }

        if (_scoreDirty == false || _submitting || YandexSdk.IsInitialized == false ||
            Time.realtimeSinceStartup < _nextSubmitAt)
            return;

        _submitting = true;
        int score = LevelProgress.BestCompletedLevel;
        Send("submit", result =>
        {
            _submitting = false;
            _nextSubmitAt = Time.realtimeSinceStartup + submitRetryDelaySeconds;

            // Retain failed scores for a later retry; guest submission waits for explicit sign-in.
            if (result.Status != "unauthorized" &&
                (result.Succeeded == false || LevelProgress.BestCompletedLevel > score))
            {
                _scoreDirty = true;
            }
            else
            {
                _scoreDirty = false;
            }
        });
    }

    internal static void Receive(string json)
    {
        try
        {
            Result result = JsonUtility.FromJson<Result>(json);

            if (result != null)
                Complete(result);
        }
        catch (ArgumentException exception)
        {
            Debug.LogWarning($"Yandex leaderboard returned invalid data: {exception.Message}");
        }
    }

    internal static void ResetSession()
    {
        Pending.Clear();
        _scoreDirty = _submitting = false;
        _nextSubmitAt = 0;
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void YG_LeaderboardRequest(string json);
#endif

    private static void Send(string action, Action<Result> callback)
    {
        if (YandexSdk.IsInitialized == false)
        {
            callback?.Invoke(new Result { Status = "unavailable" });
            return;
        }

        var request = new Request { Id = ++_nextId, Action = action, Score = LevelProgress.BestCompletedLevel };
        PendingRequest pendingRequest = new PendingRequest();

        // Do not time out an active login dialog; its lifetime belongs to the SDK and the user.
        if (action == "authorize")
        {
            pendingRequest.Deadline = float.PositiveInfinity;
        }
        else
        {
            pendingRequest.Deadline = Time.realtimeSinceStartup + RequestTimeout;
        }

        pendingRequest.Callback = callback;
        Pending.Add(request.Id, pendingRequest);
#if UNITY_WEBGL && !UNITY_EDITOR
        YG_LeaderboardRequest(JsonUtility.ToJson(request));
#else
        Complete(new Result { Id = request.Id, Status = "unavailable" });
#endif
    }

    private static void Complete(Result result)
    {
        if (Pending.TryGetValue(result.Id, out PendingRequest request) == false)
            return;

        Pending.Remove(result.Id);
        result.Entries ??= Array.Empty<Entry>();

        if (result.Authorized && string.IsNullOrEmpty(result.ScoreStatus) == false)
            SyncScore();

        request.Callback?.Invoke(result);
    }

    [Serializable]
    public class Entry
    {
        [SerializeField]
        private int rank;

        [SerializeField]
        private int score;

        [SerializeField]
        private string name;

        [SerializeField]
        private bool isUser;

        public int Rank { get => rank; set => rank = value; }

        public int Score { get => score; set => score = value; }

        public string Name { get => name; set => name = value; }

        public bool IsUser { get => isUser; set => isUser = value; }
    }

    [Serializable]
    public class Result
    {
        [SerializeField]
        private int id;

        [SerializeField]
        private string status;

        [SerializeField]
        private bool authorized;

        [SerializeField]
        private bool authorizationKnown;

        [SerializeField]
        private int userRank;

        [SerializeField]
        private int bestScore;

        [SerializeField]
        private int retryAfter;

        [SerializeField]
        private string scoreStatus;

        [SerializeField]
        private Entry[] entries = Array.Empty<Entry>();

        public int Id { get => id; set => id = value; }

        public string Status { get => status; set => status = value; }

        public bool Authorized { get => authorized; set => authorized = value; }

        public bool AuthorizationKnown { get => authorizationKnown; set => authorizationKnown = value; }

        public int UserRank { get => userRank; set => userRank = value; }

        public int BestScore { get => bestScore; set => bestScore = value; }

        public int RetryAfter { get => retryAfter; set => retryAfter = value; }

        public string ScoreStatus { get => scoreStatus; set => scoreStatus = value; }

        public Entry[] Entries { get => entries; set => entries = value; }

        public bool Succeeded => Status == "ok";
    }

    [Serializable]
    private class Request
    {
        [SerializeField]
        private int id;

        [SerializeField]
        private string action;

        [SerializeField]
        private string name = TechnicalName;

        [SerializeField]
        private int score;

        public int Id { get => id; set => id = value; }

        public string Action { get => action; set => action = value; }

        public string Name { get => name; set => name = value; }

        public int Score { get => score; set => score = value; }
    }

    private class PendingRequest
    {
        public float Deadline { get; set; }

        public Action<Result> Callback { get; set; }
    }
}
