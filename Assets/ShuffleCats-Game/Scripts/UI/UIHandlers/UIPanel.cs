using UnityEngine;

public abstract class UIPanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup _panel;

    public bool IsVisible => _panel != null && _panel.gameObject.activeInHierarchy && _panel.alpha > 0f;
    public virtual bool BlocksGameplay => IsVisible;

    protected void BindPanel(CanvasGroup panel) => _panel = panel;

    protected virtual void Show()
    {
        // The panel image sits behind its controls and follows its visibility.
        // Configure it here because some scenes override or disable the prefab image.
        if (_panel.TryGetComponent<UnityEngine.UI.Image>(out var backdrop))
        {
            backdrop.enabled = true;
            backdrop.color = new Color(0f, 0f, 0f, 0.6f);
            backdrop.sprite = null;
            backdrop.type = UnityEngine.UI.Image.Type.Simple;
            backdrop.raycastTarget = true;
        }

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
