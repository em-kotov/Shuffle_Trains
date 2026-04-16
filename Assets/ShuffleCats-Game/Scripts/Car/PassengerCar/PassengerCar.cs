using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassengerCar : MonoBehaviour
{
    private List<int> _passengersToSort;
    private bool _isReset = true;
    private bool _isNeedToSort = true;
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

        //
        _sorter.Initialize(2, passengers, holdPoints);
        //

        _passengersToSort = new(1);
        IsFinished = false;
    }

    public bool IsNeedToSort(Station foundStation)
    {
        bool isNeed = false;
        Debug.Log("Passenger Car checking station status");
        Debug.Log("Passenger Car try add station to progress");

        if (_stationOperator.TryAddStationToProgress(foundStation))
            Debug.Log("Passenger Car adding new station success");

        if (_stationOperator.IsVisited(foundStation))
        {
            Debug.Log("Passenger Car already visited this station");
            return isNeed;
        }

        Debug.Log("Passenger Car still not visited this station");
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
        Debug.Log("Passenger Car try reset progress");

        if (_isNeedToSort)
        {
            if (_isReset == false)
            {
                Debug.Log("Passenger Car reseting progress");
                _stationOperator.ResetProgress();
                _isReset = true;
            }
        }
    }

    private void TryFinishSorting()
    {
        Debug.Log("Passenger Car try finish sorting");

        if (_isNeedToSort == false)
        {
            IsFinished = true;
        }
    }

    private IEnumerator ImitateSorting(Station station)
    {
        Debug.Log("Passenger Car is sorting");
        float time = 0.8f;
        yield return new WaitForSeconds(time);

        //sorting logic here
        _sorter.DropOff(station);
        //

        _isNeedToSort = false; // for now only 1 station visit

        TryFinishSorting();
        IsSorting = false;
        Debug.Log("Passenger Car is sorted");
    }
}
