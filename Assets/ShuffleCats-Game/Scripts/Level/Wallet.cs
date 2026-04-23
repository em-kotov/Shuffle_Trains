using TMPro;
using UnityEngine;

public class Wallet : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _currentText;

    private int _currentCount;
    private int _carBonus;

    public void Initialize()
    {
        _currentCount = 0;
        UpdateCurrent(0);
        _carBonus = 4;
    }

    public void ReceiveCarBonus()
    {
        UpdateCurrent(_carBonus);
    }

    private void UpdateCurrent(int additional)
    {
        _currentCount += additional;
        ShowText();
    }

    private void ShowText()
    {
        _currentText.text = $"{_currentCount}";
    }
}
