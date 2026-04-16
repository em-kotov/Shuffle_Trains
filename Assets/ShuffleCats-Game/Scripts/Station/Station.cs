using System.Collections.Generic;
using UnityEngine;

public class Station : MonoBehaviour
{
    public List<Transform> _holdPoints;
    public CatColor _catColor;
    public int _freeSeats;
    private List<(Transform holdPoint, Passenger passenger)> _seats;

    public List<Transform> GetFreeSeats()
    {
        List<Transform> seats = new();

        for (int i = 0; i < seats.Count; i++)
        {
            if (_seats[i].passenger != null)
            {
                seats.Add(_seats[i].holdPoint);
            }
        }

        return seats;
    }
}
