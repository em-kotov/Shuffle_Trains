using UnityEngine;

public class BumpHandler : MonoBehaviour
{
    [SerializeField] private Car _car;

    private void OnEnable()
    {
        _car.Bumped += OnBumped;
    }

    private void OnDisable()
    {
        _car.Bumped -= OnBumped;
    }

    private void OnBumped()
    {
        Debug.Log("Bumped");
    }
}
