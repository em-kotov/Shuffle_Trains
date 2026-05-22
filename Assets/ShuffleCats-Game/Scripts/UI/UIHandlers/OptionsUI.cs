using UnityEngine;
using UnityEngine.UI;

public class OptionsUI : UIPanel
{
    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Toggle _musicToggle;
    [SerializeField] private Toggle _sfxToggle;

    private void OnEnable()
    {
        _optionsButton.onClick.AddListener(Open);
        _closeButton.onClick.AddListener(Close);
        _restartButton.onClick.AddListener(Restarter.ReloadScene);

        if (_musicToggle != null)
        {
            _musicToggle.onValueChanged.AddListener(OnMusicChanged);
        }
        if (_sfxToggle != null)
        {
            _sfxToggle.onValueChanged.AddListener(OnSfxChanged);
        }

        SyncToggles();
        Close();
    }

    private void OnDisable()
    {
        _optionsButton.onClick.RemoveListener(Open);
        _closeButton.onClick.RemoveListener(Close);
        _restartButton.onClick.RemoveListener(Restarter.ReloadScene);

        if (_musicToggle != null)
        {
            _musicToggle.onValueChanged.RemoveListener(OnMusicChanged);
        }
        if (_sfxToggle != null)
        {
            _sfxToggle.onValueChanged.RemoveListener(OnSfxChanged);
        }
    }

    private void Open()
    {
        _optionsButton.interactable = false;
        SyncToggles();
        Show();
    }

    private void Close()
    {
        Hide();
        _optionsButton.interactable = true;
    }

    private void SyncToggles()
    {
        AudioManager audio = AudioManager.Instance;
        
        if (audio == null)
        {
            return;
        }

        if (_musicToggle != null)
        {
            _musicToggle.SetIsOnWithoutNotify(audio.MusicOn);
        }
        if (_sfxToggle != null)
        {
            _sfxToggle.SetIsOnWithoutNotify(audio.SfxOn);
        }
    }

    private void OnMusicChanged(bool value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.MusicOn = value;
        }
    }

    private void OnSfxChanged(bool value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SfxOn = value;
        }
    }
}
