using System;

public interface IWalletSpender
{
    int Balance { get; }
    bool TrySpend(int amount);
    event Action BalanceChanged;
}
