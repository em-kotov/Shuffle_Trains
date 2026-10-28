using System.Collections.Generic;
using System.Text;

public sealed class CatHintPlanner
{
    private readonly Cat[] _cats;
    private readonly int _capacity;
    private readonly HashSet<string> _failed = new HashSet<string>();
    private int _budget;

    public CatHintPlanner(Cat[] cats, int capacity)
    {
        _cats = cats;
        _capacity = capacity;
    }

    public int FindNext(ulong parked, int[] activeOrder, StationState[] stations, int nodeBudget = 6000)
    {
        const int maximumSupportedCats = 63;

        if (_cats.Length > maximumSupportedCats || _capacity <= 0 || parked == 0)
            return -1;

        _failed.Clear();
        _budget = nodeBudget;
        var state = new State { Parked = parked, Stations = (StationState[])stations.Clone() };

        foreach (int index in activeOrder)
            state.Active.Add(CreateRider(index));

        Settle(state);

        if (state.Active.Count >= _capacity)
            return -1;

        foreach (int index in Choices(state))
        {
            State next = Launch(state, index);

            if (CanFinish(next))
                return index;
        }

        return -1;
    }

    private static string Key(State state)
    {
        var key = new StringBuilder().Append(state.Parked).Append('|');

        foreach (Rider rider in state.Active)
        {
            key.Append(rider.Index).Append(':').Append(rider.Right).Append(':');

            foreach (int color in rider.Wrong)
                key.Append(color).Append(',');

            key.Append(';');
        }

        key.Append('|');

        foreach (StationState station in state.Stations)
        {
            int stationColor;

            if (station.Count == 0)
            {
                stationColor = -1;
            }
            else
            {
                stationColor = station.Color;
            }

            key.Append(stationColor).Append(':').Append(station.Count).Append(';');
        }

        return key.ToString();
    }

    private Rider CreateRider(int index) => new Rider
    {
        Index = index, Right = _cats[index].Right, Wrong = new List<int>(_cats[index].Wrong),
    };

    private State Launch(State state, int index)
    {
        State next = state.Copy();
        next.Parked &= ~(1UL << index);
        next.Active.Add(CreateRider(index));
        Settle(next);
        return next;
    }

    private bool CanFinish(State state)
    {
        if (state.Parked == 0 && state.Active.Count == 0)
            return true;

        if (--_budget <= 0 || state.Active.Count >= _capacity)
            return false;

        string key = Key(state);

        if (_failed.Add(key) == false)
            return false;

        foreach (int index in Choices(state))
        {
            if (CanFinish(Launch(state, index)))
                return true;
        }

        return false;
    }

    private List<int> Choices(State state)
    {
        var choices = new List<int>();

        for (int i = 0; i < _cats.Length; i++)
        {
            if ((state.Parked & (1UL << i)) != 0 && (_cats[i].Blockers & state.Parked) == 0)
                choices.Add(i);
        }

        choices.Sort((a, b) =>
        {
            int difference = PickupScore(b, state) - PickupScore(a, state);

            if (difference != 0)
            {
                return difference;
            }
            else
            {
                return a.CompareTo(b);
            }
        });
        return choices;
    }

    private int PickupScore(int index, State state)
    {
        int score = 0;

        foreach (StationState station in state.Stations)
        {
            if (station.Count > 0 && station.Color == _cats[index].Color)
                score += station.Count;
        }

        return score;
    }

    private void Settle(State state)
    {
        // Each useful transfer removes a wrong ball or fills a correct seat, bounding changing laps.
        bool changed;

        do
        {
            changed = false;

            foreach (Rider rider in state.Active)
            {
                Cat cat = _cats[rider.Index];

                for (int s = 0; s < state.Stations.Length; s++)
                {
                    StationState station = state.Stations[s];
                    bool canDrop;

                    if (station.Count > 0)
                    {
                        canDrop = true;
                    }
                    else
                    {
                        canDrop = false;
                    }

                    if (canDrop == false)
                    {
                        foreach (int color in rider.Wrong)
                        {
                            bool stored = false;

                            foreach (StationState other in state.Stations)
                            {
                                if (other.Count > 0 && other.Color == color)
                                    stored = true;
                            }

                            if (stored == false)
                            {
                                canDrop = true;
                                break;
                            }
                        }
                    }

                    // Match PassengerTracker: an empty station takes the first remaining wrong color.
                    int drop;

                    if (station.Count > 0)
                    {
                        drop = station.Color;
                    }
                    else if (rider.Wrong.Count > 0)
                    {
                        drop = rider.Wrong[0];
                    }
                    else
                    {
                        drop = -1;
                    }

                    if (canDrop)
                    {
                        for (int w = 0; w < rider.Wrong.Count && station.Count < station.Capacity;)
                        {
                            if (rider.Wrong[w] != drop)
                            {
                                w++;
                                continue;
                            }

                            rider.Wrong.RemoveAt(w);
                            station.Color = drop;
                            station.Count++;
                            changed = true;
                        }
                    }

                    while (station.Count > 0 && station.Color == cat.Color &&
                        rider.Right + rider.Wrong.Count < cat.Capacity)
                    {
                        station.Count--;
                        rider.Right++;
                        changed = true;
                    }

                    state.Stations[s] = station;
                }
            }

            state.Active.RemoveAll(rider => rider.Wrong.Count == 0 && rider.Right == _cats[rider.Index].Capacity);
        }
        while (changed);
    }

    public struct StationState
    {
        public int Color;
        public int Count;
        public int Capacity;
    }

    public sealed class Cat
    {
        public int Color { get; set; }

        public int Capacity { get; set; }

        public int Right { get; set; }

        public int[] Wrong { get; set; }

        public ulong Blockers { get; set; }
    }

    private sealed class Rider
    {
        public int Index { get; set; }

        public int Right { get; set; }

        public List<int> Wrong { get; set; }

        public Rider Copy() => new Rider { Index = Index, Right = Right, Wrong = new List<int>(Wrong) };
    }

    private sealed class State
    {
        public ulong Parked { get; set; }

        public List<Rider> Active { get; } = new List<Rider>();

        public StationState[] Stations { get; set; }

        public State Copy()
        {
            var copy = new State { Parked = Parked, Stations = (StationState[])Stations.Clone() };

            foreach (Rider rider in Active)
                copy.Active.Add(rider.Copy());

            return copy;
        }
    }
}
