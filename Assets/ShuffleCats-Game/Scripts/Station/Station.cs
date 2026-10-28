using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Station : SeatTracker
{
    [SerializeField] private SpriteRenderer _model;
    [SerializeField] private List<Transform> _holdPoints;

    private List<Passenger> _passengers;
    private SorterCar _currentSorterCar;
    private Transform _presentation;
    private Vector3 _presentationPosition;

    public CatColor CatColor { get; private set; }

    public SpriteRenderer ThemeTarget => _model;

    public void SetPortraitPresentation(bool isPortraitActive)
    {
        const float portraitScale = 0.8f;
        const float horizontalPositionLimit = 8.3f;

        if (_model == null || (isPortraitActive == false && _presentation == null))
            return;

        if (_presentation == null)
        {
            // Scale only artwork and seats; the trigger and stopping point must stay on the track.
            _presentation = new GameObject("StationVisuals").transform;
            _presentation.SetParent(transform, false);
            _presentation.position = _model.transform.position;
            _presentationPosition = _presentation.localPosition;
            _model.transform.SetParent(_presentation, true);

            foreach (Transform holdPoint in _holdPoints)
                holdPoint.SetParent(_presentation, true);
        }

        _presentation.localPosition = _presentationPosition;
        float presentationScale;

        if (isPortraitActive)
        {
            presentationScale = portraitScale;
        }
        else
        {
            presentationScale = 1f;
        }

        _presentation.localScale = Vector3.one * presentationScale;

        if (isPortraitActive)
        {
            Vector3 position = _presentation.position;
            position.x = Mathf.Clamp(position.x, -horizontalPositionLimit, horizontalPositionLimit);
            _presentation.position = position;
        }
    }

    public void Initialize()
    {
        _passengers = new();
        this.Initialize(null, _holdPoints);
        SetColor(CatColor.Uncolored);
        ClearCurrentCar();
    }

    public void UpdatePassengers(List<Passenger> passengers)
    {
        if (passengers.Count <= 0)
            return;

        SetColor(passengers[0].CatColor);

        for (int i = 0; i < passengers.Count; i++)
        {
            Transform holdPoint = passengers[i].transform.parent;
            this.AddPassenger(passengers[i], holdPoint);
            _passengers.Add(passengers[i]);
        }
    }

    public override void RemovePassenger(Passenger passenger)
    {
        base.RemovePassenger(passenger);
        _passengers.Remove(passenger);

        if (_passengers.Count == 0)
        {
            SetColor(CatColor.Uncolored);
        }
    }

    public List<Passenger> GetPickupPassengers(CatColor catColor, int seatsCount)
    {
        List<Passenger> pickPassengers = new();

        if (_passengers.Count == 0 || seatsCount == 0)
            return pickPassengers;

        pickPassengers = GetPassengersByColor(catColor, seatsCount);

        return pickPassengers;
    }

    public bool TrySetCurrentCar(SorterCar sorterCar)
    {
        if (_currentSorterCar == null)
        {
            _currentSorterCar = sorterCar;
            return true;
        }

        return false;
    }

    public void ClearCurrentCar()
    {
        _currentSorterCar = null;
    }

    public bool HavePassengers(out int passengerCount, out CatColor catColor)
    {
        passengerCount = _passengers.Count;
        catColor = CatColor;
        return _passengers.Count > 0;
    }

    private void OnDrawGizmos()
    {
        const float seatGizmoRadius = 0.15f;

        List<(Transform HoldPoint, Passenger Passenger)> gizmoSeats = new();

        if (gizmoSeats.Count == 0)
        {
            foreach (Transform holdPoint in _holdPoints)
                gizmoSeats.Add((holdPoint, null));
        }

        for (int i = 0; i < gizmoSeats.Count; i++)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(gizmoSeats[i].HoldPoint.position, seatGizmoRadius);
        }
    }

    private void SetColor(CatColor catColor)
    {
        CatColor = catColor;
    }

    private List<Passenger> GetPassengersByColor(CatColor catColor, int count)
    {
        List<Passenger> passengers = new();

        List<Passenger> temporary = _passengers.FindAll(
            passenger => passenger.CatColor == catColor).ToList();

        if (temporary.Count <= count)
            return temporary;

        for (int i = 0; i < count; i++)
        {
            passengers.Add(temporary[i]);
        }

        return passengers;
    }
}
