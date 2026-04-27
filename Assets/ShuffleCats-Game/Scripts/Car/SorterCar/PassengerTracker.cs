using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PassengerTracker : SeatTracker
{
    private List<Passenger> _wrongPassengers;
    private List<Passenger> _rightPassengers;
    private List<Transform> _holdPoints;
    private List<CatColor> _wrongPassengersColors;

    public void Initialize(int totalCount, List<Passenger> passengers, List<Transform> holdPoints)
    {
        //_wrongPassengers = passengers;
        _wrongPassengers = new();
        _wrongPassengersColors = new();
        InitializeWrongPassengers(passengers);

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
        TryRemoveColor(passenger.CatColor);
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

    public bool HavePassengerInColor(CatColor catColor)
    {
        return _wrongPassengers.Any(passenger => passenger.CatColor == catColor);
    }

    public List<CatColor> GetWrongPassengersColors()
    {
        List<CatColor> colors = new List<CatColor>();

        for (int i = 0; i < _wrongPassengersColors.Count; i++)
        {
            CatColor color = _wrongPassengersColors[i];
            colors.Add(color);
        }

        return colors;
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

    private void InitializeWrongPassengers(List<Passenger> passengers)
    {
        for (int i = 0; i < passengers.Count; i++)
        {
            _wrongPassengers.Add(passengers[i]);
            TryAddColor(passengers[i].CatColor);
        }
    }

    private void TryAddColor(CatColor passengerColor)
    {
        if (_wrongPassengersColors.Contains(passengerColor))
            return;

        _wrongPassengersColors.Add(passengerColor);
    }

    private void TryRemoveColor(CatColor passengerColor)
    {
        if (HavePassengerInColor(passengerColor))
        {
            return;
        }

        _wrongPassengersColors.Remove(passengerColor);
    }
}
