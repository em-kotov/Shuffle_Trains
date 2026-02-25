using UnityEngine;

public class BumpHandler : MonoBehaviour
{
    private Car _car;

    public void Initialize(Car car)
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
