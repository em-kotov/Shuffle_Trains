using TMPro;
using UnityEngine;

public class CounterUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _currentText;
    [SerializeField] private TextMeshProUGUI _maxText;

    private int _currentCount;
    private int _maxCount;

    public void Initialize(int maxCount)
    {
        _maxCount = maxCount;
        UpdateCurrent(0);
    }

    public void UpdateCurrent(int current)
    {
        _currentCount = current;
        ShowText();
    }

    private void ShowText()
    {
        _currentText.text = $"{_currentCount}";
        _maxText.text = $" / {_maxCount}";
    }
}
