using TMPro;
using UnityEngine;

public class WalletUI : UIPanel
{
    public override bool BlocksGameplay => false;
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
