using UnityEngine;

public class AutoWin : MonoBehaviour
{
    [SerializeField] private WinUI _winUI;

    private int _totalCarsCount;
    private int _currentCarsCount;
    private int _levelNumber;
    private int _startCarsCount = 0;

    //public event Action WinLevel;

    public void Initialize(int totalCarsCount, int levelNumber)
    {
        _totalCarsCount = totalCarsCount;
        _levelNumber = levelNumber;
        _currentCarsCount = _startCarsCount;

        _winUI.HideWindow();
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
        //WinLevel?.Invoke();
        _winUI.ShowWindow(_levelNumber);
    }
}
