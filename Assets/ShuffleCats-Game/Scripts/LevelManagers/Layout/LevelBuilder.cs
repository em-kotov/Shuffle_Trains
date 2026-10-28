using System.Collections.Generic;
using UnityEngine;

public class LevelBuilder : MonoBehaviour
{
    private const string CatPrefabRoot = "Prefabs/Cats";

    [Header("Spawn Parent (optional)")]
    [SerializeField] private Transform _catsParent;

    public List<Subscriber> BuildCats(LevelConfig gridConfig, IReadOnlyList<CatPlacement> cats)
    {
        List<Subscriber> result = new();
        Transform catsParent;

        if (_catsParent != null)
        {
            catsParent = _catsParent;
        }
        else
        {
            catsParent = transform;
        }

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
            cat.Configure(
                placement.Orientation,
                placement.SignDirection,
                placement.InvertFlip,
                placement.TrackFlipSeparate,
                placement.InvertFlipTrack,
                placement.ApplyRotationFix,
                placement.RotateFixLeft,
                placement.Passengers);
            result.Add(cat);
        }

        return result;
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
            _ => null,
        };
    }

    private Subscriber ResolveCatPrefab(CatColor color, int length)
    {
        string code = ColorCode(color);

        if (code == null)
            return null;

        string path = $"{CatPrefabRoot}/1x{length}/Cat-{length}-{code}";
        return Resources.Load<Subscriber>(path);
    }
}
