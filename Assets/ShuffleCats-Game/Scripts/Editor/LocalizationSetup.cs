using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class LocalizationSetup
{
    private const string Folder = "Assets/ShuffleCats-Game/Localization";
    private const string FontPath = "Assets/ShuffleCats-Game/Resources/Fonts/Eno Black SDF.asset";

    [MenuItem("Tools/Shuffle Cats/Localization/Rebuild Tables")]
    public static void Configure()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play mode first.");

        if (AssetDatabase.IsValidFolder(Folder) == false)
            AssetDatabase.CreateFolder("Assets/ShuffleCats-Game", "Localization");

        var settings = LocalizationEditorSettings.ActiveLocalizationSettings;

        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            AssetDatabase.CreateAsset(settings, Folder + "/LocalizationSettings.asset");
            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
        }

        LocalizationSettings.Instance = settings;
        LocalizationSettings.InitializeSynchronously = false;
        var selectors = settings.GetStartupLocaleSelectors();
        selectors.Clear();
        selectors.Add(new SpecificLocaleSelector { LocaleId = new LocaleIdentifier("en") });

        foreach (string code in new[] { "en", "ru", "tr" })
        {
            if (LocalizationEditorSettings.GetLocales().Any(locale => locale.Identifier.Code == code))
                continue;

            var locale = Locale.CreateLocale(code);
            AssetDatabase.CreateAsset(locale, Folder + "/" + code + ".asset");
            LocalizationEditorSettings.AddLocale(locale);
        }

        var collection = LocalizationEditorSettings.GetStringTableCollection(Localization.TableName)
            ?? LocalizationEditorSettings.CreateStringTableCollection(Localization.TableName, Folder);
        var catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText("Assets/ShuffleCats-Game/Scripts/Editor/LocalizationStrings.json"));

        foreach (var table in collection.StringTables)
        {
            string code = table.LocaleIdentifier.Code;

            foreach (var row in catalog.Entries)
            {
                string value;

                if (code == "tr")
                {
                    value = row.Tr;
                }
                else if (code == "ru")
                {
                    value = row.Ru;
                }
                else
                {
                    value = row.En;
                }

                var entry = table.GetEntry(row.En) ?? table.AddEntry(row.En, value);
                entry.Value = value;
            }

            LocalizationEditorSettings.SetPreloadTableFlag(table, true);
            EditorUtility.SetDirty(table);
        }

        EditorUtility.SetDirty(collection.SharedData);
        EditorUtility.SetDirty(collection);
        EditorUtility.SetDirty(settings);
        LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);
        ExtendFont();
        AssetDatabase.SaveAssets();
        Validate();
    }

    [MenuItem("Tools/Shuffle Cats/Localization/Validate")]
    public static void Validate()
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection(Localization.TableName);
        int checkedCount = 0;

        foreach (var key in collection.SharedData.Entries)
        {
            foreach (var table in collection.StringTables)
            {
                string value = table.GetEntry(key.Id)?.Value;

                if (string.IsNullOrWhiteSpace(value))
                    throw new Exception("Missing translation: " + table.LocaleIdentifier + "/" + key.Key);

                string.Format(value, 12, 24);
                var expected = System.Text.RegularExpressions.Regex.Matches(key.Key, @"\{\d+\}").Cast<System.Text.RegularExpressions.Match>().Select(match => match.Value).OrderBy(x => x);
                var actual = System.Text.RegularExpressions.Regex.Matches(value, @"\{\d+\}").Cast<System.Text.RegularExpressions.Match>().Select(match => match.Value).OrderBy(x => x);

                if (expected.SequenceEqual(actual) == false)
                    throw new Exception("Format placeholders differ: " + key.Key);

                checkedCount++;
            }
        }

        if (checkedCount == 0)
            throw new Exception("No translation entries found.");

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        if (font.HasCharacters("ÇçĞğİıÖöŞşÜü", out uint[] missing, true) == false)
            throw new Exception("Turkish font coverage incomplete.");

        if (AssetDatabase.LoadAllAssetsAtPath(FontPath).OfType<Material>().Any() == false)
            throw new Exception("Font material not saved.");

        Debug.Log($"Localization validation: {checkedCount} entries; no gaps; Turkish glyphs present.");
        File.WriteAllText("Library/LocalizationValidation.txt", $"PASS: {checkedCount} table entries; no gaps; Turkish glyphs present.");
    }

    public static void BuildAddressables()
    {
        UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent(out var result);

        if (string.IsNullOrEmpty(result.Error) == false)
            throw new Exception(result.Error);
    }

    private static void ExtendFont()
    {
        const int samplingPointSize = 48;
        const int atlasPadding = 5;
        const int atlasSize = 256;

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        if (font == null)
            throw new Exception("Game UI font is missing.");

        // Avoid rebaking the atlas when the saved font and fallbacks already cover these characters.
        if (font.HasCharacters("ÇçĞğİıÖöŞşÜü", out uint[] _, true))
            return;

        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        font.TryAddCharacters("ÇçĞğİıÖöŞşÜü", out string missing);
        font.atlasPopulationMode = AtlasPopulationMode.Static;

        foreach (var atlas in font.atlasTextures)
        {
            if (AssetDatabase.Contains(atlas) == false)
                AssetDatabase.AddObjectToAsset(atlas, font);

            EditorUtility.SetDirty(atlas);
        }

        EditorUtility.SetDirty(font.material);

        if (string.IsNullOrEmpty(missing) == false)
        {
            const string fallbackPath = "Assets/ShuffleCats-Game/Resources/Fonts/Turkish/Nunito Turkish SDF.asset";
            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fallbackPath);

            if (fallback == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/ShuffleCats-Game/Resources/Fonts/Turkish/Nunito-Black.ttf");
                fallback = TMP_FontAsset.CreateFontAsset(source, samplingPointSize, atlasPadding, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, atlasSize, atlasSize);
                fallback.name = "Nunito Turkish SDF";
                AssetDatabase.CreateAsset(fallback, fallbackPath);

                foreach (var atlas in fallback.atlasTextures)
                    AssetDatabase.AddObjectToAsset(atlas, fallback);

                AssetDatabase.AddObjectToAsset(fallback.material, fallback);
            }

            fallback.atlasPopulationMode = AtlasPopulationMode.Dynamic;

            if (fallback.TryAddCharacters(missing, out string absent) == false)
                throw new Exception("Turkish fallback is missing: " + absent);

            fallback.atlasPopulationMode = AtlasPopulationMode.Static;

            if (font.fallbackFontAssetTable.Contains(fallback) == false)
                font.fallbackFontAssetTable.Add(fallback);

            EditorUtility.SetDirty(fallback);
            EditorUtility.SetDirty(fallback.material);

            foreach (var atlas in fallback.atlasTextures)
                EditorUtility.SetDirty(atlas);
        }

        EditorUtility.SetDirty(font);
    }

    [Serializable] public class Row
    {
        [SerializeField]
        private string en;

        [SerializeField]
        private string ru;

        [SerializeField]
        private string tr;

        public string En { get => en; set => en = value; }

        public string Ru { get => ru; set => ru = value; }

        public string Tr { get => tr; set => tr = value; }
    }

    [Serializable] public class Catalog
    {
        [SerializeField]
        private Row[] entries;

        public Row[] Entries { get => entries; set => entries = value; }
    }
}
