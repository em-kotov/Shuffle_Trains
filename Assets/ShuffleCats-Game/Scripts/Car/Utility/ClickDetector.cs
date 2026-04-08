using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class ClickDetector : MonoBehaviour, IPointerClickHandler
{
    private PhysicsRaycaster _raycaster;

    public event Action Clicked;

    public void Initialize(PhysicsRaycaster raycaster)
    {
        _raycaster = raycaster;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke();
    }
}
