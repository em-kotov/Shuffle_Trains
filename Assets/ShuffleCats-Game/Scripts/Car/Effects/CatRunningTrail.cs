using UnityEngine;
using UnityEngine.Rendering;

// Runs after SpriteFacingLock so the smoke always fits the visible body.
[DefaultExecutionOrder(200)]
public sealed class CatRunningTrail : MonoBehaviour
{
    private const int TrailSegments = 32;
    private const int PathCapacity = 512;
    private const float PathSampleSpacing = 0.025f;
    private SpriteRenderer _cat;
    private Transform _head;
    private Mesh _mesh;
    private Material _material;
    private MeshRenderer _renderer;
    private Transform _visual;
    private readonly Vector3[] _vertices = new Vector3[(TrailSegments + 1) * 4];
    private readonly Vector2[] _uv = new Vector2[(TrailSegments + 1) * 4];
    private readonly Vector3[] _centers = new Vector3[TrailSegments + 1];
    private readonly Vector3[] _path = new Vector3[PathCapacity];
    private int _newestPathIndex;
    private int _pathCount;
    private Vector3 _previousPosition;
    private Vector3 _direction;
    private float _visibility;
    private bool _active;

    public void Initialize(SpriteRenderer cat, Transform head,
                           CatColor color, Vector3 parkingDirection)
    {
        _cat = cat;
        _head = head;
        _direction = parkingDirection.normalized;
        ResetPath(head.position);
        _previousPosition = head.position;
        _active = false;
        _visibility = 0f;
        if (_renderer == null)
        {
            Shader shader = Resources.Load<Shader>("CatRunningSmoke");
            if (shader == null)
            {
                Debug.LogWarning("Cat running smoke shader is missing.", this);
                enabled = false;
                return;
            }

            GameObject visual = new GameObject("Running trail lines", typeof(MeshFilter), typeof(MeshRenderer));
            visual.layer = cat.gameObject.layer;
            _visual = visual.transform;
            _visual.SetParent(transform, false);
            _mesh = new Mesh { name = "Cat twin trail ribbon" };
            _mesh.MarkDynamic();
            _mesh.vertices = _vertices;
            int[] triangles = new int[TrailSegments * 12];
            for (int line = 0; line < 2; line++)
            {
              int vertexOffset = line * (TrailSegments + 1) * 2;
              for (int i = 0; i <= TrailSegments; i++)
              {
                float progress = (float)i / TrailSegments;
                _uv[vertexOffset + i * 2] = new Vector2(progress, 0);
                _uv[vertexOffset + i * 2 + 1] = new Vector2(progress, 1);
                if (i == TrailSegments) continue;
                int vertex = vertexOffset + i * 2;
                int index = (line * TrailSegments + i) * 6;
                triangles[index] = vertex;
                triangles[index + 1] = vertex + 1;
                triangles[index + 2] = vertex + 2;
                triangles[index + 3] = vertex + 2;
                triangles[index + 4] = vertex + 1;
                triangles[index + 5] = vertex + 3;
              }
            }
            _mesh.uv = _uv;
            _mesh.triangles = triangles;
            visual.GetComponent<MeshFilter>().sharedMesh = _mesh;
            _material = new Material(shader) { name = "Cat running lines" };
            _renderer = visual.GetComponent<MeshRenderer>();
            _renderer.sharedMaterial = _material;
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        Color tint = Color.Lerp(ColorHelper.GetColor(color), new Color(1f, 0.98f, 0.94f), 0.45f);
        _material.SetColor("_Color", tint);
        _renderer.enabled = false;
        enabled = true;
    }

    // Remain marked through sorting and the exit path, until the cat is disabled.
    public void Begin() => _active = true;

    private void LateUpdate()
    {
        if (_renderer == null || _cat == null || _cat.sprite == null || _head == null)
            return;

        Vector3 delta = _head.position - _previousPosition;
        _previousPosition = _head.position;
        Vector3 normal = _cat.transform.forward;
        Vector3 planarDelta = Vector3.ProjectOnPlane(delta, normal);
        // Retain the last heading at stations and while waiting for track capacity.
        if (planarDelta.sqrMagnitude > 0.000001f)
            _direction = planarDelta.normalized;

        // World-space positions keep the lines on the route actually travelled.
        // Distance sampling preserves the path while stopped and at different speeds.
        if (delta.sqrMagnitude > 100f)
            ResetPath(_head.position);
        if ((_head.position - _path[_newestPathIndex]).sqrMagnitude >= PathSampleSpacing * PathSampleSpacing)
        {
            _newestPathIndex = (_newestPathIndex + 1) % PathCapacity;
            _path[_newestPathIndex] = _head.position;
            _pathCount = Mathf.Min(_pathCount + 1, PathCapacity);
        }

        _visibility = Mathf.MoveTowards(_visibility, _active ? 1f : 0f, Time.deltaTime * 6f);
        _renderer.enabled = _visibility > 0.001f && _cat.enabled && _cat.gameObject.activeInHierarchy;
        if (!_renderer.enabled)
            return;

        Vector3 forward = Vector3.ProjectOnPlane(_direction, normal).normalized;
        Vector3 side = Vector3.Cross(normal, forward).normalized;

        // Project the sprite's own rectangle, rather than its world AABB: this
        // keeps the rear edge and full width correct for vertical and long cats.
        Vector3 extents = _cat.sprite.bounds.extents;
        Vector3 spriteX = _cat.transform.TransformVector(Vector3.right * extents.x);
        Vector3 spriteY = _cat.transform.TransformVector(Vector3.up * extents.y);
        float halfLength = Mathf.Abs(Vector3.Dot(spriteX, forward)) + Mathf.Abs(Vector3.Dot(spriteY, forward));
        float halfWidth = Mathf.Abs(Vector3.Dot(spriteX, side)) + Mathf.Abs(Vector3.Dot(spriteY, side));
        halfWidth = Mathf.Max(0.1f, halfWidth * 0.72f);
        // The hidden section under the body must not consume the visible length
        // or its fade, especially when a long cat travels horizontally.
        float visibleLength = halfWidth * 4f / (1.5f * 1.2f);
        float length = halfLength + visibleLength;
        Vector3 spriteOffset = _cat.bounds.center - _head.position;
        float roundingRadius = halfWidth * 2f;
        Vector3 anchorCorrection = _head.position - SampleRoundedPath(0f, roundingRadius);
        for (int i = 0; i <= TrailSegments; i++)
        {
            float progress = (float)i / TrailSegments;
            // Start underneath the current cat, not at an older rear-distance
            // sample. The sprite hides the overlap; corners cannot open a gap.
            float distance = progress * length;
            // Spatial rounding removes entry kinks without a time-based follow
            // delay. Pin the newest point exactly to the cat this frame.
            Vector3 pathPosition = SampleRoundedPath(distance, roundingRadius) +
                anchorCorrection * Mathf.Exp(-distance / roundingRadius);
            _centers[i] = _head.position + spriteOffset + Vector3.ProjectOnPlane(pathPosition - _head.position, normal);
        }

        // Separate narrow strips avoid folded UVs from painting two strokes on
        // one wide ribbon. Shared miter joins keep both edges connected at entry.
        float lineHalfWidth = halfWidth * 0.055f * 1.5f * 1.7f;
        for (int i = 0; i <= TrailSegments; i++)
        {
            Vector3 incoming = i > 0 ? (_centers[i - 1] - _centers[i]).normalized :
                (_centers[0] - _centers[1]).normalized;
            Vector3 outgoing = i < TrailSegments ? (_centers[i] - _centers[i + 1]).normalized : incoming;
            Vector3 incomingSide = Vector3.Cross(normal, incoming).normalized;
            Vector3 outgoingSide = Vector3.Cross(normal, outgoing).normalized;
            Vector3 join = (incomingSide + outgoingSide).normalized;
            if (join.sqrMagnitude < 0.001f) join = incomingSide;
            join /= Mathf.Max(0.5f, Vector3.Dot(join, incomingSide));
            for (int line = 0; line < 2; line++)
            {
                float offset = (line == 0 ? -1f : 1f) * halfWidth * 0.3f;
                int vertex = line * (TrailSegments + 1) * 2 + i * 2;
                float distance = (float)i / TrailSegments * length;
                float fadeProgress = Mathf.Clamp01((distance - halfLength) / visibleLength);
                _uv[vertex] = new Vector2(fadeProgress, 0f);
                _uv[vertex + 1] = new Vector2(fadeProgress, 1f);
                _vertices[vertex] = _visual.InverseTransformPoint(_centers[i] + join * (offset - lineHalfWidth));
                _vertices[vertex + 1] = _visual.InverseTransformPoint(_centers[i] + join * (offset + lineHalfWidth));
            }
        }
        _mesh.vertices = _vertices;
        _mesh.uv = _uv;
        _mesh.RecalculateBounds();
        _renderer.sortingLayerID = _cat.sortingLayerID;
        _renderer.sortingOrder = _cat.sortingOrder - 1;
        _material.SetFloat("_Opacity", _visibility);
    }

    private Vector3 SampleRoundedPath(float distance, float radius)
    {
        return (SamplePath(distance - radius) +
                SamplePath(distance - radius * 0.5f) * 4f +
                SamplePath(distance) * 6f +
                SamplePath(distance + radius * 0.5f) * 4f +
                SamplePath(distance + radius)) / 16f;
    }

    private void ResetPath(Vector3 position)
    {
        _newestPathIndex = 0;
        _pathCount = 1;
        _path[0] = position;
    }

    private Vector3 SamplePath(float distance)
    {
        if (distance <= 0f)
            return _head.position - _direction * distance;
        Vector3 newer = _head.position;
        Vector3 backward = -_direction;
        for (int i = 0; i < _pathCount; i++)
        {
            int index = (_newestPathIndex - i + PathCapacity) % PathCapacity;
            Vector3 older = _path[index];
            Vector3 segment = older - newer;
            float segmentLength = segment.magnitude;
            if (segmentLength > 0.00001f)
            {
                backward = segment / segmentLength;
                if (distance <= segmentLength)
                    return newer + backward * distance;
                distance -= segmentLength;
            }
            newer = older;
        }

        // Before enough movement has been recorded, extend the oldest heading.
        return newer + backward * distance;
    }

    private void OnDisable()
    {
        if (_renderer != null)
            _renderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
        if (_material != null) Destroy(_material);
        if (_visual != null) Destroy(_visual.gameObject);
    }
}
