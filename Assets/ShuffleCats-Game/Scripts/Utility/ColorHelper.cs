using UnityEngine;

public static class ColorHelper
{
    public static Color GetColor(CatColor catColor)
    {
        switch (catColor)
        {
            case CatColor.Blue:
                return new Color(0.62f, 0.79f, 0.97f, 1f);
            case CatColor.Green:
                return new Color(0.81f, 0.91f, 0.73f, 1f);
            case CatColor.Pink:
                return new Color(1f, 0.66f, 0.82f, 1f);
            case CatColor.Yellow:
                return new Color(1f, 0.96f, 0.6f, 1f);
            case CatColor.Purple:
                return new Color(0.81f, 0.57f, 0.94f, 1f);
            case CatColor.Orange:
                return new Color(0.99f, 0.68f, 0.48f, 1f);
            case CatColor.Brown:
                return new Color(0.72f, 0.54f, 0.5f, 1f);
            case CatColor.Iridiscent:
                return new Color(0.942f, 0.915f, 1f, 1f);
            default:
                return Color.white;
        }
    }

    public static Color GetGizmoColor(CatColor catColor)
    {
        switch (catColor)
        {
            case CatColor.Blue:
                return Color.blue;
            case CatColor.Green:
                return Color.green;
            case CatColor.Pink:
                return Color.magenta;
            case CatColor.Yellow:
                return Color.yellow;
            case CatColor.Purple:
                return Color.red;
            case CatColor.Orange:
                return Color.yellow;
            case CatColor.Brown:
                return Color.black;
            case CatColor.Iridiscent:
                return Color.magenta;
            default:
                return Color.black;
        }
    }
}
