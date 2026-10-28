using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Components;

public static class LocalizationPlayModeChecks
{
    [MenuItem("Tools/Shuffle Cats/Localization/Check Language Switching (Play Mode)")]
    public static void Run()
    {
        if (EditorApplication.isPlaying == false || Localization.IsReady == false)
            throw new InvalidOperationException("Start the game from Boot before running localization checks.");

        EditorCoroutineUtility.StartCoroutineOwnerless(Check());
    }

    private static IEnumerator Check()
    {
        bool hadPreference = PlayerPrefs.HasKey(SaveKeys.Language);
        string savedPreference = Localization.Preference;
        string preview = EditorPrefs.GetString(Localization.EditorLanguageKey, "ru");
        var selector = UnityEngine.Object.FindObjectOfType<LanguageSelectorUI>(true);
        Transform panel = selector.transform.parent;
        bool active = panel.gameObject.activeSelf;
        panel.gameObject.SetActive(true);
        var probe = new GameObject("LocalizationProbe", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        probe.gameObject.SetActive(false);
        var report = new System.Text.StringBuilder();
        LeaderboardUI leaderboard = null;
        int level = PlayerPrefs.GetInt(SaveKeys.LevelIndex);
        int money = PlayerPrefs.GetInt(SaveKeys.Money);
        try
        {
            foreach (string code in new[] { "tr", "en", "ru", "tr", "en" })
            {
                selector.transform.Find(code).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                yield return WaitFrames();
                Assert(Localization.Code(Localization.Current) == code, "Selected locale " + code);
                Assert(PlayerPrefs.GetString(SaveKeys.Language) == code, "Saved choice " + code);
                string title;

                if (code == "tr")
                {
                    title = "AYARLAR";
                }
                else if (code == "ru")
                {
                    title = "НАСТРОЙКИ";
                }
                else
                {
                    title = "SETTINGS";
                }

                Assert(panel.Find("Title").GetComponent<TMP_Text>().text == title, "Options title " + code);
                Assert(selector.transform.Find(code + "/Selected").gameObject.activeSelf, "Selection check " + code);
                Assert(selector.GetComponentsInChildren<Transform>(true).Count(t => t.name == "Selected" && t.gameObject.activeSelf) == 1, "Exactly one selection");
                report.AppendLine("PASS switch, saved choice, live title and selection: " + code);
            }

            LocalizationBinder.Set(probe, "Level {0} completed!", 12);
            Localization.Select("tr");
            yield return WaitFrames();
            probe.gameObject.SetActive(true);
            yield return WaitFrames();
            Assert(probe.text == "Bölüm 12 tamamlandı!", "Inactive formatted label catches up");
            Assert(probe.GetComponents<LocalizeStringEvent>().Length == 1, "One binding per label");
            report.AppendLine("PASS inactive label and formatted win text");

            panel.gameObject.SetActive(false);
            leaderboard = UnityEngine.Object.FindObjectOfType<LeaderboardUI>();
            leaderboard.Open();
            yield return WaitFrames();
            Localization.Select("en");
            yield return WaitFrames();
            var status = leaderboard.GetComponentsInChildren<TMP_Text>(true).First(label => label.name == "Status");
            Assert(status.text == "Rankings are available on Yandex Games.", "Leaderboard status stays current after switching");
            Localization.Select("tr");
            yield return WaitFrames();
            Assert(status.text == "Sıralama Yandex Games'te kullanılabilir.", "Leaderboard status switches to Turkish");
            leaderboard.Close();
            report.AppendLine("PASS dynamically composed leaderboard status");

            EditorPrefs.SetString(Localization.EditorLanguageKey, "tr-TR");
            Localization.Select(Localization.Auto);
            yield return WaitFrames();
            Assert(Localization.Current == Localization.Language.Turkish, "Auto Turkish region");
            Assert(Localization.Preference == Localization.Auto, "Auto remains the saved preference");
            EditorPrefs.SetString(Localization.EditorLanguageKey, "fr-FR");
            Localization.Select(Localization.Auto);
            yield return WaitFrames();
            Assert(Localization.Current == Localization.Language.English, "Unsupported automatic locale falls back to English");
            Assert(Localization.Resolve("RU_ru") == Localization.Language.Russian, "Case and region normalization");
            Assert(Localization.NormalizePreference("invalid") == Localization.Auto, "Invalid saved preference falls back to Auto");
            Assert(PlayerPrefs.GetInt(SaveKeys.LevelIndex) == level && PlayerPrefs.GetInt(SaveKeys.Money) == money, "Progress unchanged");
            report.AppendLine("PASS Auto detection, locale fallback, invalid preference, progress preserved");
            File.WriteAllText("Library/LocalizationPlayChecks.txt", report.ToString());
            Debug.Log(report.ToString());
        }
        finally
        {
            if (leaderboard != null)
                leaderboard.Close();

            EditorPrefs.SetString(Localization.EditorLanguageKey, preview);
            Localization.Select(savedPreference);

            if (hadPreference == false)
            {
                PlayerPrefs.DeleteKey(SaveKeys.Language);
                PlayerPrefs.Save();
            }

            panel.gameObject.SetActive(active);
            UnityEngine.Object.Destroy(probe.gameObject);
        }
    }

    private static IEnumerator WaitFrames()
    {
        const double settleTimeSeconds = 0.4;

        double until = EditorApplication.timeSinceStartup + settleTimeSeconds;

        while (EditorApplication.timeSinceStartup < until)
            yield return null;
    }

    private static void Assert(bool condition, string message)
    {
        if (condition)
            return;

        File.WriteAllText("Library/LocalizationPlayChecks.txt", "FAIL: " + message);
        throw new Exception("Localization: " + message);
    }
}
