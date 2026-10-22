using System.Collections.Generic;
using UnityEngine;

public class ShopRepository : IShopRepository
{
    private readonly HashSet<string> _owned;

    public ShopRepository(ShopCatalog catalog)
    {
        if (catalog != null && PlayerPrefs.GetInt(SaveKeys.ShopSeeded, 0) == 0)
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
        return PlayerPrefs.GetString(SaveKeys.ShopEquipped, string.Empty);
    }

    public void SetEquipped(string id)
    {
        PlayerPrefs.SetString(SaveKeys.ShopEquipped, id);
        PlayerPrefs.Save();
        CloudSave.Push();
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

        PlayerPrefs.SetString(SaveKeys.ShopOwned, string.Join(",", ownedIds));

        Theme defaultEquipped = catalog.GetDefaultEquipped();

        if (defaultEquipped != null)
        {
            PlayerPrefs.SetString(SaveKeys.ShopEquipped, defaultEquipped.Id);
        }

        PlayerPrefs.SetInt(SaveKeys.ShopSeeded, 1);
        PlayerPrefs.Save();
        CloudSave.Push();
    }

    private HashSet<string> LoadOwned()
    {
        HashSet<string> result = new HashSet<string>();
        string raw = PlayerPrefs.GetString(SaveKeys.ShopOwned, string.Empty);

        if (string.IsNullOrEmpty(raw))
        {
            return result;
        }

        foreach (string id in raw.Split(','))
        {
            if (string.IsNullOrEmpty(id) == false)
            {
                result.Add(id);
            }
        }

        return result;
    }

    private void SaveOwned()
    {
        PlayerPrefs.SetString(SaveKeys.ShopOwned, string.Join(",", _owned));
        PlayerPrefs.Save();
        CloudSave.Push();
    }
}
