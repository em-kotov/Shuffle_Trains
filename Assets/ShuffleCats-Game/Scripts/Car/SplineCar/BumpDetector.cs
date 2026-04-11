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
    private string _stationResetLayerName = "StationReset";
    private string _liquidationAreaName = "LiquidationArea";
    private bool _isScanning = true;
    private bool _isLiquidationFound = false;
    private Coroutine _scanRoutine;

    public event Action<float> Bumped;
    public event Action<Station> StationFound;
    public event Action ResetFound;
    public event Action LiquidationFound;

    public void Initialize()
    {
        _detectLayer = LayerMask.GetMask(_carsLayerName, _stationLayerName,
                                _stationResetLayerName, _liquidationAreaName);
        Debug.Log("Bump Detector initialized");
        StartScanning();
    }

    private IEnumerator RaycastWithInterval()
    {
        WaitForSeconds wait = new WaitForSeconds(0.05f);

        while (_isScanning)
        {
            Physics.SyncTransforms();

            //Debug.Log("Bump Detector scan routine 1 cycle");
            Scan(_backwardOrigin.position, _backwardOrigin.forward);

            yield return wait;
        }
    }

    private void Scan(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, _maxRayDistance, _detectLayer))
        {
            int hitLayer = hit.transform.gameObject.layer;
            Debug.Log("Bump Detector detected object on a layer: " + hitLayer + ", name: " + hit.transform.gameObject.name);

            if (hitLayer == LayerMask.NameToLayer(_carsLayerName))
            {
                Debug.Log("Bump Detector Bumped at car");
                Bump(hit.distance);
            }
            else if (hitLayer == LayerMask.NameToLayer(_stationLayerName))
            {
                Debug.Log("Bump Detector Found a station");
                bool hasStation = hit.transform.gameObject.TryGetComponent(out Station foundStation);

                Debug.Log("Bump Detector station: " + foundStation);

                if (hasStation)
                {
                    WhenStationFound(foundStation);
                }
            }
            else if (hitLayer == LayerMask.NameToLayer(_stationResetLayerName))
            {
                Debug.Log("Bump Detector found a station Reset");
                OnResetFound();
            }
            else if (hitLayer == LayerMask.NameToLayer(_liquidationAreaName))
            {
                Debug.Log("Bump Detector found a liquidation Area");
                OnLiquidationFound();
            }
        }
    }

    private void StartScanning()
    {
        StopScanning();

        Debug.Log("Bump Detector starts scanning");
        _isScanning = true;
        _scanRoutine = StartCoroutine(RaycastWithInterval());
    }

    private void StopScanning()
    {
        Debug.Log("Bump Detector stops scan routine");
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

    private void WhenStationFound(Station foundStation)
    {
        StationFound?.Invoke(foundStation);
    }

    private void OnResetFound()
    {
        ResetFound?.Invoke();
    }

    private void OnLiquidationFound()
    {
        if (_isLiquidationFound)
            return;

        _isLiquidationFound = true;
        LiquidationFound?.Invoke();
    }

    public void Deactivate()
    {
        Debug.Log("Bump Detector Received inquiry for deactivate");
        StopScanning();
    }

    public void OnSplinePaused()
    {
        Debug.Log("Bump Detector Received inquiry for scan stop");
        StopScanning();
    }

    public void OnSplineResumed()
    {
        Debug.Log("Bump Detetor Received inquiry for scan resume");
        StartScanning();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(_backwardOrigin.position, _backwardOrigin.forward * _maxRayDistance);
    }
}
