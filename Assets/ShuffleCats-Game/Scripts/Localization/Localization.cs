using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class Localization
{
    public const string TableName = "Game UI";
    public const string Auto = "auto";
    public enum Language { English, Russian, Turkish }
    public static event Action Changed;

    private static readonly Dictionary<string, StringTable> Tables = new();
    private static readonly List<AsyncOperationHandle<StringTable>> Handles = new();
    private static readonly HashSet<string> RussianCodes = new() { "ru", "be", "kk", "uk", "uz" };
    public static bool IsReady { get; private set; }
    public static string Preference => NormalizePreference(PlayerPrefs.GetString(SaveKeys.Language, Auto));
    public static Language Current => Resolve(LocalizationSettings.SelectedLocale?.Identifier.Code);

#if UNITY_EDITOR
    public const string EditorLanguageKey = "ShuffleCats.PreviewLanguage";
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

        foreach (var handle in Handles)
            if (handle.IsValid())
                Addressables.Release(handle);

        Handles.Clear();
        Tables.Clear();
        IsReady = false;
        Changed = null;
    }

    public static IEnumerator Initialize()
    {
        if (IsReady)
            yield break;

        yield return LocalizationSettings.InitializationOperation;

        foreach (Locale locale in LocalizationSettings.AvailableLocales.Locales)
        {
            var handle = LocalizationSettings.StringDatabase.GetTableAsync(TableName, locale);
            yield return handle;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError("Could not load UI translations for " + locale.Identifier.Code);
                continue;
            }

            // Retain loaded tables so immediate WebGL lookups never need WaitForCompletion.
            Addressables.ResourceManager.Acquire(handle);
            Handles.Add(handle);
            Tables[locale.Identifier.Code] = handle.Result;
        }

        IsReady = true;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        ApplyPreference();
    }

    public static void Select(string preference)
    {
        preference = NormalizePreference(preference);
        PlayerPrefs.SetString(SaveKeys.Language, preference);
        PlayerPrefs.Save();
        var previous = LocalizationSettings.SelectedLocale;
        ApplyPreference();

        // Auto and a manual choice can resolve to the same locale.
        if (previous == LocalizationSettings.SelectedLocale)
            Changed?.Invoke();
    }

    private static void ApplyPreference()
    {
        if (IsReady == false)
            return;

        string code = Preference;

        if (code == Auto)
            code = DetectLanguageCode();

        var locale = LocalizationSettings.AvailableLocales.GetLocale(Code(Resolve(code)));

        if (locale != null)
            LocalizationSettings.SelectedLocale = locale;
    }

    private static string DetectLanguageCode()
    {
#if UNITY_EDITOR
        return UnityEditor.EditorPrefs.GetString(EditorLanguageKey, "ru");
#else
        if (YandexSdk.IsInitialized)
            return YandexSdk.Language;

        switch (Application.systemLanguage)
        {
            case SystemLanguage.Turkish:
                return "tr";
            case SystemLanguage.Russian:
                return "ru";
            case SystemLanguage.Ukrainian:
                return "uk";
            case SystemLanguage.Belarusian:
                return "be";
            default:
                return "en";
        }
#endif
    }

    public static string Get(string english)
    {
        if (string.IsNullOrEmpty(english))
            return english;

        if (Tables.TryGetValue(Code(Current), out var table))
        {
            return table.GetEntry(english)?.LocalizedValue ?? english;
        }
        else
        {
            return english;
        }
    }

    public static bool HasKey(string english) => string.IsNullOrEmpty(english) == false
        && Tables.TryGetValue("en", out var table) && table.GetEntry(english) != null;
    public static string Format(string english, params object[] arguments) => string.Format(Get(english), arguments);
    private static void OnLocaleChanged(Locale locale) => Changed?.Invoke();
    public static string NormalizePreference(string value)
    {
        if (value == "en" || value == "ru" || value == "tr")
        {
            return value;
        }
        else
        {
            return Auto;
        }
    }

    public static string Code(Language language)
    {
        if (language == Language.Turkish)
        {
            return "tr";
        }
        else if (language == Language.Russian)
        {
            return "ru";
        }
        else
        {
            return "en";
        }
    }

    public static Language Resolve(string languageCode)
    {
        string code = (languageCode ?? string.Empty).Trim().ToLowerInvariant().Split('-', '_')[0];

        if (code == "tr")
        {
            return Language.Turkish;
        }
        else if (RussianCodes.Contains(code))
        {
            return Language.Russian;
        }
        else
        {
            return Language.English;
        }
    }
}
