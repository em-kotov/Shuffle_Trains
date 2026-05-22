public interface IShopRepository
{
    bool IsOwned(string id);
    void MarkOwned(string id);
    string GetEquipped(SkinCategory category);
    void SetEquipped(SkinCategory category, string id);
}
