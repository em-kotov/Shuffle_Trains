using System;
using UnityEngine;

public class BumpDetector : MonoBehaviour
{
    //raycast 4 sides, spline onle 2 sides 

    [SerializeField] private Transform _forwardOrigin;
    [SerializeField] private Transform _backwardOrigin;

    private float _radius = 0.3f;
    private float _maxDistance = 0.2f;
    private LayerMask _detectLayer;

    public event Action<bool, float> Bumped;

    private void Awake()
    {
        _detectLayer = LayerMask.GetMask("Cars");
    }

    private void FixedUpdate()
    {
        Scan(_forwardOrigin.position, _forwardOrigin.forward, true);
        Scan(_backwardOrigin.position, _backwardOrigin.forward, false);
    }

    private void Scan(Vector3 origin, Vector3 direction, bool isForward)
    {
        if (Physics.SphereCast(origin, _radius, direction, out RaycastHit hit, _maxDistance, _detectLayer))
        {
            // Debug.Log($"Object detected: {hit.collider.gameObject.name}, Distance: {hit.distance}");
            Bumped?.Invoke(isForward, hit.distance);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_forwardOrigin.position, _radius);
        Gizmos.DrawRay(_forwardOrigin.position, _forwardOrigin.forward * _maxDistance);

        Gizmos.DrawWireSphere(_backwardOrigin.position, _radius);
        Gizmos.DrawRay(_backwardOrigin.position, _backwardOrigin.forward * _maxDistance);
    }

    // private void OnCollisionEnter(Collision collision)
    // {
    //         Debug.Log(this + " collided with " + collision.gameObject);

    //     if (collision.gameObject.TryGetComponent(out BumpDetector listener) == true)
    //     {
    //         Debug.Log("collision have listener");
    //         Bumped?.Invoke(collision.GetContact(0).point, collision.gameObject.transform.position);
    //     }
    // }
}
