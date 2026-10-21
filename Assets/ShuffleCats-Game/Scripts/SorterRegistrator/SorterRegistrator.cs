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

    // Only diagnose a stable track state. Reserved slots can belong to cats
    // still entering or finished cats leaving, either of which can change it.
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

        // With no possible transfer, another lap cannot free a track slot.
        // An empty station alone is not evidence of a useful move.
        return true;
    }

    public bool HaveStationInColor(CatColor catColor)
    {
        return _stations.Any(station => station.CatColor == catColor);
    }

    public bool HaveMoves()
    {
        Station station = null;
        CatColor stationColor = CatColor.Uncolored;
        int passengersCount = 0;

        SorterCar car = null;
        int seatsCount = 0;
        CatColor carColor;

        for (int i = 0; i < _stations.Count; i++)
        {
            station = _stations[i];

            if (station.HavePassengers(out passengersCount, out stationColor) == false)
            {
                return true;
            }

            for (int j = 0; j < _cars.Count; j++)
            {
                car = _cars[j];

                if (car != null)
                {
                    if (car.HaveSeats(out seatsCount, out carColor))
                    {
                        if (carColor == stationColor && seatsCount <= passengersCount)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }
}
