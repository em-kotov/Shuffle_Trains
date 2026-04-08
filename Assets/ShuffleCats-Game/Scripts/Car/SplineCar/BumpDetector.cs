using System;
using System.Collections;
using UnityEngine;

public class BumpDetector : MonoBehaviour
{
    [SerializeField] private Transform _forwardOrigin;
    [SerializeField] private Transform _backwardOrigin;

    private float _maxRayDistance = 2f;
    private float _bumpedDistance = 2f;
    private LayerMask _detectLayer;
    private string _carsLayerName = "Cars";
    private string _stationLayerName = "Station";
    private bool _isScanning = true;
    private Coroutine _scanRoutine;

    public event Action<float> Bumped;
    public event Action StationFound;

    public void Initialize()
    {
        _detectLayer = LayerMask.GetMask(_carsLayerName, _stationLayerName);
        StartScanning();
    }

    private IEnumerator RaycastWithInterval()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);

        while (_isScanning)
        {
            Physics.SyncTransforms();

            Scan(_backwardOrigin.position, _backwardOrigin.forward);

            yield return wait;
        }
    }

    private void Scan(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, _maxRayDistance, _detectLayer))
        {
            int hitLayer = hit.transform.gameObject.layer;

            if (hitLayer == LayerMask.NameToLayer(_carsLayerName))
            {
                Bump(hit.distance);
            }
            else if (hitLayer == LayerMask.NameToLayer(_stationLayerName))
            {
                WhenStationFound();
            }
        }
    }

    private void StartScanning()
    {
        StopScanning();

        _isScanning = true;
        _scanRoutine = StartCoroutine(RaycastWithInterval());
    }

    private void StopScanning()
    {
        _isScanning = false;

        if (_scanRoutine != null)
        {
            StopCoroutine(RaycastWithInterval());
            _scanRoutine = null;
        }
    }

    private void Bump(float distanceToObject)
    {
        if (distanceToObject <= _bumpedDistance)
        {
            Bumped?.Invoke(distanceToObject);
        }
    }

    private void WhenStationFound()
    {
        StationFound?.Invoke();
    }

    public void OnSplinePaused()
    {
        Debug.Log("Scanning stopped");
        StopScanning();
    }

    public void OnSplineResumed()
    {
        Debug.Log("Scanning started");
        StartScanning();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(_backwardOrigin.position, _backwardOrigin.forward * _maxRayDistance);
    }
}
