using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Splines;

public class GameManager : MonoBehaviour
{
    [SerializeField] private ParkingSlotsHandler _parkingHandler;
    [SerializeField] private SplineContainer _origSpline;
    [SerializeField] private PhysicsRaycaster _raycaster;
    [SerializeField] private List<Subscriber> _carSubscribers;

    private void Awake()
    {
        _parkingHandler.Initialize();

        foreach (Subscriber subscriber in _carSubscribers)
        {
            subscriber.Initialize(_parkingHandler, _origSpline, _raycaster);
        }
    }
}
