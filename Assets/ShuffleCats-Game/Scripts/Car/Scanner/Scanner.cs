using System;
using System.Collections;
using UnityEngine;

public class Scanner : MonoBehaviour
{
    [SerializeField] private Transform _backwardOrigin;
    [SerializeField] private float _bumpedDistance = 0.5f;
    [SerializeField] private float _clearedDistance = 0.9f;

    private float _scanInterval = 0.01f;
    private float _sphereCastRadius = 0.2f;
    private LayerMask _carsDetectLayer;
    private LayerMask _stationsDetectLayer;
    private string _carsLayerName = "Cars";
    private string _stationLayerName = "Station";
    private string _stationResetLayerName = "StationReset";
    private bool _isScanning = true;
    private bool _isBlockedByCar;
    private Coroutine _scanRoutine;

    public event Action BlockedAhead;
    public event Action PathCleared;
    public event Action<Station> StationFound;
    public event Action ResetFound;

    public void Initialize()
    {
        _carsDetectLayer = LayerMask.GetMask(_carsLayerName);
        _stationsDetectLayer = LayerMask.GetMask(_stationLayerName, _stationResetLayerName);
        StartScanning();
    }

    public void StartScanning()
    {
        StopScanning();

        _isScanning = true;
        _isBlockedByCar = false;
        _scanRoutine = StartCoroutine(RaycastWithInterval());
    }

    public void StopScanning()
    {
        _isScanning = false;

        if (_scanRoutine != null)
        {
            StopCoroutine(_scanRoutine);
            _scanRoutine = null;
        }
    }

    private IEnumerator RaycastWithInterval()
    {
        WaitForSeconds wait = new WaitForSeconds(_scanInterval);

        while (_isScanning)
        {
            Physics.SyncTransforms();
            ScanForCars(_backwardOrigin.position, -_backwardOrigin.forward);
            ScanForStations(_backwardOrigin.position, _backwardOrigin.forward);

            yield return wait;
        }
    }

    private void ScanForCars(Vector3 origin, Vector3 direction)
    {
        bool carBlocking = false;
        float threshold = _isBlockedByCar ? _clearedDistance : _bumpedDistance;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, _clearedDistance, _carsDetectLayer))
        {
            if (hit.transform.gameObject.layer == LayerMask.NameToLayer(_carsLayerName))
            {
                carBlocking = hit.distance <= threshold;
            }
        }

        UpdateBlockedState(carBlocking);
    }

    private void ScanForStations(Vector3 origin, Vector3 direction)
    {
        // Start the sweep slightly behind the origin so the sphere never begins
        // already overlapping a station ahead (SphereCast ignores start-overlap).
        // Extend the distance by the same offset so forward reach is unchanged.
        float backOffset = _sphereCastRadius + 0.05f;
        Vector3 castOrigin = origin - (direction * backOffset);
        float castDistance = _clearedDistance + backOffset;

        if (Physics.SphereCast(castOrigin, _sphereCastRadius, direction, out RaycastHit hit, castDistance, _stationsDetectLayer))
        {
            int hitLayer = hit.transform.gameObject.layer;

            if (hitLayer == LayerMask.NameToLayer(_stationLayerName))
            {
                bool hasStation = hit.transform.gameObject.TryGetComponent(out Station foundStation);

                if (hasStation)
                {
                    InvokeStation(foundStation);
                }
            }
            else if (hitLayer == LayerMask.NameToLayer(_stationResetLayerName))
            {
                InvokeReset();
            }
        }
    }

    private void UpdateBlockedState(bool carBlocking)
    {
        if (carBlocking == _isBlockedByCar)
            return;

        _isBlockedByCar = carBlocking;

        if (carBlocking)
        {
            BlockedAhead?.Invoke();
        }
        else
        {
            PathCleared?.Invoke();
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
        Gizmos.DrawRay(_backwardOrigin.position, -_backwardOrigin.forward *
                     _clearedDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(_backwardOrigin.position, _backwardOrigin.forward *
                     _clearedDistance);

        // Visualize the station SphereCast volume: spheres at the swept cast's
        // start and end, matching the back-offset and distance used in
        // ScanForStations.
        float backOffset = _sphereCastRadius + 0.05f;
        Vector3 sphereStart = _backwardOrigin.position -
                            (_backwardOrigin.forward * backOffset);
        Vector3 sphereEnd = sphereStart +
                            (_backwardOrigin.forward * (_clearedDistance + backOffset));
        Gizmos.DrawWireSphere(sphereStart, _sphereCastRadius);
        Gizmos.DrawWireSphere(sphereEnd, _sphereCastRadius);
    }
}
