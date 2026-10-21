using System;
using UnityEngine;

public class AutoWin : MonoBehaviour
{
    [SerializeField] private WinUI _winUI;
    [SerializeField] private LastLevelUI _lastLevelUI;

    private int _totalCarsCount;
    private int _currentCarsCount;
    private int _levelNumber;
    private bool _isLastLevel;
    private int _startCarsCount = 0;

    public event Action WinLevel;

    public void Initialize(int totalCarsCount, int levelNumber, bool isLastLevel)
    {
        _totalCarsCount = totalCarsCount;
        _levelNumber = levelNumber;
        _isLastLevel = isLastLevel;
        _currentCarsCount = _startCarsCount;

        _winUI.HideWindow();

        if (_lastLevelUI != null)
            _lastLevelUI.HideWindow();
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
        YandexSdk.GameplayStop();

        if (_isLastLevel && _lastLevelUI != null)
        {
            _lastLevelUI.ShowWindow();
        }
        else
        {
            _winUI.ShowWindow(_levelNumber);
        }
    }
}
