using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

// Eno Black is the primary Latin/Cyrillic UI font. A small baked Nunito fallback
// supplies the six Turkish letters not present in the original font.
//
// It also clears the per-instance font overrides the gameplay scenes carry on
// the Canvas prefab. Those override both m_fontAsset and m_sharedMaterial and
// silently beat anything set on the prefab itself, which is why editing the
// prefab alone appears to do nothing.
//
// The atlas is baked here and the asset switched to Static so the WebGL player
// never rasterizes a glyph at runtime. Re-run after changing the font or the
// character set.
public static class UiFontTool
{
    private const string SourceFontPath = "Assets/ShuffleCats-Game/Resources/Fonts/Eno Black.ttf";
    private const string FontAssetPath = "Assets/ShuffleCats-Game/Resources/Fonts/Eno Black SDF.asset";
    // Every prefab holding UI text. ShopSection and ShopSlot are instantiated at
    // runtime by ShopUI, so they are never part of a scene and would be missed
    // by a scene-only sweep.
    private static readonly string[] PrefabPaths =
    {
        "Assets/ShuffleCats-Game/Resources/Prefabs/UI/Canvas.prefab",
        "Assets/ShuffleCats-Game/Resources/Prefabs/UI/ShopSection.prefab",
        "Assets/ShuffleCats-Game/Resources/Prefabs/UI/ShopSlot.prefab",
    };
    private const string PreviousUiFontPath = "Assets/FPSFont/FPS Gaming Font/Square-Black SDF.asset";

    private static readonly string[] ScenePaths =
    {
        "Assets/ShuffleCats-Game/Scenes/Level-1.unity",
    };

    // Printable ASCII covers the English UI and every number the game formats.
    private const string Latin =
        " !\"#$%&'()*+,-./0123456789:;<=>?@" +
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`" +
        "abcdefghijklmnopqrstuvwxyz{|}~";

    private const string Cyrillic =
        "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ" +
        "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";

    private const string Punctuation = "«»—…";
    // Eno supplies these; the remaining Turkish glyphs use the baked fallback.
    private const string Turkish = "ÇçÖöÜü";

    // 48pt keeps ~165 glyphs inside a single 1024 atlas. SDF rescales cleanly,
    // so this is well above what the UI renders at.
    private const int SamplingPointSize = 48;
    private const int AtlasPadding = 5;
    private const int AtlasSize = 1024;

    [MenuItem("Tools/Yandex Games/Rebuild UI Font")]
    public static void Rebuild()
    {
        TMP_FontAsset font = BakeFontAsset();

        if (font == null)
        {
            return;
        }

        DropPreviousFallback();

        foreach (string prefabPath in PrefabPaths)
        {
            ApplyToPrefab(font, prefabPath);
        }

        ClearSceneOverrides();

        AssetDatabase.SaveAssets();
        Debug.Log($"UiFontTool - {font.characterTable.Count} glyphs in {font.atlasTextures.Length} atlas(es); " +
                    "prefab and scene overrides now all point at Eno Black.");
    }

    private static TMP_FontAsset BakeFontAsset()
    {
        Font source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);

        if (source == null)
        {
            Debug.LogError($"UiFontTool - source font not found at {SourceFontPath}");
            return null;
        }

        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(
            source,
            SamplingPointSize,
            AtlasPadding,
            GlyphRenderMode.SDFAA,
            AtlasSize,
            AtlasSize,
            AtlasPopulationMode.Dynamic,
            true);

        if (font == null)
        {
            Debug.LogError("UiFontTool - TMP could not create the font asset");
            return null;
        }

        font.name = Path.GetFileNameWithoutExtension(FontAssetPath);

        if (font.TryAddCharacters(Latin + Cyrillic + Turkish + Punctuation, out string missing) == false)
        {
            Debug.LogError($"UiFontTool - {Path.GetFileName(SourceFontPath)} is missing: {missing}");
            return null;
        }

        font.atlasPopulationMode = AtlasPopulationMode.Static;
        var turkishFallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/ShuffleCats-Game/Resources/Fonts/Turkish/Nunito Turkish SDF.asset");
        if (turkishFallback != null) font.fallbackFontAssetTable.Add(turkishFallback);

        AssetDatabase.DeleteAsset(FontAssetPath);
        AssetDatabase.CreateAsset(font, FontAssetPath);

        // The atlas textures and material only survive as sub-assets of the font.
        foreach (Texture2D atlas in font.atlasTextures)
        {
            atlas.name = font.name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlas, font);
        }

        font.material.name = font.name + " Material";
        AssetDatabase.AddObjectToAsset(font.material, font);

        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(FontAssetPath);

        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
    }

    // Eno Black carries Cyrillic itself, so the old font's Cyrillic fallback
    // is no longer used. The current font keeps its Turkish fallback.
    private static void DropPreviousFallback()
    {
        TMP_FontAsset previous = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PreviousUiFontPath);

        if (previous == null || previous.fallbackFontAssetTable == null || previous.fallbackFontAssetTable.Count == 0)
        {
            return;
        }

        previous.fallbackFontAssetTable = new List<TMP_FontAsset>();
        EditorUtility.SetDirty(previous);
    }

    private static void ApplyToPrefab(TMP_FontAsset font, string prefabPath)
    {
        GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"UiFontTool - prefab not found at {prefabPath}");
            return;
        }

        int changed = 0;

        foreach (TMP_Text label in prefab.GetComponentsInChildren<TMP_Text>(true))
        {
            if (label.font == font)
            {
                continue;
            }

            label.font = font;
            label.fontSharedMaterial = font.material;
            changed++;
        }

        PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefab);

        Debug.Log($"UiFontTool - retargeted {changed} label(s) in {Path.GetFileName(prefabPath)}");
    }

    // Reverting rather than re-pointing: with the override gone the prefab is
    // the single place the UI font is set, so the next font swap needs one edit.
    private static void ClearSceneOverrides()
    {
        foreach (string scenePath in ScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int reverted = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(label) == false)
                    {
                        continue;
                    }

                    reverted += Revert(label, "m_fontAsset");
                    reverted += Revert(label, "m_sharedMaterial");
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"UiFontTool - reverted {reverted} font override(s) in {Path.GetFileName(scenePath)}");
        }
    }

    private static int Revert(TMP_Text label, string propertyName)
    {
        SerializedObject serialized = new SerializedObject(label);
        SerializedProperty property = serialized.FindProperty(propertyName);

        if (property == null || property.prefabOverride == false)
        {
            return 0;
        }

        PrefabUtility.RevertPropertyOverride(property, InteractionMode.AutomatedAction);
        return 1;
    }
}
