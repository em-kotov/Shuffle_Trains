using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Captures the LevelManager's scene contents into data assets:
//  - "Capture Base Config" grabs the shared grid/track/movement + full station
//    list (run once for the game).
//  - "Capture Level Layout" grabs just the cats + which stations are active
//    (run per level). Stations' active state is read from their scene activeSelf.
public static class LevelConfigCapture
{
    private const string OutputFolder = "Assets/ShuffleCats-Game/Resources/Levels";

    [MenuItem("ShuffleCats/Reset Level Progress")]
    public static void ResetProgress()
    {
        LevelProgress.Reset();
        EditorUtility.DisplayDialog("Reset Level Progress",
            "Progress reset to the first level in the sequence.", "OK");
    }

    [MenuItem("ShuffleCats/Capture Base Config")]
    public static void Capture()
    {
        LevelManager manager = Object.FindObjectOfType<LevelManager>();

        if (manager == null)
        {
            EditorUtility.DisplayDialog("Capture Base Config",
                "No LevelManager found in the open scene.", "OK");
            return;
        }

        SerializedObject managerSo = new(manager);

        int startX = managerSo.FindProperty("_startX").intValue;
        int startY = managerSo.FindProperty("_startY").intValue;
        float cellSize = managerSo.FindProperty("_cellSize").floatValue;
        float axisYLevel = managerSo.FindProperty("_axisYLevel").floatValue;
        int gridWidth = managerSo.FindProperty("_parkingGridWidth").intValue;
        int gridHeight = managerSo.FindProperty("_parkingGridHeight").intValue;

        List<CatPlacement> cats = CaptureCats(managerSo, startX, startY, cellSize);
        List<StationPlacement> stations = CaptureStations(managerSo);

        LevelConfig config = ScriptableObject.CreateInstance<LevelConfig>();
        config.EditorSetLevel(
            managerSo.FindProperty("_levelNumber").intValue,
            managerSo.FindProperty("_levelWinBonus").intValue,
            managerSo.FindProperty("_levelCarBonus").intValue);
        config.EditorSetGrid(gridWidth, gridHeight, cellSize, startX, startY, axisYLevel);
        config.EditorSetTrack(
            managerSo.FindProperty("_maxCarsOnTrackCount").intValue,
            managerSo.FindProperty("_waitTime").floatValue);
        config.EditorSetCars(
            managerSo.FindProperty("_trackSpeed").floatValue,
            managerSo.FindProperty("_slideDuration").floatValue,
            managerSo.FindProperty("_searchMin").floatValue,
            managerSo.FindProperty("_searchMax").floatValue);
        config.EditorSetLayout(cats, stations);

        EnsureFolder();
        SaveAsset(config, $"{OutputFolder}/BaseConfig.asset");

        EditorUtility.DisplayDialog("Capture Base Config",
            $"Captured grid, track, movement and {stations.Count} stations into " +
            $"BaseConfig.", "OK");
    }

    [MenuItem("ShuffleCats/Capture Level Layout")]
    public static void CaptureLevelLayout()
    {
        LevelManager manager = Object.FindObjectOfType<LevelManager>();

        if (manager == null)
        {
            EditorUtility.DisplayDialog("Capture Level Layout",
                "No LevelManager found in the open scene.", "OK");
            return;
        }

        SerializedObject managerSo = new(manager);

        int startX = managerSo.FindProperty("_startX").intValue;
        int startY = managerSo.FindProperty("_startY").intValue;
        float cellSize = managerSo.FindProperty("_cellSize").floatValue;

        List<CatPlacement> cats = CaptureCats(managerSo, startX, startY, cellSize);
        List<bool> activeStations = CaptureActiveStations(managerSo);

        LevelLayout layout = ScriptableObject.CreateInstance<LevelLayout>();
        layout.EditorSet(
            managerSo.FindProperty("_levelNumber").intValue,
            managerSo.FindProperty("_levelWinBonus").intValue,
            managerSo.FindProperty("_levelCarBonus").intValue,
            managerSo.FindProperty("_maxCarsOnTrackCount").intValue,
            cats, activeStations);

        EnsureFolder();
        string defaultName = $"Level-{layout.LevelNumber}-Layout";
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Level Layout", defaultName, "asset",
            "Choose where to save this level's layout.", OutputFolder);

        if (string.IsNullOrEmpty(path))
        {
            Object.DestroyImmediate(layout);
            return;
        }

        SaveAsset(layout, path);

        int activeCount = activeStations.FindAll(active => active).Count;

        EditorUtility.DisplayDialog("Capture Level Layout",
            $"Captured {cats.Count} cats and {activeCount}/{activeStations.Count} " +
            $"active stations into {System.IO.Path.GetFileNameWithoutExtension(path)}.", "OK");
    }

    private static List<CatPlacement> CaptureCats(SerializedObject managerSo,
                                            int startX, int startY, float cellSize)
    {
        List<CatPlacement> cats = new();
        SerializedProperty carsProp = managerSo.FindProperty("_carSubscribers");

        for (int i = 0; i < carsProp.arraySize; i++)
        {
            Subscriber subscriber = carsProp.GetArrayElementAtIndex(i).objectReferenceValue as Subscriber;

            if (subscriber == null)
                continue;

            // Skip cats toggled off in the scene, so only active cats are captured.
            if (!subscriber.gameObject.activeSelf)
                continue;

            SerializedObject catSo = new(subscriber);
            CatColor color = (CatColor)catSo.FindProperty("_carColor").intValue;
            int length = catSo.FindProperty("_length").intValue;
            CarOrientation orientation = (CarOrientation)catSo.FindProperty("_orientation").intValue;
            float signDirection = catSo.FindProperty("_signDirection").floatValue;

            // The invert-flip checkbox lives on the SpriteFacingLock sitting on the
            // cat's CatTexture; capture it so the built level reproduces the facing.
            bool invertFlip = false;
            bool trackFlipSeparate = false;
            bool invertFlipTrack = false;
            SpriteFacingLock facingLock = subscriber.GetComponentInChildren<SpriteFacingLock>(true);

            if (facingLock != null)
            {
                SerializedObject lockSo = new(facingLock);
                invertFlip = lockSo.FindProperty("_invertFlip").boolValue;
                trackFlipSeparate = lockSo.FindProperty("_trackFlipSeparate").boolValue;
                invertFlipTrack = lockSo.FindProperty("_invertFlipTrack").boolValue;
            }

            // The rotation-fix checkboxes live on the cat's RotationFixer; capture
            // them so the built level reproduces the on-track-entry rotation snap.
            bool applyRotationFix = false;
            bool rotateFixLeft = false;
            RotationFixer rotationFixer = subscriber.GetComponentInChildren<RotationFixer>(true);

            if (rotationFixer != null)
            {
                SerializedObject fixerSo = new(rotationFixer);
                applyRotationFix = fixerSo.FindProperty("_applyRotation").boolValue;
                rotateFixLeft = fixerSo.FindProperty("_rotateLeft").boolValue;
            }

            List<CatColor> passengers = new();
            SerializedProperty colorsProp = catSo.FindProperty("_colors");

            for (int j = 0; j < colorsProp.arraySize; j++)
            {
                passengers.Add((CatColor)colorsProp.GetArrayElementAtIndex(j).intValue);
            }

            Vector3 position = subscriber.transform.position;
            Vector2Int cell = new(
                Mathf.RoundToInt((position.x - startX) / cellSize),
                Mathf.RoundToInt((position.z - startY) / cellSize));
            Vector3 eulerAngles = subscriber.transform.eulerAngles;

            cats.Add(new CatPlacement(color, length, cell, eulerAngles,
                                    orientation, signDirection, invertFlip,
                                    trackFlipSeparate, invertFlipTrack,
                                    applyRotationFix, rotateFixLeft, passengers));
        }

        return cats;
    }

    private static List<StationPlacement> CaptureStations(SerializedObject managerSo)
    {
        List<StationPlacement> stations = new();
        SerializedProperty stationsProp = managerSo.FindProperty("_stations");

        Dictionary<SpriteRenderer, (ThemeApplier.StationSpriteVariant variant, bool flipX, bool flipY)>
            themeEntries = ReadThemeEntries();

        for (int i = 0; i < stationsProp.arraySize; i++)
        {
            Station station = stationsProp.GetArrayElementAtIndex(i).objectReferenceValue as Station;

            if (station == null)
                continue;

            Transform t = station.transform;

            ThemeApplier.StationSpriteVariant variant = ThemeApplier.StationSpriteVariant.A;
            bool flipX = false;
            bool flipY = false;

            if (station.ThemeTarget != null && themeEntries.TryGetValue(station.ThemeTarget, out var theme))
            {
                variant = theme.variant;
                flipX = theme.flipX;
                flipY = theme.flipY;
            }

            stations.Add(new StationPlacement(t.position, t.eulerAngles, variant, flipX, flipY));
        }

        return stations;
    }

    private static List<bool> CaptureActiveStations(SerializedObject managerSo)
    {
        List<bool> active = new();
        SerializedProperty stationsProp = managerSo.FindProperty("_stations");

        for (int i = 0; i < stationsProp.arraySize; i++)
        {
            Station station = stationsProp.GetArrayElementAtIndex(i).objectReferenceValue as Station;
            active.Add(station != null && station.gameObject.activeSelf);
        }

        return active;
    }

    private static Dictionary<SpriteRenderer, (ThemeApplier.StationSpriteVariant, bool, bool)> ReadThemeEntries()
    {
        Dictionary<SpriteRenderer, (ThemeApplier.StationSpriteVariant, bool, bool)> map = new();
        ThemeApplier applier = Object.FindObjectOfType<ThemeApplier>();

        if (applier == null)
            return map;

        SerializedObject applierSo = new(applier);
        SerializedProperty list = applierSo.FindProperty("_stations");

        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty entry = list.GetArrayElementAtIndex(i);
            SpriteRenderer target = entry.FindPropertyRelative("Target").objectReferenceValue as SpriteRenderer;

            if (target == null)
                continue;

            ThemeApplier.StationSpriteVariant variant =
                (ThemeApplier.StationSpriteVariant)entry.FindPropertyRelative("Sprite").enumValueIndex;
            bool flipX = entry.FindPropertyRelative("FlipX").boolValue;
            bool flipY = entry.FindPropertyRelative("FlipY").boolValue;

            map[target] = (variant, flipX, flipY);
        }

        return map;
    }

    private static void SaveAsset(ScriptableObject asset, string assetPath)
    {
        ScriptableObject existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
        ScriptableObject saved;

        if (existing != null && existing.GetType() == asset.GetType())
        {
            // Overwrite contents in place so the asset GUID (and any scene
            // reference to it) survives a re-capture.
            EditorUtility.CopySerialized(asset, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(asset);
            saved = existing;
        }
        else
        {
            AssetDatabase.CreateAsset(asset, assetPath);
            saved = asset;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorGUIUtility.PingObject(saved);
        Selection.activeObject = saved;
    }

    private static void EnsureFolder()
    {
        if (AssetDatabase.IsValidFolder(OutputFolder))
            return;

        string parent = "Assets/ShuffleCats-Game/Resources";

        if (!AssetDatabase.IsValidFolder(parent))
            AssetDatabase.CreateFolder("Assets/ShuffleCats-Game", "Resources");

        AssetDatabase.CreateFolder(parent, "Levels");
    }
}
