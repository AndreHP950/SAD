using UnityEngine;
using UnityEngine.EventSystems;
using SAD.InputSystem;

public class MobileHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ButtonType { Accelerate, Brake }
    public ButtonType type;

    public void OnPointerDown(PointerEventData eventData)
    {
        SetState(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetState(false);
    }

    private void SetState(bool pressed)
    {
        switch (type)
        {
            case ButtonType.Accelerate:
                MobileInput.AccelerateHeld = pressed;
                break;
            case ButtonType.Brake:
                MobileInput.BrakeHeld = pressed;
                break;
        }
    }
}