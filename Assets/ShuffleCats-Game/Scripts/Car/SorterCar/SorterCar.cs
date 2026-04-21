using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SorterCar : MonoBehaviour
{
    private bool _isReset = true;
    private bool _isNeedToSort;
    private StationOperator _stationOperator;
    private PassengerTracker _passengerTracker;
    private int _totalCount;
    private CatColor _catColor;
    private SorterRegistrator _sorterRegistrator;

    public bool IsFinished { get; private set; }
    public bool IsSorting { get; private set; }

    public void Initialize(StationOperator stationOperator, PassengerTracker passengerTracker,
                        List<Passenger> passengers, List<Transform> holdPoints,
                        CatColor catColor, int stationCount, SorterRegistrator sorterRegistrator)
    {
        _stationOperator = stationOperator;
        _passengerTracker = passengerTracker;
        _catColor = catColor;
        _sorterRegistrator = sorterRegistrator;

        _sorterRegistrator.Register(this);
        _stationOperator.Initialize(stationCount);
        _totalCount = holdPoints.Count;
        _passengerTracker.Initialize(_totalCount, passengers, holdPoints);

        for (int i = 0; i < holdPoints.Count; i++)
        {
            AssignPassenger(passengers[i], holdPoints[i]);
        }

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
        if (station.TrySetCurrentCar(this))
        {
            _stationOperator.MarkVisited(station);
            IsSorting = true;
            StartCoroutine(ImitateSorting(station));
        }
    }

    public void TryResetStationProgress() //station operator method
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

    public bool HaveSeats(out int count, out CatColor catColor)
    {
        catColor = _catColor;
        return _passengerTracker.HaveFreeSeats(out count);
    }

    private void TryFinishSorting()
    {
        if (_isNeedToSort == false)
        {
            IsFinished = true;
            _sorterRegistrator.Unregister(this);
        }
    }

    private IEnumerator ImitateSorting(Station station)
    {
        List<Transform> seats = station.GetFreeSeats();
        List<Passenger> dropPassengers = _passengerTracker.GetDropOffPassengers(station.CatColor, seats.Count);

        float timeForPassenger = 0.25f;
        WaitForSeconds wait = new WaitForSeconds(timeForPassenger);

        //Debug.Log($"PassengerCar - drop passengers: {dropPassengers.Count}, station seats: {seats.Count}, car color: {_catColor}");

        for (int i = 0; i < dropPassengers.Count; i++)
        {
            _passengerTracker.RemovePassenger(dropPassengers[i]);
            AssignPassenger(dropPassengers[i], seats[i]);
            yield return wait;
        }

        station.UpdatePassengers(dropPassengers);

        List<Transform> pickupSeats = _passengerTracker.GetFreeSeats();
        List<Passenger> pickPassengers = station.GetPickupPassengers(_catColor, pickupSeats.Count);

        //Debug.Log($"PassengerCar - pickup passengers: {pickPassengers.Count}, sorter seats: {pickupSeats.Count}, car color: {_catColor}");

        for (int i = 0; i < pickPassengers.Count; i++)
        {
            station.RemovePassenger(pickPassengers[i]);
            _passengerTracker.AddPassenger(pickPassengers[i], pickupSeats[i]);
            AssignPassenger(pickPassengers[i], pickupSeats[i]);
            yield return wait;
        }

        station.ClearCurrentCar();

        _isNeedToSort = IsNeedToSort(); //from this and below are for reset and exit

        if (_stationOperator.IsNeedToReset())
        {
            _isReset = false;
        }

        TryFinishSorting();
        IsSorting = false;
    }

    private bool IsNeedToSort()
    {
        return _passengerTracker.HasPassengersToSort();
    }

    private void AssignPassenger(Passenger passenger, Transform parent)
    {
        passenger.transform.SetParent(parent);
        passenger.transform.SetLocalPositionAndRotation(Vector3.zero,
                                            Quaternion.Euler(Vector3.zero));
    }
}
