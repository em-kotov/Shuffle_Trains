using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Sorter : MonoBehaviour
{
    private List<Passenger> _wrongPassengers;
    private List<Passenger> _rightPassengers;
    private List<Transform> _holdPoints;
    private List<(Transform holdPoint, Passenger passenger)> _seats;
    //private int _passengersToSort;

    public void Initialize(int totalCount, List<Passenger> passengers, List<Transform> holdPoints)
    {
        _wrongPassengers = passengers;
        _holdPoints = holdPoints;
        _rightPassengers = new();

        AssignPassengers(_wrongPassengers, _holdPoints);
        //_passengersToSort = _wrongPassengers.Count;

        _seats = new();

        for (int i = 0; i < _holdPoints.Count; i++)
        {
            _seats.Add((_holdPoints[i], _wrongPassengers[i]));
        }
    }

    public bool HasPassengersToSort()
    {
        return _wrongPassengers.Count > 0 || _rightPassengers.Count != _holdPoints.Count;
    }

    public void DropPassenger(Passenger passenger, Transform seat)
    {
        int seatIndex = _seats.FindIndex(seat => seat.passenger == passenger);
        _seats[seatIndex] = new(_seats[seatIndex].holdPoint, null);

        _wrongPassengers.Remove(passenger);
        //_passengersToSort--;
        passenger.transform.SetParent(seat);
        passenger.transform.SetLocalPositionAndRotation(Vector3.zero,
                                            Quaternion.Euler(Vector3.zero));
    }

    public void AcceptPassenger(Passenger passenger, Transform holdPoint)
    {
        if (passenger == null)
            return;

        int seatIndex = _seats.FindIndex(seat => seat.holdPoint == holdPoint);
        _seats[seatIndex] = new(_seats[seatIndex].holdPoint, passenger);
        _rightPassengers.Add(passenger);

        passenger.transform.SetParent(holdPoint);
        passenger.transform.SetLocalPositionAndRotation(Vector3.zero,
                                            Quaternion.Euler(Vector3.zero));
    }

    public List<Transform> GetFreeSeats()
    {
        List<Transform> seats = new();

        for (int i = 0; i < _seats.Count; i++)
        {
            if (_seats[i].passenger == null)
            {
                seats.Add(_seats[i].holdPoint);
                Debug.Log("Sorter - get free seats added a seat");
            }
        }

        return seats;
    }

    public List<Passenger> GetDropOffPassengers(CatColor catColor, int seatsCount)
    {
        List<Passenger> dropPassengers = new();

        if (_wrongPassengers.Count == 0 || seatsCount == 0)
        {
            return dropPassengers;
        }

        bool isStationColored = true;

        if (catColor == CatColor.Uncolored)
        {
            isStationColored = false;
        }

        if (isStationColored)
        {
            dropPassengers = GetPassengersByColor(catColor, seatsCount);
        }
        else
        {
            dropPassengers = GetPassengersOfFirstColor(seatsCount);
        }

        return dropPassengers;
    }

    private void AssignPassengers(List<Passenger> passengers, List<Transform> holdPoints)
    {
        for (int i = 0; i < passengers.Count; i++)
        {
            passengers[i].transform.SetParent(holdPoints[i]);
            passengers[i].transform.SetLocalPositionAndRotation(Vector3.zero,
                                            Quaternion.Euler(Vector3.zero));
        }
    }

    private List<Passenger> GetPassengersOfFirstColor(int count)
    {
        return GetPassengersByColor(GetFirstColorOfPassengers(), count);
    }

    private List<Passenger> GetPassengersByColor(CatColor catColor, int count)
    {
        List<Passenger> dropPassengers = new();

        List<Passenger> temporary = _wrongPassengers.FindAll(
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
        return _wrongPassengers[firstIndex].CatColor;
    }
}
