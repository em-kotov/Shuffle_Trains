using UnityEngine;

public static class HintSettings
{
    private static bool? _enabled;

    public static bool Enabled
    {
        get => _enabled ?? (_enabled = PlayerPrefs.GetInt(SaveKeys.Hints, 1) != 0).Value;
        set
        {
            if (Enabled == value) return;
            _enabled = value;
            PlayerPrefs.SetInt(SaveKeys.Hints, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => _enabled = null;
}
