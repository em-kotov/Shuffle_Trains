using UnityEngine;
using UnityEngine.UI;

public class LooseUI : UIPanel
{
    [SerializeField] private Button _restartButton;

    private void OnEnable()
    {
        _restartButton.onClick.AddListener(Restarter.ReloadScene);
    }

    private void OnDisable()
    {
        _restartButton.onClick.RemoveListener(Restarter.ReloadScene);
    }

    public void ShowWindow()
    {
        base.Show();
        _restartButton.interactable = true;
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
