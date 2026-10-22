using UnityEngine;
using UnityEngine.UI;

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
