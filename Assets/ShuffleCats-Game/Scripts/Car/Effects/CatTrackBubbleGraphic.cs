using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class CatTrackBubbleGraphic : UnityEngine.UI.MaskableGraphic
{
    private const int CurveSteps = 12;
    private static readonly Color Outline = new Color32(173, 141, 123, 255);
    private static readonly Color WaitingInk = new Color32(126, 109, 140, 255);
    private static readonly Color ReadyInk = new Color32(60, 125, 112, 255);
    private static readonly Color BumpInk = new Color32(207, 116, 116, 255);
    private static readonly Color HeartEdge = new Color32(197, 100, 123, 255);
    private static readonly Color HeartPink = new Color32(244, 144, 161, 255);
    private static readonly Color HoneyEdge = new Color32(188, 143, 88, 255);
    private static readonly Color Honey = new Color32(255, 216, 133, 255);
    private static readonly Vector2[] Thumb =
    {
        new Vector2(52, 42), new Vector2(60, 38), new Vector2(79, 38),
        new Vector2(85, 43), new Vector2(89, 61), new Vector2(85, 67),
        new Vector2(72, 67), new Vector2(74, 79), new Vector2(71, 85),
        new Vector2(66, 85), new Vector2(62, 78), new Vector2(60, 69),
        new Vector2(52, 60),
    };

    private static readonly Vector2[] Cuff =
    {
        new Vector2(44, 38), new Vector2(55, 38),
        new Vector2(55, 62), new Vector2(44, 62),
    };

    private static readonly Vector2[] BumpZigzag =
    {
        new Vector2(-5f, 16f), new Vector2(7f, 9f), new Vector2(-7f, 1f),
        new Vector2(5f, -6f), new Vector2(-4f, -16f),
    };

    private readonly Vector2[] _outline = new Vector2[CurveSteps * 7];
    private readonly Vector2[] _ink = new Vector2[81];
    private int _outlineCount;
    private CatTrackIndicator.IndicatorState _state;
    private CatTrackIndicator.ExitReaction _reaction;
    private float _animation;

    public void SetAnimation(
        CatTrackIndicator.IndicatorState state,
        float animation,
        CatTrackIndicator.ExitReaction reaction = CatTrackIndicator.ExitReaction.Heart)
    {
        const float minimumAnimationChange = 0.04f;

        if (_state == state && _reaction == reaction && (state == CatTrackIndicator.IndicatorState.Ready ||
            Mathf.Abs(_animation - animation) < minimumAnimationChange))
            return;

        _state = state;
        _reaction = reaction;
        _animation = animation;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        const float outlineWidth = 3.4f;
        const int highlightPointCount = 15;
        const float highlightSegmentCount = 14f;
        const float highlightStartAngle = 0.65f;
        const float highlightEndAngle = 2.45f;
        const float highlightStrokeWidth = 2.8f;

        mesh.Clear();
        bool ready;

        if (_state == CatTrackIndicator.IndicatorState.Ready)
        {
            ready = true;
        }
        else
        {
            ready = false;
        }

        _outlineCount = 0;
        Curve(new Vector2(34, 34), new Vector2(17, 43), new Vector2(16, 64), new Vector2(29, 78));
        Curve(new Vector2(29, 78), new Vector2(44, 94), new Vector2(83, 95), new Vector2(103, 83));
        Curve(new Vector2(103, 83), new Vector2(123, 73), new Vector2(123, 50), new Vector2(108, 37));
        Curve(new Vector2(108, 37), new Vector2(94, 24), new Vector2(69, 26), new Vector2(55, 31));
        Curve(new Vector2(55, 31), new Vector2(45, 24), new Vector2(33, 13), new Vector2(26, 10));
        Curve(new Vector2(26, 10), new Vector2(24, 9), new Vector2(23, 11), new Vector2(25, 15));
        Curve(new Vector2(25, 15), new Vector2(29, 22), new Vector2(32, 28), new Vector2(34, 34));

        Fill(mesh, new Vector2(0, -3f), new Color32(110, 86, 76, 20), new Color32(110, 86, 76, 20));
        Stroke(mesh, _outline, _outlineCount, true, 6f, new Color32(110, 86, 76, 12), new Vector2(0, -3f));
        Color bottom;

        if (ready)
        {
            bottom = new Color32(222, 243, 218, 255);
        }
        else
        {
            bottom = new Color32(255, 224, 198, 255);
        }

        if (_state == CatTrackIndicator.IndicatorState.Bumped)
            bottom = new Color32(255, 225, 222, 255);
        else if (_state == CatTrackIndicator.IndicatorState.Exiting)
            bottom = new Color32(255, 233, 218, 255);

        Fill(mesh, Vector2.zero, bottom, new Color32(255, 253, 241, 255));
        Stroke(mesh, _outline, _outlineCount, true, outlineWidth, Outline, Vector2.zero);

        for (int i = 0; i < highlightPointCount; i++)
        {
            float angle = Mathf.Lerp(highlightStartAngle, highlightEndAngle, i / highlightSegmentCount);
            _ink[i] = new Vector2(70f + (Mathf.Cos(angle) * 37f), 58f + (Mathf.Sin(angle) * 25f));
        }

        Stroke(mesh, _ink, highlightPointCount, false, highlightStrokeWidth, new Color(1f, 1f, 1f, 0.8f), Vector2.zero);

        if (ready)
        {
            _ink[0] = new Vector2(70, 74);
            _ink[1] = new Vector2(68, 53);
            Stroke(mesh, _ink, 2, false, 8f, ReadyInk, Vector2.zero);
            Disc(mesh, new Vector2(67.5f, 42), 4.5f, ReadyInk);
        }
        else if (_state == CatTrackIndicator.IndicatorState.Exiting)
        {
            DrawExitReaction(mesh);
        }
        else if (_state == CatTrackIndicator.IndicatorState.Bumped)
        {
            const int zigzagCount = 2;
            const float swayFrequency = 3f;
            const float swayPhaseOffset = 1.7f;
            const float swayAmplitude = 1.1f;
            const float zigzagStrokeWidth = 4.2f;
            const float zigzagJoinRadius = 2.1f;

            for (int line = 0; line < zigzagCount; line++)
            {
                Vector2 center = new Vector2(52f + (line * 31f), 57f);
                float sway = Mathf.Sin((_animation * swayFrequency) + (line * swayPhaseOffset)) * swayAmplitude;

                for (int i = 0; i < BumpZigzag.Length; i++)
                    _ink[i] = center + BumpZigzag[i] + new Vector2(sway, 0f);

                Stroke(mesh, _ink, BumpZigzag.Length, false, zigzagStrokeWidth, BumpInk, Vector2.zero);

                for (int i = 1; i < BumpZigzag.Length - 1; i++)
                    Disc(mesh, _ink[i], zigzagJoinRadius, BumpInk);
            }
        }
        else if (_state == CatTrackIndicator.IndicatorState.Waiting)
        {
            const int dotCount = 3;
            const float pulseFrequency = 2f;
            const float minimumDotOpacity = 0.65f;

            for (int i = 0; i < dotCount; i++)
            {
                float pulse = Mathf.Max(0f, Mathf.Sin((_animation * pulseFrequency) - (i * Mathf.PI * 2f / dotCount)));
                Color tint = WaitingInk;
                tint.a = Mathf.Lerp(minimumDotOpacity, 1f, pulse);
                Disc(mesh, new Vector2(50f + (i * 18f), 55f + (pulse * 4f)), 4.5f + pulse, tint);
            }
        }
        else
        {
            const float breathingFrequency = 1.2f;
            const float breathingAmplitude = 0.08f;
            const float coilTurnsInHalfTurns = 7.4f;
            const float coilRotationAmplitude = 0.3f;
            const float coilTiltFrequency = 0.6f;
            const float coilTiltAmplitude = 0.65f;
            const float coilStrokeWidth = 3.2f;

            float breathe = 1f + (Mathf.Sin(_animation * breathingFrequency) * breathingAmplitude);
            Vector2 center = new Vector2(67f, 57f);

            for (int i = 0; i < _ink.Length; i++)
            {
                float t = i / (float)(_ink.Length - 1);
                float angle = (t * Mathf.PI * coilTurnsInHalfTurns) + (Mathf.Sin(_animation) * coilRotationAmplitude);
                float tilt = Mathf.Sin((t * 9f) + (_animation * coilTiltFrequency)) * coilTiltAmplitude;
                float x = Mathf.Sin(angle) * (14f + (Mathf.Sin((t * 8f) + _animation) * 3f));
                float y = Mathf.Cos(angle) * (11f + (Mathf.Cos((t * 9f) - _animation) * 2f));
                _ink[i] = new Vector2(
                    44f + (t * 46f) + (x * Mathf.Cos(tilt)) - (y * Mathf.Sin(tilt)),
                    57f + (x * Mathf.Sin(tilt)) + (y * Mathf.Cos(tilt)) + (Mathf.Sin((t * 11f) + _animation) * 2f));
                _ink[i] = center + ((_ink[i] - center) * breathe);
            }

            Stroke(mesh, _ink, _ink.Length, false, coilStrokeWidth, WaitingInk, Vector2.zero);
        }
    }

    private void DrawExitReaction(VertexHelper mesh)
    {
        const float animationFrequency = 1.8f;
        const float pulseAmplitude = 0.055f;
        const float bounceAmplitude = 1.2f;
        const float bounceOffset = 3f;

        float pulse = 1f + (Mathf.Sin(_animation * animationFrequency) * pulseAmplitude);

        if (_reaction == CatTrackIndicator.ExitReaction.Heart)
        {
            Heart(mesh, new Vector2(69, 60), 22f * pulse, 2.4f);
            _ink[0] = new Vector2(55, 67);
            _ink[1] = new Vector2(57, 70);
            _ink[2] = new Vector2(61, 71);
            Stroke(mesh, _ink, 3, false, 2.5f, new Color32(255, 231, 220, 230), Vector2.zero);
        }
        else if (_reaction == CatTrackIndicator.ExitReaction.HeartEyes)
        {
            Vector2 center = new Vector2(69, 58);
            Disc(mesh, center, 23f, HoneyEdge, 40);
            Disc(mesh, center, 21f, Honey, 40);
            Heart(mesh, new Vector2(59.5f, 64), 8f * pulse, 1.2f);
            Heart(mesh, new Vector2(78.5f, 64), 8f * pulse, 1.2f);

            for (int i = 0; i < 17; i++)
            {
                float t = i / 16f;
                float s = 1f - t;
                _ink[i] = (s * s * new Vector2(60, 52)) + (2f * s * t * new Vector2(69, 42))
                    + (t * t * new Vector2(78, 52));
            }

            Stroke(mesh, _ink, 17, false, 3.4f, new Color32(151, 106, 78, 255), Vector2.zero);
        }
        else
        {
            Vector2 bounce = new Vector2(0f, (Mathf.Sin(_animation * animationFrequency) * bounceAmplitude) - bounceOffset);
            int count = RoundedShape(Thumb, bounce);
            FillShape(
                mesh,
                _ink,
                count,
                new Vector2(68, 57) + bounce,
                new Color32(243, 181, 118, 255),
                new Color32(255, 229, 175, 255));
            Stroke(mesh, _ink, count, true, 2.4f, HoneyEdge, Vector2.zero);
            count = RoundedShape(Cuff, bounce);
            FillShape(
                mesh,
                _ink,
                count,
                new Vector2(49.5f, 50) + bounce,
                new Color32(235, 195, 80, 255),
                new Color32(255, 232, 145, 255));
            Stroke(mesh, _ink, count, true, 2.2f, new Color32(195, 159, 78, 255), Vector2.zero);

            for (int i = 0; i < 2; i++)
            {
                _ink[0] = new Vector2(79, 49 + (i * 8)) + bounce;
                _ink[1] = new Vector2(85, 49 + (i * 8)) + bounce;
                Stroke(mesh, _ink, 2, false, 1.5f, HoneyEdge, Vector2.zero);
            }
        }
    }

    private void Heart(VertexHelper mesh, Vector2 center, float halfWidth, float border)
    {
        const int count = 48;

        for (int i = 0; i < count; i++)
        {
            float t = i * Mathf.PI * 2f / count;
            float sine = Mathf.Sin(t);
            _ink[i] = center + (new Vector2(
                16f * sine * sine * sine,
                (13f * Mathf.Cos(t)) - (5f * Mathf.Cos(2f * t)) - (2f * Mathf.Cos(3f * t)) - Mathf.Cos(4f * t))
                * (halfWidth / 16f));
        }

        FillShape(mesh, _ink, count, center, HeartEdge, HeartPink);
        Stroke(mesh, _ink, count, true, border, HeartEdge, Vector2.zero);
    }

    private int RoundedShape(Vector2[] corners, Vector2 offset)
    {
        int count = 0;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 a = Vector2.Lerp(corners[i], corners[(i + corners.Length - 1) % corners.Length], 0.2f);
            Vector2 b = Vector2.Lerp(corners[i], corners[(i + 1) % corners.Length], 0.2f);

            for (int step = 0; step < 4; step++)
            {
                float t = step / 3f;
                float s = 1f - t;
                _ink[count++] = (s * s * a) + (2f * s * t * corners[i]) + (t * t * b) + offset;
            }
        }

        return count;
    }

    private void FillShape(
        VertexHelper mesh,
        Vector2[] points,
        int count,
        Vector2 center,
        Color bottom,
        Color top)
    {
        float minY = points[0].y, maxY = minY;

        for (int i = 1; i < count; i++)
        {
            minY = Mathf.Min(minY, points[i].y);
            maxY = Mathf.Max(maxY, points[i].y);
        }

        int first = mesh.currentVertCount;
        AddVertex(mesh, center, Color.Lerp(bottom, top, Mathf.InverseLerp(minY, maxY, center.y)));

        for (int i = 0; i < count; i++)
            AddVertex(mesh, points[i], Color.Lerp(bottom, top, Mathf.InverseLerp(minY, maxY, points[i].y)));

        for (int i = 0; i < count; i++)
            mesh.AddTriangle(first, first + 1 + i, first + 1 + ((i + 1) % count));
    }

    private void Curve(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        for (int i = 0; i < CurveSteps; i++)
        {
            float t = i / (float)CurveSteps;
            float s = 1f - t;
            _outline[_outlineCount++] = (s * s * s * a) + (3f * s * s * t * b) +
                (3f * s * t * t * c) + (t * t * t * d);
        }
    }

    private void Fill(VertexHelper mesh, Vector2 offset, Color bottom, Color top)
    {
        const float centerGradientBlend = 0.58f;
        const int gradientBottom = 15;
        const int gradientTop = 85;

        int first = mesh.currentVertCount;
        AddVertex(mesh, new Vector2(65, 54) + offset, Color.Lerp(bottom, top, centerGradientBlend));

        for (int i = 0; i < _outlineCount; i++)
            AddVertex(mesh, _outline[i] + offset, Color.Lerp(bottom, top, Mathf.InverseLerp(gradientBottom, gradientTop, _outline[i].y)));

        for (int i = 0; i < _outlineCount; i++)
            mesh.AddTriangle(first, first + 1 + i, first + 1 + ((i + 1) % _outlineCount));
    }

    private void Stroke(
        VertexHelper mesh,
        Vector2[] points,
        int count,
        bool closed,
        float thickness,
        Color tint,
        Vector2 offset)
    {
        const float minimumSegmentSqrMagnitude = 0.0001f;
        const float minimumJoinDotProduct = 0.5f;
        const int verticesPerPoint = 4;
        const int bandsPerSegment = 3;

        const float fringe = 0.8f;
        int first = mesh.currentVertCount;
        Color clear = new Color(tint.r, tint.g, tint.b, 0f);

        for (int i = 0; i < count; i++)
        {
            int previousIndex;

            if (i == 0)
            {
                if (closed)
                {
                    previousIndex = count - 1;
                }
                else
                {
                    previousIndex = 0;
                }
            }
            else
            {
                previousIndex = i - 1;
            }

            Vector2 before = points[i] - points[previousIndex];
            int nextIndex;

            if (i == count - 1)
            {
                if (closed)
                {
                    nextIndex = 0;
                }
                else
                {
                    nextIndex = i;
                }
            }
            else
            {
                nextIndex = i + 1;
            }

            Vector2 after = points[nextIndex] - points[i];

            if (before.sqrMagnitude < minimumSegmentSqrMagnitude)
                before = after;

            if (after.sqrMagnitude < minimumSegmentSqrMagnitude)
                after = before;

            Vector2 normalA = new Vector2(-before.y, before.x).normalized;
            Vector2 normalB = new Vector2(-after.y, after.x).normalized;
            Vector2 normal = (normalA + normalB).normalized;
            normal /= Mathf.Max(minimumJoinDotProduct, Vector2.Dot(normal, normalA));
            Vector2 p = points[i] + offset;
            AddVertex(mesh, p - (normal * ((thickness * 0.5f) + fringe)), clear);
            AddVertex(mesh, p - (normal * thickness * 0.5f), tint);
            AddVertex(mesh, p + (normal * thickness * 0.5f), tint);
            AddVertex(mesh, p + (normal * ((thickness * 0.5f) + fringe)), clear);
        }

        int segments;

        if (closed)
        {
            segments = count;
        }
        else
        {
            segments = count - 1;
        }

        for (int i = 0; i < segments; i++)
        {
            int a = first + (i * verticesPerPoint);
            int b = first + (((i + 1) % count) * verticesPerPoint);

            for (int band = 0; band < bandsPerSegment; band++)
            {
                mesh.AddTriangle(a + band, b + band, b + band + 1);
                mesh.AddTriangle(a + band, b + band + 1, a + band + 1);
            }
        }

        if (closed == false)
        {
            Disc(mesh, points[0] + offset, thickness * 0.5f, tint);
            Disc(mesh, points[count - 1] + offset, thickness * 0.5f, tint);
        }
    }

    private void Disc(VertexHelper mesh, Vector2 center, float radius, Color tint, int segments = 16)
    {
        int first = mesh.currentVertCount;
        AddVertex(mesh, center, tint);

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            AddVertex(mesh, center + (direction * radius), tint);
            AddVertex(mesh, center + (direction * (radius + 0.8f)), new Color(tint.r, tint.g, tint.b, 0));
        }

        for (int i = 0; i < segments; i++)
        {
            int a = first + 1 + (i * 2);
            int b = first + 1 + (((i + 1) % segments) * 2);
            mesh.AddTriangle(first, a, b);
            mesh.AddTriangle(a, a + 1, b + 1);
            mesh.AddTriangle(a, b + 1, b);
        }
    }

    private void AddVertex(VertexHelper mesh, Vector2 point, Color tint)
    {
        const float referenceWidth = 128f;
        const float referenceHeight = 100f;

        Rect rect = rectTransform.rect;
        mesh.AddVert(
            new Vector3(
                rect.xMin + (point.x * rect.width / referenceWidth),
                rect.yMin + (point.y * rect.height / referenceHeight),
                0f),
            tint,
            Vector2.zero);
    }
}
