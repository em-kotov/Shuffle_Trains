using System.Collections.Generic;
using UnityEngine;

public class StationOperator : MonoBehaviour
{
    private List<(Station station, bool isVisited)> _stationsProgress;
    private List<Station> _stations;
    private int _totalStationCount;

    public void Initialize(int stationCount)
    {
        _totalStationCount = stationCount;

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

        // TEMP DIAGNOSTIC — remove after debugging station misses.
        int visited = _stationsProgress.FindAll(progress => progress.isVisited).Count;
        Debug.Log($"[ScanDiag] {name} MarkVisited station={station.name} " +
                  $"visited={visited}/{_totalStationCount} tracked={_stationsProgress.Count}", this);
    }

    public void ResetProgress()
    {
        for (int i = 0; i < _stationsProgress.Count; i++)
        {
            _stationsProgress[i] = (_stationsProgress[i].station, false);
        }

        // TEMP DIAGNOSTIC — remove after debugging station misses.
        Debug.Log($"[ScanDiag] {name} ResetProgress cleared visited flags " +
                  $"(tracked={_stationsProgress.Count})", this);
    }

    public bool IsNeedToReset()
    {
        if (_stations.Count < _totalStationCount)
        {
            return false;
        }

        for (int i = 0; i < _stationsProgress.Count; i++)
        {
            if (_stationsProgress[i].isVisited == false)
            {
                return false;
            }
        }

        return true;
    }
}
