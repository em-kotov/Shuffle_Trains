using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class LanguageCardGraphic : UnityEngine.UI.MaskableGraphic
{
    [SerializeField]
    [FormerlySerializedAs("Radius")]
    private float _radius = 8;

    [SerializeField]
    [FormerlySerializedAs("BorderWidth")]
    private float _borderWidth = 1.5f;

    [SerializeField]
    [FormerlySerializedAs("BorderColor")]
    private Color _borderColor = new Color32(190, 162, 122, 255);

    public float Radius
    {
        get => _radius;
        set => _radius = value;
    }

    public float BorderWidth
    {
        get => _borderWidth;
        set => _borderWidth = value;
    }

    public Color BorderColor
    {
        get => _borderColor;
        set => _borderColor = value;
    }

    public static LanguageCardGraphic WindowBackground(RectTransform parent, bool shadow = false)
    {
        const int cornerRadius = 20;
        const float borderWidth = 2.5f;

        if (parent == null)
            return null;

        var source = parent.GetComponent<UnityEngine.UI.Image>();

        if (source != null)
            source.enabled = false;

        var rect = parent.Find("RoundedCard") as RectTransform;

        if (rect == null)
        {
            rect = new GameObject("RoundedCard", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.SetAsFirstSibling();
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var card = rect.GetComponent<LanguageCardGraphic>();

        if (card == null)
            card = rect.gameObject.AddComponent<LanguageCardGraphic>();

        if (shadow)
        {
            card.color = new Color32(72, 52, 36, 75);
        }
        else
        {
            card.color = new Color32(252, 242, 220, 255);
        }

        card.Radius = cornerRadius;
        card.BorderWidth = borderWidth;

        if (shadow)
        {
            card.BorderColor = card.color;
        }
        else
        {
            card.BorderColor = new Color32(187, 157, 119, 255);
        }

        card.raycastTarget = false;
        card.SetVerticesDirty();
        return card;
    }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();

        if (BorderWidth <= 0)
        {
            // Overlapping shadow border/interior meshes would darken the centre twice.
            Rounded(mesh, rect, Radius, color);
            return;
        }

        Rounded(mesh, rect, Radius, BorderColor);
        rect.xMin += BorderWidth;
        rect.yMin += BorderWidth;
        rect.xMax -= BorderWidth;
        rect.yMax -= BorderWidth;
        Rounded(mesh, rect, Mathf.Max(0, Radius - BorderWidth), color);
    }

    private static void Rounded(UnityEngine.UI.VertexHelper mesh, Rect rect, float radius, Color tint)
    {
        radius = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * 0.5f);
        int first = mesh.currentVertCount;
        mesh.AddVert(rect.center, tint, Vector2.zero);
        const int segments = 10;

        for (int corner = 0; corner < 4; corner++)
        {
            float centerX;

            if (corner == 0 || corner == 3)
            {
                centerX = rect.xMax - radius;
            }
            else
            {
                centerX = rect.xMin + radius;
            }

            float centerY;

            if (corner < 2)
            {
                centerY = rect.yMax - radius;
            }
            else
            {
                centerY = rect.yMin + radius;
            }

            Vector2 center = new Vector2(centerX, centerY);

            for (int i = 0; i <= segments; i++)
            {
                float angle = ((corner * 90) + (i * 90f / segments)) * Mathf.Deg2Rad;
                mesh.AddVert(center + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius), tint, Vector2.zero);
            }
        }

        int count = 4 * (segments + 1);

        for (int i = 0; i < count; i++)
            mesh.AddTriangle(first, first + 1 + i, first + 1 + ((i + 1) % count));
    }
}
