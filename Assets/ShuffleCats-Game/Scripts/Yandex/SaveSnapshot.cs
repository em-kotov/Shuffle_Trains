using System;
using UnityEngine;

// The serialized form of the player's progress. JsonUtility turns this straight
// into the object handed to player.setData(), so the field names are also the
// keys stored on Yandex's side - renaming one drops that value for existing players.
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

    // Progress only ever moves forward, so the further-along snapshot wins.
    // Money breaks the tie when both are on the same level - that is the case
    // of a second device that only spent time in the shop.
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
        PlayerPrefs.SetFloat(SaveKeys.MusicVolume,
            version >= 2 ? Mathf.Clamp01(musicVolume) : AudioManager.DefaultVolume);
        PlayerPrefs.SetFloat(SaveKeys.SfxVolume,
            version >= 2 ? Mathf.Clamp01(sfxVolume) : AudioManager.DefaultVolume);

        // Only overwrite the shop state when the cloud actually carries one -
        // an empty string would wipe the locally seeded default skins.
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
