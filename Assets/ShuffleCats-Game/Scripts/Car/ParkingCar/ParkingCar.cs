using System;
using System.Collections;
using UnityEngine;

public class ParkingCar : MonoBehaviour
{
    private ParkingRegistrator _parkingRegistrator;
    private Mover _mover;
    private CarOrientation _orientation = CarOrientation.Horizontal;
    private float _signDirection = 1;
    private int _length;
    private Vector3 _carPosition;
    private bool _isMoving = false;
    private bool _isAtBorderCell = false;

    public event Action Bumped;
    public event Action IsOnBorder;

    public void Initialize(ParkingRegistrator parkingRegistrator, Mover mover,
                        CarOrientation orientation, float sign, int length)
    {
        _parkingRegistrator = parkingRegistrator;
        _mover = mover;
        _orientation = orientation;
        _signDirection = sign;
        _length = length;

        _carPosition = transform.position;
        _parkingRegistrator.RegisterCar(_carPosition, this);
        _parkingRegistrator.RegisterTail(this, _carPosition,
                            _orientation, _signDirection, _length);
    }

    public void OnClick()
    {
        if (_isMoving)
            return;

        Vector3 furthestSlotToMoveIn = DetermineNextSlot(out CellOccupancy cellOccupancy);

        if (cellOccupancy == CellOccupancy.Car)
        {
            Bumped?.Invoke();
        }

        if (cellOccupancy == CellOccupancy.Border)
        {
            _isAtBorderCell = true;
        }

        StartCoroutine(SmoothMoveTo(furthestSlotToMoveIn));
    }

    public Vector3 DetermineNextSlot(out CellOccupancy cellOccupancy)
    {
        _carPosition = transform.position;

        return _parkingRegistrator.GetFurthestCellToMove(this, _carPosition,
                            _orientation, _signDirection, out cellOccupancy);
    }

    public void OnFinishedMoving()
    {
        _isMoving = false;

        if (_isAtBorderCell)
        {
            IsOnBorder?.Invoke();
            return;
        }

        _parkingRegistrator.RegisterCar(transform.position, this);
        _parkingRegistrator.RegisterTail(this, transform.position,
                            _orientation, _signDirection, _length);
    }

    public void Deactivate()
    {
        _parkingRegistrator.UnregisterCar(transform.position, this,
                            _orientation, _signDirection, _length);
    }

    private IEnumerator SmoothMoveTo(Vector3 target)
    {
        _isMoving = true;
        _parkingRegistrator.UnregisterCar(transform.position, this,
                            _orientation, _signDirection, _length);

        _mover.MoveTo(target);

        yield return new WaitUntil(() => _isMoving == false);
    }
}
