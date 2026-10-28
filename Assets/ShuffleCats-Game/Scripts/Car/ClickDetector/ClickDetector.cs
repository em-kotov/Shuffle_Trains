using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class ClickDetector : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
{
    private PhysicsRaycaster _raycaster;

    public event Action Pressed;

    public event Action Clicked;

    public bool InteractionAllowed { get; set; } = true;

    public void Initialize(PhysicsRaycaster raycaster)
    {
        _raycaster = raycaster;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (YandexSdk.IsPaused || InteractionAllowed == false)
            return;

        Pressed?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (YandexSdk.IsPaused || InteractionAllowed == false)
            return;

        Clicked?.Invoke();
    }
}
