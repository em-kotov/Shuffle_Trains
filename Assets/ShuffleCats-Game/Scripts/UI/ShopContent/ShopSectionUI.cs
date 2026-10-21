using TMPro;
using UnityEngine;

public class ShopSectionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _header;
    [SerializeField] private Transform _slotsParent;

    public Transform SlotsParent => _slotsParent;

    private UnityEngine.UI.GridLayoutGroup _grid;
    private UnityEngine.UI.ScrollRect _scroll;
    private UnityEngine.UI.VerticalLayoutGroup _sectionLayout;
    private float _lastWidth = -1;

    private void Awake()
    {
        ((RectTransform)transform).pivot = new Vector2(0.5f, 1);
        ((RectTransform)_slotsParent).pivot = new Vector2(0.5f, 1);
        _grid = _slotsParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        _sectionLayout = GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        if (_grid != null)
        {
            _grid.cellSize = new Vector2(136, 204);
            _grid.spacing = new Vector2(12, 12);
            _grid.padding = new RectOffset(0, 0, 0, 4);
        }
        if (_sectionLayout != null)
        {
            _sectionLayout.padding = new RectOffset(2, 2, 4, 4);
            _sectionLayout.spacing = 10;
        }
        if (_header != null)
        {
            _header.fontSize = _header.fontSizeMax = 20;
            _header.fontStyle = FontStyles.Normal;
            _header.rectTransform.sizeDelta = new Vector2(0, 28);
        }
    }

    private void LateUpdate()
    {
        if (_slotsParent == null) return;
        if (_grid == null) _grid = _slotsParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (_grid == null) return;

        if (_scroll == null) _scroll = GetComponentInParent<UnityEngine.UI.ScrollRect>();
        if (_sectionLayout == null) _sectionLayout = GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        // Use the viewport, not the grid's own width: a three-column grid can
        // impose a minimum width on its parents and otherwise never shrink.
        float width = _scroll != null && _scroll.viewport != null
            ? _scroll.viewport.rect.width - (_sectionLayout != null ? _sectionLayout.padding.horizontal : 0)
            : ((RectTransform)_slotsParent).rect.width;
        if (Mathf.Approximately(width, _lastWidth)) return;
        _lastWidth = width;
        float available = width - _grid.padding.horizontal;
        int columns = Mathf.Clamp(Mathf.FloorToInt((available + _grid.spacing.x) / (136 + _grid.spacing.x)), 1, 3);
        _grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
        _grid.constraintCount = columns;
        _grid.cellSize = new Vector2(Mathf.Min(208, (available - (columns - 1) * _grid.spacing.x) / columns), 204);
        _grid.childAlignment = TextAnchor.UpperCenter;
        if (_scroll != null)
        {
            // Reflow before restoring the top: ScrollRect can otherwise clamp
            // against the old row height during an orientation change.
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_slotsParent);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
            _scroll.StopMovement();
            _scroll.verticalNormalizedPosition = 1;
        }
    }

    public void SetHeader(string text)
    {
        if (_header != null)
        {
            _header.color = new Color32(92, 62, 47, 255);
            LocalizationBinder.Set(_header, text);
        }
    }
}
