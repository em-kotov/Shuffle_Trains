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
    [SerializeField] private int _segmentsCount = 10;
    [SerializeField] private int _maxCarsOnTrackCount = 6;
    [SerializeField] private float _waitTime = 0.3f;

    [Header("Cars")]
    [SerializeField] private float _trackSpeed = 16f;
    [SerializeField] private float _slideDuration = 0.3f;
    [SerializeField] private float _searchMin = 0f;
    [SerializeField] private float _searchMax = 1f;

    [Header("References")]
    [SerializeField] private ParkingRegistrator _parkingRegistrator;
    [SerializeField] private GridCalculator _gridCalculator;
    [SerializeField] private TrackRegistrator _trackRegistrator;
    [SerializeField] private CounterUI _counterUI;
    [SerializeField] private SplineContainer _trackSpline;
    [SerializeField] private PhysicsRaycaster _raycaster;
    [SerializeField] private List<Subscriber> _carSubscribers;

    private void Awake()
    {
        _parkingRegistrator.Initialize(_parkingGridWidth, _parkingGridHeight,
                                        _cellSize, _gridCalculator);

        _trackRegistrator.Initialize(_trackSpline, _counterUI, _segmentsCount, 
                                    _maxCarsOnTrackCount);

        foreach (Subscriber subscriber in _carSubscribers)
        {
            subscriber.Initialize(_parkingRegistrator, _trackSpline, _raycaster,
                                    _trackRegistrator, _trackSpeed, _slideDuration,
                                    _waitTime, _searchMin, _searchMax);
        }
    }
}
