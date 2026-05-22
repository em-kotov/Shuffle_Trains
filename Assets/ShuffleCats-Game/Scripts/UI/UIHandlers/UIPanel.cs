using UnityEngine;

public abstract class UIPanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup _panel;

    protected virtual void Show()
    {
        _panel.alpha = 1;
        _panel.blocksRaycasts = true;
        _panel.interactable = true;
        _panel.gameObject.SetActive(true);
    }

    protected virtual void Hide()
    {
        _panel.alpha = 0;
        _panel.blocksRaycasts = false;
        _panel.interactable = false;
        _panel.gameObject.SetActive(false);
    }
}
