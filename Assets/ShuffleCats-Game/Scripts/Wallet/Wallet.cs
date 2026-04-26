using UnityEngine;

public class Wallet : MonoBehaviour
{
    private WalletUI _walletUI;
    private int _levelCarBonus;
    private int _levelWinBonus;
    private int _currentMoney;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _currentMoney = PlayerPrefs.GetInt("PlayerMoney", 0);
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
            SaveMoney();
    }

    private void OnDisable()
    {
        SaveMoney();
    }

    public void Initialize(int levelWinBonus, int levelCarBonus, WalletUI walletUI)
    {
        _levelCarBonus = levelCarBonus;
        _levelWinBonus = levelWinBonus;
        _walletUI = walletUI;

        ShowCurrent();
    }

    public void ReceiveCarBonus()
    {
        AddMoney(_levelCarBonus);
    }

    public void ReceiveWinBonus()
    {
        AddMoney(_levelWinBonus);
    }

    private void AddMoney(int amount)
    {
        _currentMoney += amount;
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        _walletUI.ShowAmount(_currentMoney);
        Debug.Log("Wallet - current: " + _currentMoney);
    }

    private void SaveMoney()
    {
        PlayerPrefs.GetInt("PlayerMoney", _currentMoney);
        PlayerPrefs.Save();
    }
}
