using UnityEngine;
using UnityEngine.UI;

public class OptionsUI : UIPanel
{
    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _restartButton;

    private void OnEnable()
    {
        _optionsButton.onClick.AddListener(OpenOptions);
        _closeButton.onClick.AddListener(CloseOptions);
        _restartButton.onClick.AddListener(Restarter.ReloadScene);
        CloseOptions();
    }

    private void OnDisable()
    {
        _optionsButton.onClick.RemoveListener(OpenOptions);
        _closeButton.onClick.RemoveListener(CloseOptions);
        _restartButton.onClick.RemoveListener(Restarter.ReloadScene);
    }

    private void OpenOptions()
    {
        _optionsButton.interactable = false;
        Show();
    }

    private void CloseOptions()
    {
        Hide();
        _optionsButton.interactable = true;
    }
}
