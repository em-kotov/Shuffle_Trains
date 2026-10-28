using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : UIPanel
{
    private readonly List<ShopSectionUI> _sections = new List<ShopSectionUI>();
    private readonly List<ShopSlotUI> _slots = new List<ShopSlotUI>();

    [SerializeField] private Shop _shop;
    [SerializeField] private Button _openButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Transform _sectionsParent;
    [SerializeField] private ShopSectionUI _sectionPrefab;
    [SerializeField] private ShopSlotUI _slotPrefab;

    private void OnEnable()
    {
        _openButton.onClick.AddListener(Open);
        _closeButton.onClick.AddListener(Close);
        _openButton.onClick.AddListener(AudioManager.PlayUIClickStatic);
        _closeButton.onClick.AddListener(AudioManager.PlayUIClickStatic);
        _shop.Changed += RefreshAllSlots;
        Close();
    }

    private void OnDisable()
    {
        _openButton.onClick.RemoveListener(Open);
        _closeButton.onClick.RemoveListener(Close);
        _openButton.onClick.RemoveListener(AudioManager.PlayUIClickStatic);
        _closeButton.onClick.RemoveListener(AudioManager.PlayUIClickStatic);
        _shop.Changed -= RefreshAllSlots;
        YandexSdk.SetShopOpen(false);
    }

    private void Open()
    {
        _openButton.interactable = false;
        Rebuild();
        Show();
        YandexSdk.SetShopOpen(true);
    }

    private void Close()
    {
        Hide();
        _openButton.interactable = true;
        YandexSdk.SetShopOpen(false);
    }

    private void Rebuild()
    {
        ClearSections();

        if (_shop.Catalog == null)
        {
            return;
        }

        ShopSectionUI sectionUI = Instantiate(_sectionPrefab, _sectionsParent);
        sectionUI.SetHeader(_shop.Catalog.ShopHeader);

        foreach (Theme theme in _shop.Catalog.Themes)
        {
            if (theme == null)
            {
                continue;
            }

            ShopSlotUI slot = Instantiate(_slotPrefab, sectionUI.SlotsParent);
            slot.Bind(theme, _shop);
            _slots.Add(slot);
        }

        _sections.Add(sectionUI);
        var scroll = _sectionsParent.GetComponentInParent<ScrollRect>();

        if (scroll != null)
        {
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1;
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
                section.gameObject.SetActive(false);
                Destroy(section.gameObject);
            }
        }

        _sections.Clear();
        _slots.Clear();
    }
}
