using System.Collections.Generic;
using UnityEngine;

public class ParkingRegistrator : MonoBehaviour
{
    [SerializeField, Min(1f)] private float _normalTrackSpeedMultiplier = 1.7f;
    [Tooltip("Additional track speed multiplier after the final cat leaves parking.")]
    [SerializeField, Min(1f)] private float _finalCatsSpeedMultiplier = 2.7f / 1.7f;

    private GridCalculator _gridCalculator;
    private ParkingCar[,] _gridCells;
    private List<ParkingCar> _parkingCars;
    private float _cellSize;

    public float TrackSpeedMultiplier
    {
        get
        {
            float normalMultiplier = Mathf.Max(1f, _normalTrackSpeedMultiplier);
            float finalMultiplier;

            if (_parkingCars != null && _parkingCars.Count == 0)
            {
                finalMultiplier = Mathf.Max(1f, _finalCatsSpeedMultiplier);
            }
            else
            {
                finalMultiplier = 1f;
            }

            return normalMultiplier * finalMultiplier;
        }
    }

    public bool HasMovingCars
    {
        get
        {
            foreach (ParkingCar car in _parkingCars)
                if (car.IsMoving)
                    return true;

            return false;
        }
    }

    public void Initialize(int width, int height, float cellSize,
                    int startX, int startY, float axisYLevel,
                    GridCalculator gridCalculator, int totalCount)
    {
        _cellSize = cellSize;
        _gridCalculator = gridCalculator;
        _gridCalculator.Initialize(width, height, cellSize, startX,
                                startY, axisYLevel, this);
        _gridCells = _gridCalculator.InitializeGrid();
        _parkingCars = new();
    }

    public void RegisterCar(Vector3 newPosition, ParkingCar car)
    {
        Vector2Int newCell = _gridCalculator.WorldToGrid(newPosition);
        newCell = _gridCalculator.ClampToGrid(newCell);

        if (_gridCells[newCell.x, newCell.y] == null)
        {
            _gridCells[newCell.x, newCell.y] = car;
            car.transform.position = _gridCalculator.GridToWorld(newCell);
        }

        if (_parkingCars.Contains(car) == false)
            _parkingCars.Add(car);
    }

    // Reserve destination cells before motion so another cat cannot enter during the slide.
    public void ReserveCells(Vector3 targetPosition, ParkingCar car,
                        CarOrientation orientation, float sign, int length)
    {
        Vector2Int newCell = _gridCalculator.WorldToGrid(targetPosition);
        newCell = _gridCalculator.ClampToGrid(newCell);

        if (_gridCells[newCell.x, newCell.y] == null)
            _gridCells[newCell.x, newCell.y] = car;

        RegisterTail(car, targetPosition, orientation, sign, length);

        if (_parkingCars.Contains(car) == false)
            _parkingCars.Add(car);
    }

    public void UnregisterCar(Vector3 oldPosition, ParkingCar car,
                        CarOrientation orientation, float sign, float length)
    {
        Vector2Int oldCell = _gridCalculator.WorldToGrid(oldPosition);
        oldCell = _gridCalculator.ClampToGrid(oldCell);

        if (_gridCells[oldCell.x, oldCell.y] == car)
            _gridCells[oldCell.x, oldCell.y] = null;

        UnregisterTail(car, oldPosition, orientation, sign, length);

        if (_parkingCars.Contains(car))
            _parkingCars.Remove(car);
    }

    public void RegisterTail(ParkingCar car, Vector3 current,
                            CarOrientation orientation, float sign, float length)
    {
        float oppositeDirection = -1f;
        float tailDistance = (length - 1f) * _cellSize;
        Vector3 target = _gridCalculator.CalculateTargetPosition(current,
                        orientation, sign * oppositeDirection, tailDistance);
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

    public void UnregisterTail(ParkingCar car, Vector3 current,
                        CarOrientation orientation, float sign, float length)
    {
        float oppositeDirection = -1f;
        float tailDistance = (length - 1f) * _cellSize;
        Vector3 target = _gridCalculator.CalculateTargetPosition(current,
                        orientation, sign * oppositeDirection, tailDistance);
        List<Vector2Int> tailCells = _gridCalculator.GetVisitedCells(target, current);

        int minCountForRegisterTail = 2;

        if (tailCells.Count < minCountForRegisterTail)
        {
            return;
        }

        int tailStartIndex = 1;

        for (int i = tailStartIndex; i < tailCells.Count; i++)
        {
            if (_gridCells[tailCells[i].x, tailCells[i].y] == car)
            {
                _gridCells[tailCells[i].x, tailCells[i].y] = null;
            }
        }
    }

    public Vector3 GetFurthestCellToMove(ParkingCar car, Vector3 currentPosition,
                    CarOrientation orientation, float sign, out CellOccupancy cellOccupancy, out ParkingCar bumpedCar)
    {
        return _gridCalculator.GetFurthestCellToMove(car, currentPosition, orientation, sign, out cellOccupancy, out bumpedCar);
    }

    public ParkingCar GetCar(int gridX, int gridY)
    {
        return _gridCells[gridX, gridY];
    }

    public List<ParkingCar> GetBlockingCars(ParkingCar car, CarOrientation orientation, float sign)
    {
        var blockers = new List<ParkingCar>();
        Vector3 target = _gridCalculator.CalculateTargetPosition(car.transform.position,
            orientation, sign, (_gridCells.GetLength(0) + _gridCells.GetLength(1)) * _cellSize);

        foreach (Vector2Int cell in _gridCalculator.GetVisitedCells(target, car.transform.position))
        {
            ParkingCar occupant = _gridCells[cell.x, cell.y];

            if (occupant != null && occupant != car && blockers.Contains(occupant) == false)
                blockers.Add(occupant);
        }

        return blockers;
    }

    public bool IsPossibleToMove(out ParkingCar car)
    {
        car = null;

        if (_parkingCars.Count == 0)
            return true;

        for (int i = 0; i < _parkingCars.Count; i++)
        {
            car = _parkingCars[i];
            Vector3 nextSlot = car.DetermineNextSlot(out CellOccupancy cellOccupancy, out _);

            if (cellOccupancy == CellOccupancy.Border)
            {
                return true;
            }
        }

        return false;
    }
}
