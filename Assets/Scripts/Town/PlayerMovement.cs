using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public class EventMapping
{
    public UnityEngine.Tilemaps.TileBase eventTile; // Cục gạch trên bản đồ
    public EventData eventData; // Dữ liệu sự kiện tương ứng
}
public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance { get; private set; }
    public Tilemap fogMap;
    public Tilemap groundMap;
    public Tilemap eventMap;
    public Tilemap enemyMap;

    [Header("Hệ thống Sự kiện Map")]
    public TileBase keyTile;
    public TileBase lockedDoorTile;
    public TileBase exitTile;
    public TileBase entryTile;
    [Tooltip("Tile ô vàng. Phải khớp với goldTile trên DungeonGenerator.")]
    public TileBase goldTile;
    private bool isAtExitDoor = false;

    [Header("Mouse Interaction")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject hoverHighlight; // SpriteRenderer để hiện ô hover
    private static readonly Vector3Int[] FourDirs = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

    [Header("Kho Sự kiện Lựa chọn (Ghép Gạch với Sự Kiện)")]
    public System.Collections.Generic.List<EventMapping> eventMappings;
    // Biến lưu trữ số chìa khóa đang có trong tầng này
    public static int currentKeys = 0;

    public GameObject exitDialog;
    public GameObject leftMessage;

    [Header("Phí Rút Lui Về Town")]
    [Tooltip("Số vàng bị trừ khi người chơi rút lui qua Cửa Vào. Không áp dụng khi qua Cửa Ra.")]
    [SerializeField] private int retreatGoldCost = 30;
    [Tooltip("Text trong exitDialog hiển thị số vàng sẽ mất. Để trống nếu không dùng.")]
    public TextMeshProUGUI retreatCostText;
    // BIẾN STATIC: Lưu vị trí người chơi để quay lại từ Combat
    public static Vector3Int savedDungeonPosition;
    public static bool isReturningFromCombat = false;
    public static int currentFloor = 1; 
    public static int currentMapSeed = 100;

    // LƯU TRÚ VĨNH VIỄN FOG VÀ ENEMY MAP
    public static System.Collections.Generic.HashSet<Vector3Int> clearedFogTiles = new System.Collections.Generic.HashSet<Vector3Int>();
    public static System.Collections.Generic.HashSet<Vector3Int> defeatedEnemiesTiles = new System.Collections.Generic.HashSet<Vector3Int>();
    public static System.Collections.Generic.HashSet<Vector3Int> activatedEventTiles = new System.Collections.Generic.HashSet<Vector3Int>();
    public static Vector3Int combatEnemyPosition; // Vị trí quái đang đánh
    public static bool isBossFight = false;        // Đánh dấu lượt chiến này là boss

    // ── THEO DÕI VÀNG / EXP KIẾM ĐƯỢC TRONG TẦNG HIỆN TẠI ──────────────────
    // Reset khi vượt tầng thành công. Dùng để tính phạt khi chết/phát điên.
    public static int floorGoldEarned = 0;
    public static int floorExpEarned  = 0;

    /// <summary>Reset bộ đếm khi bắt đầu tầng mới.</summary>
    public static void ResetFloorTracking()
    {
        floorGoldEarned = 0;
        floorExpEarned  = 0;
    }

    /// <summary>
    /// Trừ phạt khi chết / phát điên: mất nửa vàng và nửa EXP kiếm được trong tầng.
    /// Vàng và EXP không bao giờ xuống âm.
    /// </summary>
    public static void ApplyDeathPenalty()
    {
        if (PlayerManager.Instance == null) return;

        int goldPenalty = floorGoldEarned / 2;
        int expPenalty  = floorExpEarned  / 2;

        PlayerManager.Instance.gold -= goldPenalty;
        if (PlayerManager.Instance.gold < 0) PlayerManager.Instance.gold = 0;

        // Trừ EXP: không rollback cấp độ, chỉ trừ currentExp xuống tối thiểu 0
        PlayerManager.Instance.currentExp -= expPenalty;
        if (PlayerManager.Instance.currentExp < 0) PlayerManager.Instance.currentExp = 0;

        Debug.Log($"[PENALTY] Chết/Điên tại tầng {currentFloor}: -{ goldPenalty} Vàng, -{expPenalty} EXP (nửa số kiếm được trong tầng).");
        ResetFloorTracking(); // Xóa bộ đếm sau khi đã áp hình phạt
    }

    private Vector3Int currentCellPosition;
    private bool isInteracting = false; 
    private bool hasLeftDungeon = false;
    private bool isEventLocked = false;   // Khóa di chuyển khi đang xem sự kiện
    private bool isDeathLocked = false;   // Khóa di chuyển khi đang hiện bảng chết/phát điên
    private bool isAtLockedDoor = false;  // Đang đứng trước cửa khóa
    private Vector3Int pendingDoorPosition; // Vị trí cửa đang chờ mở

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

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
            //3. Xóa các sự kiện đã kích hoạt khỏi Map
            if (eventMap != null)
            {
                foreach (Vector3Int cell in activatedEventTiles)
                    eventMap.SetTile(cell, null);
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
            activatedEventTiles.Clear();
            ResetFloorTracking(); // Tầng mới → xóa bộ đếm vàng/EXP
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
        // Highlight luôn cập nhật — nhưng tắt khi bị khóa
        bool isLocked = isInteracting || hasLeftDungeon || isEventLocked || isDeathLocked;
        if (isLocked)
        {
            ClearHighlight();
            return;
        }

        // === KEYBOARD (giữ nguyên) ===
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) MovePlayer(Vector3Int.up);
            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) MovePlayer(Vector3Int.down);
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) MovePlayer(Vector3Int.left);
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) MovePlayer(Vector3Int.right);

            // ── TEST SHORTCUT: Ấn phím 5 hoặc 0 để giả lập tầng boss ──
            if (keyboard.digit5Key.wasPressedThisFrame)
            {
                currentFloor = 5;
                isBossFight  = true;  // Bắt buộc CombatManager spawn đúng boss Nhện
                Debug.Log("[TEST] Tầng = 5 | isBossFight = true");
            }
            if (keyboard.digit0Key.wasPressedThisFrame)
            {
                currentFloor = 10;
                isBossFight  = true;
                Debug.Log("[TEST] Tầng = 10 | isBossFight = true");
            }

            // ── RESET GAME SHORTCUT: Shift + Delete ──
            if (keyboard.deleteKey.wasPressedThisFrame && keyboard.shiftKey.isPressed)
            {
                SaveSystem.DeleteSave();
                Debug.LogWarning("[TEST] Đã xóa save! Khởi động lại game...");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Town"); 
            }
        }

        // === MOUSE ===
        HandleMouse();
    }

    void HandleMouse()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) { ClearHighlight(); return; }

        Camera cam = (mainCamera != null) ? mainCamera : Camera.main;
        if (cam == null) { ClearHighlight(); return; }

        // Convert mouse position → world → tilemap cell
        Vector2 screenPos = mouse.position.ReadValue();
        Vector3 worldPos3 = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        Vector3Int hoveredCell = groundMap.WorldToCell(new Vector3(worldPos3.x, worldPos3.y, 0f));

        // Tìm hướng kề cạnh hợp lệ
        Vector3Int validDir = Vector3Int.zero;
        bool hasValidAdj = false;
        foreach (var dir in FourDirs)
        {
            if (hoveredCell == currentCellPosition + dir && groundMap.HasTile(currentCellPosition + dir))
            {
                validDir  = dir;
                hasValidAdj = true;
                break;
            }
        }

        // Di chuyển hoverHighlight đến ô hợp lệ
        if (hoverHighlight != null)
        {
            if (hasValidAdj)
            {
                Vector3 cellCenter = groundMap.GetCellCenterWorld(currentCellPosition + validDir);
                hoverHighlight.transform.position = new Vector3(cellCenter.x, cellCenter.y, hoverHighlight.transform.position.z);
                hoverHighlight.SetActive(true);
            }
            else
            {
                hoverHighlight.SetActive(false);
            }
        }

        // Click chuột — chỉ khi hover ô hợp lệ VÀ không click trúng UI
        if (hasValidAdj && mouse.leftButton.wasPressedThisFrame)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            MovePlayer(validDir);
        }
    }

    void ClearHighlight()
    {
        if (hoverHighlight != null) hoverHighlight.SetActive(false);
    }

    void MovePlayer(Vector3Int direction)
    {
        Vector3Int targetPosition = currentCellPosition + direction;

        // 1. KIỂM TRA VẬT CẢN TRƯỚC KHI BƯỚC VÀO
        if (eventMap != null && eventMap.HasTile(targetPosition))
        {
            TileBase targetEvent = eventMap.GetTile(targetPosition);

            // ĐỤNG CỮA KHÓA
            if (targetEvent == lockedDoorTile)
            {
                if (currentKeys <= 0)
                {
                    // KHÔNG CÓ CHÌA -> Chỉ hiện text, không di chuyển vào
                    if (FloatingTextManager.Instance != null)
                        FloatingTextManager.Instance.SpawnText(targetPosition, "🔑 Cần Chìa Khóa!", Color.red);
                    return; 
                }
                else
                {
                    // CÓ CHÌA KHÓA -> Hiện bảng xác nhận, không tiêu chìa hay xóa cửa ngay
                    isAtLockedDoor = true;
                    isAtExitDoor = true;
                    pendingDoorPosition = targetPosition; // Lưu vị trí cửa để xử lý khi xác nhận
                    TriggerDoorEvent();
                    return;
                }
            }
        }

        if (groundMap.HasTile(targetPosition))
        {
            currentCellPosition = targetPosition;
            SnapToGrid();
            ClearFog(currentCellPosition);

            // -- HỆ THỐNG TIÊU HAO NHU YẾU PHẨM & TRỪNG PHẠT --
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.food -= 1;
                if (PlayerManager.Instance.food < 0) PlayerManager.Instance.food = 0;

                if (PlayerManager.Instance.food == 0)
                {
                    // Lỗi đói: Phạt 2 HP mỗi bước di chuyển
                    PlayerManager.Instance.currentHP -= 2;
                    if (PlayerManager.Instance.currentHP <= 0)
                    {
                        // Dungeon Death do đói — hiện bảng thay vì về Town ngay
                        if (DungeonDeathUI.Instance != null)
                            DungeonDeathUI.Instance.ShowDeathPanel();
                        else
                        {
                            // Fallback nếu không có DungeonDeathUI trong scene
                            PlayerManager.Instance.currentHP = PlayerManager.Instance.maxHP;
                            DeathContext.Pending = DeathContext.DeathType.DungeonDeath;
                            SaveSystem.Instance?.Save();
                            UnityEngine.SceneManagement.SceneManager.LoadScene("Town");
                        }
                        return;
                    }

                    // Lỗi đói: Tỷ lệ mất 1 SEN tăng lên 16%
                    if (UnityEngine.Random.Range(0f, 100f) < 16f) PlayerManager.Instance.ReduceSanity(1);
                }
                else
                {
                    // Bình thường: Tỷ lệ mất 1 SEN là 5%
                    if (UnityEngine.Random.Range(0f, 100f) < 5f) PlayerManager.Instance.ReduceSanity(1);
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
                TileBase steppedEvent = eventMap.GetTile(currentCellPosition);

                if (steppedEvent == exitTile || steppedEvent == lockedDoorTile)
                {
                    isAtExitDoor = true; // Đang đứng ở cửa ra
                    TriggerDoorEvent();
                }
                else if (steppedEvent == entryTile)
                {
                    isAtExitDoor = false; // Đang đứng ở cửa vào 
                    TriggerDoorEvent();
                }
                else if (steppedEvent == keyTile)
                {
                    currentKeys++;
                    eventMap.SetTile(currentCellPosition, null); // Xóa chìa khóa khỏi map
                    activatedEventTiles.Add(currentCellPosition);
                    Debug.Log("Nhặt được 1 Chìa Khóa!");
                    if (FloatingTextManager.Instance != null)
                        FloatingTextManager.Instance.SpawnText(transform.position, "+1 Chìa khóa", Color.yellow);
                }
                else if (steppedEvent == goldTile)
                {
                    // NHẶT Ô VÀNG: +Random(goldMin, goldMax) vàng
                    int gMin = 20;
                    int gMax = 60;
                    if (DungeonGenerator.Instance != null)
                    {
                        gMin = DungeonGenerator.Instance.goldMin;
                        gMax = DungeonGenerator.Instance.goldMax;
                    }
                    int gained = UnityEngine.Random.Range(gMin, gMax + 1);
                    if (PlayerManager.Instance != null) PlayerManager.Instance.gold += gained;
                    floorGoldEarned += gained; // Track cho hình phạt chết
                    eventMap.SetTile(currentCellPosition, null);
                    activatedEventTiles.Add(currentCellPosition);
                    Debug.Log($"Nhặt được {gained} Vàng!");
                    if (FloatingTextManager.Instance != null)
                        FloatingTextManager.Instance.SpawnText(transform.position, $"+{gained} Vàng", Color.yellow);
                }
                else
                {
                    // KIỂM TRA ĐẠP TRÚNG SỰ KIỆN LỰA CHỌN THEO ĐÚNG HÌNH ẢNH GẠCH
                    if (eventMappings != null && eventMappings.Count > 0)
                    {
                        // Tìm xem cục gạch người chơi vừa đạp (steppedEvent) có nằm trong danh sách ghép cặp không
                        EventMapping mapping = eventMappings.Find(m => m.eventTile == steppedEvent);

                        if (mapping != null && mapping.eventData != null)
                        {
                            // Gọi đúng sự kiện đã được ghép cặp với hình ảnh gạch đó
                            if (EventManager.Instance != null)
                                EventManager.Instance.TriggerEvent(mapping.eventData);

                            // Xóa ô gạch đó đi để không bị đạp lại
                            eventMap.SetTile(currentCellPosition, null);
                            activatedEventTiles.Add(currentCellPosition);
                        }
                    }
                }

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
        combatEnemyPosition  = currentCellPosition;
        isReturningFromCombat = true;

        // Kiểm tra xem tile đang đứng có phải boss tile không
        if (DungeonGenerator.Instance != null && enemyMap != null)
        {
            TileBase tileHere = enemyMap.GetTile(currentCellPosition);
            isBossFight = DungeonGenerator.Instance.bossTiles.Contains(tileHere);
        }
        else isBossFight = false;

        SceneManager.LoadScene("Combat");
    }
    private void TriggerDoorEvent()
    {
        isInteracting = true; // Khóa di chuyển
        exitDialog.SetActive(true); // Hiển thị bảng hỏi

        // Cập nhật text hiển thị phí rút lui
        if (retreatCostText != null)
        {
            if (!isAtExitDoor)
            {
                // Cửa Vào → có phí rút lui
                retreatCostText.text = $"Quay về thị trấn sẽ mất {retreatGoldCost} vàng";
                retreatCostText.gameObject.SetActive(true);
            }
            else
            {
                // Cửa Ra / Cửa Khóa → không mất phí
                retreatCostText.gameObject.SetActive(false);
            }
        }

        // Ẩn HUDCanvas (DontDestroyOnLoad) để tránh chặn raycast lên các nút
        if (UIManager.Instance != null) UIManager.Instance.SetHUDVisible(false);
    }

    // Hàm gắn vào nút "Có" (Trở về Thị Trấn)
    public void ConfirmLeave()
    {
        exitDialog.SetActive(false);

        // Nếu là cửa khóa, tiêu chìa và xóa tile
        if (isAtLockedDoor && currentKeys > 0)
        {
            currentKeys--;
            if (eventMap != null) eventMap.SetTile(pendingDoorPosition, null);
            isAtLockedDoor = false;
        }

        // NẾU LÀ CỬA RA MẶC ĐỊNH THÌ MỚI TĂNG TẦNG
        if (isAtExitDoor)
        {
            if (currentFloor < 10) currentFloor++;
            else currentFloor = 1;
            Debug.Log("Tiến tới tầng tiếp theo!");
            ResetFloorTracking(); // Vượt tầng thành công → xóa bộ đếm, không bị phạt
            
            // Guild: Nhận 1 lần lương & Reset Nhiệm vụ khi hoàn thành tầng
            if (GuildManager.Instance != null)
            {
                GuildManager.Instance.AddSalaryClaim();
                GuildManager.Instance.RefreshBoard();
            }
        }
        else
        {
            // RÚT LUI qua Cửa Vào → trừ phí
            if (PlayerManager.Instance != null)
            {
                int actualCost = Mathf.Min(retreatGoldCost, PlayerManager.Instance.gold);
                PlayerManager.Instance.gold -= actualCost;
                Debug.Log($"Rút lui về Town, mất {actualCost} vàng.");
            }
        }

        if (ShopManager.Instance != null) ShopManager.Instance.RefreshShop();
        if (UIManager.Instance != null) UIManager.Instance.SetHUDVisible(true);
        SaveSystem.Instance?.Save();   // Lưu TRƯỚC khi load scene
        SceneManager.LoadScene("Town");
    }

    // Hàm gắn vào nút "Không"
    public void CancelLeave()
    {
        exitDialog.SetActive(false);
        if (UIManager.Instance != null) UIManager.Instance.SetHUDVisible(true);
        isInteracting = false; // Mở khóa di chuyển, người chơi có thể đi tiếp
        isAtLockedDoor = false;
        isAtExitDoor = false;
    }

    // Gọi từ EventManager để khóa/mở di chuyển khi đang xem sự kiện
    public void SetEventLock(bool locked)
    {
        isEventLocked = locked;
    }

    // Gọi từ DungeonDeathUI để khóa di chuyển khi bảng chết/phát điên đang hiện
    public void SetDeathLock(bool locked)
    {
        isDeathLocked = locked;
    }
}