using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopCatalog", menuName = "ShuffleCats/Shop/Catalog")]
public class ShopCatalog : ScriptableObject
{
    [SerializeField] private string _shopHeader = "Themes";
    [SerializeField] private List<Theme> _themes = new List<Theme>();

    public string ShopHeader => _shopHeader;
    public IReadOnlyList<Theme> Themes => _themes;

    public Theme Find(string id)
    {
        foreach (Theme theme in _themes)
        {
            if (theme != null && theme.Id == id)
            {
                return theme;
            }
        }

        return null;
    }

    public Theme GetDefaultEquipped()
    {
        foreach (Theme theme in _themes)
        {
            if (theme != null && theme.EquippedByDefault)
            {
                return theme;
            }
        }

        return null;
    }
}
