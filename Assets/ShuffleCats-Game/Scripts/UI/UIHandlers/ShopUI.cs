using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : UIPanel
{
    [SerializeField] private Shop _shop;
    [SerializeField] private Button _openButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Transform _sectionsParent;
    [SerializeField] private ShopSectionUI _sectionPrefab;
    [SerializeField] private ShopSlotUI _slotPrefab;

    private readonly List<ShopSectionUI> _sections = new List<ShopSectionUI>();
    private readonly List<ShopSlotUI> _slots = new List<ShopSlotUI>();

    private void OnEnable()
    {
        _openButton.onClick.AddListener(Open);
        _closeButton.onClick.AddListener(Close);
        _shop.Changed += RefreshAllSlots;
        Close();
    }

    private void OnDisable()
    {
        _openButton.onClick.RemoveListener(Open);
        _closeButton.onClick.RemoveListener(Close);
        _shop.Changed -= RefreshAllSlots;
    }

    private void Open()
    {
        _openButton.interactable = false;
        Rebuild();
        Show();
    }

    private void Close()
    {
        Hide();
        _openButton.interactable = true;
    }

    private void Rebuild()
    {
        ClearSections();

        if (_shop.Catalog == null)
        {
            return;
        }

        foreach (ShopCatalog.CategorySection section in _shop.Catalog.Sections)
        {
            ShopSectionUI sectionUI = Instantiate(_sectionPrefab, _sectionsParent);
            sectionUI.SetHeader(section.Header);

            foreach (SkinItem item in section.Items)
            {
                if (item == null)
                {
                    continue;
                }

                ShopSlotUI slot = Instantiate(_slotPrefab, sectionUI.SlotsParent);
                slot.Bind(item, _shop);
                _slots.Add(slot);
            }

            _sections.Add(sectionUI);
        }
    }

    private void RefreshAllSlots()
    {
        foreach (ShopSlotUI slot in _slots)
        {
            if (slot != null)
            {
                slot.Refresh();
            }
        }
    }

    private void ClearSections()
    {
        foreach (ShopSectionUI section in _sections)
        {
            if (section != null)
            {
                Destroy(section.gameObject);
            }
        }

        _sections.Clear();
        _slots.Clear();
    }
}
