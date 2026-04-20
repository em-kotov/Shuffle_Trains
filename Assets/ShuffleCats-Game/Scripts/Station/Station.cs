using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Station : SeatTracker
{
    [SerializeField] private List<Transform> _holdPoints;

    private List<Passenger> _passengers;
    private SorterCar _currentSorterCar;

    public CatColor CatColor { get; private set; }

    public void Initialize()
    {
        _passengers = new();
        base.Initialize(null, _holdPoints);
        SetColor(CatColor.Uncolored);
        ClearCurrentCar();
    }

    public void UpdatePassengers(List<Passenger> passengers)
    {
        if (passengers.Count <= 0)
            return;

        SetColor(passengers[0].CatColor);

        for (int i = 0; i < passengers.Count; i++)
        {
            Transform holdPoint = passengers[i].transform.parent;
            base.AddPassenger(passengers[i], holdPoint);
            _passengers.Add(passengers[i]);
        }
    }

    override public void RemovePassenger(Passenger passenger)
    {
        base.RemovePassenger(passenger);
        _passengers.Remove(passenger);

        if (_passengers.Count == 0)
        {
            SetColor(CatColor.Uncolored);
        }
    }

    public List<Passenger> GetPickupPassengers(CatColor catColor, int seatsCount)
    {
        List<Passenger> pickPassengers = new();

        if (_passengers.Count == 0 || seatsCount == 0)
            return pickPassengers;

        pickPassengers = GetPassengersByColor(catColor, seatsCount);

        return pickPassengers;
    }

    public bool TrySetCurrentCar(SorterCar sorterCar)
    {
        if (_currentSorterCar == null)
        {
            _currentSorterCar = sorterCar;
            return true;
        }

        return false;
    }

    public void ClearCurrentCar()
    {
        _currentSorterCar = null;
    }

    private void SetColor(CatColor catColor)
    {
        CatColor = catColor;
    }

    private List<Passenger> GetPassengersByColor(CatColor catColor, int count)
    {
        List<Passenger> passengers = new();

        List<Passenger> temporary = _passengers.FindAll(
            passenger => passenger.CatColor == catColor).ToList();

        if (temporary.Count <= count)
            return temporary;

        for (int i = 0; i < count; i++)
        {
            passengers.Add(temporary[i]);
        }

        return passengers;
    }

    private void OnDrawGizmos()
    {
        List<(Transform holdPoint, Passenger passenger)> gizmoSeats = new();

        if (gizmoSeats.Count == 0)
        {
            foreach (Transform holdPoint in _holdPoints)
                gizmoSeats.Add((holdPoint, null));
        }

        for (int i = 0; i < gizmoSeats.Count; i++)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(gizmoSeats[i].holdPoint.position, 0.15f);
        }
    }
}
