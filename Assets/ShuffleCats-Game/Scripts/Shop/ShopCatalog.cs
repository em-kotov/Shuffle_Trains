using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopCatalog", menuName = "ShuffleCats/Shop/Catalog")]
public class ShopCatalog : ScriptableObject
{
    [Serializable]
    public class CategorySection
    {
        [SerializeField] private SkinCategory _category;
        [SerializeField] private string _header;
        [SerializeField] private List<SkinItem> _items = new List<SkinItem>();

        public SkinCategory Category => _category;
        public string Header => _header;
        public IReadOnlyList<SkinItem> Items => _items;
    }

    [SerializeField] private List<CategorySection> _sections = new List<CategorySection>();

    public IReadOnlyList<CategorySection> Sections => _sections;

    public IEnumerable<SkinItem> AllItems()
    {
        foreach (CategorySection section in _sections)
        {
            foreach (SkinItem item in section.Items)
            {
                if (item != null)
                {
                    yield return item;
                }
            }
        }
    }

    public SkinItem Find(string id)
    {
        foreach (SkinItem item in AllItems())
        {
            if (item.Id == id)
            {
                return item;
            }
        }
        return null;
    }

    public bool TryGetCategoryOf(SkinItem item, out SkinCategory category)
    {
        foreach (CategorySection section in _sections)
        {
            foreach (SkinItem candidate in section.Items)
            {
                if (candidate == item)
                {
                    category = section.Category;
                    return true;
                }
            }
        }
        category = default;
        return false;
    }
}
