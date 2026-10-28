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

    // Device-specific language choice is intentionally excluded from cloud saves.
    public const string Language = "Localization.Language";

    // Keep the assistance preference local to this device.
    public const string Hints = "Hints.Enabled";
}
