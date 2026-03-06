using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

public class TrackRegistrator : MonoBehaviour
{
    [SerializeField] private SplineContainer _track;
    [SerializeField] private int _segmentsCount = 10;

    private List<(float start, float end, Car car)> _segments;

    private void Start()
    {
        Initialize();
        Register(0.34f);
    }

    private void Initialize()
    {
        _segments = new();

        float range = 1f / _segmentsCount;
        float start = 0f;
        float end = start + range;

        for (int i = 0; i < _segmentsCount; i++)
        {
            _segments.Add((start, end, null));
            start += range;
            end += range;
        }
    }

    public void Register(float t)
    {
        t = Mathf.Clamp01(t);

        int index = _segments.FindIndex(segment => segment.start < t && segment.end >= t);
        Debug.Log(index);
    }
}
