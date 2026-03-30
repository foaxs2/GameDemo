using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    public Tilemap fogMap;
    public Tilemap groundMap;
    public Tilemap eventMap; 

    public GameObject exitDialog; // Giao diện bảng hỏi Có/Không
    public GameObject leftMessage; // Giao diện thông báo kết thúc

    private Vector3Int currentCellPosition;
    private bool isInteracting = false; // Khóa di chuyển khi đang chọn UI
    private bool hasLeftDungeon = false; // Khóa hoàn toàn khi đã rời đi

    void Start()
    {
        currentCellPosition = fogMap.WorldToCell(transform.position);
        SnapToGrid();
        ClearFog(currentCellPosition);

        if (exitDialog != null) exitDialog.SetActive(false);
        if (leftMessage != null) leftMessage.SetActive(false);
    }

    void Update()
    {
        // Vô hiệu hóa phím mũi tên nếu đang hiện bảng hỏi hoặc đã rời dungeon
        if (isInteracting || hasLeftDungeon) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.upArrowKey.wasPressedThisFrame) MovePlayer(Vector3Int.up);
        if (keyboard.downArrowKey.wasPressedThisFrame) MovePlayer(Vector3Int.down);
        if (keyboard.leftArrowKey.wasPressedThisFrame) MovePlayer(Vector3Int.left);
        if (keyboard.rightArrowKey.wasPressedThisFrame) MovePlayer(Vector3Int.right);
    }

    void MovePlayer(Vector3Int direction)
    {
        Vector3Int targetPosition = currentCellPosition + direction;

        if (groundMap.HasTile(targetPosition))
        {
            currentCellPosition = targetPosition;
            SnapToGrid();
            ClearFog(currentCellPosition);

            // Kiểm tra xem vị trí vừa bước tới có phải là cánh cửa không
            if (eventMap != null && eventMap.HasTile(currentCellPosition))
            {
                TriggerDoorEvent();
            }
        }
    }

    void SnapToGrid()
    {
        transform.position = fogMap.GetCellCenterWorld(currentCellPosition);
    }

    void ClearFog(Vector3Int cellPos)
    {
        if (fogMap.HasTile(cellPos))
        {
            fogMap.SetTile(cellPos, null);
        }
    }

    private void TriggerDoorEvent()
    {
        isInteracting = true; // Khóa di chuyển
        exitDialog.SetActive(true); // Hiển thị bảng hỏi
    }

    // Hàm gắn vào nút "Có"
    public void ConfirmLeave()
    {
        exitDialog.SetActive(false);
        SceneManager.LoadScene("Town");
    }

    // Hàm gắn vào nút "Không"
    public void CancelLeave()
    {
        exitDialog.SetActive(false);
        isInteracting = false; // Mở khóa di chuyển, người chơi có thể đi tiếp
    }
}