using System.Collections.Generic;
using UnityEngine;

// Spawns the per-level cats described by a layout's cat list. Cat prefabs are
// resolved from Resources by naming convention
// (Resources/Prefabs/Cats/1x{length}/Cat-{length}-{colorCode}), which matches
// the existing prefab layout, so no per-color inspector wiring is needed.
//
// Stations are NOT spawned here: they are level-invariant (same spots/rotation
// every level) and live in the scene, where LevelManager just toggles which are
// active. Spawning them from a single prefab loses their per-instance sprite/
// hold-point offsets, which is why they stay scene-authored.
public class LevelBuilder : MonoBehaviour
{
    [Header("Spawn Parent (optional)")]
    [SerializeField] private Transform _catsParent;

    private const string CatPrefabRoot = "Prefabs/Cats";

    public List<Subscriber> BuildCats(LevelConfig gridConfig, IReadOnlyList<CatPlacement> cats)
    {
        List<Subscriber> result = new();
        Transform catsParent = _catsParent != null ? _catsParent : transform;

        foreach (CatPlacement placement in cats)
        {
            Subscriber prefab = ResolveCatPrefab(placement.Color, placement.Length);

            if (prefab == null)
            {
                Debug.LogError($"LevelBuilder - missing cat prefab for color " +
                            $"{placement.Color} length {placement.Length}");
                continue;
            }

            Vector3 worldPosition = gridConfig.CellToWorld(placement.Cell);
            Subscriber cat = Instantiate(prefab, worldPosition, Quaternion.Euler(placement.EulerAngles));
            cat.transform.SetParent(catsParent, worldPositionStays: true);
            cat.Configure(placement.Orientation, placement.SignDirection,
                        placement.InvertFlip, placement.TrackFlipSeparate,
                        placement.InvertFlipTrack, placement.ApplyRotationFix,
                        placement.RotateFixLeft, placement.Passengers);
            result.Add(cat);
        }

        return result;
    }

    private Subscriber ResolveCatPrefab(CatColor color, int length)
    {
        string code = ColorCode(color);

        if (code == null)
            return null;

        string path = $"{CatPrefabRoot}/1x{length}/Cat-{length}-{code}";
        return Resources.Load<Subscriber>(path);
    }

    private static string ColorCode(CatColor color)
    {
        return color switch
        {
            CatColor.Blue => "B",
            CatColor.Green => "G",
            CatColor.Pink => "P",
            CatColor.Yellow => "Y",
            CatColor.Purple => "Pu",
            CatColor.Orange => "O",
            CatColor.Brown => "Br",
            CatColor.Iridiscent => "Ir",
            _ => null
        };
    }
}
