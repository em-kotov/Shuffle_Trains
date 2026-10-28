using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class LanguageFlagGraphic : UnityEngine.UI.MaskableGraphic
{
    [SerializeField]
    [FormerlySerializedAs("Code")]
    private string _code = "ru";

    public string Code
    {
        get => _code;
        set => _code = value;
    }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        Color red = new Color32(224, 35, 58, 255);
        Color white = new Color32(255, 253, 247, 255);

        if (Code == "ru")
        {
            Quad(mesh, white, 0, 2f / 3, 1.5f, 1);
            Quad(mesh, new Color32(39, 83, 163, 255), 0, 1f / 3, 1.5f, 2f / 3);
            Quad(mesh, red, 0, 0, 1.5f, 1f / 3);
        }
        else if (Code == "en")
        {
            Quad(mesh, new Color32(37, 61, 121, 255), 0, 0, 1.5f, 1);
            Polygon(mesh, white, new(0, 0), new(0.2f, 0), new(1.5f, 0.86f), new(1.5f, 1), new(1.3f, 1), new(0, 0.14f));
            Polygon(mesh, white, new(0, 1), new(0.2f, 1), new(1.5f, 0.14f), new(1.5f, 0), new(1.3f, 0), new(0, 0.86f));
            Polygon(mesh, red, new(0, 0), new(0.08f, 0), new(1.5f, 0.94f), new(1.5f, 1), new(1.42f, 1), new(0, 0.06f));
            Polygon(mesh, red, new(0, 1), new(0.08f, 1), new(1.5f, 0.06f), new(1.5f, 0), new(1.42f, 0), new(0, 0.94f));
            Quad(mesh, white, 0.59f, 0, 0.91f, 1);
            Quad(mesh, white, 0, 0.34f, 1.5f, 0.66f);
            Quad(mesh, red, 0.66f, 0, 0.84f, 1);
            Quad(mesh, red, 0, 0.41f, 1.5f, 0.59f);
        }
        else if (Code == "tr")
        {
            Quad(mesh, red, 0, 0, 1.5f, 1);
            Disc(mesh, white, new Vector2(0.55f, 0.5f), 0.25f);
            Disc(mesh, red, new Vector2(0.615f, 0.5f), 0.20f);
            Vector2 center = new(0.91f, 0.5f);

            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI / 5;
                float b = (i + 1) * Mathf.PI / 5;
                float firstRadius;

                if (i % 2 == 0)
                {
                    firstRadius = 0.125f;
                }
                else
                {
                    firstRadius = 0.052f;
                }

                float secondRadius;

                if (i % 2 == 0)
                {
                    secondRadius = 0.052f;
                }
                else
                {
                    secondRadius = 0.125f;
                }

                Polygon(
                    mesh,
                    white,
                    center,
                    center + (new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * firstRadius),
                    center + (new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * secondRadius));
            }
        }
    }

    private void Quad(UnityEngine.UI.VertexHelper mesh, Color tint, float x0, float y0, float x1, float y1)
        => Polygon(mesh, tint, new(x0, y0), new(x1, y0), new(x1, y1), new(x0, y1));

    private void Disc(UnityEngine.UI.VertexHelper mesh, Color tint, Vector2 center, float radius)
    {
        var points = new Vector2[64];

        for (int i = 0; i < points.Length; i++)
        {
            float angle = i * Mathf.PI * 2 / points.Length;
            points[i] = center + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }

        Polygon(mesh, tint, points);
    }

    private void Polygon(UnityEngine.UI.VertexHelper mesh, Color tint, params Vector2[] points)
    {
        Rect rect = GetPixelAdjustedRect();
        int first = mesh.currentVertCount;

        foreach (Vector2 p in points)
            mesh.AddVert(new Vector2(rect.xMin + (p.x * rect.width / 1.5f), rect.yMin + (p.y * rect.height)), tint * color, Vector2.zero);

        for (int i = 1; i < points.Length - 1; i++)
            mesh.AddTriangle(first, first + i, first + i + 1);
    }
}
