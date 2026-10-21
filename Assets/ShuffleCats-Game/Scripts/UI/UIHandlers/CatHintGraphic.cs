using UnityEngine;

// Project the actual sprite plane, including its breathing, rotation and flips.
// The shader outlines texture alpha, so transparent margins never become a box.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CatHintGraphic : UnityEngine.UI.MaskableGraphic
{
    private const float PaddingUnits = 7f;
    private static readonly int UVBoundsId = Shader.PropertyToID("_SpriteUVBounds");
    private static readonly int AnimationId = Shader.PropertyToID("_HintAge");
    private static readonly int GlowScaleId = Shader.PropertyToID("_GlowScale");
    private readonly Vector2[] _positions = new Vector2[4];
    private readonly Vector2[] _textureUV = new Vector2[4];
    private readonly Vector2[] _shapeUV = new Vector2[4];
    private Sprite _sprite;
    private Material _glow;
    private Vector2 _uvOrigin, _uvX, _uvY;
    private Rect _spriteRect;
    private bool _ready;

    public override Texture mainTexture => _sprite != null ? _sprite.texture : Texture2D.whiteTexture;

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
        Shader shader = Resources.Load<Shader>("CatHintGlow");
        if (shader != null)
        {
            _glow = new Material(shader) { name = "Cat hint silhouette glow" };
            material = _glow;
        }
    }

    public void Follow(SpriteRenderer cat, Camera camera, Canvas canvas, float age)
    {
        _ready = false;
        if (_glow == null || cat == null || cat.sprite == null || !cat.enabled)
        {
            SetVerticesDirty();
            return;
        }
        if (_sprite != cat.sprite) CacheSprite(cat.sprite);

        Vector3 bottomLeft = Project(cat, camera, _spriteRect.min);
        Vector3 bottomRight = Project(cat, camera, new Vector2(_spriteRect.xMax, _spriteRect.yMin));
        Vector3 topLeft = Project(cat, camera, new Vector2(_spriteRect.xMin, _spriteRect.yMax));
        // Extra geometry admits the outer glow beyond the original sprite edge.
        // Scale with the HUD so the outline survives high-DPI rendering and
        // downscaled Game View previews, with a minimum width on small phones.
        float glowScale = Mathf.Max(1f, canvas.scaleFactor);
        float paddingPixels = PaddingUnits * glowScale;
        float padX = paddingPixels * _spriteRect.width / Mathf.Max(1f, Vector2.Distance(bottomLeft, bottomRight));
        float padY = paddingPixels * _spriteRect.height / Mathf.Max(1f, Vector2.Distance(bottomLeft, topLeft));
        Rect padded = Rect.MinMaxRect(_spriteRect.xMin - padX, _spriteRect.yMin - padY,
            _spriteRect.xMax + padX, _spriteRect.yMax + padY);
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        for (int i = 0; i < 4; i++)
        {
            Vector2 local = new Vector2(i == 1 || i == 2 ? padded.xMax : padded.xMin,
                i >= 2 ? padded.yMax : padded.yMin);
            Vector3 screen = Project(cat, camera, local);
            if (screen.z <= 0f) { SetVerticesDirty(); return; }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screen, uiCamera, out _positions[i]);
            _textureUV[i] = _uvOrigin + _uvX * local.x + _uvY * local.y;
            _shapeUV[i] = new Vector2((local.x - _spriteRect.xMin) / _spriteRect.width,
                (local.y - _spriteRect.yMin) / _spriteRect.height);
        }
        // TEXCOORD1 carries the moving glint's position around the silhouette.
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
        _glow.SetFloat(AnimationId, age);
        _glow.SetFloat(GlowScaleId, glowScale);
        _ready = true;
        SetVerticesDirty();
    }

    private static Vector3 Project(SpriteRenderer cat, Camera camera, Vector2 point)
    {
        if (cat.flipX) point.x = -point.x;
        if (cat.flipY) point.y = -point.y;
        return camera.WorldToScreenPoint(cat.transform.TransformPoint(point));
    }

    private void CacheSprite(Sprite sprite)
    {
        _sprite = sprite;
        _spriteRect = new Rect(-sprite.pivot / sprite.pixelsPerUnit, sprite.rect.size / sprite.pixelsPerUnit);
        // Recover the UV mapping from the mesh, supporting trimmed/rotated atlas
        // sprites without requiring readable textures or changing the cat material.
        Vector2[] vertices = sprite.vertices;
        Vector2[] uv = sprite.uv;
        ushort[] triangles = sprite.triangles;
        int a = triangles[0], b = triangles[1], c = triangles[2];
        Vector2 ab = vertices[b] - vertices[a], ac = vertices[c] - vertices[a];
        Vector2 uvAB = uv[b] - uv[a], uvAC = uv[c] - uv[a];
        float determinant = ab.x * ac.y - ab.y * ac.x;
        _uvX = (uvAB * ac.y - uvAC * ab.y) / determinant;
        _uvY = (uvAC * ab.x - uvAB * ac.x) / determinant;
        _uvOrigin = uv[a] - _uvX * vertices[a].x - _uvY * vertices[a].y;
        Vector2 min = uv[0], max = uv[0];
        foreach (Vector2 coordinate in uv) { min = Vector2.Min(min, coordinate); max = Vector2.Max(max, coordinate); }
        _glow.SetVector(UVBoundsId, new Vector4(min.x, min.y, max.x, max.y));
        SetMaterialDirty();
    }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        if (!_ready) return;
        for (int i = 0; i < 4; i++)
        {
            UnityEngine.UIVertex vertex = UnityEngine.UIVertex.simpleVert;
            vertex.position = _positions[i];
            vertex.color = color;
            vertex.uv0 = _textureUV[i];
            vertex.uv1 = _shapeUV[i];
            mesh.AddVert(vertex);
        }
        mesh.AddTriangle(0, 1, 2);
        mesh.AddTriangle(0, 2, 3);
    }

    protected override void OnDestroy()
    {
        if (_glow != null) Destroy(_glow);
        base.OnDestroy();
    }
}
