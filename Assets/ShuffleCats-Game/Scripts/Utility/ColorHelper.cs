using UnityEngine;

public static class ColorHelper
{
    public static Color GetColor(CatColor catColor)
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

    public static Color GetGizmoColor(CatColor catColor)
    {
        Color color = Color.black;

        if (catColor == CatColor.Blue)
        {
            color = Color.blue;
        }
        else if (catColor == CatColor.Green)
        {
            color = Color.green;
        }
        else if (catColor == CatColor.Pink)
        {
            color = Color.magenta;
        }

        return color;
    }
}
