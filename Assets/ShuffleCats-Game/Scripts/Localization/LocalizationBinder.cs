using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;

// Each binding retains its English key, including when returning to English.
public class LocalizationBinder : MonoBehaviour
{
    private static LocalizationBinder _instance;

    public static void Create()
    {
        if (_instance != null) return;
        var host = new GameObject(nameof(LocalizationBinder));
        _instance = host.AddComponent<LocalizationBinder>();
        DontDestroyOnLoad(host);
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply(scene);

    public static void Apply(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
                if (label.GetComponent<LocalizationIgnore>() == null && label.GetComponent<LocalizeStringEvent>() == null && Localization.HasKey(label.text))
                    Set(label, label.text);
            foreach (UnityEngine.UI.Text label in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                if (label.GetComponent<LocalizeStringEvent>() != null || !Localization.HasKey(label.text)) continue;
                string key = label.text;
                var binding = label.gameObject.AddComponent<LocalizeStringEvent>();
                binding.OnUpdateString.AddListener(value => label.text = value);
                binding.StringReference = new LocalizedString(Localization.TableName, key);
                label.text = Localization.Get(key);
                binding.RefreshString();
            }
        }
    }

    public static void Set(TMP_Text label, string key, params object[] arguments)
    {
        if (label == null) return;
        if (!Localization.HasKey(key)) { label.text = key; return; }
        var binding = label.GetComponent<LocalizeStringEvent>();
        if (binding == null)
        {
            binding = label.gameObject.AddComponent<LocalizeStringEvent>();
            binding.OnUpdateString.AddListener(value =>
            {
                label.text = value;
                UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(label.rectTransform);
            });
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = Mathf.Min(10, label.fontSize);
            label.enableAutoSizing = true;
            if (key == "Try again?" || key == "SETTINGS" || key == "SHOP")
                label.enableWordWrapping = false;
        }
        binding.StringReference = new LocalizedString(Localization.TableName, key) { Arguments = arguments };
        label.text = Localization.Format(key, arguments);
        binding.RefreshString();
    }
}
