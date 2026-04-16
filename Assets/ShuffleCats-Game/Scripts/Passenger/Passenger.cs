using System;
using UnityEngine;

[Serializable]
public class Passenger : MonoBehaviour
{
    public CatColor CatColor { get; private set; }

    public void SetColor(CatColor catColor)
    {
        CatColor = catColor;
    }
}
