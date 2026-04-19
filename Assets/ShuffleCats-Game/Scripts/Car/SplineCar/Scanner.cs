using System;
using System.Collections;
using UnityEngine;

public class Scanner : MonoBehaviour
{
    [SerializeField] private Transform _backwardOrigin;

    private float _maxRayDistance = 2f;
    private float _bumpedDistance = 2f;
    private LayerMask _detectLayer;
    private string _carsLayerName = "Cars";
    private string _stationLayerName = "Station";
    private string _stationResetLayerName = "StationReset";
    private bool _isScanning = true;
    private Coroutine _scanRoutine;

    public event Action BumpFound;
    public event Action<Station> StationFound;
    public event Action ResetFound;

    public void Initialize()
    {
        _detectLayer = LayerMask.GetMask(_carsLayerName, _stationLayerName,
                                _stationResetLayerName);
        StartScanning();
    }

    public void StartScanning()
    {
        StopScanning();

        _isScanning = true;
        _scanRoutine = StartCoroutine(RaycastWithInterval());
    }

    public void StopScanning()
    {
        _isScanning = false;

        if (_scanRoutine != null)
        {
            StopCoroutine(RaycastWithInterval());
            _scanRoutine = null;
        }
    }

    private IEnumerator RaycastWithInterval()
    {
        WaitForSeconds wait = new WaitForSeconds(0.1f);

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
            //Debug.Log("Bump Detector detected object on a layer: " + hitLayer + ", name: " + hit.transform.gameObject.name);

            if (hitLayer == LayerMask.NameToLayer(_carsLayerName))
            {
                //Debug.Log("Bump Detector Bumped at car");
                InvokeBump(hit.distance);
            }
            else if (hitLayer == LayerMask.NameToLayer(_stationLayerName))
            {
                //Debug.Log("Bump Detector Found a station");
                bool hasStation = hit.transform.gameObject.TryGetComponent(out Station foundStation);

                //Debug.Log("Bump Detector station: " + foundStation);

                if (hasStation)
                {
                    InvokeStation(foundStation);
                }
            }
            else if (hitLayer == LayerMask.NameToLayer(_stationResetLayerName))
            {
                //Debug.Log("Bump Detector found a station Reset");
                InvokeReset();
            }
        }
    }

    private void InvokeBump(float distanceToObject)
    {
        if (distanceToObject <= _bumpedDistance)
        {
            BumpFound?.Invoke();
        }
    }

    private void InvokeStation(Station foundStation)
    {
        StationFound?.Invoke(foundStation);
    }

    private void InvokeReset()
    {
        ResetFound?.Invoke();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(_backwardOrigin.position, _backwardOrigin.forward * _maxRayDistance);
    }
}
