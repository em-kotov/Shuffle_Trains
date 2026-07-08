using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class ClickDetector : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
{
    private PhysicsRaycaster _raycaster;

    public event Action Pressed;
    public event Action Clicked;

    public void Initialize(PhysicsRaycaster raycaster)
    {
        _raycaster = raycaster;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Pressed?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke();
    }
}
