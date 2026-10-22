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
    private ParkingCar _pendingBumpedCar;
    private Vector2 _pendingImpactDir;

    public bool IsMoving => _isMoving;
    public bool IsAtBorderCell => _isAtBorderCell;
    public System.Collections.Generic.List<ParkingCar> GetBlockingCars() =>
        _parkingRegistrator.GetBlockingCars(this, _orientation, _signDirection);

    public event Action Bumped;
    public event Action IsOnBorder;
    public event Action DepartureStarted;
    public event Action<Vector2> BumpReceived;
    public event Action<Vector2> BumpAttempted;

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

    public void OnPress()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMeow();
    }

    public void OnClick()
    {
        if (_isMoving)
            return;

        Vector3 furthestSlotToMoveIn = DetermineNextSlot(out CellOccupancy cellOccupancy, out ParkingCar bumpedCar);

        _pendingBumpedCar = null;
        bool willMove;

        if ((furthestSlotToMoveIn - _carPosition).sqrMagnitude > 0.0001f)
        {
            willMove = true;
        }
        else
        {
            willMove = false;
        }

        if (cellOccupancy == CellOccupancy.Car)
        {
            Bumped?.Invoke();

            if (bumpedCar != null)
            {
                Vector2 impactDir;

                if (_orientation == CarOrientation.Horizontal)
                {
                    impactDir = new Vector2(_signDirection, 0f);
                }
                else
                {
                    impactDir = new Vector2(0f, _signDirection);
                }

                if (willMove)
                {
                    _pendingBumpedCar = bumpedCar;
                    _pendingImpactDir = impactDir;
                }
                else
                {
                    BumpAttempted?.Invoke(impactDir);
                    bumpedCar.PlayBumpEffect(impactDir);
                }
            }
        }

        if (cellOccupancy == CellOccupancy.Border)
        {
            _isAtBorderCell = true;
            DepartureStarted?.Invoke();

            if (willMove == false)
            {
                IsOnBorder?.Invoke();
                return;
            }
        }

        if (willMove)
            StartCoroutine(SmoothMoveTo(furthestSlotToMoveIn));
    }

    public Vector3 DetermineNextSlot(out CellOccupancy cellOccupancy, out ParkingCar bumpedCar)
    {
        _carPosition = transform.position;

        return _parkingRegistrator.GetFurthestCellToMove(this, _carPosition,
                            _orientation, _signDirection, out cellOccupancy, out bumpedCar);
    }

    public void PlayBumpEffect(Vector2 impactDir)
    {
        BumpReceived?.Invoke(impactDir);
    }

    public void OnFinishedMoving()
    {
        _isMoving = false;

        if (_isAtBorderCell)
        {
            // Keep the border cell reserved while waiting, or another cat can move into it.
            _parkingRegistrator.RegisterCar(transform.position, this);
            _parkingRegistrator.RegisterTail(this, transform.position,
                                _orientation, _signDirection, _length);
            IsOnBorder?.Invoke();
            return;
        }

        _parkingRegistrator.RegisterCar(transform.position, this);
        _parkingRegistrator.RegisterTail(this, transform.position,
                            _orientation, _signDirection, _length);

        if (_pendingBumpedCar != null)
        {
            BumpAttempted?.Invoke(_pendingImpactDir);
            _pendingBumpedCar.PlayBumpEffect(_pendingImpactDir);
            _pendingBumpedCar = null;
        }
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
        _parkingRegistrator.ReserveCells(target, this,
                            _orientation, _signDirection, _length);

        _mover.MoveTo(target);

        yield return new WaitUntil(() => _isMoving == false);
    }
}
