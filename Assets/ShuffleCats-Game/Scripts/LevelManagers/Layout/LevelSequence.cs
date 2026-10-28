using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelSequence", menuName = "ShuffleCats/Level Sequence")]
public class LevelSequence : ScriptableObject
{
    [SerializeField] private List<LevelLayout> _levels = new();

    public int Count => _levels.Count;

    public IReadOnlyList<LevelLayout> Levels => _levels;

    public LevelLayout Get(int index)
    {
        if (index < 0 || index >= _levels.Count)
            return null;

        return _levels[index];
    }
}
