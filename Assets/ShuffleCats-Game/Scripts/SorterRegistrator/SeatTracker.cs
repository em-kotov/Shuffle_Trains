using System.Collections.Generic;
using UnityEngine;

public class SeatTracker : MonoBehaviour
{
    private List<(Transform holdPoint, Passenger passenger)> _seats;

    virtual protected void Initialize(List<Passenger> passengers, List<Transform> holdPoints)
    {
        _seats = new();

        Passenger passenger = null;
        bool havePassengers = true;

        if (passengers == null)
            havePassengers = false;

        for (int i = 0; i < holdPoints.Count; i++)
        {
            if (havePassengers)
                passenger = passengers[i];

            _seats.Add((holdPoints[i], passenger));
        }
    }

    virtual public void AddPassenger(Passenger passenger, Transform holdPoint)
    {
        int seatIndex = _seats.FindIndex(seat => seat.holdPoint == holdPoint);
        _seats[seatIndex] = new(_seats[seatIndex].holdPoint, passenger);
    }

    virtual public void RemovePassenger(Passenger passenger)
    {
        int seatIndex = _seats.FindIndex(seat => seat.passenger == passenger);
        _seats[seatIndex] = new(_seats[seatIndex].holdPoint, null);
    }

    virtual public List<Transform> GetFreeSeats()
    {
        List<Transform> seats = new();

        for (int i = 0; i < _seats.Count; i++)
        {
            if (_seats[i].passenger == null)
            {
                seats.Add(_seats[i].holdPoint);
            }
        }

        return seats;
    }
}
