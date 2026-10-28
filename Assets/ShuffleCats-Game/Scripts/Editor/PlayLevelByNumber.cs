using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PlayLevelByNumber : EditorWindow
{
    private const string SequencePath = "Assets/ShuffleCats-Game/Resources/Levels/LevelSequence.asset";
    private const string BootPath = "Assets/ShuffleCats-Game/Scenes/Boot.unity";
    private const string GameplayPath = "Assets/ShuffleCats-Game/Scenes/Level-1.unity";
    private ScrollView _levels;

    public void CreateGUI()
    {
        const int availabilityRefreshIntervalMilliseconds = 100;

        rootVisualElement.Clear();
        Label hint = new Label("Click a level to play. Level progress is temporary until Play stops.");
        hint.AddToClassList("level-picker-hint");
        rootVisualElement.Add(hint);
        _levels = new ScrollView();
        _levels.AddToClassList("level-list");
        StyleSheet styles = AssetDatabase.LoadAssetAtPath<StyleSheet>(
            "Assets/ShuffleCats-Game/Scripts/Editor/PlayLevelByNumber.uss");

        if (styles != null)
            rootVisualElement.styleSheets.Add(styles);

        rootVisualElement.Add(_levels);
        RefreshLevels();
        rootVisualElement.schedule.Execute(UpdateAvailability).Every(availabilityRefreshIntervalMilliseconds);
    }

    [MenuItem("Tools/Shuffle Cats/Play Level by Number")]
    private static void Open()
    {
        GetWindow<PlayLevelByNumber>("Play Level by Number").minSize = new Vector2(320f, 240f);
    }

    private static void Play(int index)
    {
        LevelSequence sequence = AssetDatabase.LoadAssetAtPath<LevelSequence>(SequencePath);

        if (sequence == null || sequence.Get(index) == null)
            return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootPath) == null ||
            AssetDatabase.LoadAssetAtPath<SceneAsset>(GameplayPath) == null)
        {
            Debug.LogError("Cannot play level: Boot or Level-1 scene is missing.");
            return;
        }

        LevelProgress.EditorPreviewIndex = index;

        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPaused = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameplayPath);
        }
        else
        {
            EditorPlayMode.ApplyStartScene();
            EditorApplication.EnterPlaymode();
        }
    }

    private void OnProjectChange()
    {
        if (_levels != null)
            RefreshLevels();
    }

    private void RefreshLevels()
    {
        _levels.Clear();
        LevelSequence sequence = AssetDatabase.LoadAssetAtPath<LevelSequence>(SequencePath);

        if (sequence == null || sequence.Count == 0)
        {
            _levels.Add(new Label("No levels found in LevelSequence."));
            return;
        }

        for (int i = 0; i < sequence.Count; i++)
        {
            int index = i;
            LevelLayout layout = sequence.Get(index);
            Button button = new Button(() => Play(index));

            if (layout != null)
            {
                button.text = $"Level {layout.LevelNumber}";
            }
            else
            {
                button.text = $"Missing level (slot {index + 1})";
            }

            button.SetEnabled(layout != null);
            _levels.Add(button);
        }

        UpdateAvailability();
    }

    private void UpdateAvailability()
    {
        bool transitioning;

        if (EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying)
        {
            transitioning = true;
        }
        else
        {
            transitioning = false;
        }

        bool booting;

        if (EditorApplication.isPlaying && SceneManager.GetActiveScene().path != GameplayPath)
        {
            booting = true;
        }
        else
        {
            booting = false;
        }

        _levels.SetEnabled(EditorApplication.isCompiling == false && transitioning == false && booting == false);
    }
}
