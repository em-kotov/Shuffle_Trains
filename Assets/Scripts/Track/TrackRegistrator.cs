using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

public class TrackRegistrator : MonoBehaviour
{
    // [SerializeField] private SplineContainer _track;

    private Dictionary<float, Car> _trackGrid;
    private float _step = 0.05f;

    private void Start()
    {
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        _trackGrid = new();

        for (float x = 0; x < 1f; x += _step)
        {
            _trackGrid.Add(x, null);
        }
    }

    public bool IsFree(float t )
    {
        Debug.Log(t);
        return true;
    }
}
