using UnityEngine;

public class WinScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup _winScreen;

    public void Show()
    {
        _winScreen.alpha = 1;
    }

    public void Hide()
    {
        _winScreen.alpha = 0;
    }
}
