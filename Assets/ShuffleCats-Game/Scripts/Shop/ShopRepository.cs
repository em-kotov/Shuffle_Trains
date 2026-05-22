using System.Collections.Generic;
using UnityEngine;

public class ShopRepository : IShopRepository
{
    private const string OwnedKey = "Shop.Owned";
    private const string EquippedKeyPrefix = "Shop.Equipped.";
    private const string SeededKey = "Shop.Seeded.v2";

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

    public string GetEquipped(SkinCategory category)
    {
        return PlayerPrefs.GetString(EquippedKeyPrefix + category, string.Empty);
    }

    public void SetEquipped(SkinCategory category, string id)
    {
        PlayerPrefs.SetString(EquippedKeyPrefix + category, id);
        PlayerPrefs.Save();
    }

    private void Seed(ShopCatalog catalog)
    {
        List<string> ownedIds = new List<string>();

        foreach (SkinItem item in catalog.AllItems())
        {
            if (item.OwnedByDefault)
            {
                ownedIds.Add(item.Id);
            }
        }

        PlayerPrefs.SetString(OwnedKey, string.Join(",", ownedIds));

        foreach (ShopCatalog.CategorySection section in catalog.Sections)
        {
            foreach (SkinItem item in section.Items)
            {
                if (item != null && item.EquippedByDefault)
                {
                    PlayerPrefs.SetString(EquippedKeyPrefix + section.Category, item.Id);
                    break;
                }
            }
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
