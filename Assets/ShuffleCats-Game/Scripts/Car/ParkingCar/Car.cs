using System;
using System.Collections;
using UnityEngine;

public class Car : MonoBehaviour
{
    private ParkingRegistrator _parkingRegistrator;
    private Mover _mover;
    private CarOrientation _orientation = CarOrientation.Horizontal;
    private float _signDirection = 1;
    private int _length;
    private Vector3 _carStartPosition;
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

        _carStartPosition = transform.position;
        _parkingRegistrator.RegisterCar(_carStartPosition, this);
        _parkingRegistrator.RegisterTail(this, _carStartPosition,
                            _orientation, _signDirection, _length);
    }

    public void OnClick()
    {
        if (_isMoving)
            return;

        _carStartPosition = transform.position;

        Vector3 furthestSlotToMoveIn = _parkingRegistrator.GetFurthestCellToMove(
                                    this, _carStartPosition, _orientation, 
                                    _signDirection, out CellOccupancy cellOccupancy);

        // if (transform.position != furthestSlotToMoveIn)
        // {
        StartCoroutine(SmoothMoveTo(furthestSlotToMoveIn));
        // }

        if (cellOccupancy == CellOccupancy.Car)
        {
            Bumped?.Invoke();
        }

        if (cellOccupancy == CellOccupancy.Border)
        {
            _isAtBorderCell = true;
        }
    }

    public void OnFinishedMoving()
    {
        Debug.Log("Car on finished moving");
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
