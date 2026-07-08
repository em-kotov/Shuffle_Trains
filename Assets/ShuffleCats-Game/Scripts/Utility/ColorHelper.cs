using System.Collections.Generic;
using UnityEngine;

public static class ColorHelper
{
    //private static Dictionary<CatColor,Color>;

    public static Color GetColor(CatColor catColor)
    {
        Color color = Color.white;

        if (catColor == CatColor.Blue)
        {
            color = new Color(0.62f, 0.79f, 0.97f, 1f);
        }
        else if (catColor == CatColor.Green)
        {
            color = new Color(0.81f, 0.91f, 0.73f, 1f);
        }
        else if (catColor == CatColor.Pink) 
        {
            color = new Color(1f, 0.66f, 0.82f, 1f);
        }
        else if (catColor == CatColor.Yellow)
        {
            color = new Color(1f, 0.96f, 0.6f, 1f);
        }
        else if (catColor == CatColor.Purple)
        {
            color = new Color(0.81f, 0.57f, 0.94f, 1f);
        }
        else if (catColor == CatColor.Orange)
        {
            color = new Color(0.99f, 0.68f, 0.48f, 1f);
        }
        else if (catColor == CatColor.Brown) 
        {
            color = new Color(0.72f, 0.54f, 0.5f, 1f);
        }
        else if (catColor == CatColor.Iridiscent)
        {
            color = new Color(0.942f, 0.915f, 1f, 1f);
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
        else if (catColor == CatColor.Yellow)
        {
            color = Color.yellow;
        }
        else if (catColor == CatColor.Purple)
        {
            color = Color.red;
        }
        else if (catColor == CatColor.Orange)
        {
            color = Color.yellow;
        }
        else if (catColor == CatColor.Brown)
        {
            color = Color.black;
        }
        else if (catColor == CatColor.Iridiscent)
        {
            color = Color.magenta;
        }

        return color;
    }
}
