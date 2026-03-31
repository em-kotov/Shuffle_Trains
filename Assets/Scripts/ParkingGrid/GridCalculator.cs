using System;
using System.Collections.Generic;
using UnityEngine;

public class GridCalculator : MonoBehaviour
{
    private int _width;
    private int _height;
    private float _cellSize;
    private float _distanceMultiplier;
    private ParkingRegistrator _registrator;

    public void Initialize(int width, int height, float cellSize, ParkingRegistrator registrator)
    {
        _width = width;
        _height = height;
        _cellSize = cellSize;
        _registrator = registrator;
        _distanceMultiplier = _width + _height;
    }

    public Vector3 GetFurthestCellToMove(Car car, Vector3 current, CarOrientation orientation,
                                         float sign, out CellOccupancy cellOccupancy)
    {
        cellOccupancy = CellOccupancy.Free;
        Vector2Int currentCell = WorldToGrid(current);
        Vector2Int furthestCell = currentCell;

        Vector3 target = CalculateTargetPosition(current, orientation, sign, _distanceMultiplier);
        List<Vector2Int> visitedCells = GetVisitedCells(target, current);

        for (int i = 0; i < visitedCells.Count; i++)
        {
            furthestCell = visitedCells[i];
            Car occupyingCar = _registrator.GetCar(furthestCell.x, furthestCell.y);

            if (occupyingCar != null && occupyingCar != car)
            {
                cellOccupancy = CellOccupancy.Car;

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

        if (cellOccupancy == CellOccupancy.Free && furthestCell == visitedCells[visitedCells.Count - 1])
        {
            // Debug.Log("can go to border. visited cells count " + visitedCells.Count + ". furthest cell " + furthestCell);
            cellOccupancy = CellOccupancy.Border;
        }

        return GridToWorld(furthestCell);
    }

    public Vector3 CalculateTargetPosition(Vector3 current, CarOrientation orientation, float sign, float distance)
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

    public List<Vector2Int> GetVisitedCells(Vector3 target, Vector3 current)
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

    public Vector2Int ClampToGrid(Vector2Int target)
    {
        int newX = Mathf.Clamp(target.x, 0, _width - 1);
        int newY = Mathf.Clamp(target.y, 0, _height - 1);

        return new Vector2Int(newX, newY);
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x / _cellSize);
        int y = Mathf.RoundToInt(worldPosition.z / _cellSize);

        return new Vector2Int(x, y);
    }

    public Vector3 GridToWorld(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * _cellSize, 0, gridPosition.y * _cellSize);
    }

    public Car[,] InitializeGrid()
    {
        Car[,] cells = new Car[_width, _height];

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                cells[x, y] = null;
            }
        }

        return cells;
    }

    private void OnDrawGizmos()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                Vector3 cellCenter = GridToWorld(new Vector2Int(x, y));
                Gizmos.DrawWireCube(cellCenter, new Vector3(_cellSize * 0.9f, 0.1f, _cellSize * 0.9f));
            }
        }
    }
}
