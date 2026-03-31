using System;
using UnityEngine;

public class BumpDetector : MonoBehaviour
{
    //raycast 4 sides, spline onle 2 sides 

    private float _radius = 0.4f;
    private float _maxDistance = 0.5f;
    private LayerMask _detectLayer;

    public event Action<Vector3, float> Bumped;

    private void Awake()
    {
        _detectLayer = LayerMask.GetMask("Cars");
    }

    private void FixedUpdate()
    {
        ScanForward();
    }

    private void ScanForward()
    {
        Vector3 direction = transform.forward;

        if (Physics.SphereCast(transform.position, _radius, direction, out RaycastHit hit, _maxDistance, _detectLayer))
        {
            // Debug.Log($"Object detected: {hit.collider.gameObject.name}, Distance: {hit.distance}");
            Bumped?.Invoke(Vector3.forward, hit.distance);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _radius);
        Gizmos.DrawRay(transform.position, transform.forward * _maxDistance);
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
