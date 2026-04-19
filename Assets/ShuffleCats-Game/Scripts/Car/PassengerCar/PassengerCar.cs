using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassengerCar : MonoBehaviour
{
    private bool _isReset = true;
    private bool _isNeedToSort;
    private StationOperator _stationOperator;
    private Sorter _sorter;
    private int _totalCount;
    private CatColor _catColor;

    public bool IsFinished { get; private set; }
    public bool IsSorting { get; private set; }

    public void Initialize(StationOperator stationOperator, Sorter sorter,
                        List<Passenger> passengers, List<Transform> holdPoints,
                        CatColor catColor, int stationCount)
    {
        _stationOperator = stationOperator;
        _sorter = sorter;
        _catColor = catColor;

        _stationOperator.Initialize(stationCount);

        _totalCount = holdPoints.Count;
        _sorter.Initialize(_totalCount, passengers, holdPoints);

        _isNeedToSort = IsNeedToSort();
        IsFinished = false;
    }

    public bool IsNeedToSort(Station foundStation) //station operator method,
                                                   // determines if station was visited this loop
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
        Debug.Log("Passenger Car - sort is in progress");
    }

    public void TryResetStationProgress() //station operator method
    {
        if (_isNeedToSort)
        {
            if (_isReset == false)
            {
                _stationOperator.ResetProgress();
                _isReset = true;
                Debug.Log("Passenger Car - station progress reseted");
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
        List<Transform> seats = station.GetFreeSeats();
        List<Passenger> dropPassengers = _sorter.GetDropOffPassengers(station.CatColor, seats.Count);

        float timeForPassenger = 0.25f;
        WaitForSeconds wait = new WaitForSeconds(timeForPassenger);

        Debug.Log($"PassengerCar - drop passengers: {dropPassengers.Count}, station seats: {seats.Count}, car color: {_catColor}");

        for (int i = 0; i < dropPassengers.Count; i++)
        {
            _sorter.DropPassenger(dropPassengers[i], seats[i]);
            yield return wait;
        }

        station.UpdatePassengers(dropPassengers);

        //pickup passengers logic here
        List<Transform> pickupSeats = _sorter.GetFreeSeats();
        List<Passenger> pickPassengers = station.GetPickupPassengers(_catColor, pickupSeats.Count);

        Debug.Log($"PassengerCar - pickup passengers: {pickPassengers.Count}, sorter seats: {pickupSeats.Count}, car color: {_catColor}");

        for (int i = 0; i < pickPassengers.Count; i++)
        {
            station.RemovePassenger(pickPassengers[i]);
            _sorter.AcceptPassenger(pickPassengers[i], pickupSeats[i]);
            yield return wait;
        }
        //

        _isNeedToSort = IsNeedToSort();
        Debug.Log($"Passenger Car - is need to sort: {_isNeedToSort}");

        //reset check here
        if (_stationOperator.IsNeedToReset())
        {
            _isReset = false;
        }

        TryFinishSorting();
        IsSorting = false;
    }

    private bool IsNeedToSort()
    {
        return _sorter.HasPassengersToSort();
    }
}
