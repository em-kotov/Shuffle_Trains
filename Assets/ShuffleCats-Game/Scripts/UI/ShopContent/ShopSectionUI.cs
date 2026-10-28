using TMPro;
using UnityEngine;

public class ShopSectionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _header;
    [SerializeField] private Transform _slotsParent;

    private UnityEngine.UI.GridLayoutGroup _grid;
    private UnityEngine.UI.ScrollRect _scroll;
    private UnityEngine.UI.VerticalLayoutGroup _sectionLayout;
    private float _lastWidth = -1;

    public Transform SlotsParent => _slotsParent;

    private void Awake()
    {
        const int cellWidth = 136;
        const int cellHeight = 204;
        const int cellSpacing = 12;
        const int gridBottomPadding = 4;
        const int sectionHorizontalPadding = 2;
        const int sectionVerticalPadding = 4;
        const int headerSpacing = 10;
        const int headerFontSize = 20;
        const int headerHeight = 28;

        ((RectTransform)transform).pivot = new Vector2(0.5f, 1);
        ((RectTransform)_slotsParent).pivot = new Vector2(0.5f, 1);
        _grid = _slotsParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        _sectionLayout = GetComponent<UnityEngine.UI.VerticalLayoutGroup>();

        if (_grid != null)
        {
            _grid.cellSize = new Vector2(cellWidth, cellHeight);
            _grid.spacing = new Vector2(cellSpacing, cellSpacing);
            _grid.padding = new RectOffset(0, 0, 0, gridBottomPadding);
        }

        if (_sectionLayout != null)
        {
            _sectionLayout.padding = new RectOffset(sectionHorizontalPadding, sectionHorizontalPadding, sectionVerticalPadding, sectionVerticalPadding);
            _sectionLayout.spacing = headerSpacing;
        }

        if (_header != null)
        {
            _header.fontSize = _header.fontSizeMax = headerFontSize;
            _header.fontStyle = FontStyles.Normal;
            _header.rectTransform.sizeDelta = new Vector2(0, headerHeight);
        }
    }

    private void LateUpdate()
    {
        const int minimumCellWidth = 136;
        const int maximumCellWidth = 208;
        const int cellHeight = 204;
        const int maximumColumns = 3;
        const int minimumColumns = 1;

        if (_slotsParent == null)
            return;

        if (_grid == null)
            _grid = _slotsParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();

        if (_grid == null)
            return;

        if (_scroll == null)
            _scroll = GetComponentInParent<UnityEngine.UI.ScrollRect>();

        if (_sectionLayout == null)
            _sectionLayout = GetComponent<UnityEngine.UI.VerticalLayoutGroup>();

        // The grid can impose a minimum width on its parents; measure the viewport instead.
        float width;

        if (_scroll != null && _scroll.viewport != null)
        {
            width = _scroll.viewport.rect.width;
            int horizontalPadding;

            if (_sectionLayout != null)
            {
                horizontalPadding = _sectionLayout.padding.horizontal;
            }
            else
            {
                horizontalPadding = 0;
            }

            width -= horizontalPadding;
        }
        else
        {
            width = ((RectTransform)_slotsParent).rect.width;
        }

        if (Mathf.Approximately(width, _lastWidth))
            return;

        _lastWidth = width;
        float available = width - _grid.padding.horizontal;
        int columns = Mathf.Clamp(Mathf.FloorToInt((available + _grid.spacing.x) / (minimumCellWidth + _grid.spacing.x)), minimumColumns, maximumColumns);
        _grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
        _grid.constraintCount = columns;
        _grid.cellSize = new Vector2(Mathf.Min(maximumCellWidth, (available - ((columns - 1) * _grid.spacing.x)) / columns), cellHeight);
        _grid.childAlignment = TextAnchor.UpperCenter;

        if (_scroll != null)
        {
            // Reflow first or ScrollRect may clamp against the previous orientation's row height.
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
