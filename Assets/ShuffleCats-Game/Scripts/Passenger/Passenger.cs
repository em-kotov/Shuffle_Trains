using UnityEngine;

public class Passenger : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _model;

    public CatColor CatColor { get; private set; }

    public void AssignColor(CatColor catColor)
    {
        CatColor = catColor;
        Color color = GetColor(CatColor);
        Debug.Log("Passenger - was assigned color r: " + color.r.ToString());
        _model.color = color;
    }

    private Color GetColor(CatColor catColor)
    {
        Color color = Color.white;

        if (catColor == CatColor.Blue)
        {
            color = new Color(0.26f, 0.75f, 0.96f, 1f);
        }
        else if (catColor == CatColor.Green)
        {
            color = new Color(0.63f, 1f, 0.63f, 1f);
        }
        else if (catColor == CatColor.Pink)
        {
            color = new Color(1f, 0.51f, 0.67f, 1f);
        }

        return color;
    }
}
