using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Splines;

public class PassengerCar : MonoBehaviour
{
    private List<(Station station, bool isVisited)> _stationsProgress;
    private List<Station> _stations;

    private bool _isReset = true;

    public event Action StopFound;
    public event Action PassengerReleased;

    [ContextMenu("Stop At the Station")]
    public void StopAtStation(Station foundStation)
    {
        Debug.Log("Passenger Car stop found invoke");
        //StopFound?.Invoke();
        OnStationFound(foundStation);
    }

    [ContextMenu("Continue")]
    public void Continue()
    {
        PassengerReleased?.Invoke();
    }

    public void Initialize()
    {
        _stationsProgress = new();
        _stations = new();
    }

    private void OnStationFound(Station foundStation)
    {
        Debug.Log("Passenger Car on station found");

        if (_stations.Contains(foundStation) == false)
        {
            Debug.Log("Passenger Car adding new station to a progress");
            _stations.Add(foundStation);
            _stationsProgress.Add((foundStation, false));
        }

        Debug.Log("Passenger Car checking found station");
        int foundStationIndex = _stationsProgress.FindIndex(progress => progress.station == foundStation);

        if (_stationsProgress[foundStationIndex].isVisited)
        {
            Debug.Log("Passenger Car checking found station already visited");
            return;
        }

        Debug.Log("Passenger Car found station still not visited, invoking event to stop");
        StopFound?.Invoke();
        _stationsProgress[foundStationIndex] = (_stationsProgress[foundStationIndex].station, true);
    }

    public void OnResetFound()
    {
        Debug.Log("Passenger Car on Reset found");

        for (int i = 0; i < _stationsProgress.Count; i++)
        {
            _stationsProgress[i] = (_stationsProgress[i].station, false);
        }
    }

    private void ChooseExitSegment(SplineContainer splineContainer)
    {
        // S// var spline = splineContainer.Spline;
        //var segment = spline.
    }
}
