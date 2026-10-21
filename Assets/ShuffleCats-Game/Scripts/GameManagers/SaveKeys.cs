// Every PlayerPrefs key the game persists. Progress/audio keys are mirrored by
// SaveSnapshot; device-specific preferences are marked explicitly below.
public static class SaveKeys
{
    public const string LevelIndex = "CurrentLevelIndex";
    public const string BestCompletedLevel = "BestCompletedLevel";
    public const string Money = "PlayerMoney";
    public const string ShopOwned = "Shop.Owned";
    public const string ShopEquipped = "Shop.Equipped";
    public const string ShopSeeded = "Shop.Seeded.v3";
    public const string Music = "Audio.Music";
    public const string Sfx = "Audio.Sfx";
    public const string MusicVolume = "Audio.MusicVolume";
    public const string SfxVolume = "Audio.SfxVolume";
    // Device preference: Auto follows this device/platform. Deliberately
    // excluded from the progress snapshot in cloud saves.
    public const string Language = "Localization.Language";
    // Local assistance preference, kept on this device like the language choice.
    public const string Hints = "Hints.Enabled";
}
