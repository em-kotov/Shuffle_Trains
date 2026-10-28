using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SorterRegistrator : MonoBehaviour
{
    private List<Station> _stations;
    private List<SorterCar> _cars;
    private HashSet<SorterCar> _activeTrackCars;

    public bool HasUnfinishedCars => _cars.Count > 0;

    public void Initialize(List<Station> stations)
    {
        _stations = stations;
        _cars = new();
        _activeTrackCars = new();
    }

    public void Register(SorterCar sorterCar)
    {
        if (_cars.Contains(sorterCar) == false)
        {
            _cars.Add(sorterCar);
        }
    }

    public void Unregister(SorterCar sorterCar)
    {
        _activeTrackCars.Remove(sorterCar);

        if (_cars.Contains(sorterCar))
        {
            _cars.Remove(sorterCar);
        }
    }

    public void MarkOnTrack(SorterCar sorterCar)
    {
        if (_cars.Contains(sorterCar))
            _activeTrackCars.Add(sorterCar);
    }

    // Reserved slots can belong to entering or exiting cats; wait for a stable track state.
    public bool IsTrackDeadlocked(int occupiedSlots)
    {
        if (_activeTrackCars.Count == 0 || _activeTrackCars.Count != occupiedSlots)
            return false;

        foreach (SorterCar car in _activeTrackCars)
        {
            if (car.IsSorting || car.IsFinished)
                return false;

            foreach (Station station in _stations)
            {
                if (car.CanExchangeWith(station))
                    return false;
            }
        }

        // An empty station is not enough: without a possible transfer, another lap cannot free a slot.
        return true;
    }

    public bool HaveStationInColor(CatColor catColor)
    {
        return _stations.Any(station => station.CatColor == catColor);
    }
}
