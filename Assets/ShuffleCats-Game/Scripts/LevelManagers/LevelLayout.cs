using System.Collections.Generic;
using UnityEngine;

// Per-level data that varies: the cat layout and which of the shared base
// stations are active. Everything else (grid, track, movement, station
// positions/rotations/theme) comes from the shared LevelConfig base.
[CreateAssetMenu(fileName = "LevelLayout", menuName = "ShuffleCats/Level Layout")]
public class LevelLayout : ScriptableObject
{
    [Header("Level")]
    [SerializeField] private int _levelNumber = 1;
    [SerializeField] private int _levelWinBonus = 100;
    [SerializeField] private int _levelCarBonus = 10;

    [Header("Track")]
    [Tooltip("Per-level cats-on-track limit shown in the CatsCounter UI " +
            "(MaxTextPrimary). 0 means inherit the base config's value.")]
    [SerializeField] private int _maxCarsOnTrackCount = 0;

    [Header("Cats")]
    [SerializeField] private List<CatPlacement> _cats = new();

    [Tooltip("One flag per station in the base config, in the same order. " +
            "Empty means all base stations are active.")]
    [SerializeField] private List<bool> _activeStations = new();

    public int LevelNumber => _levelNumber;
    public int LevelWinBonus => _levelWinBonus;
    public int LevelCarBonus => _levelCarBonus;
    // 0 means "inherit the base config's value" (see LevelManager.ApplyConfig).
    public int MaxCarsOnTrackCount => _maxCarsOnTrackCount;
    public IReadOnlyList<CatPlacement> Cats => _cats;
    public IReadOnlyList<bool> ActiveStations => _activeStations;

#if UNITY_EDITOR
    public void EditorSet(int levelNumber, int winBonus, int carBonus,
                        int maxCarsOnTrackCount,
                        List<CatPlacement> cats, List<bool> activeStations)
    {
        _levelNumber = levelNumber;
        _levelWinBonus = winBonus;
        _levelCarBonus = carBonus;
        _maxCarsOnTrackCount = maxCarsOnTrackCount;
        _cats = cats;
        _activeStations = activeStations;
    }
#endif
}
