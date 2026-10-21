using UnityEngine;

// Compose the vector artwork in one fragment before applying the button tint.
// Fading overlapping mesh pieces separately makes their hidden edges show through.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class HintLampGraphic : UnityEngine.UI.MaskableGraphic
{
    public static readonly Color DisabledTint = new Color(0.75f, 0.75f, 0.75f, 0.45f);
    private Material _lampMaterial;

    protected override void Awake()
    {
        base.Awake();
        Shader shader = Resources.Load<Shader>("HintLamp");
        if (shader != null)
        {
            _lampMaterial = new Material(shader) { name = "Hint lamp composite" };
            material = _lampMaterial;
        }
    }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        mesh.AddVert(new Vector2(rect.xMin, rect.yMin), color, Vector2.zero);
        mesh.AddVert(new Vector2(rect.xMin, rect.yMax), color, Vector2.up);
        mesh.AddVert(new Vector2(rect.xMax, rect.yMax), color, Vector2.one);
        mesh.AddVert(new Vector2(rect.xMax, rect.yMin), color, Vector2.right);
        mesh.AddTriangle(0, 1, 2);
        mesh.AddTriangle(2, 3, 0);
    }

    protected override void OnDestroy()
    {
        if (_lampMaterial != null) Destroy(_lampMaterial);
        base.OnDestroy();
    }
}
