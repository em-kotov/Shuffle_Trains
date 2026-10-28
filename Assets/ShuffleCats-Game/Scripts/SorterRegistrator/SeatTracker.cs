using System.Collections.Generic;
using UnityEngine;

public class SeatTracker : MonoBehaviour
{
    private List<(Transform HoldPoint, Passenger Passenger)> _seats;

    public virtual void AddPassenger(Passenger passenger, Transform holdPoint)
    {
        int seatIndex = _seats.FindIndex(seat => seat.HoldPoint == holdPoint);
        _seats[seatIndex] = new(_seats[seatIndex].HoldPoint, passenger);
    }

    public virtual void RemovePassenger(Passenger passenger)
    {
        int seatIndex = _seats.FindIndex(seat => seat.Passenger == passenger);
        _seats[seatIndex] = new(_seats[seatIndex].HoldPoint, null);
    }

    public virtual List<Transform> GetFreeSeats()
    {
        List<Transform> seats = new();

        for (int i = 0; i < _seats.Count; i++)
        {
            if (_seats[i].Passenger == null)
            {
                seats.Add(_seats[i].HoldPoint);
            }
        }

        return seats;
    }

    protected virtual void Initialize(List<Passenger> passengers, List<Transform> holdPoints)
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
}
