using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Subscriber : MonoBehaviour
{
    [SerializeField] private Car _car;
    [SerializeField] private Mover _mover;
    [SerializeField] private PhysicsRaycaster _raycaster;
    [SerializeField] private List<ClickDetector> _clickDetectors;

    private void OnEnable()
    {
        SubscribeClickDetectors();
        _mover.FinishedMoving += _car.OnFinishedMoving;
    }

    private void OnDisable()
    {
        UnsubscribeClickDetectors();
        _mover.FinishedMoving -= _car.OnFinishedMoving;
    }

    private void SubscribeClickDetectors()
    {
        foreach (ClickDetector clickDetector in _clickDetectors)
        {
            clickDetector.Initialize(_raycaster);
            clickDetector.Clicked += _car.OnClick;
        }
    }

    private void UnsubscribeClickDetectors()
    {
        foreach (ClickDetector clickDetector in _clickDetectors)
        {
            clickDetector.Clicked -= _car.OnClick;
        }
    }
}
