using TMPro;
using UnityEngine;

public class CounterUI : UIPanel
{
    [SerializeField] private TextMeshProUGUI _currentCarsText;
    [SerializeField] private TextMeshProUGUI _maxCarsText;

    public void ShowCount(int current, int max)
    {
        _currentCarsText.text = $"{current} ";
        _maxCarsText.text = $" / {max}";
    }
}
