using UnityEngine;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class LanguageCardGraphic : UnityEngine.UI.MaskableGraphic
{
    public float Radius = 8;
    public float BorderWidth = 1.5f;
    public Color BorderColor = new Color32(190, 162, 122, 255);

    // The settings, shop, leaderboard and result windows share one frame at every size.
    public static LanguageCardGraphic WindowBackground(RectTransform parent, bool shadow = false)
    {
        if (parent == null) return null;
        var source = parent.GetComponent<UnityEngine.UI.Image>();
        if (source != null) source.enabled = false;
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
        if (card == null) card = rect.gameObject.AddComponent<LanguageCardGraphic>();
        card.color = shadow ? new Color32(72, 52, 36, 75) : new Color32(252, 242, 220, 255);
        card.Radius = 20;
        card.BorderWidth = 2.5f;
        card.BorderColor = shadow ? card.color : new Color32(187, 157, 119, 255);
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
            // A translucent, borderless shadow needs one fill; overlapping
            // border and interior meshes would make its centre twice as dark.
            Rounded(mesh, rect, Radius, color);
            return;
        }
        Rounded(mesh, rect, Radius, BorderColor);
        rect.xMin += BorderWidth; rect.yMin += BorderWidth;
        rect.xMax -= BorderWidth; rect.yMax -= BorderWidth;
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
            Vector2 center = new Vector2(corner == 0 || corner == 3 ? rect.xMax - radius : rect.xMin + radius,
                corner < 2 ? rect.yMax - radius : rect.yMin + radius);
            for (int i = 0; i <= segments; i++)
            {
                float angle = (corner * 90 + i * 90f / segments) * Mathf.Deg2Rad;
                mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
            }
        }
        int count = 4 * (segments + 1);
        for (int i = 0; i < count; i++) mesh.AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % count);
    }
}
