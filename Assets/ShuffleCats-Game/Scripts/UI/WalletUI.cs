using TMPro;
using UnityEngine;

public class WalletUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _currentText;

    private void OnEnable()
    {
        // Wallet.Instance.Updated += UpdateText;
    }

    private void OnDisable()
    {
        // Wallet.Instance.Updated -= UpdateText;
    }

    private void UpdateText(int count)
    {
        _currentText.text = $"{count}";
    }
}
