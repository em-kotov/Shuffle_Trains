using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Station : MonoBehaviour
{
    [SerializeField] private List<Transform> _holdPoints;

    private List<(Transform holdPoint, Passenger passenger)> _seats;
    private List<Passenger> _passengers;

    public CatColor CatColor { get; private set; }

    public void Initialize()
    {
        _passengers = new();
        _seats = new();

        for (int i = 0; i < _holdPoints.Count; i++)
        {
            _seats.Add((_holdPoints[i], null));
        }

        SetColor(CatColor.Uncolored);
    }

    public void UpdatePassengers(List<Passenger> passengers)
    {
        if (passengers.Count <= 0)
            return;

        SetColor(passengers[0].CatColor);

        for (int i = 0; i < passengers.Count; i++)
        {
            Transform holdPoint = passengers[i].transform.parent;
            int seatIndex = _seats.FindIndex(seat => seat.holdPoint == holdPoint);

            _seats[seatIndex] = new(_seats[seatIndex].holdPoint, passengers[i]);
            _passengers.Add(passengers[i]);
            Debug.Log("Station - update passengers count: " + _passengers.Count);
        }
    }

    public List<Transform> GetFreeSeats()
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

    public List<Passenger> GetPickupPassengers(CatColor catColor, int seatsCount)
    {
        List<Passenger> pickPassengers = new();

        if (_passengers.Count == 0 || seatsCount == 0)
        {
            return pickPassengers;
        }

        pickPassengers = GetPassengersByColor(catColor, seatsCount);
        Debug.Log($"Station - on get pickup passengers count: {_passengers.Count}");

        return pickPassengers;
    }

    public void RemovePassenger(Passenger passenger)
    {
        int seatIndex = _seats.FindIndex(seat => seat.passenger == passenger);
        _seats[seatIndex] = new(_seats[seatIndex].holdPoint, null);

        _passengers.Remove(passenger);
    }

    private List<Passenger> GetPassengersByColor(CatColor catColor, int count)
    {
        List<Passenger> passengers = new();

        List<Passenger> temporary = _passengers.FindAll(
            passenger => passenger.CatColor == catColor).ToList();
        Debug.Log($"Station - get passenger by color count: {temporary.Count}");

        if (temporary.Count <= count)
            return temporary;

        for (int i = 0; i < count; i++)
        {
            passengers.Add(temporary[i]);
        }

        return passengers;
    }

    private void SetColor(CatColor catColor)
    {
        CatColor = catColor;
    }

    private void OnDrawGizmos()
    {
        List<(Transform holdPoint, Passenger passenger)> gizmoSeats = new();

        if (gizmoSeats.Count == 0)
        {
            foreach (Transform holdPoint in _holdPoints)
            {
                gizmoSeats.Add((holdPoint, null));
            }
        }

        for (int i = 0; i < gizmoSeats.Count; i++)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(gizmoSeats[i].holdPoint.position, 0.15f);
        }
    }
}
