using System.Collections.Generic;
using UnityEngine;

public class ShopRepository : IShopRepository
{
    private const string OwnedKey = "Shop.Owned";
    private const string EquippedKey = "Shop.Equipped";
    private const string SeededKey = "Shop.Seeded.v3";

    private readonly HashSet<string> _owned;

    public ShopRepository(ShopCatalog catalog)
    {
        if (catalog != null && PlayerPrefs.GetInt(SeededKey, 0) == 0)
        {
            Seed(catalog);
        }

        _owned = LoadOwned();
    }

    public bool IsOwned(string id)
    {
        return _owned.Contains(id);
    }

    public void MarkOwned(string id)
    {
        if (_owned.Add(id))
        {
            SaveOwned();
        }
    }

    public string GetEquipped()
    {
        return PlayerPrefs.GetString(EquippedKey, string.Empty);
    }

    public void SetEquipped(string id)
    {
        PlayerPrefs.SetString(EquippedKey, id);
        PlayerPrefs.Save();
    }

    private void Seed(ShopCatalog catalog)
    {
        List<string> ownedIds = new List<string>();

        foreach (Theme theme in catalog.Themes)
        {
            if (theme != null && theme.OwnedByDefault)
            {
                ownedIds.Add(theme.Id);
            }
        }

        PlayerPrefs.SetString(OwnedKey, string.Join(",", ownedIds));

        Theme defaultEquipped = catalog.GetDefaultEquipped();
        if (defaultEquipped != null)
        {
            PlayerPrefs.SetString(EquippedKey, defaultEquipped.Id);
        }

        PlayerPrefs.SetInt(SeededKey, 1);
        PlayerPrefs.Save();
    }

    private HashSet<string> LoadOwned()
    {
        HashSet<string> result = new HashSet<string>();
        string raw = PlayerPrefs.GetString(OwnedKey, string.Empty);

        if (string.IsNullOrEmpty(raw))
        {
            return result;
        }

        foreach (string id in raw.Split(','))
        {
            if (!string.IsNullOrEmpty(id))
            {
                result.Add(id);
            }
        }

        return result;
    }

    private void SaveOwned()
    {
        PlayerPrefs.SetString(OwnedKey, string.Join(",", _owned));
        PlayerPrefs.Save();
    }
}
