using UnityEngine;

public static class HintSettings
{
    private static bool? _enabled;

    public static bool Enabled
    {
        get
        {
            if (_enabled.HasValue == false)
            {
                if (PlayerPrefs.GetInt(SaveKeys.Hints, 1) != 0)
                {
                    _enabled = true;
                }
                else
                {
                    _enabled = false;
                }
            }

            return _enabled.Value;
        }

        set
        {
            if (Enabled == value)
                return;

            _enabled = value;
            int enabledValue;

            if (value)
            {
                enabledValue = 1;
            }
            else
            {
                enabledValue = 0;
            }

            PlayerPrefs.SetInt(SaveKeys.Hints, enabledValue);
            PlayerPrefs.Save();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => _enabled = null;
}
