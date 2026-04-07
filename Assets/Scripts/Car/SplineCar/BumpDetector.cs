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

    public event Action<float> Bumped;

    public void Initialize()
    {
        _detectLayer = LayerMask.GetMask("Cars");
        StartCoroutine(RaycastWithInterval());
    }

    private void Scan(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, _maxRayDistance, _detectLayer))
        {
            float distanceToObject = hit.distance;

            if (distanceToObject <= _bumpedDistance)
            {
                Bumped?.Invoke(distanceToObject);
            }
        }
    }

    private IEnumerator RaycastWithInterval()
    {
        while (true)
        {
            Physics.SyncTransforms();

            Scan(_backwardOrigin.position, _backwardOrigin.forward);

            yield return new WaitForSeconds(0.2f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(_backwardOrigin.position, _backwardOrigin.forward * _maxRayDistance);
    }
}
