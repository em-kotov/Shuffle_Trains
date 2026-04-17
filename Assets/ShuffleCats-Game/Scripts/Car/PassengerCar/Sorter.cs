using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class Sorter : MonoBehaviour
{
    private int _totalPassengersCount;
    private Transform _passengersAssignParent;
    private List<Passenger> _passengers;
    private List<Transform> _holdPoints;
    private int _passengersToSort;

    public void Initialize(int totalCount, List<Passenger> passengers, List<Transform> holdPoints)
    {
        _totalPassengersCount = totalCount;
        _passengers = passengers;
        _holdPoints = holdPoints;

        AssignPassengers(_passengers, _holdPoints);
        _passengersToSort = _passengers.Count;
    }

    public void DropOff(Station station)
    {
        List<Transform> seats = station.GetFreeSeats();
        int seatsCount = seats.Count;

        if (_passengersToSort == 0 || seatsCount == 0)
        {
            return;
        }

        bool isStationColored = true;

        if (station._catColor == CatColor.Uncolored)
        {
            isStationColored = false;
        }

        List<Passenger> dropPassengers;

        if (isStationColored)
        {
            dropPassengers = GetPassengersByColor(station._catColor, seatsCount);
        }
        else
        {
            dropPassengers = GetPassengersOfFirstColor(seatsCount);
        }

        // deassign passengers from cat
        RemovePassengers(dropPassengers);
        AssignPassengers(dropPassengers, seats);
    }

    private List<Passenger> GetPassengersOfFirstColor(int count)
    {
        return GetPassengersByColor(GetFirstColorOfPassengers(), count);
    }

    private List<Passenger> GetPassengersByColor(CatColor catColor, int count)
    {
        List<Passenger> dropPassengers = new();

        List<Passenger> temporary = _passengers.FindAll(
            passenger => passenger.CatColor == catColor).ToList();

        if (temporary.Count <= count)
            return temporary;

        for (int i = 0; i < count; i++)
        {
            dropPassengers.Add(temporary[i]);
        }

        return dropPassengers;
    }

    private CatColor GetFirstColorOfPassengers()
    {
        int firstIndex = 0;
        return _passengers[firstIndex].CatColor;
    }

    private void AssignPassengers(List<Passenger> passengers, List<Transform> holdPoints)
    {
        for (int i = 0; i < passengers.Count; i++)
        {
            passengers[i].transform.SetParent(holdPoints[i]);
            passengers[i].transform.localPosition = Vector3.zero;
            passengers[i].transform.localRotation = Quaternion.Euler(Vector3.zero);
        }
    }

    private void RemovePassengers(List<Passenger> passengersToRemove)
    {
        for (int i = 0; i < passengersToRemove.Count; i++)
        {
            _passengers.Remove(passengersToRemove[i]);
        }
    }

    // private void OnDrawGizmos()
    // {
    //     Gizmos.color = Color.magenta;

    //     if (_holdPoints.Count != 0)
    //     {
    //         for (int i = 0; i < _holdPoints.Count; i++)
    //         {
    //             Gizmos.DrawWireSphere(_holdPoints[i].position, 0.15f);
    //         }
    //     }
    // }
}
