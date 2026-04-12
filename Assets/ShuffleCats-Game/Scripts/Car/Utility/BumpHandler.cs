using UnityEngine;

public class BumpHandler : MonoBehaviour
{
    private ParkingCar _parkingCar;

    public void Initialize(ParkingCar car)
    {
        // _car = car;
    }

    private void OnEnable()
    {
        // _car.Bumped += OnBumped;
    }

    private void OnDisable()
    {
        // _car.Bumped -= OnBumped;
    }

    private void OnBumped()
    {
        Debug.Log("Bumped");
    }
}
