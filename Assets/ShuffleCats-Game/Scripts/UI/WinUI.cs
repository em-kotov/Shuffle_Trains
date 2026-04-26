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
    }

    private void OnDisable()
    {
        _nextButton.onClick.RemoveListener(LevelLoader.LoadNextLevel);
    }

    public void ShowWindow(int levelNumber)
    {
        base.Show();
        _nextButton.interactable = true;
        ShowText(levelNumber);
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
