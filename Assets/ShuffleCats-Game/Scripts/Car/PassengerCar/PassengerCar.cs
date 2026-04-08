using System;
using UnityEngine;

public class PassengerCar : MonoBehaviour
{
    public event Action StopFound;
    public event Action PassengerReleased;

    [ContextMenu("Stop At the Station")]
    public void StopAtStation()
    {
        StopFound?.Invoke();
    }

    [ContextMenu("Continue")]
    public void Continue()
    {
        PassengerReleased?.Invoke();
    }
}
