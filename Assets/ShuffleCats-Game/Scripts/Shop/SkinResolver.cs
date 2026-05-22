public static class SkinResolver
{
    public static SkinItem GetEquipped(ShopCatalog catalog, SkinCategory category)
    {
        if (catalog == null)
        {
            return null;
        }

        ShopRepository repository = new ShopRepository(catalog);
        string equippedId = repository.GetEquipped(category);

        if (!string.IsNullOrEmpty(equippedId))
        {
            SkinItem item = catalog.Find(equippedId);
            if (item != null)
            {
                return item;
            }
        }

        foreach (ShopCatalog.CategorySection section in catalog.Sections)
        {
            if (section.Category != category)
            {
                continue;
            }

            foreach (SkinItem item in section.Items)
            {
                if (item != null && item.EquippedByDefault)
                {
                    return item;
                }
            }
        }

        return null;
    }
}
