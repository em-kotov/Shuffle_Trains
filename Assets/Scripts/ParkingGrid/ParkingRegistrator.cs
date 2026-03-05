using System.Collections.Generic;
using UnityEngine;

public class ParkingRegistrator : MonoBehaviour
{
    private GridCalculator _gridCalculator;
    private Car[,] _gridCells;

    public void Initialize(int width, int height, float cellSize, GridCalculator gridCalculator)
    {
        _gridCalculator = gridCalculator;
        _gridCalculator.Initialize(width, height, cellSize, this);
        _gridCells = _gridCalculator.InitializeGrid();
    }

    public void RegisterCar(Vector3 newPosition, Car car)
    {
        Vector2Int newCell = _gridCalculator.WorldToGrid(newPosition);
        newCell = _gridCalculator.ClampToGrid(newCell);

        if (_gridCells[newCell.x, newCell.y] == null)
        {
            _gridCells[newCell.x, newCell.y] = car;
            car.transform.position = _gridCalculator.GridToWorld(newCell);
        }
    }

    public void UnregisterCar(Vector3 oldPosition, Car car)
    {
        Vector2Int oldCell = _gridCalculator.WorldToGrid(oldPosition);
        oldCell = _gridCalculator.ClampToGrid(oldCell);

        if (_gridCells[oldCell.x, oldCell.y] == car)
            _gridCells[oldCell.x, oldCell.y] = null;
    }

    public void RegisterTail(Car car, Vector3 current, CarOrientation orientation, float sign, float length)
    {
        float oppositeDirection = -1f;
        float exclusiveDistance = 1f;
        Vector3 target = _gridCalculator.CalculateTargetPosition(current, orientation, sign * oppositeDirection, length - exclusiveDistance);
        List<Vector2Int> tailCells = _gridCalculator.GetVisitedCells(target, current);

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

    public Vector3 GetFurthestCellToMove(Car car, Vector3 currentPosition, CarOrientation orientation,
                                            float sign, out CellOccupancy cellOccupancy)
    {
        return _gridCalculator.GetFurthestCellToMove(car, currentPosition, orientation, sign, out cellOccupancy);
    }

    public Car GetCar(int gridX, int gridY)
    {
        return _gridCells[gridX, gridY];
    }
}
