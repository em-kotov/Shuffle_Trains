using UnityEngine;
using System.Collections.Generic;

public class ParkingGridExample : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 6;  // Number of columns
    [SerializeField] private int gridHeight = 8; // Number of rows
    [SerializeField] private float cellSize = 1f; // Size of each cell in world units

    [Header("Visualization (Optional)")]
    // [SerializeField] private bool showGridGizmos = true;
    // [SerializeField] private Color emptyColor = Color.green;
    // [SerializeField] private Color occupiedColor = Color.red;

    // 2D array to store which car occupies each cell (null = empty)
    private CarExample[,] gridCells;

    // Singleton pattern for easy access
    public static ParkingGridExample Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        InitializeGrid();
    }

    /// <summary>
    /// Creates empty grid
    /// </summary>
    private void InitializeGrid()
    {
        gridCells = new CarExample[gridWidth, gridHeight];

        // Initialize all cells as empty (null)
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                gridCells[x, y] = null;
            }
        }

        Debug.Log($"Grid initialized: {gridWidth}x{gridHeight} = {gridWidth * gridHeight} cells");
    }

    /// <summary>
    /// Converts world position to grid coordinates
    /// This is KEY - it bridges between Unity's world space and our grid logic
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x / cellSize);
        int y = Mathf.RoundToInt(worldPosition.z / cellSize); // Using Z for 3D games

        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Converts grid coordinates back to world position
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPosition)
    {
        return new Vector3(
            gridPosition.x * cellSize,
            0, // Y is up in 3D
            gridPosition.y * cellSize
        );
    }

    /// <summary>
    /// Check if grid coordinates are within bounds
    /// </summary>
    public bool IsValidGridPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridWidth &&
               gridPos.y >= 0 && gridPos.y < gridHeight;
    }

    /// <summary>
    /// Check if a specific cell is occupied
    /// </summary>
    public bool IsCellOccupied(Vector2Int gridPos)
    {
        if (!IsValidGridPosition(gridPos))
            return true; // Out of bounds = considered occupied

        return gridCells[gridPos.x, gridPos.y] != null;
    }

    /// <summary>
    /// MAIN FUNCTION: Check if car can move to target position
    /// This is called BEFORE moving - preventing the "already in slot" problem
    /// </summary>
    public bool CanCarMoveTo(CarExample car, Vector3 targetWorldPosition, out string blockReason)
    {
        blockReason = "";

        // Get all cells this car would occupy at target position
        List<Vector2Int> targetCells = GetCellsForCar(targetWorldPosition, car.GetOrientation(), car.GetCarLength());

        // Check each cell
        foreach (Vector2Int cell in targetCells)
        {
            // Out of bounds?
            if (!IsValidGridPosition(cell))
            {
                blockReason = $"Out of bounds at {cell}";
                return false;
            }

            // Occupied by another car?
            CarExample occupyingCar = gridCells[cell.x, cell.y];
            if (occupyingCar != null && occupyingCar != car)
            {
                blockReason = $"Blocked by {occupyingCar.name} at {cell}";
                return false;
            }
        }

        return true; // All cells are free!
    }

    /// <summary>
    /// Get all grid cells a car would occupy at a given position
    /// This is the "secret sauce" - calculating which cells belong to the car
    /// </summary>
    public List<Vector2Int> GetCellsForCar(Vector3 worldPosition, CarOrientation orientation, int carLength)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        Vector2Int startCell = WorldToGrid(worldPosition);

        // Add cells based on orientation and length
        if (orientation == CarOrientation.Horizontal)
        {
            // Car extends along X axis
            for (int i = 0; i < carLength; i++)
            {
                cells.Add(new Vector2Int(startCell.x + i, startCell.y));
            }
        }
        else // Vertical
        {
            // Car extends along Y axis (Z in 3D world)
            for (int i = 0; i < carLength; i++)
            {
                cells.Add(new Vector2Int(startCell.x, startCell.y + i));
            }
        }

        return cells;
    }

    /// <summary>
    /// Register car's position in grid (call after successful move)
    /// </summary>
    public void RegisterCar(CarExample car)
    {
        List<Vector2Int> cells = GetCellsForCar(car.transform.position, car.GetOrientation(), car.GetCarLength());

        foreach (Vector2Int cell in cells)
        {
            if (IsValidGridPosition(cell))
            {
                gridCells[cell.x, cell.y] = car;
            }
        }
    }

    /// <summary>
    /// Remove car from grid (call before moving)
    /// </summary>
    public void UnregisterCar(CarExample car)
    {
        // Clear all cells occupied by this car
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (gridCells[x, y] == car)
                {
                    gridCells[x, y] = null;
                }
            }
        }
    }

    public void UpdateCarPosition(CarExample car, Vector3 pos)
    {
        
    }
}
