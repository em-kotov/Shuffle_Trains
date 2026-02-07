using UnityEngine;

/// <summary>
/// Represents a single car in the parking lot
/// Handles movement and grid interaction
/// </summary>
public class CarExample : MonoBehaviour
{
    [Header("Car Properties")]
    [SerializeField] private CarOrientation orientation = CarOrientation.Horizontal;
    [SerializeField] private int carLength = 2; // How many cells this car occupies
    [SerializeField] private float moveSpeed = 5f;

    [Header("Movement Settings")]
    // [SerializeField] private float dragSensitivity = 0.1f;

    private bool isDragging = false;
    private Vector3 dragStartPosition;
    private Vector3 carStartPosition;
    private Camera mainCamera;

    // Current grid position (cached)
    private Vector2Int currentGridPosition;

    private void Start()
    {
        mainCamera = Camera.main;

        // Register this car in the grid at start
        if (ParkingGridExample.Instance != null)
        {
            ParkingGridExample.Instance.RegisterCar(this);
            currentGridPosition = ParkingGridExample.Instance.WorldToGrid(transform.position);
            Debug.Log($"{name} registered at grid position {currentGridPosition}");
        }
    }

    // Public getters for grid system
    public CarOrientation GetOrientation() => orientation;
    public int GetCarLength() => carLength;

    private void OnMouseDown()
    {
        // Start dragging
        isDragging = true;
        dragStartPosition = GetMouseWorldPosition();
        carStartPosition = transform.position;
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 currentMousePos = GetMouseWorldPosition();
        Vector3 dragDelta = currentMousePos - dragStartPosition;

        // Only allow movement in car's orientation direction
        Vector3 constrainedDelta = Vector3.zero;

        if (orientation == CarOrientation.Horizontal)
        {
            constrainedDelta = new Vector3(dragDelta.x, 0, 0);
        }
        else // Vertical
        {
            constrainedDelta = new Vector3(0, 0, dragDelta.z);
        }

        // Calculate target position
        Vector3 targetPosition = carStartPosition + constrainedDelta;

        // THIS IS THE KEY: Check BEFORE moving
        if (ParkingGridExample.Instance.CanCarMoveTo(this, targetPosition, out string blockReason))
        {
            // Move is valid - smooth movement
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
        }
        else
        {
            // Move is blocked - play bump effect
            Debug.Log($"{name} blocked: {blockReason}");
            PlayBumpEffect();
        }
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        // Snap to nearest grid position
        SnapToGrid();
    }

    /// <summary>
    /// Snap car to nearest valid grid cell
    /// </summary>
    private void SnapToGrid()
    {
        if (ParkingGridExample.Instance == null) return;

        // Find nearest grid position
        Vector2Int nearestGrid = ParkingGridExample.Instance.WorldToGrid(transform.position);
        Vector3 snappedPosition = ParkingGridExample.Instance.GridToWorld(nearestGrid);

        // Check if we can actually be at this position
        if (ParkingGridExample.Instance.CanCarMoveTo(this, snappedPosition, out string reason))
        {
            // Update position in grid system
            ParkingGridExample.Instance.UpdateCarPosition(this, snappedPosition);
            currentGridPosition = nearestGrid;
            Debug.Log($"{name} snapped to {currentGridPosition}");
        }
        else
        {
            // Can't snap here - return to last valid position
            Vector3 lastValidPosition = ParkingGridExample.Instance.GridToWorld(currentGridPosition);
            transform.position = lastValidPosition;
            Debug.Log($"{name} returned to {currentGridPosition}");
        }
    }

    /// <summary>
    /// Visual/audio feedback when car hits obstacle
    /// </summary>
    private void PlayBumpEffect()
    {
        // Add your bump animation/sound here
        // For example:
        // - Shake the car slightly
        // - Play bump sound
        // - Show particles

        // Simple example: small shake
        transform.position += Random.insideUnitSphere * 0.02f;
    }

    /// <summary>
    /// Get mouse position in world space
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return transform.position;
    }

    // Visualize car's grid cells in Scene view
    private void OnDrawGizmosSelected()
    {
        if (ParkingGridExample.Instance == null) return;

        Gizmos.color = Color.yellow;
        var cells = ParkingGridExample.Instance.GetCellsForCar(transform.position, orientation, carLength);

        foreach (var cell in cells)
        {
            Vector3 cellWorld = ParkingGridExample.Instance.GridToWorld(cell);
            Gizmos.DrawWireCube(cellWorld, Vector3.one * 0.95f);
        }
    }
}