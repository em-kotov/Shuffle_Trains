using System;
using System.Collections.Generic;
using UnityEngine;

public class GridCalculator : MonoBehaviour
{
    private int _width;
    private int _height;
    private float _cellSize;
    private int _startX;
    private int _startY;
    private int _endX;
    private int _endY;
    private float _axisYLevel;
    private float _distanceMultiplier;
    private Vector3 _gridOrigin;
    private ParkingRegistrator _registrator;

    public void Initialize(int width, int height, float cellSize, int startX,
                            int startY, float axisYLevel, ParkingRegistrator registrator)
    {
        _width = width;
        _height = height;
        _cellSize = cellSize;
        _startX = startX;
        _startY = startY;
        _axisYLevel = axisYLevel;
        _registrator = registrator;

        _endX = _startX + _width;
        _endY = _startY + _height;
        _distanceMultiplier = _width + _height;
        _gridOrigin = new Vector3(_startX, _axisYLevel, _startY);
    }

    public Vector3 GetFurthestCellToMove(Car car, Vector3 current, CarOrientation orientation,
                                         float sign, out CellOccupancy cellOccupancy)
    {
        cellOccupancy = CellOccupancy.Free;
        Vector2Int currentCell = WorldToGrid(current);
        Vector2Int furthestCell = currentCell;

        Vector3 target = CalculateTargetPosition(current, orientation, sign, _distanceMultiplier);
        List<Vector2Int> visitedCells = GetVisitedCells(target, current);

        int firstIndex = 0;
        int indexStep = 1;

        for (int i = firstIndex; i < visitedCells.Count; i++)
        {
            furthestCell = visitedCells[i];
            Car occupyingCar = _registrator.GetCar(furthestCell.x, furthestCell.y);

            if (occupyingCar != null && occupyingCar != car)
            {
                cellOccupancy = CellOccupancy.Car;

                if (i == firstIndex)
                {
                    furthestCell = currentCell;
                }
                else
                {
                    furthestCell = visitedCells[i - indexStep];
                }

                break;
            }
        }

        if (cellOccupancy == CellOccupancy.Free && furthestCell == visitedCells[visitedCells.Count - indexStep])
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

        int noDifferenceValue = 0;

        if (differenceX != noDifferenceValue)
        {
            int step = MathF.Sign(differenceX);

            for (int x = currentCell.x + step; x != targetCell.x + step; x += step)
            {
                visitedCells.Add(new Vector2Int(x, currentCell.y));
            }
        }

        if (differenceY != noDifferenceValue)
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
        int firstIndex = 0;
        int indexStep = 1;

        int newX = Mathf.Clamp(target.x, firstIndex, _width - indexStep);
        int newY = Mathf.Clamp(target.y, firstIndex, _height - indexStep);

        return new Vector2Int(newX, newY);
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x - _gridOrigin.x / _cellSize);
        int y = Mathf.RoundToInt(worldPosition.z - _gridOrigin.z / _cellSize);

        return new Vector2Int(x, y);
    }

    public Vector3 GridToWorld(Vector2Int gridPosition)
    {
        float worldX = (gridPosition.x * _cellSize) + _gridOrigin.x;
        float worldZ = (gridPosition.y * _cellSize) + _gridOrigin.z;
        return new Vector3(worldX, _axisYLevel, worldZ);
    }

    public Car[,] InitializeGrid()
    {
        Car[,] cells = new Car[_width, _height];
        // int firstIndex = 0;
        // int indexStep = 1;

        // for (int x = firstIndex; x < _width-indexStep; x++)
        // {
        //     for (int y = _startY; y < _endY; y++)
        //     {
        //         cells[x, y] = null;
        //     }
        // }

        //1 2 - 4 5
        //x - 1 2 3 4 
        //y - 2 3 4 5 6
        //new car[4, 6]
        //indexes and axis values
        return cells;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        float ySize = 0.1f;
        float sideMultiplier = 0.9f;
        int firstIndex = 0;

        for (int x = firstIndex; x < _width; x++)
        {
            for (int y = firstIndex; y < _height; y++)
            {
                Vector3 cellCenter = GridToWorld(new Vector2Int(x, y));
                Gizmos.DrawWireCube(cellCenter, new Vector3(
                                   _cellSize * sideMultiplier, ySize,
                                   _cellSize * sideMultiplier));
            }
        }
    }
}
