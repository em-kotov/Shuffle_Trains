using TMPro;
using UnityEngine;

public class AutoWin : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _currentText;

    private int _totalCarsCount;
    private int _currentCarsCount;

    public void Initialize(int totalCarsCount)
    {
        _totalCarsCount = totalCarsCount;
        _currentCarsCount = 0;
    }

    public void AddCar()
    {
        _currentCarsCount++;

        if (_currentCarsCount == _totalCarsCount)
        {
            ShootWin();
        }
    }

    public void ShootWin()
    {
        ShowText();
    }

    private void ShowText()
    {
        _currentText.text = $"Auto win - You win!";
    }
}
