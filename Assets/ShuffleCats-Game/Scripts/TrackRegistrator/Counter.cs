using UnityEngine;

public class Counter : MonoBehaviour
{
    [SerializeField] private CounterUI _counterUI;

    private int _startCount = 0;

    public int CurrentCount {get;private set;}
    public int MaxCount {get;private set;}

    public void Initialize(int maxCount)
    {
        MaxCount = maxCount;
        CurrentCount = _startCount;
    }

    public void Add()
    {
        CurrentCount++;
    }
    
    public void Remove()
    {
        CurrentCount--;
    }

    public void ShowCurrent()
    {
        _counterUI.ShowCount(CurrentCount, MaxCount);
    }
}
