using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class TutorialSpotlightGraphic : UnityEngine.UI.MaskableGraphic
{
    private const float Radius = 16;
    private static readonly Color Shade = new Color32(20, 24, 37, 190);
    private static readonly Color Rim = new Color32(255, 221, 120, 255);
    private readonly List<Rect> _holes = new();
    private readonly List<float> _columns = new();
    private readonly List<Rect> _bands = new();

    public static bool Contains(Rect hole, Vector2 point)
    {
        if (hole.Contains(point) == false)
            return false;

        float radius = Mathf.Min(Radius, Mathf.Min(hole.width, hole.height) * 0.5f);
        Vector2 nearest = new Vector2(
            Mathf.Clamp(point.x, hole.xMin + radius, hole.xMax - radius),
            Mathf.Clamp(point.y, hole.yMin + radius, hole.yMax - radius));
        return (point - nearest).sqrMagnitude <= radius * radius;
    }

    public void SetHoles(IReadOnlyList<Rect> holes)
    {
        _holes.Clear();

        foreach (Rect hole in holes)
            _holes.Add(hole);

        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();

        if (_holes.Count == 0)
            return;

        Rect all = rectTransform.rect;
        _columns.Clear();
        _columns.Add(all.xMin);
        _columns.Add(all.xMax);

        foreach (Rect hole in _holes)
        {
            _columns.Add(Mathf.Clamp(hole.xMin, all.xMin, all.xMax));
            _columns.Add(Mathf.Clamp(hole.xMax, all.xMin, all.xMax));
        }

        _columns.Sort();

        // Partition overlapping focus areas so one spotlight's shade cannot cover another.
        for (int i = 1; i < _columns.Count; i++)
        {
            float left = _columns[i - 1], right = _columns[i];

            if (right <= left)
                continue;

            float middle = (left + right) * 0.5f;
            _bands.Clear();

            foreach (Rect hole in _holes)
            {
                if (middle > hole.xMin && middle < hole.xMax)
                    _bands.Add(hole);
            }

            _bands.Sort((a, b) => a.yMin.CompareTo(b.yMin));
            float bottom = all.yMin;

            foreach (Rect hole in _bands)
            {
                Quad(mesh, Rect.MinMaxRect(left, bottom, right, Mathf.Clamp(hole.yMin, all.yMin, all.yMax)));
                bottom = Mathf.Max(bottom, Mathf.Min(hole.yMax, all.yMax));
            }

            Quad(mesh, Rect.MinMaxRect(left, bottom, right, all.yMax));
        }

        // Draw all shade before the rim, or later corner triangles leave dark speckles on it.
        foreach (Rect hole in _holes)
            DrawHole(mesh, hole, false);

        foreach (Rect hole in _holes)
            DrawHole(mesh, hole, true);
    }

    private static void DrawHole(UnityEngine.UI.VertexHelper mesh, Rect hole, bool drawRim)
    {
        const float rimWidth = 2.5f;

        float radius = Mathf.Min(Radius, Mathf.Min(hole.width, hole.height) * 0.5f);
        const int segments = 10;
        Vector2 firstInner = default, firstOuter = default, previousInner = default, previousOuter = default;

        for (int corner = 0; corner < 4; corner++)
        {
            float signX;

            if (corner == 0 || corner == 3)
            {
                signX = 1;
            }
            else
            {
                signX = -1;
            }

            float signY;

            if (corner < 2)
            {
                signY = 1;
            }
            else
            {
                signY = -1;
            }

            Vector2 sign = new Vector2(signX, signY);
            Vector2 tip = hole.center + Vector2.Scale(hole.size * 0.5f, sign);
            Vector2 center = tip - (sign * radius);

            for (int i = 0; i <= segments; i++)
            {
                float angle = ((corner * 90) + (i * 90f / segments)) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 inner = center + (direction * radius);
                Vector2 outer = center + (direction * (radius + rimWidth));

                if (drawRim == false)
                {
                    if (i > 0)
                        Triangle(mesh, tip, previousInner, inner, Shade);
                }
                else if (corner != 0 || i != 0)
                {
                    Strip(mesh, previousInner, previousOuter, inner, outer);
                }
                else
                {
                    firstInner = inner;
                    firstOuter = outer;
                }

                previousInner = inner;
                previousOuter = outer;
            }
        }

        if (drawRim)
            Strip(mesh, previousInner, previousOuter, firstInner, firstOuter);
    }

    private static void Quad(UnityEngine.UI.VertexHelper mesh, Rect r)
    {
        if (r.width <= 0 || r.height <= 0)
            return;

        Triangle(mesh, r.min, new Vector2(r.xMin, r.yMax), r.max, Shade);
        Triangle(mesh, r.min, r.max, new Vector2(r.xMax, r.yMin), Shade);
    }

    private static void Strip(UnityEngine.UI.VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        Triangle(mesh, a, b, d, Rim);
        Triangle(mesh, a, d, c, Rim);
    }

    private static void Triangle(UnityEngine.UI.VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        int start = mesh.currentVertCount;
        mesh.AddVert(a, color, Vector2.zero);
        mesh.AddVert(b, color, Vector2.zero);
        mesh.AddVert(c, color, Vector2.zero);
        mesh.AddTriangle(start, start + 1, start + 2);
    }
}
