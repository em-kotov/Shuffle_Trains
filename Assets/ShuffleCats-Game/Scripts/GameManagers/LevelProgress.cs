using UnityEngine;

public static class LevelProgress
{
    // Legacy saves store the next zero-based level index, which also counts completed levels.
    public static int BestCompletedLevel => Mathf.Max(0,
        Mathf.Max(PlayerPrefs.GetInt(SaveKeys.BestCompletedLevel, 0),
            PlayerPrefs.GetInt(SaveKeys.LevelIndex, 0)));

    public static void RecordCompletion(int levelNumber)
    {
#if UNITY_EDITOR
        if (EditorPreviewIndex >= 0)
            return;

#endif
        MergeBestCompletedLevel(levelNumber);
        CloudSave.Push();
        YandexLeaderboard.SyncScore();
    }

    public static void MergeBestCompletedLevel(int levelNumber)
    {
        PlayerPrefs.SetInt(SaveKeys.BestCompletedLevel, Mathf.Max(BestCompletedLevel, levelNumber));
        PlayerPrefs.Save();
    }
#if UNITY_EDITOR
    public static int EditorPreviewIndex
    {
        get => UnityEditor.SessionState.GetInt("ShuffleCats.LevelPreviewIndex", -1);
        set => UnityEditor.SessionState.SetInt("ShuffleCats.LevelPreviewIndex", value);
    }
#endif

    public static int CurrentIndex
    {
        get
        {
#if UNITY_EDITOR
            if (EditorPreviewIndex >= 0)
                return EditorPreviewIndex;

#endif
            return PlayerPrefs.GetInt(SaveKeys.LevelIndex, 0);
        }
        set
        {
#if UNITY_EDITOR
            if (EditorPreviewIndex >= 0)
            {
                EditorPreviewIndex = Mathf.Max(0, value);
                return;
            }

#endif
            PlayerPrefs.SetInt(SaveKeys.LevelIndex, Mathf.Max(0, value));
            PlayerPrefs.Save();
            CloudSave.Push();
        }
    }

    public static void Advance()
    {
        CurrentIndex = CurrentIndex + 1;
    }

    public static void Reset()
    {
        MergeBestCompletedLevel(BestCompletedLevel);
        CurrentIndex = 0;
    }
}
