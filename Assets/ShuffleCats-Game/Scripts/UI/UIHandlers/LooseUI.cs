using UnityEngine;
using UnityEngine.UI;

public class LooseUI : UIPanel
{
    [SerializeField] private Button _restartButton;
    public Button RestartButton => _restartButton;

    private void OnEnable()
    {
        _restartButton.onClick.AddListener(Restarter.ReloadScene);
        _restartButton.onClick.AddListener(AudioManager.PlayUIClickStatic);
    }

    private void OnDisable()
    {
        _restartButton.onClick.RemoveListener(Restarter.ReloadScene);
        _restartButton.onClick.RemoveListener(AudioManager.PlayUIClickStatic);
    }

    public void ShowWindow()
    {
        base.Show();
        _restartButton.interactable = true;
        YandexSdk.GameplayStop();
    }

    public void HideWindow()
    {
        _restartButton.interactable = false;
        base.Hide();
    }

    // private void ShowText(string author)
    // {
    //     _currentText.text = $"Auto loose - Seems you're stuck. " +
    //                         $"Click to restart - {author}";
    // }
}
