using System;
using UnityEngine;

// These field names are persisted cloud keys; renaming them requires a save migration.
[Serializable]
public class SaveSnapshot
{
    public const int CurrentVersion = 3;
    [SerializeField]
    private int version = CurrentVersion;

    [SerializeField]
    private int levelIndex;

    [SerializeField]
    private int bestCompletedLevel;

    [SerializeField]
    private int money;

    [SerializeField]
    private string shopOwned;

    [SerializeField]
    private string shopEquipped;

    [SerializeField]
    private int shopSeeded;

    [SerializeField]
    private int music = 1;

    [SerializeField]
    private int sfx = 1;

    [SerializeField]
    private float musicVolume = AudioManager.DefaultVolume;

    [SerializeField]
    private float sfxVolume = AudioManager.DefaultVolume;

    public int Version { get => version; set => version = value; }

    public int LevelIndex { get => levelIndex; set => levelIndex = value; }

    public int BestCompletedLevel { get => bestCompletedLevel; set => bestCompletedLevel = value; }

    public int Money { get => money; set => money = value; }

    public string ShopOwned { get => shopOwned; set => shopOwned = value; }

    public string ShopEquipped { get => shopEquipped; set => shopEquipped = value; }

    public int ShopSeeded { get => shopSeeded; set => shopSeeded = value; }

    public int Music { get => music; set => music = value; }

    public int Sfx { get => sfx; set => sfx = value; }

    public float MusicVolume { get => musicVolume; set => musicVolume = value; }

    public float SfxVolume { get => sfxVolume; set => sfxVolume = value; }

    public static SaveSnapshot FromLocal()
    {
        return new SaveSnapshot
        {
            Version = CurrentVersion,
            LevelIndex = PlayerPrefs.GetInt(SaveKeys.LevelIndex, 0),
            BestCompletedLevel = LevelProgress.BestCompletedLevel,
            Money = PlayerPrefs.GetInt(SaveKeys.Money, 0),
            ShopOwned = PlayerPrefs.GetString(SaveKeys.ShopOwned, string.Empty),
            ShopEquipped = PlayerPrefs.GetString(SaveKeys.ShopEquipped, string.Empty),
            ShopSeeded = PlayerPrefs.GetInt(SaveKeys.ShopSeeded, 0),
            Music = PlayerPrefs.GetInt(SaveKeys.Music, 1),
            Sfx = PlayerPrefs.GetInt(SaveKeys.Sfx, 1),
            MusicVolume = PlayerPrefs.GetFloat(SaveKeys.MusicVolume, AudioManager.DefaultVolume),
            SfxVolume = PlayerPrefs.GetFloat(SaveKeys.SfxVolume, AudioManager.DefaultVolume),
        };
    }

    public bool IsAheadOf(SaveSnapshot other)
    {
        if (LevelIndex != other.LevelIndex)
        {
            return LevelIndex > other.LevelIndex;
        }

        return Money > other.Money;
    }

    public void ApplyToLocal()
    {
        const int volumeSettingsVersion = 2;

        LevelProgress.MergeBestCompletedLevel(Mathf.Max(BestCompletedLevel, LevelIndex));
        PlayerPrefs.SetInt(SaveKeys.LevelIndex, Mathf.Max(0, LevelIndex));
        PlayerPrefs.SetInt(SaveKeys.Money, Mathf.Max(0, Money));
        PlayerPrefs.SetInt(SaveKeys.ShopSeeded, ShopSeeded);
        PlayerPrefs.SetInt(SaveKeys.Music, Music);
        PlayerPrefs.SetInt(SaveKeys.Sfx, Sfx);

        // Version 1 did not store volume; missing JSON floats must not mute old saves.
        float savedMusicVolume;

        if (Version >= volumeSettingsVersion)
        {
            savedMusicVolume = Mathf.Clamp01(MusicVolume);
        }
        else
        {
            savedMusicVolume = AudioManager.DefaultVolume;
        }

        PlayerPrefs.SetFloat(SaveKeys.MusicVolume, savedMusicVolume);
        float savedSfxVolume;

        if (Version >= volumeSettingsVersion)
        {
            savedSfxVolume = Mathf.Clamp01(SfxVolume);
        }
        else
        {
            savedSfxVolume = AudioManager.DefaultVolume;
        }

        PlayerPrefs.SetFloat(SaveKeys.SfxVolume, savedSfxVolume);

        // Missing cloud shop data must not erase locally seeded defaults.
        if (string.IsNullOrEmpty(ShopOwned) == false)
        {
            PlayerPrefs.SetString(SaveKeys.ShopOwned, ShopOwned);
        }

        if (string.IsNullOrEmpty(ShopEquipped) == false)
        {
            PlayerPrefs.SetString(SaveKeys.ShopEquipped, ShopEquipped);
        }

        PlayerPrefs.Save();
        AudioManager.Instance?.RefreshSettings();
    }
}
