using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Sorter : MonoBehaviour
{
    private List<Passenger> _wrongPassengers;
    private List<Transform> _holdPoints;
    private int _passengersToSort;

    public void Initialize(int totalCount, List<Passenger> passengers, List<Transform> holdPoints)
    {
        _wrongPassengers = passengers;
        _holdPoints = holdPoints;

        AssignPassengers(_wrongPassengers, _holdPoints);
        _passengersToSort = _wrongPassengers.Count;
    }

    public bool HasPassengersToSort()
    {
        return _wrongPassengers.Count > 0;
    }

    public void DropPassenger(Passenger passenger, Transform seat)
    {
        _wrongPassengers.Remove(passenger);
        passenger.transform.SetParent(seat);
        passenger.transform.SetLocalPositionAndRotation(Vector3.zero,
                                            Quaternion.Euler(Vector3.zero));
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

    public List<Passenger> GetDropOffPassengers(CatColor catColor, int seatsCount)
    {
        List<Passenger> dropPassengers = new();

        if (_passengersToSort == 0 || seatsCount == 0)
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
