using TMPro;
using UnityEngine;

public class WalletUI : UIPanel
{
    [SerializeField] private TextMeshProUGUI _currentMoneyText;
    [SerializeField] private TextMeshProUGUI _levelBalanceText;

    public void ShowAmount(int current)
    {
        _currentMoneyText.text = $"{current}";
    }

    public void ShowLevelBalance(int balance)
    {
        _levelBalanceText.text = $"+{balance}";
    }
}
