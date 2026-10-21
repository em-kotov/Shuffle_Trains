using UnityEngine;

// A scalable outlined glove, with its fingertip at the RectTransform origin.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class TutorialHandGraphic : UnityEngine.UI.MaskableGraphic
{
    private static readonly Color Outline = new Color32(79, 60, 51, 255);
    private static readonly Color Fill = new Color32(255, 253, 244, 255);

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        // Draw all dark silhouettes first, then their connected cream interiors.
        Glove(mesh, new Vector2(2, -3), 3, new Color32(0, 0, 0, 65));
        Glove(mesh, Vector2.zero, 2.4f, Outline);
        Glove(mesh, Vector2.zero, 0, Fill);
        Capsule(mesh, new Vector2(11, -27), new Vector2(11, -40), 1, Outline);
        Capsule(mesh, new Vector2(21, -31), new Vector2(21, -41), 1, Outline);
        Capsule(mesh, new Vector2(30, -36), new Vector2(30, -43), 1, Outline);
        Capsule(mesh, new Vector2(3, -58), new Vector2(28, -58), 1.2f, Outline);
    }

    private static void Glove(UnityEngine.UI.VertexHelper mesh, Vector2 offset, float stroke, Color tint)
    {
        Capsule(mesh, offset + new Vector2(0, -7), offset + new Vector2(0, -39), 6 + stroke, tint);
        Capsule(mesh, offset + new Vector2(11, -27), offset + new Vector2(11, -45), 6 + stroke, tint);
        Capsule(mesh, offset + new Vector2(21, -31), offset + new Vector2(21, -46), 6 + stroke, tint);
        Capsule(mesh, offset + new Vector2(30, -36), offset + new Vector2(28, -48), 6 + stroke, tint);
        Capsule(mesh, offset + new Vector2(4, -40), offset + new Vector2(23, -46), 13 + stroke, tint);
        Capsule(mesh, offset + new Vector2(-11, -33), offset + new Vector2(5, -50), 6 + stroke, tint);
        Capsule(mesh, offset + new Vector2(6, -55), offset + new Vector2(25, -55), 8 + stroke, tint);
    }

    private static void Capsule(UnityEngine.UI.VertexHelper mesh, Vector2 from, Vector2 to, float radius, Color tint)
    {
        int start = mesh.currentVertCount;
        mesh.AddVert((from + to) * 0.5f, tint, Vector2.zero);
        float direction = Mathf.Atan2(to.y - from.y, to.x - from.x);
        const int half = 12;
        for (int end = 0; end < 2; end++)
        for (int i = 0; i <= half; i++)
        {
            float angle = direction + (end == 0 ? -Mathf.PI * 0.5f : Mathf.PI * 0.5f) + i * Mathf.PI / half;
            mesh.AddVert((end == 0 ? to : from) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
        }
        int count = (half + 1) * 2;
        for (int i = 0; i < count; i++) mesh.AddTriangle(start, start + i + 1, start + (i + 1) % count + 1);
    }
}
