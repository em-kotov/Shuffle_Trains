using UnityEngine;

// Persisted progress through the LevelSequence (which level the player is on).
public static class LevelProgress
{
    private const string Key = "CurrentLevelIndex";

    public static int CurrentIndex
    {
        get => PlayerPrefs.GetInt(Key, 0);
        set
        {
            PlayerPrefs.SetInt(Key, Mathf.Max(0, value));
            PlayerPrefs.Save();
        }
    }

    public static void Advance()
    {
        CurrentIndex = CurrentIndex + 1;
    }

    public static void Reset()
    {
        CurrentIndex = 0;
    }
}
