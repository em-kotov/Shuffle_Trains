using System;
using UnityEngine;

public class Wallet : MonoBehaviour, IWalletSpender
{
    private WalletUI _walletUI;
    private int _levelCarBonus;
    private int _levelWinBonus;
    private int _currentMoney;
    private int _levelBalance;
    private int _defaultBalance = 0;

    private readonly string _playerMoneyName = "PlayerMoney";

    public int Balance => _currentMoney;

    public event Action BalanceChanged;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _currentMoney = PlayerPrefs.GetInt(_playerMoneyName, _defaultBalance);
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0 || _currentMoney < amount)
        {
            return false;
        }

        _currentMoney -= amount;
        SaveMoney();
        ShowCurrent();
        BalanceChanged?.Invoke();
        return true;
    }

    public void Initialize(int levelWinBonus, int levelCarBonus, WalletUI walletUI)
    {
        _levelCarBonus = levelCarBonus;
        _levelWinBonus = levelWinBonus;
        _walletUI = walletUI;

        _currentMoney = PlayerPrefs.GetInt(_playerMoneyName, _defaultBalance);
        _levelBalance = _defaultBalance;

        ShowCurrent();
        ShowLevelBalance();
    }

    public void ReceiveCarBonus()
    {
        AddToLevelBalance(_levelCarBonus);
    }

    public void ReceiveWinBonus()
    {
        AddToLevelBalance(_levelWinBonus);
        AddToCurrent();
        ShowCurrent();
    }

    private void AddToLevelBalance(int amount)
    {
        _levelBalance += amount;
        ShowLevelBalance();
    }

    private void ShowLevelBalance()
    {
        if (_walletUI != null)
        {
            _walletUI.ShowLevelBalance(_levelBalance);
        }
    }

    private void ShowCurrent()
    {
        if (_walletUI != null)
        {
            _walletUI.ShowAmount(_currentMoney);
        }
    }

    private void AddToCurrent()
    {
        if (_levelBalance > _defaultBalance)
        {
            _currentMoney += _levelBalance;
            SaveMoney();
            _levelBalance = _defaultBalance;
            BalanceChanged?.Invoke();
        }
    }

    private void SaveMoney()
    {
        PlayerPrefs.SetInt(_playerMoneyName, _currentMoney);
        PlayerPrefs.Save();
    }
}
