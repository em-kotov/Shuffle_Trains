using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PassengerTracker : SeatTracker
{
    private List<Passenger> _wrongPassengers;
    private List<Passenger> _rightPassengers;
    private List<Transform> _holdPoints;

    public void Initialize(int totalCount, List<Passenger> passengers, List<Transform> holdPoints)
    {
        _wrongPassengers = passengers;
        _holdPoints = holdPoints;
        _rightPassengers = new();
        base.Initialize(_wrongPassengers, _holdPoints);
    }

    override public void AddPassenger(Passenger passenger, Transform holdPoint)
    {
        base.AddPassenger(passenger, holdPoint);
        _rightPassengers.Add(passenger);
    }

    override public void RemovePassenger(Passenger passenger)
    {
        base.RemovePassenger(passenger);
        _wrongPassengers.Remove(passenger);
    }

    public List<Passenger> GetDropOffPassengers(CatColor catColor, int count)
    {
        List<Passenger> dropPassengers = new();

        if (_wrongPassengers.Count == 0 || count == 0)
            return dropPassengers;

        if (catColor == CatColor.Uncolored)
        {
            dropPassengers = GetPassengersByColor(GetFirstColorOfPassengers(), count);
        }
        else
        {
            dropPassengers = GetPassengersByColor(catColor, count);
        }

        return dropPassengers;
    }

    public bool HasPassengersToSort()
    {
        return _wrongPassengers.Count > 0 || _rightPassengers.Count < _holdPoints.Count;
    }

    public bool HaveFreeSeats(out int seatsCount)
    {
        seatsCount = _holdPoints.Count - _rightPassengers.Count - _wrongPassengers.Count;
        return seatsCount > 0;
    }

    private CatColor GetFirstColorOfPassengers()
    {
        int firstIndex = 0;
        return _wrongPassengers[firstIndex].CatColor;
    }

    private List<Passenger> GetPassengersByColor(CatColor catColor, int count)
    {
        List<Passenger> passengers = new();

        List<Passenger> temporary = _wrongPassengers.FindAll(
            passenger => passenger.CatColor == catColor).ToList();

        if (temporary.Count <= count)
            return temporary;

        for (int i = 0; i < count; i++)
        {
            passengers.Add(temporary[i]);
        }

        return passengers;
    }
}
