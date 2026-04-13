using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;

public class PassengerCar : MonoBehaviour
{
    private List<(Station station, bool isVisited)> _stationsProgress;
    private List<Station> _stations;
    private List<int> _passengersToSort;

    private bool _isReset = true;
    private bool _isNeedToSort = true;

    public bool IsStopping { get; private set; }
    public bool IsFinished { get; private set; }
    public bool IsSorting { get; private set; }

    public void Initialize()
    {
        _stationsProgress = new();
        _stations = new();

        _passengersToSort = new(1);
        IsFinished = false;
    }

    public void TryStopAtStation(Station foundStation)
    {
        IsStopping = false;
        Debug.Log("Passenger Car try stop at station");
        TryAddStationToProgress(foundStation);

        if (IsVisited(foundStation, out int stationIndex))
        {
            Debug.Log("Passenger Car checking found station already visited");
            return;
        }

        IsStopping = true;
        Debug.Log("Passenger Car found station still not visited, is stopping");
        MarkVisited(stationIndex);
        IsSorting = true;
        StartCoroutine(ImitateSorting());
    }

    public void TryResetStationProgress()
    {
        Debug.Log("Passenger Car try reset progress");

        if (_isNeedToSort)
        {
            if (_isReset == false)
            {
                ResetProgress();
                _isReset = true;
            }
        }
    }

    public void TryFinishSorting()
    {
        Debug.Log("Passenger Car try finish sorting");

        if (_isNeedToSort == false)
        {
            IsFinished = true;
        }
    }

    private void TryAddStationToProgress(Station station)
    {
        Debug.Log("Passenger Car try add station to progress");

        if (_stations.Contains(station) == false)
        {
            Debug.Log("Passenger Car adding new station success");
            _stations.Add(station);
            _stationsProgress.Add((station, false));
        }
    }

    private bool IsVisited(Station station, out int stationIndex)
    {
        Debug.Log("Passenger Car checking status of station");
        stationIndex = _stationsProgress.FindIndex(progress => progress.station == station);

        return _stationsProgress[stationIndex].isVisited;
    }

    private void MarkVisited(int stationIndex)
    {
        _stationsProgress[stationIndex] = (_stationsProgress[stationIndex].station, true);
    }

    private void ResetProgress()
    {
        Debug.Log("Passenger Car reseting progress");

        for (int i = 0; i < _stationsProgress.Count; i++)
        {
            _stationsProgress[i] = (_stationsProgress[i].station, false);
        }
    }

    private IEnumerator ImitateSorting()
    {
        Debug.Log("Passenger Car is sorting");
        float time = 0.8f;
        yield return new WaitForSeconds(time);
        //sorting logic here

        _isNeedToSort = false; // for now only 1 station visit

        TryFinishSorting();
        IsSorting = false;
        Debug.Log("Passenger Car is sorted");
    }
}
