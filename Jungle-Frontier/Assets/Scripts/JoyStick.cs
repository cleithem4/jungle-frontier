using UnityEngine.EventSystems;
using UnityEngine;

public class JoyStick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public RectTransform background;
    public RectTransform handle;

    private Vector2 inputVector = Vector2.zero;
    private Vector2 startPos;
    public float deadZone = 0.1f; // added dead zone for smoother control

    public Vector2 InputDirection => inputVector;

    public void OnPointerDown(PointerEventData eventData)
    {
        background.position = eventData.position;
        background.gameObject.SetActive(true);
        startPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 direction = eventData.position - startPos;
        float radius = background.sizeDelta.x * 0.5f;
        Vector2 rawInput = direction / radius;

        // Apply dead zone
        if (rawInput.magnitude < deadZone)
        {
            inputVector = Vector2.zero;
        }
        else
        {
            inputVector = Vector2.ClampMagnitude(rawInput, 1.0f);
        }

        handle.anchoredPosition = inputVector * radius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
        background.gameObject.SetActive(false);
    }
}
