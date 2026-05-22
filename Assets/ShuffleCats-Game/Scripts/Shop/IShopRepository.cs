public interface IShopRepository
{
    bool IsOwned(string id);
    void MarkOwned(string id);
    string GetEquipped();
    void SetEquipped(string id);
}
