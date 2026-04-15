using System.Collections.Generic;
using UnityEngine;

public class StationOperator : MonoBehaviour
{
    private List<(Station station, bool isVisited)> _stationsProgress;
    private List<Station> _stations;

    public void Initialize()
    {
        _stationsProgress = new();
        _stations = new();
    }

    public bool TryAddStationToProgress(Station station)
    {
        if (_stations.Contains(station))
            return false;

        _stations.Add(station);
        _stationsProgress.Add((station, false));
        return true;
    }

    public bool IsVisited(Station station)
    {
        int stationIndex = _stationsProgress.FindIndex(progress => progress.station == station);
        return _stationsProgress[stationIndex].isVisited;
    }

    public void MarkVisited(Station station)
    {
        int stationIndex = _stationsProgress.FindIndex(progress => progress.station == station);
        _stationsProgress[stationIndex] = (_stationsProgress[stationIndex].station, true);
    }

    public void ResetProgress()
    {
        for (int i = 0; i < _stationsProgress.Count; i++)
        {
            _stationsProgress[i] = (_stationsProgress[i].station, false);
        }
    }
}
