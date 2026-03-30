using UnityEngine;
using UnityEngine.InputSystem;

public class TownInteraction : MonoBehaviour
{
    public string locationName;

    void Update()
    {
        if (Mouse.current == null) return;

        // Kiểm tra thao tác nhấn chuột trái
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Lấy tọa độ chuột trên màn hình
            Vector2 mousePosition = Mouse.current.position.ReadValue();

            // Chuyển đổi tọa độ màn hình sang tọa độ không gian 2D trong game
            Vector2 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);

            // Bắn một tia (Raycast) tại vị trí click để kiểm tra va chạm
            RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);

            // Xác nhận tia bắn trúng Collider 2D của chính đối tượng chứa script này
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                Debug.Log("Bạn đã click vào: " + locationName);

                // Mã xử lý logic tiếp theo (mở giao diện, chuyển scene...) đặt tại đây
            }
        }
    }
}