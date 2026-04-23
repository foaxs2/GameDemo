using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    public Tilemap fogMap;
    public Tilemap groundMap;
    public Tilemap eventMap;
    public Tilemap enemyMap;

    public GameObject exitDialog; // Giao diện bảng hỏi Có/Không
    public GameObject leftMessage; // Giao diện thông báo kết thúc
    // BIẾN STATIC: Lưu vị trí người chơi để quay lại từ Combat
    public static Vector3Int savedDungeonPosition;
    public static bool isReturningFromCombat = false;
    public static int currentFloor = 1; // Tầng hiện tại
    public static int currentMapSeed = 100; // Seed để giữ bản đồ không đổi khi load lại scene

    // LƯU TRÚ VĨNH VIỄN FOG VÀ ENEMY MAP
    public static System.Collections.Generic.HashSet<Vector3Int> clearedFogTiles = new System.Collections.Generic.HashSet<Vector3Int>();
    public static System.Collections.Generic.HashSet<Vector3Int> defeatedEnemiesTiles = new System.Collections.Generic.HashSet<Vector3Int>();
    public static Vector3Int combatEnemyPosition; // Vị trí quái đang đánh

    private Vector3Int currentCellPosition;
    private bool isInteracting = false; // Khóa di chuyển khi đang chọn UI
    private bool hasLeftDungeon = false; // Khóa hoàn toàn khi đã rời đi

    void Start()
    {
        // Kiểm tra nếu vừa quay lại từ trận đánh
        if (isReturningFromCombat)
        {
            // Sinh lại CHÍNH XÁC bản đồ cũ bằng Seed đã lưu
            if (DungeonGenerator.Instance != null)
            {
                DungeonGenerator.Instance.GenerateDungeon(currentFloor, currentMapSeed);
            }

            currentCellPosition = savedDungeonPosition;
            isReturningFromCombat = false;

            // 1. Phục hồi sương mù đã mở
            if (fogMap != null)
            {
                foreach (Vector3Int cell in clearedFogTiles)
                    fogMap.SetTile(cell, null);
            }

            // 2. Xóa các enemy đã đánh bại khỏi Map
            if (enemyMap != null)
            {
                foreach (Vector3Int cell in defeatedEnemiesTiles)
                    enemyMap.SetTile(cell, null);
            }
        }
        else
        {
            // Lần đầu vào Dungeon hoặc Xuống tầng mới -> Bốc Seed mới
            currentMapSeed = Random.Range(100, 999999);

            if (DungeonGenerator.Instance != null)
            {
                DungeonGenerator.Instance.GenerateDungeon(currentFloor, currentMapSeed);
            }

            // Tầng mới / Lần đầu -> reset toàn bộ sương mù và xác quái vật
            clearedFogTiles.Clear();
            defeatedEnemiesTiles.Clear();

            // Vì DungeonGenerator vừa dịch chuyển Transform của Player đến vị trí rỗng trên map hiện tại
            // Ta set lại currentCellPosition dựa trên Transform mới đó.
            currentCellPosition = fogMap.WorldToCell(transform.position);
        }

        SnapToGrid();
        ClearFog(currentCellPosition);

        if (exitDialog != null) exitDialog.SetActive(false);
        if (leftMessage != null) leftMessage.SetActive(false);
    }

    void Update()
    {
        if (isInteracting || hasLeftDungeon) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) MovePlayer(Vector3Int.up);
        if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) MovePlayer(Vector3Int.down);
        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) MovePlayer(Vector3Int.left);
        if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) MovePlayer(Vector3Int.right);
    }

    void MovePlayer(Vector3Int direction)
    {
        Vector3Int targetPosition = currentCellPosition + direction;

        if (groundMap.HasTile(targetPosition))
        {
            currentCellPosition = targetPosition;
            SnapToGrid();
            ClearFog(currentCellPosition);

            // -- HỆ THỐNG TIÊU HAO NHU YẾU PHẨM & TRỪNG PHẠT --
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.food -= 1;
                if (PlayerManager.Instance.food < 0) 
                    PlayerManager.Instance.food = 0;

                if (PlayerManager.Instance.food == 0)
                {
                    // Lỗi đói: Phạt 2 HP mỗi bước di chuyển
                    PlayerManager.Instance.currentHP -= 2;
                    if (PlayerManager.Instance.currentHP <= 0)
                    {
                        PlayerManager.Instance.currentHP = 0;
                        Debug.Log("Người chơi gục ngã vì đói khát...");
                        // Có thể gọi Game Over tại đây
                    }
                    
                    // Lỗi đói: Tỷ lệ mất 1 SEN tăng lên 12%
                    if (UnityEngine.Random.Range(0f, 100f) < 12f)
                        PlayerManager.Instance.ReduceSanity(1);
                }
                else
                {
                    // Bình thường: Tỷ lệ mất 1 SEN là 5%
                    if (UnityEngine.Random.Range(0f, 100f) < 5f)
                        PlayerManager.Instance.ReduceSanity(1);
                }
            }

            // 1. KIỂM TRA SỰ KIỆN CHIẾN ĐẤU (EnemyMap)
            if (enemyMap != null && enemyMap.HasTile(currentCellPosition))
            {
                TriggerCombatEvent();
                return; // Ngừng kiểm tra các sự kiện khác
            }

            // 2. KIỂM TRA CỬA RA (EventMap)
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
            clearedFogTiles.Add(cellPos); // Lưu lại ô này đã được mở
        }
    }
    private void TriggerCombatEvent()
    {
        // Lưu vị trí hiện tại
        savedDungeonPosition = currentCellPosition;
        combatEnemyPosition = currentCellPosition; // Lưu vị trí con quái để Combat truyền về
        isReturningFromCombat = true;

        // Tạm thời không xóa trực tiếp bằng Map ở đây mà giao cho CheckBattleEnd xử lý sau trận
        // Nếu thắng thì nó sẽ được ghi vào danh sách defeatedEnemiesTiles

        Debug.Log("Đụng độ kẻ thù! Đang chuyển sang Scene Combat...");
        SceneManager.LoadScene("Combat"); // Thay tên đúng với Scene chiến đấu của bạn
    }
    private void TriggerDoorEvent()
    {
        isInteracting = true; // Khóa di chuyển
        exitDialog.SetActive(true); // Hiển thị bảng hỏi
    }

    // Hàm gắn vào nút "Có" (Trở về Thị Trấn)
    public void ConfirmLeave()
    {
        exitDialog.SetActive(false);

        // Đánh dấu đã qua tầng và Về Thành
        if (currentFloor < 10)
        {
            currentFloor++;
            Debug.Log("Thoát ải thành công! Chuẩn bị cho Tầng " + currentFloor + ". Đang trở về Thị Trấn...");
        }
        else
        {
            Debug.Log("Chinh phục thành công Hầm Ngục 10 Tầng! Đang trở về Thị Trấn...");
            // Sinh xong 10 tầng thì Reset về 1 cho lần chơi sau
            currentFloor = 1;
        }

        // THÊM DÒNG NÀY: Làm mới Cửa hàng khi về làng
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.RefreshShop();
        }
        SceneManager.LoadScene("Town"); 
    }

    // Hàm gắn vào nút "Không"
    public void CancelLeave()
    {
        exitDialog.SetActive(false);
        isInteracting = false; // Mở khóa di chuyển, người chơi có thể đi tiếp
    }
}