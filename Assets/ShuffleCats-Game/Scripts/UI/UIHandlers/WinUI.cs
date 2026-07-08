using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WinUI : UIPanel
{
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private Button _nextButton;

    private void OnEnable()
    {
        _nextButton.onClick.AddListener(LevelLoader.LoadNextLevel);
        _nextButton.onClick.AddListener(AudioManager.PlayUIClickStatic);
    }

    private void OnDisable()
    {
        _nextButton.onClick.RemoveListener(LevelLoader.LoadNextLevel);
        _nextButton.onClick.RemoveListener(AudioManager.PlayUIClickStatic);
    }

    public void ShowWindow(int levelNumber)
    {
        base.Show();
        _nextButton.interactable = true;
        ShowText(levelNumber);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayWin();
    }

    public void HideWindow()
    {
        _nextButton.interactable = false;
        base.Hide();
    }

    private void ShowText(int levelNumber)
    {
        _levelText.text = $"Level {levelNumber} completed!";
    }
}
