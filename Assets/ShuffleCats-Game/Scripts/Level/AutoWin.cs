using System;
using UnityEngine;

public class AutoWin : MonoBehaviour
{
    [SerializeField] private WinScreen _winScreen;

    private int _totalCarsCount;
    private int _currentCarsCount;

    public event Action WinLevel;

    public void Initialize(int totalCarsCount)
    {
        _totalCarsCount = totalCarsCount;
        _currentCarsCount = 0;
        _winScreen.Hide();
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
        WinLevel?.Invoke();
        _winScreen.Show();
    }
}
