using UnityEngine;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class ClickFeedbackGraphic : UnityEngine.UI.Graphic
{
    private const float Duration = 0.4f;
    private const int Capacity = 12;
    private const int StrokeCount = 8;
    private static readonly Color Cream = new Color32(255, 248, 231, 255);
    private static readonly Color Pink = new Color32(239, 151, 163, 255);
    private readonly Burst[] _bursts = new Burst[Capacity];
    private int _nextBurst;

    private struct Burst
    {
        public Vector2 Position;
        public float StartedAt;
        public bool Active;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (Application.isBatchMode || FindObjectOfType<ClickFeedbackGraphic>() != null)
            return;

        var root = new GameObject("ClickFeedback", typeof(RectTransform), typeof(Canvas));
        DontDestroyOnLoad(root);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var graphic = new GameObject("ClickStrokes", typeof(RectTransform), typeof(ClickFeedbackGraphic));
        var rect = (RectTransform)graphic.transform;
        rect.SetParent(root.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    private void Update()
    {
        if (Application.isFocused)
        {
            // Unity synthesizes mouse input from touches; handling both would emit two bursts per tap.
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);

                    if (touch.phase == TouchPhase.Began)
                        Emit(touch.position);
                }
            }
            else if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                Emit(Input.mousePosition);
            }
        }

        bool changed = false;

        for (int i = 0; i < Capacity; i++)
        {
            if (_bursts[i].Active == false)
                continue;

            changed = true;

            if (Time.unscaledTime - _bursts[i].StartedAt >= Duration)
                _bursts[i].Active = false;
        }

        if (changed)
            SetVerticesDirty();
    }

    private void Emit(Vector2 screenPosition)
    {
        if (screenPosition.x < 0 || screenPosition.y < 0
            || screenPosition.x >= Screen.width || screenPosition.y >= Screen.height)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPosition, null, out Vector2 localPosition) == false)
            return;

        _bursts[_nextBurst] = new Burst
        {
            Position = localPosition,
            StartedAt = Time.unscaledTime,
            Active = true
        };
        _nextBurst = (_nextBurst + 1) % Capacity;
    }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();

        for (int i = 0; i < Capacity; i++)
        {
            if (_bursts[i].Active == false)
                continue;

            float progress = Mathf.Clamp01((Time.unscaledTime - _bursts[i].StartedAt) / Duration);
            DrawBurst(mesh, _bursts[i].Position, progress);
        }
    }

    private static void DrawBurst(UnityEngine.UI.VertexHelper mesh, Vector2 center, float progress)
    {
        float fade = 1 - progress;
        float expansion = 1 - fade * fade * fade;
        Color cream = Cream;
        Color pink = Pink;
        cream.a = pink.a = fade * 0.9f;
        float outerDistance = Mathf.Lerp(13, 42, expansion);
        float length = Mathf.Lerp(11, 1, progress);
        float width = Mathf.Lerp(2.8f, 1.4f, progress);

        for (int i = 0; i < StrokeCount; i++)
        {
            float angle = (15 + i * 360f / StrokeCount) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float lengthScale;

            if (i % 2 == 0)
            {
                lengthScale = 1;
            }
            else
            {
                lengthScale = 0.85f;
            }

            float tip = outerDistance * lengthScale;
            Vector2 from = center + direction * Mathf.Max(0, tip - length);
            Vector2 to = center + direction * tip;
            Color strokeColor;

            if (i % 2 == 0)
            {
                strokeColor = cream;
            }
            else
            {
                strokeColor = pink;
            }

            Stroke(mesh, from, to, width, strokeColor);
        }
    }

    private static void Stroke(UnityEngine.UI.VertexHelper mesh, Vector2 from, Vector2 to, float width, Color tint)
    {
        const int CapSegments = 6;
        const int ContourCount = (CapSegments + 1) * 2;
        int start = mesh.currentVertCount;
        float radius = width * 0.5f;
        float heading = Mathf.Atan2(to.y - from.y, to.x - from.x);
        Color transparent = tint;
        transparent.a = 0;
        mesh.AddVert((from + to) * 0.5f, tint, Vector2.zero);

        for (int cap = 0; cap < 2; cap++)
            for (int i = 0; i <= CapSegments; i++)
            {
                float capOffset;

                if (cap == 0)
                {
                    capOffset = -Mathf.PI * 0.5f;
                }
                else
                {
                    capOffset = Mathf.PI * 0.5f;
                }

                float angle = heading + capOffset + i * Mathf.PI / CapSegments;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 end;

                if (cap == 0)
                {
                    end = to;
                }
                else
                {
                    end = from;
                }

                mesh.AddVert(end + direction * radius, tint, Vector2.zero);
                mesh.AddVert(end + direction * (radius + 0.65f), transparent, Vector2.zero);
            }

        for (int i = 0; i < ContourCount; i++)
        {
            int current = start + 1 + i * 2;
            int next = start + 1 + (i + 1) % ContourCount * 2;
            mesh.AddTriangle(start, current, next);
            mesh.AddTriangle(current, current + 1, next + 1);
            mesh.AddTriangle(current, next + 1, next);
        }
    }
}
