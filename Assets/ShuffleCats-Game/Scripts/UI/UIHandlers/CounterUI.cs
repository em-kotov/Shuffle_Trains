using TMPro;
using UnityEngine;

public class CounterUI : UIPanel
{
    public override bool BlocksGameplay => false;
    [SerializeField] private TextMeshProUGUI _currentCarsText;
    [SerializeField] private TextMeshProUGUI _maxCarsText;

    public void ShowCount(int current, int max)
    {
        _currentCarsText.text = current.ToString();
        _maxCarsText.text = $"/{max}";

        // Center the pair as a single x/x readout, including two-digit counts.
        float currentWidth = _currentCarsText.preferredWidth;
        float maxWidth = _maxCarsText.preferredWidth;
        float left = -(currentWidth + maxWidth) * 0.5f;
        PositionCount(_currentCarsText.rectTransform, left, currentWidth);
        PositionCount(_maxCarsText.rectTransform, left + currentWidth, maxWidth);
    }

    private static void PositionCount(RectTransform rect, float left, float width)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(left, 0f);
        rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
    }
}
