using TMPro;
using UnityEngine;

public class ShopSectionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _header;
    [SerializeField] private Transform _slotsParent;

    public Transform SlotsParent => _slotsParent;

    public void SetHeader(string text)
    {
        if (_header != null)
        {
            _header.text = text;
        }
    }
}
