using UnityEngine;

public class Passenger : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _model;

    public CatColor CatColor { get; private set; }

    public void AssignColor(CatColor catColor)
    {
        CatColor = catColor;
        Color color = ColorHelper.GetColor(CatColor);
        _model.color = color;
    }
}
