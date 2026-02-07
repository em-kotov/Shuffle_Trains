using System;
using System.Collections;
using UnityEngine;

public class Car : MonoBehaviour
{
    [SerializeField] private ParkingSlotsHandler _parkingSlotHandler;
    [SerializeField] private Mover _mover;
    [SerializeField] private CarOrientation _orientation = CarOrientation.Horizontal;
    [SerializeField] private float _signDirection = 1;
    [SerializeField] private int _length;

    private Vector3 _carStartPosition;
    private bool _isMoving = false;

    public event Action Bumped;

    private void Start()
    {
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
    }

    public void OnFinishedMoving()
    {
        _parkingSlotHandler.RegisterCar(transform.position, this);
        _parkingSlotHandler.RegisterTail(this, transform.position, _orientation, _signDirection, _length);
        _isMoving = false;
    }

    private IEnumerator SmoothMoveTo(Vector3 target)
    {
        _isMoving = true;
        _parkingSlotHandler.UnregisterCar(transform.position, this);

        _mover.MoveTo(target);

        yield return new WaitUntil(() => _isMoving == false);
    }
}
