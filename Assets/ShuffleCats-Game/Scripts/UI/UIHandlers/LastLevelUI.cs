using UnityEngine;
using UnityEngine.UI;

// Shown when the final level in the sequence is completed. Its button restarts
// from level 1 while keeping the wallet money (money lives in PlayerPrefs under
// a separate key, so only the level index is reset).
public class LastLevelUI : UIPanel
{
    [SerializeField] private Button _playAgainButton;

    private void OnEnable()
    {
        _playAgainButton.onClick.AddListener(LevelLoader.RestartFromFirstLevel);
        _playAgainButton.onClick.AddListener(AudioManager.PlayUIClickStatic);
    }

    private void OnDisable()
    {
        _playAgainButton.onClick.RemoveListener(LevelLoader.RestartFromFirstLevel);
        _playAgainButton.onClick.RemoveListener(AudioManager.PlayUIClickStatic);
    }

    public void ShowWindow()
    {
        base.Show();
        _playAgainButton.interactable = true;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayWin();
    }

    public void HideWindow()
    {
        _playAgainButton.interactable = false;
        base.Hide();
    }
}
