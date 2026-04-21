using System.Collections.Generic;
using UnityEngine;

public class SorterRegistrator : MonoBehaviour
{
    private List<Station> _stations;
    private List<SorterCar> _cars;

    public void Initialize(List<Station> stations)
    {
        _stations = stations;
        _cars = new();
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
        if (_cars.Contains(sorterCar))
        {
            _cars.Remove(sorterCar);
        }
    }

    public bool HaveMoves()
    {
        Station station = null;
        CatColor stationColor = CatColor.Uncolored;
        int stationCount = 0;

        SorterCar car = null;
        int carCount = 0;
        CatColor carColor;

        for (int i = 0; i < _stations.Count; i++)
        {
            station = _stations[i];

            if (station.HavePassengers(out stationCount, out stationColor) == false)
            {
                return true;
            }

            for (int j = 0; j < _cars.Count; j++)
            {
                car = _cars[j];

                if (car != null)
                {
                    if (car.HaveSeats(out carCount, out carColor))
                    {
                        if (carColor == stationColor && carCount <= stationCount)
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
