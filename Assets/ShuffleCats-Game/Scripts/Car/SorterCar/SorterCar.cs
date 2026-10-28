using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SorterCar : MonoBehaviour
{
    private bool _isNeedToSort;
    private StationOperator _stationOperator;
    private PassengerTracker _passengerTracker;
    private int _totalCount;
    private CatColor _catColor;
    private SorterRegistrator _sorterRegistrator;

    public bool IsFinished { get; private set; }

    public bool IsSorting { get; private set; }

    public void Initialize(
        StationOperator stationOperator,
        PassengerTracker passengerTracker,
        List<Passenger> passengers,
        List<Transform> holdPoints,
        CatColor catColor,
        int stationCount,
        SorterRegistrator sorterRegistrator)
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
        if (station.TrySetCurrentCar(this))
        {
            _stationOperator.MarkVisited(station);
            IsSorting = true;
            StartCoroutine(ImitateSorting(station));
        }
    }

    public void TryResetStationProgress()
    {
        if (_stationOperator.HasAnyVisited())
        {
            _stationOperator.ResetProgress();
        }
    }

    public bool CanExchangeWith(Station station)
    {
        if (IsFinished)
            return false;

        DetermineColorToDrop(station, out bool canDrop);

        if (canDrop && _passengerTracker.GetDropOffPassengers(
                station.CatColor, station.GetFreeSeats().Count).Count > 0)
            return true;

        return _passengerTracker.HaveFreeSeats(out _) &&
               station.HavePassengers(out _, out CatColor color) && color == _catColor;
    }

    private void TryFinishSorting()
    {
        if (_isNeedToSort == false)
        {
            IsFinished = true;
            _sorterRegistrator.Unregister(this);
        }
    }

    private CatColor DetermineColorToDrop(Station station, out bool haveColorsForDrop)
    {
        haveColorsForDrop = true;

        if (station.CatColor != CatColor.Uncolored)
        {
            return station.CatColor;
        }

        CatColor colorToDrop = CatColor.Uncolored;
        List<CatColor> canDropColors = new List<CatColor>();

        List<CatColor> wrongColors = new List<CatColor>();
        wrongColors = _passengerTracker.GetWrongPassengersColors();

        for (int i = 0; i < wrongColors.Count; i++)
        {
            if (_sorterRegistrator.HaveStationInColor(wrongColors[i]))
            {
                continue;
            }

            colorToDrop = wrongColors[i];
            canDropColors.Add(colorToDrop);
            break;
        }

        if (canDropColors.Count == 0)
        {
            haveColorsForDrop = false;
        }

        return colorToDrop;
    }

    private IEnumerator ImitateSorting(Station station)
    {
        float timeForPassenger = 0.25f;
        WaitForSeconds wait = new WaitForSeconds(timeForPassenger);

        bool haveColorsForDrop;
        CatColor colorToDrop = DetermineColorToDrop(station, out haveColorsForDrop);

        if (haveColorsForDrop)
        {
            List<Transform> seats = station.GetFreeSeats();
            List<Passenger> dropPassengers = _passengerTracker.GetDropOffPassengers(station.CatColor, seats.Count);

            for (int i = 0; i < dropPassengers.Count; i++)
            {
                _passengerTracker.RemovePassenger(dropPassengers[i]);
                AssignPassenger(dropPassengers[i], seats[i]);
                yield return wait;
            }

            station.UpdatePassengers(dropPassengers);
        }

        List<Transform> pickupSeats = _passengerTracker.GetFreeSeats();
        List<Passenger> pickPassengers = station.GetPickupPassengers(_catColor, pickupSeats.Count);

        for (int i = 0; i < pickPassengers.Count; i++)
        {
            station.RemovePassenger(pickPassengers[i]);
            _passengerTracker.AddPassenger(pickPassengers[i], pickupSeats[i]);
            AssignPassenger(pickPassengers[i], pickupSeats[i]);
            yield return wait;
        }

        station.ClearCurrentCar();

        _isNeedToSort = IsNeedToSort();

        TryFinishSorting();
        IsSorting = false;
    }

    private bool IsNeedToSort()
    {
        return _passengerTracker.HasPassengersToSort();
    }

    private void AssignPassenger(Passenger passenger, Transform parent)
    {
        passenger.transform.SetParent(parent, false);
        passenger.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        passenger.transform.localScale = Vector3.one;
    }
}
