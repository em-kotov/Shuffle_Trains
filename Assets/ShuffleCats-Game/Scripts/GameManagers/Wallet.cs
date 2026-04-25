using System;

public class Wallet : Singleton<Wallet>
{
    private int _currentCount = 0;
    private int _carBonus;

    public event Action<int> Updated;

    public void Initialize(int startBonus, int carBonus)
    {
        UpdateCurrent(startBonus);
        _carBonus = carBonus;
    }

    public void ReceiveCarBonus()
    {
        UpdateCurrent(_carBonus);
    }

    private void UpdateCurrent(int additional)
    {
        _currentCount += additional;
        Updated?.Invoke(_currentCount);
    }
}
