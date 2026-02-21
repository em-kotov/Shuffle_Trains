using System;
using System.Collections;
using UnityEngine;

public class Car : MonoBehaviour
{
    [SerializeField] private CarOrientation _orientation = CarOrientation.Horizontal;
    [SerializeField] private float _signDirection = 1;
    [SerializeField] private int _length;

    private ParkingSlotsHandler _parkingSlotHandler;
    private Mover _mover;
    private Vector3 _carStartPosition;
    private bool _isMoving = false;
    private bool _isReadyToEnterTrack = false;

    public event Action Bumped;
    public event Action IsOnBorder;

    public void Initialize(ParkingSlotsHandler parkingHandler, Mover mover)
    {
        _parkingSlotHandler = parkingHandler;
        _mover = mover;
        _carStartPosition = transform.position;
        _parkingSlotHandler.RegisterCar(_carStartPosition, this);
    }

    public void OnClick()
    {
        if (_isMoving)
            return;

        _carStartPosition = transform.position;

        Vector3 furthestSlotToMoveIn = _parkingSlotHandler.GetFurthestCellToMove(this, _carStartPosition,
                                        _orientation, _signDirection, out CellOccupancy cellOccupancy);

        if (transform.position != furthestSlotToMoveIn)
        {
            StartCoroutine(SmoothMoveTo(furthestSlotToMoveIn));
        }

        if (cellOccupancy == CellOccupancy.Car)
        {
            Bumped?.Invoke();
        }

        if (cellOccupancy == CellOccupancy.Border)
        {
            _isReadyToEnterTrack = true;
        }
    }

    public void OnFinishedMoving()
    {
        _isMoving = false;

        if (_isReadyToEnterTrack)
        {
            IsOnBorder?.Invoke();
            return;
        }

        _parkingSlotHandler.RegisterCar(transform.position, this);
        _parkingSlotHandler.RegisterTail(this, transform.position, _orientation, _signDirection, _length);
    }

    private IEnumerator SmoothMoveTo(Vector3 target)
    {
        _isMoving = true;
        _parkingSlotHandler.UnregisterCar(transform.position, this);

        _mover.MoveTo(target);

        yield return new WaitUntil(() => _isMoving == false);
    }
}
