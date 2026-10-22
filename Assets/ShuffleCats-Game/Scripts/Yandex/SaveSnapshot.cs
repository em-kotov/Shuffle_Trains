using System;
using UnityEngine;

// These field names are persisted cloud keys; renaming them requires a save migration.
[Serializable]
public class SaveSnapshot
{
    public const int CurrentVersion = 3;

    public int version = CurrentVersion;
    public int levelIndex;
    public int bestCompletedLevel;
    public int money;
    public string shopOwned;
    public string shopEquipped;
    public int shopSeeded;
    public int music = 1;
    public int sfx = 1;
    public float musicVolume = AudioManager.DefaultVolume;
    public float sfxVolume = AudioManager.DefaultVolume;

    public static SaveSnapshot FromLocal()
    {
        return new SaveSnapshot
        {
            version = CurrentVersion,
            levelIndex = PlayerPrefs.GetInt(SaveKeys.LevelIndex, 0),
            bestCompletedLevel = LevelProgress.BestCompletedLevel,
            money = PlayerPrefs.GetInt(SaveKeys.Money, 0),
            shopOwned = PlayerPrefs.GetString(SaveKeys.ShopOwned, string.Empty),
            shopEquipped = PlayerPrefs.GetString(SaveKeys.ShopEquipped, string.Empty),
            shopSeeded = PlayerPrefs.GetInt(SaveKeys.ShopSeeded, 0),
            music = PlayerPrefs.GetInt(SaveKeys.Music, 1),
            sfx = PlayerPrefs.GetInt(SaveKeys.Sfx, 1),
            musicVolume = PlayerPrefs.GetFloat(SaveKeys.MusicVolume, AudioManager.DefaultVolume),
            sfxVolume = PlayerPrefs.GetFloat(SaveKeys.SfxVolume, AudioManager.DefaultVolume),
        };
    }

    public bool IsAheadOf(SaveSnapshot other)
    {
        if (levelIndex != other.levelIndex)
        {
            return levelIndex > other.levelIndex;
        }

        return money > other.money;
    }

    public void ApplyToLocal()
    {
        LevelProgress.MergeBestCompletedLevel(Mathf.Max(bestCompletedLevel, levelIndex));
        PlayerPrefs.SetInt(SaveKeys.LevelIndex, Mathf.Max(0, levelIndex));
        PlayerPrefs.SetInt(SaveKeys.Money, Mathf.Max(0, money));
        PlayerPrefs.SetInt(SaveKeys.ShopSeeded, shopSeeded);
        PlayerPrefs.SetInt(SaveKeys.Music, music);
        PlayerPrefs.SetInt(SaveKeys.Sfx, sfx);
        // Version 1 did not store volume; missing JSON floats must not mute old saves.
        float savedMusicVolume;

        if (version >= 2)
        {
            savedMusicVolume = Mathf.Clamp01(musicVolume);
        }
        else
        {
            savedMusicVolume = AudioManager.DefaultVolume;
        }

        PlayerPrefs.SetFloat(SaveKeys.MusicVolume, savedMusicVolume);
        float savedSfxVolume;

        if (version >= 2)
        {
            savedSfxVolume = Mathf.Clamp01(sfxVolume);
        }
        else
        {
            savedSfxVolume = AudioManager.DefaultVolume;
        }

        PlayerPrefs.SetFloat(SaveKeys.SfxVolume, savedSfxVolume);

        // Missing cloud shop data must not erase locally seeded defaults.
        if (string.IsNullOrEmpty(shopOwned) == false)
        {
            PlayerPrefs.SetString(SaveKeys.ShopOwned, shopOwned);
        }

        if (string.IsNullOrEmpty(shopEquipped) == false)
        {
            PlayerPrefs.SetString(SaveKeys.ShopEquipped, shopEquipped);
        }

        PlayerPrefs.Save();
        AudioManager.Instance?.RefreshSettings();
    }
}
