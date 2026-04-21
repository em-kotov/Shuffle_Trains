using UnityEngine;
using UnityEngine.Splines;

public class TrackEditHelper : MonoBehaviour
{
    [Tooltip("Change to match gizmo cube (1 segment) with car wagon (for finding car length)")]
    [SerializeField] float _segmentsCount = 53f;
    [SerializeField] SplineContainer _track;

    private float _segmentLength;
    private float _carLength = 0f;
    private float _trackLength;
    private Vector3 _size;
    private Color _color = Color.blue;

    private void OnDrawGizmos()
    {
        Gizmos.color = _color;

        _trackLength = _track.Spline.GetLength();
        FindCarLength();
        Gizmos.DrawWireCube(transform.position, _size);
    }

    private void FindCarLength()
    {
        _segmentLength = _trackLength / _segmentsCount;

        if (_carLength != _segmentLength)
        {
            _carLength = _segmentLength;
            _size = new(_carLength, _carLength, _carLength);
            //Debug.Log($"car length: {_carLength}. track length: {_trackLength}. " +
            //            $"segment count: {(int)_segmentsCount}");
        }
    }
}
