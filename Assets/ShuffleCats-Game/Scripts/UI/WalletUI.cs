using TMPro;
using UnityEngine;

public class WalletUI : UIPanel
{
    [SerializeField] private TextMeshProUGUI _currentMoneyText;

    public void ShowAmount(int current)
    {
        _currentMoneyText.text = $"{current}";
    }
}
