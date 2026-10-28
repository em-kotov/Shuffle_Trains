using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class EditorPlayMode
{
    private const string BootScenePath = "Assets/ShuffleCats-Game/Scenes/Boot.unity";
    private const string PlayFromBootKey = "ShuffleCats.PlayFromBoot";

    private const string PlayFromBootMenu = "Tools/Shuffle Cats/Play From Boot Scene";
    private const string RussianMenu = "Tools/Shuffle Cats/Preview Language/Russian";
    private const string EnglishMenu = "Tools/Shuffle Cats/Preview Language/English";
    private const string TurkishMenu = "Tools/Shuffle Cats/Preview Language/Turkish";

    private const string Russian = "ru";
    private const string English = "en";

    static EditorPlayMode()
    {
        ApplyStartScene();
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                LevelProgress.EditorPreviewIndex = -1;
                ApplyStartScene();
            }
        };
    }

    private static bool PlayFromBoot => EditorPrefs.GetBool(PlayFromBootKey, true);

    private static string PreviewLanguage => EditorPrefs.GetString(Localization.EditorLanguageKey, Russian);

    internal static void ApplyStartScene()
    {
        if (PlayFromBoot == false && LevelProgress.EditorPreviewIndex < 0)
        {
            EditorSceneManager.playModeStartScene = null;
            return;
        }

        SceneAsset boot = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);

        if (boot == null)
        {
            Debug.LogWarning($"EditorPlayMode - boot scene not found at {BootScenePath}");
            return;
        }

        EditorSceneManager.playModeStartScene = boot;
    }

    [MenuItem(PlayFromBootMenu)]
    private static void TogglePlayFromBoot()
    {
        EditorPrefs.SetBool(PlayFromBootKey, PlayFromBoot == false);
        ApplyStartScene();
    }

    [MenuItem(PlayFromBootMenu, true)]
    private static bool ValidatePlayFromBoot()
    {
        Menu.SetChecked(PlayFromBootMenu, PlayFromBoot);
        return true;
    }

    [MenuItem(RussianMenu)]
    private static void PreviewRussian()
    {
        EditorPrefs.SetString(Localization.EditorLanguageKey, Russian);
    }

    [MenuItem(RussianMenu, true)]
    private static bool ValidateRussian()
    {
        Menu.SetChecked(RussianMenu, PreviewLanguage == Russian);
        return true;
    }

    [MenuItem(EnglishMenu)]
    private static void PreviewEnglish()
    {
        EditorPrefs.SetString(Localization.EditorLanguageKey, English);
    }

    [MenuItem(EnglishMenu, true)]
    private static bool ValidateEnglish()
    {
        Menu.SetChecked(EnglishMenu, PreviewLanguage == English);
        return true;
    }

    [MenuItem(TurkishMenu)]
    private static void PreviewTurkish() => EditorPrefs.SetString(Localization.EditorLanguageKey, "tr");

    [MenuItem(TurkishMenu, true)]
    private static bool ValidateTurkish()
    {
        Menu.SetChecked(TurkishMenu, PreviewLanguage == "tr");
        return true;
    }
}
