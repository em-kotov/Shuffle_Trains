using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopSlotUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _price;
    [SerializeField] private TextMeshProUGUI _status;
    [SerializeField] private Button _actionButton;
    [SerializeField] private Color _lockedColor = new Color(0.878f, 0.478f, 0.373f);
    [SerializeField] private Color _ownedColor = new Color(0.506f, 0.698f, 0.604f);
    [SerializeField] private Color _equippedColor = new Color(0.949f, 0.800f, 0.561f);

    private SkinItem _item;
    private Shop _shop;

    public void Bind(SkinItem item, Shop shop)
    {
        _item = item;
        _shop = shop;

        _actionButton.onClick.RemoveAllListeners();
        _actionButton.onClick.AddListener(OnClick);

        Refresh();
    }

    public void Refresh()
    {
        if (_item == null)
        {
            return;
        }

        if (_item.SlotIcon != null)
        {
            _icon.sprite = _item.SlotIcon;
        }
        _icon.color = _item.Tint;

        SlotStatus status = _shop.GetStatus(_item);

        switch (status)
        {
            case SlotStatus.Locked:
                _status.text = "Buy";
                _status.color = _lockedColor;
                _price.text = _item.Price.ToString();
                _actionButton.interactable = true;
                break;
            case SlotStatus.Owned:
                _status.text = "Equip";
                _status.color = _ownedColor;
                _price.text = string.Empty;
                _actionButton.interactable = true;
                break;
            case SlotStatus.Equipped:
                _status.text = "Equipped";
                _status.color = _equippedColor;
                _price.text = string.Empty;
                _actionButton.interactable = false;
                break;
        }
    }

    private void OnClick()
    {
        if (_item == null || _shop == null)
        {
            return;
        }

        SlotStatus status = _shop.GetStatus(_item);

        if (status == SlotStatus.Locked)
        {
            _shop.TryBuy(_item);
        }
        else if (status == SlotStatus.Owned)
        {
            _shop.Equip(_item);
        }
    }
}
