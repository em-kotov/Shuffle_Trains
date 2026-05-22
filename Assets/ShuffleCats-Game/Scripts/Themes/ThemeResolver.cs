public static class ThemeResolver
{
    public static Theme GetEquipped(ShopCatalog catalog)
    {
        if (catalog == null)
        {
            return null;
        }

        ShopRepository repository = new ShopRepository(catalog);
        string equippedId = repository.GetEquipped();

        if (!string.IsNullOrEmpty(equippedId))
        {
            Theme theme = catalog.Find(equippedId);
            if (theme != null)
            {
                return theme;
            }
        }

        return catalog.GetDefaultEquipped();
    }
}
