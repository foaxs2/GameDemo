using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Component gắn trên RawImage_DungeonViewport để chuyển tiếp toàn bộ sự kiện chuột
/// (Hover, Click, Drag, Scroll) sang DungeonCameraController.
/// </summary>
public class DungeonViewportInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IScrollHandler, IPointerMoveHandler, IPointerExitHandler, IPointerEnterHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        DungeonCameraController.Instance?.OnPointerDown(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        DungeonCameraController.Instance?.OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        DungeonCameraController.Instance?.OnPointerUp(eventData);
    }

    public void OnScroll(PointerEventData eventData)
    {
        DungeonCameraController.Instance?.OnScroll(eventData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        DungeonCameraController.Instance?.OnPointerEnter(eventData);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        DungeonCameraController.Instance?.OnPointerMove(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        DungeonCameraController.Instance?.OnPointerExit(eventData);
    }
}
