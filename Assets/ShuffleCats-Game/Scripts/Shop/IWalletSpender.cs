using System;

public interface IWalletSpender
{
    event Action BalanceChanged;

    int Balance { get; }

    bool TrySpend(int amount);
}
