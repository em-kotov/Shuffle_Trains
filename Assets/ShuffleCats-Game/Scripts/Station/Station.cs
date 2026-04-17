using System.Collections.Generic;
using UnityEngine;

public class Station : MonoBehaviour
{
    public List<Transform> _holdPoints;
    public CatColor _catColor;
    public int _freeSeats;
    [SerializeField] private List<(Transform holdPoint, Passenger passenger)> _seats;

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

    private void OnDrawGizmos()
    {
        _seats = new();

        if (_seats.Count == 0)
        {
            foreach (Transform holdPoint in _holdPoints)
            {
                _seats.Add((holdPoint, null));
            }
        }

        for (int i = 0; i < _seats.Count; i++)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(_seats[i].holdPoint.position, 0.15f);
        }
    }
}
