using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Splines;

public class LevelManager : MonoBehaviour
{
    [Header("Parking")]
    [SerializeField] private int _parkingGridWidth = 6;
    [SerializeField] private int _parkingGridHeight = 8;
    [SerializeField] private float _cellSize = 1f;

    [Header("Track")]
    [SerializeField] private float _trackSpeed = 16f;
    [SerializeField] private float _slideDuration = 0.3f;
    [SerializeField] private float _searchMin = 0f;
    [SerializeField] private float _searchMax = 1f;

    [Header("References")]
    [SerializeField] private ParkingRegistrator _parkingRegistrator;
    [SerializeField] private GridCalculator _gridCalculator;
    [SerializeField] private SplineContainer _origSpline;
    [SerializeField] private PhysicsRaycaster _raycaster;
    [SerializeField] private List<Subscriber> _carSubscribers;

    private void Awake()
    {
        _parkingRegistrator.Initialize(_parkingGridWidth, _parkingGridHeight,
                                        _cellSize, _gridCalculator);

        foreach (Subscriber subscriber in _carSubscribers)
        {
            subscriber.Initialize(_parkingRegistrator, _origSpline, _raycaster, _trackSpeed,
                                _slideDuration, _searchMin, _searchMax);
        }
    }
}
