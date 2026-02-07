using System;
using System.Collections.Generic;
using UnityEngine;

public class ParkingSlotsHandler : MonoBehaviour
{
    [SerializeField] private int _gridWidth = 6;
    [SerializeField] private int _gridHeight = 8;
    [SerializeField] private float _cellSize = 1f;

    private float _distanceMultiplier;
    private Car[,] _gridCells;

    private void Awake()
    {
        _distanceMultiplier = _gridWidth + _gridHeight;
        InitializeGrid();
    }

    public void SnapCarToCellFromPosition(Car car, Vector3 position)
    {
        Vector2Int cell = WorldToGrid(position);
        cell = ClampToGrid(cell);
        car.transform.position = GridToWorld(cell);
    }

    public void RegisterCar(Vector3 newPosition, Car car)
    {
        Vector2Int newCell = WorldToGrid(newPosition);
        newCell = ClampToGrid(newCell);

        if (_gridCells[newCell.x, newCell.y] == null)
        {
            _gridCells[newCell.x, newCell.y] = car;
            car.transform.position = GridToWorld(newCell);
        }
    }

    public void UnregisterCar(Vector3 oldPosition, Car car)
    {
        Vector2Int oldCell = WorldToGrid(oldPosition);
        oldCell = ClampToGrid(oldCell);

        if (_gridCells[oldCell.x, oldCell.y] == car)
            _gridCells[oldCell.x, oldCell.y] = null;
    }

    public void RegisterTail(Car car, Vector3 current, CarOrientation orientation, float sign, float length)
    {
        float opposite = -1f;
        float exclusiveDistance = 1f;
        Vector3 target = CalculateTargetPosition(current, orientation, sign * opposite, length - exclusiveDistance);
        List<Vector2Int> tailCells = GetVisitedCells(target, current);

        int minCountForRegisterTail = 2;

        if (tailCells.Count < minCountForRegisterTail)
        {
            return;
        }

        int tailStartIndex = 1;

        for (int i = tailStartIndex; i < tailCells.Count; i++)
        {
            if (_gridCells[tailCells[i].x, tailCells[i].y] == null)
            {
                _gridCells[tailCells[i].x, tailCells[i].y] = car;
            }
        }
    }

    public Vector3 GetFurthestCellToMove(Car car, Vector3 current, CarOrientation orientation,
                                            float sign, out int codeWhatIsForward)
    {
        codeWhatIsForward = 0;
        Vector2Int currentCell = WorldToGrid(current);
        Vector2Int furthestCell = currentCell;

        Vector3 target = CalculateTargetPosition(current, orientation, sign, _distanceMultiplier);
        List<Vector2Int> visitedCells = GetVisitedCells(target, current);

        for (int i = 0; i < visitedCells.Count; i++)
        {
            furthestCell = visitedCells[i];
            Car occupyingCar = _gridCells[furthestCell.x, furthestCell.y];

            if (occupyingCar != null && occupyingCar != car)
            {
                codeWhatIsForward = 1;

                if (i == 0)
                {
                    furthestCell = currentCell;
                }
                else
                {
                    furthestCell = visitedCells[i - 1];
                }

                break;
            }
        }

        return GridToWorld(furthestCell);
    }

    private Vector3 CalculateTargetPosition(Vector3 current, CarOrientation orientation, float sign, float distance)
    {
        Vector3 target = current;

        if (orientation == CarOrientation.Horizontal)
        {
            target.x = current.x + (sign * distance);
        }
        else
        {
            target.z = current.z + (sign * distance);
        }

        return target;
    }

    private List<Vector2Int> GetVisitedCells(Vector3 target, Vector3 current)
    {
        List<Vector2Int> visitedCells = new List<Vector2Int>();
        Vector2Int currentCell = WorldToGrid(current);
        Vector2Int targetCell = WorldToGrid(target);
        targetCell = ClampToGrid(targetCell);

        int differenceX = targetCell.x - currentCell.x;
        int differenceY = targetCell.y - currentCell.y;

        visitedCells.Add(currentCell); //to return at least current cell

        if (differenceX != 0)
        {
            int step = MathF.Sign(differenceX);

            for (int x = currentCell.x + step; x != targetCell.x + step; x += step)
            {
                visitedCells.Add(new Vector2Int(x, currentCell.y));
            }
        }

        if (differenceY != 0)
        {
            int step = MathF.Sign(differenceY);

            for (int y = currentCell.y + step; y != targetCell.y + step; y += step)
            {
                visitedCells.Add(new Vector2Int(currentCell.x, y));
            }
        }

        return visitedCells;
    }

    private Vector2Int ClampToGrid(Vector2Int target)
    {
        int newX = Mathf.Clamp(target.x, 0, _gridWidth - 1);
        int newY = Mathf.Clamp(target.y, 0, _gridHeight - 1);

        return new Vector2Int(newX, newY);
    }

    private Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x / _cellSize);
        int y = Mathf.RoundToInt(worldPosition.z / _cellSize);

        return new Vector2Int(x, y);
    }

    private Vector3 GridToWorld(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * _cellSize, 0, gridPosition.y * _cellSize);
    }

    private void InitializeGrid()
    {
        _gridCells = new Car[_gridWidth, _gridHeight];

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                _gridCells[x, y] = null;
            }
        }
    }

    private void OnDrawGizmos()
    {
        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                Vector3 cellCenter = GridToWorld(new Vector2Int(x, y));
                Gizmos.DrawWireCube(cellCenter, new Vector3(_cellSize * 0.9f, 0.1f, _cellSize * 0.9f));
            }
        }
    }
}
