using System.Collections.Generic;
using UnityEngine;

public class Station : MonoBehaviour
{
    [SerializeField] private List<Transform> _holdPoints;

    private List<(Transform holdPoint, Passenger passenger)> _seats;

    public CatColor CatColor { get; private set; }

    public void Initialize()
    {
        _seats = new();

        for (int i = 0; i < _holdPoints.Count; i++)
        {
            _seats.Add((_holdPoints[i], null));
        }

        CatColor = CatColor.Uncolored;
    }

    public void UpdatePassengers(List<Passenger> passengers)
    {
        if (passengers.Count <= 0)
            return;

        CatColor = passengers[0].CatColor;

        for (int i = 0; i < passengers.Count; i++)
        {
            Transform holdPoint = passengers[i].transform.parent;
            int seatIndex = _seats.FindIndex(seat => seat.holdPoint == holdPoint);

            _seats[seatIndex] = new(_seats[seatIndex].holdPoint, passengers[i]);
            Debug.Log($"Station - update passengers added passenger {passengers[i].name} to a seat");
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
                Debug.Log("Station - get free seats added free seat");
            }
        }

        return seats;
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
