using UnityEngine;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class CupGraphic : UnityEngine.UI.MaskableGraphic
{
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        Color brown = new Color32(148, 103, 64, 255);
        Color gold = new Color32(255, 209, 91, 255);
        Color light = new Color32(255, 244, 216, 255);
        Disc(mesh, new Vector2(0.5f, 0.46f), 0.46f, new Color32(92, 69, 55, 65));
        Disc(mesh, Vector2.one * 0.5f, 0.46f, new Color32(171, 138, 111, 255));
        Disc(mesh, Vector2.one * 0.5f, 0.42f, new Color32(255, 244, 216, 255));
        Ring(mesh, new Vector2(0.29f, 0.61f), 0.13f, 0.074f, brown);
        Ring(mesh, new Vector2(0.71f, 0.61f), 0.13f, 0.074f, brown);
        Ring(mesh, new Vector2(0.29f, 0.61f), 0.116f, 0.088f, gold);
        Ring(mesh, new Vector2(0.71f, 0.61f), 0.116f, 0.088f, gold);
        Polygon(
            mesh,
            brown,
            new Vector2(0.45f, 0.25f),
            new Vector2(0.55f, 0.25f),
            new Vector2(0.55f, 0.48f),
            new Vector2(0.45f, 0.48f));
        Polygon(
            mesh,
            gold,
            new Vector2(0.47f, 0.27f),
            new Vector2(0.53f, 0.27f),
            new Vector2(0.53f, 0.46f),
            new Vector2(0.47f, 0.46f));
        Polygon(
            mesh,
            brown,
            new Vector2(0.27f, 0.78f),
            new Vector2(0.73f, 0.78f),
            new Vector2(0.69f, 0.52f),
            new Vector2(0.6f, 0.42f),
            new Vector2(0.5f, 0.39f),
            new Vector2(0.4f, 0.42f),
            new Vector2(0.31f, 0.52f));
        Polygon(
            mesh,
            gold,
            new Vector2(0.29f, 0.76f),
            new Vector2(0.71f, 0.76f),
            new Vector2(0.67f, 0.53f),
            new Vector2(0.585f, 0.44f),
            new Vector2(0.5f, 0.415f),
            new Vector2(0.415f, 0.44f),
            new Vector2(0.33f, 0.53f));
        Polygon(
            mesh,
            light,
            new Vector2(0.33f, 0.73f),
            new Vector2(0.44f, 0.73f),
            new Vector2(0.44f, 0.5f),
            new Vector2(0.38f, 0.56f));
        Polygon(
            mesh,
            brown,
            new Vector2(0.33f, 0.31f),
            new Vector2(0.67f, 0.31f),
            new Vector2(0.7f, 0.2f),
            new Vector2(0.3f, 0.2f));
        Polygon(
            mesh,
            gold,
            new Vector2(0.346f, 0.29f),
            new Vector2(0.654f, 0.29f),
            new Vector2(0.674f, 0.22f),
            new Vector2(0.326f, 0.22f));
    }

    private Vector2 Point(Vector2 point)
    {
        Rect rect = GetPixelAdjustedRect();
        float side = Mathf.Min(rect.width, rect.height);
        return rect.center + ((point - (Vector2.one * 0.5f)) * side);
    }

    private void Polygon(UnityEngine.UI.VertexHelper mesh, Color tint, params Vector2[] points)
    {
        int first = mesh.currentVertCount;

        foreach (Vector2 point in points)
            mesh.AddVert(Point(point), tint * color, Vector2.zero);

        for (int i = 1; i < points.Length - 1; i++)
            mesh.AddTriangle(first, first + i, first + i + 1);
    }

    private void Disc(UnityEngine.UI.VertexHelper mesh, Vector2 center, float radius, Color tint)
    {
        const int segments = 48;
        var points = new Vector2[segments];

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            points[i] = center + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }

        Polygon(mesh, tint, points);
    }

    private void Ring(UnityEngine.UI.VertexHelper mesh, Vector2 center, float outer, float inner, Color tint)
    {
        for (int i = 0; i < 32; i++)
        {
            float a = i * Mathf.PI / 16, b = (i + 1) * Mathf.PI / 16;
            var start = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var end = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
            Polygon(
                mesh,
                tint,
                center + (start * inner),
                center + (start * outer),
                center + (end * outer),
                center + (end * inner));
        }
    }
}
