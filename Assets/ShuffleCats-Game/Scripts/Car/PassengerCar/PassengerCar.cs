using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassengerCar : MonoBehaviour
{
    private bool _isReset = true;
    private bool _isNeedToSort;
    private StationOperator _stationOperator;
    private Sorter _sorter;

    public bool IsFinished { get; private set; }
    public bool IsSorting { get; private set; }

    public void Initialize(StationOperator stationOperator,
            Sorter sorter, List<Passenger> passengers, List<Transform> holdPoints)
    {
        _stationOperator = stationOperator;
        _sorter = sorter;

        _stationOperator.Initialize();

        _sorter.Initialize(2, passengers, holdPoints);

        _isNeedToSort = IsNeedToSort();
        IsFinished = false;
    }

    public bool IsNeedToSort(Station foundStation)
    {
        bool isNeed = false;

        _stationOperator.TryAddStationToProgress(foundStation);

        if (_stationOperator.IsVisited(foundStation))
        {
            return isNeed;
        }

        isNeed = true;
        return isNeed;
    }

    public void Sort(Station station)
    {
        _stationOperator.MarkVisited(station);
        IsSorting = true;
        StartCoroutine(ImitateSorting(station));
    }

    public void TryResetStationProgress()
    {
        if (_isNeedToSort)
        {
            if (_isReset == false)
            {
                _stationOperator.ResetProgress();
                _isReset = true;
            }
        }
    }

    private void TryFinishSorting()
    {
        if (_isNeedToSort == false)
        {
            IsFinished = true;
        }
    }

    private IEnumerator ImitateSorting(Station station)
    {
        Debug.Log("Passenger Car is sorting");

        List<Transform> seats = station.GetFreeSeats();
        List<Passenger> dropPassengers = _sorter.GetDropOffPassengers(station.CatColor, seats.Count);

        float timeForPassenger = 0.25f;
        WaitForSeconds wait = new WaitForSeconds(timeForPassenger);

        for (int i = 0; i < dropPassengers.Count; i++)
        {
            _sorter.DropPassenger(dropPassengers[i], seats[i]);
            yield return wait;
        }

        station.UpdatePassengers(dropPassengers);

        // here should determine _isNeedToSort
        _isNeedToSort = IsNeedToSort();

        TryFinishSorting();
        IsSorting = false;
        Debug.Log("Passenger Car is sorted");
    }

    private bool IsNeedToSort()
    {
        return _sorter.HasPassengersToSort();
    }
}
