using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[System.Serializable]
public class DungeonFloorData
{
    public int minSize = 5;
    public int maxSize = 8;
    public int maxEnemies = 3;
    public int maxEvents = 2; // Rương, bẫy...
}

public class DungeonGenerator : MonoBehaviour
{
    public static DungeonGenerator Instance;

    [Header("Core Tilemaps")]
    public Tilemap groundMap;
    public Tilemap fogMap;
    public Tilemap enemyMap;
    public Tilemap eventMap;

    [Header("Player Ref")]
    public Transform playerTransform;

    [Header("Floor Configs (Tầng 1 -> Tầng N)")]
    public List<DungeonFloorData> floorConfigs = new List<DungeonFloorData>();

    [Header("Tile Assets (Gạch sinh ra)")]
    public TileBase groundTile;
    public TileBase fogTile;
    public TileBase exitTile;
    public List<TileBase> enemyTiles = new List<TileBase>();
    
    [Header("Boss Configs Linh Hoạt (Tầng 10, 20...)")]
    [Tooltip("Danh sách cấu hình Boss theo tầng. Để trống sẽ fallback về tầng 10 & 20 mặc định.")]
    public List<BossFloorConfig> bossFloorConfigs = new List<BossFloorConfig>();

    [Header("Quái Tinh Anh (Elite Encounter)")]
    [Tooltip("Danh sách các tầng xuất hiện ô Quái Tinh Anh (không tính tầng Boss)")]
    public List<int> eliteEncounterFloors = new List<int>() { 3, 7, 12, 16, 18 };
    [Tooltip("Tile đại diện cho Quái Tinh Anh trên bản đồ (EnemyMap)")]
    public TileBase eliteEnemyTile;

    [Header("Random Event Tiles (Rương, Bẫy...)")]
    public List<TileBase> eventTiles = new List<TileBase>();

    [Header("Ô Vàng (Nhặt vàng trực tiếp)")]
    [Tooltip("Tile hiển thị ô vàng. Kéo Tile Asset vào đây trong Inspector.")]
    public TileBase goldTile;
    [Range(0f, 100f)]
    [Tooltip("Tỷ lệ % mỗi ô còn trống được đặt thành ô vàng.")]
    public float goldTileChance = 5f;
    [Tooltip("Vàng tối thiểu nhận khi bước vào ô vàng.")]
    public int goldMin = 20;
    [Tooltip("Vàng tối đa nhận khi bước vào ô vàng.")]
    public int goldMax = 60;

    [Header("Gạch Cửa Vào (Sẽ sinh dưới chân Player)")]
    public TileBase entryTileGen;

    [Header("Hệ thống Cửa Khóa (Sinh theo cặp)")]
    public TileBase keyTileGen;
    public TileBase lockedDoorTileGen;
    [Range(0f, 100f)] public float lockedDoorChance = 40f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    #region HELPER METHODS

    /// <summary>Kiểm tra xem một tầng có phải là tầng Boss không.</summary>
    public bool IsBossFloor(int floor)
    {
        if (bossFloorConfigs != null && bossFloorConfigs.Count > 0)
            return bossFloorConfigs.Exists(b => b != null && b.floorNumber == floor);
        
        // Fallback mặc định: Tầng 10 và Tầng 20 là Boss
        return (floor == 10 || floor == 20);
    }

    /// <summary>Lấy BossFloorConfig của một tầng cụ thể (nếu có).</summary>
    public BossFloorConfig GetBossFloorConfig(int floor)
    {
        if (bossFloorConfigs != null)
            return bossFloorConfigs.Find(b => b != null && b.floorNumber == floor);
        return null;
    }

    /// <summary>Kiểm tra xem một TileBase có phải là Tile của Boss hay không.</summary>
    public bool IsBossTile(TileBase tile)
    {
        if (tile == null || bossFloorConfigs == null) return false;

        foreach (var config in bossFloorConfigs)
        {
            if (config != null && config.bossTile != null && config.bossTile == tile)
                return true;
        }

        return false;
    }

    /// <summary>Kiểm tra xem một TileBase có phải là Tile Quái Tinh Anh (Elite) hay không.</summary>
    public bool IsEliteTile(TileBase tile)
    {
        if (tile == null || eliteEnemyTile == null) return false;
        return (tile == eliteEnemyTile);
    }

    #endregion

    public int TotalTiles { get; private set; } = 42;
    public int CurrentFloorWidth { get; private set; } = 7;
    public int CurrentFloorHeight { get; private set; } = 6;

    public struct FloorPreset
    {
        public int width;
        public int height;
        public int monsters;
        public int events;
    }

    /// <summary>
    /// Lấy cấu hình chuẩn cho từng tầng theo bảng thiết kế CanKiemTra.md: Width = 7 cố định.
    /// </summary>
    public FloorPreset GetFloorPreset(int floor)
    {
        switch (floor)
        {
            case 1:  return new FloorPreset { width = 7, height = 7,  monsters = 5,  events = 4 };
            case 2:  return new FloorPreset { width = 7, height = 7,  monsters = 5,  events = 4 };
            case 3:  return new FloorPreset { width = 7, height = 7,  monsters = 5,  events = 4 }; // +1 Elite
            case 4:  return new FloorPreset { width = 7, height = 7,  monsters = 6,  events = 4 };
            case 5:  return new FloorPreset { width = 7, height = 7,  monsters = 6,  events = 4 };
            case 6:  return new FloorPreset { width = 7, height = 8,  monsters = 7,  events = 5 };
            case 7:  return new FloorPreset { width = 7, height = 8,  monsters = 7,  events = 5 }; // +1 Elite
            case 8:  return new FloorPreset { width = 7, height = 9,  monsters = 8,  events = 5 };
            case 9:  return new FloorPreset { width = 7, height = 9,  monsters = 8,  events = 5 };
            case 10: return new FloorPreset { width = 7, height = 9,  monsters = 5,  events = 8 }; // Boss 1 (+1 Boss)
            case 11: return new FloorPreset { width = 7, height = 9,  monsters = 8,  events = 5 };
            case 12: return new FloorPreset { width = 7, height = 10, monsters = 8,  events = 6 }; // +1 Elite
            case 13: return new FloorPreset { width = 7, height = 10, monsters = 8,  events = 6 };
            case 14: return new FloorPreset { width = 7, height = 11, monsters = 10, events = 6 };
            case 15: return new FloorPreset { width = 7, height = 11, monsters = 10, events = 6 };
            case 16: return new FloorPreset { width = 7, height = 12, monsters = 10, events = 7 }; // +1 Elite
            case 17: return new FloorPreset { width = 7, height = 12, monsters = 10, events = 7 };
            case 18: return new FloorPreset { width = 7, height = 13, monsters = 11, events = 8 }; // +1 Elite
            case 19: return new FloorPreset { width = 7, height = 13, monsters = 11, events = 8 };
            case 20: return new FloorPreset { width = 7, height = 14, monsters = 8,  events = 12 }; // Đại Boss (+1 Boss)
            default:
                int h = Mathf.Clamp(6 + (floor - 1) / 2, 6, 14);
                return new FloorPreset { width = 7, height = h, monsters = 8, events = 6 };
        }
    }

    public void GenerateDungeon(int currentFloor, int seed)
    {
        // Fix cứng trạng thái ngẫu nhiên bằng Seed để Map không đổi khi đánh quái xong quay lại
        Random.InitState(seed);

        // 1. XÁC ĐỊNH KÍCH THƯỚC MAP THEO BẢNG CHUẨN CAN KIEM TRA (Width = 7 Cố Định)
        bool isBoss = IsBossFloor(currentFloor);
        FloorPreset preset = GetFloorPreset(currentFloor);

        int width = preset.width;
        int height = preset.height;
        TotalTiles = width * height;
        CurrentFloorWidth = width;
        CurrentFloorHeight = height;

        // 2. Xóa sạch mọi thứ tàn dư của map cũ/vẽ tay & Reset Grid về (0,0,0)
        if (groundMap != null)
        {
            if (groundMap.layoutGrid != null) groundMap.layoutGrid.transform.position = Vector3.zero;
            groundMap.ClearAllTiles();
        }
        if (fogMap != null) fogMap.ClearAllTiles();
        if (enemyMap != null) enemyMap.ClearAllTiles();
        if (eventMap != null) eventMap.ClearAllTiles();

        List<Vector3Int> availableSlots = new List<Vector3Int>();

        // 3. Fill Ground và Fog: Tọa độ từ (0, 0) đến (width-1, height-1)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (groundTile != null && groundMap != null) groundMap.SetTile(pos, groundTile);
                if (fogTile != null && fogMap != null) fogMap.SetTile(pos, fogTile);
                
                availableSlots.Add(pos);
            }
        }

        // Tự động tìm hoặc gắn DungeonCameraController nếu chưa có trong scene
        DungeonCameraController camCtrl = DungeonCameraController.Instance;
        if (camCtrl == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                camCtrl = cam.GetComponent<DungeonCameraController>();
                if (camCtrl == null) camCtrl = cam.gameObject.AddComponent<DungeonCameraController>();
            }
        }
        if (camCtrl != null)
        {
            camCtrl.SetupFloorBounds(height);
        }
        else
        {
            // Fallback: tự căn giữa Camera.main về (3.5, 3.5, -10)
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 3.5f;
                cam.transform.position = new Vector3(3.5f, 3.5f, -10f);
            }
        }

        // 4. Đặt Player vào vị trí ngẫu nhiên
        Vector3Int playerCellPos = Vector3Int.zero;
        if (availableSlots.Count > 0 && playerTransform != null && groundMap != null)
        {
            int pIndex = Random.Range(0, availableSlots.Count);
            playerCellPos = availableSlots[pIndex];
            
            // PlayerMovement sẽ tự cập nhật currentCellPosition khi Start() chạy
            playerTransform.position = groundMap.GetCellCenterWorld(playerCellPos);
            
            // Đặt cửa ngay dưới chân Player
            if (entryTileGen != null && eventMap != null)
            {
                eventMap.SetTile(playerCellPos, entryTileGen);
            }
            availableSlots.RemoveAt(pIndex);
        }

        // 5. HỆ THỐNG LỐI RA (Cửa Khóa vs Cửa Gỗ)
        bool hasLockedDoor = false;

        if (isBoss)
        {
            // === TẦNG BOSS: CỬA RA LUÔN LUÔN LÀ CỬA KHÓA, KHÔNG SINH CHÌA KHÓA NGẪU NHIÊN ===
            if (lockedDoorTileGen != null && eventMap != null && availableSlots.Count > 0)
            {
                int dIndex = Random.Range(0, availableSlots.Count);
                eventMap.SetTile(availableSlots[dIndex], lockedDoorTileGen);
                availableSlots.RemoveAt(dIndex);
                hasLockedDoor = true;
                Debug.Log($"[DungeonGenerator] Tầng Boss {currentFloor}: Cửa Ra BẮT BUỘC là Cửa Khóa. Chìa khóa sẽ rơi ra khi tiêu diệt Boss!");
            }
        }
        else
        {
            // === TẦNG THƯỜNG: Đổ xúc xắc xem tầng có dùng Cửa Khóa + Chìa Khóa ngẫu nhiên không ===
            if (keyTileGen != null && lockedDoorTileGen != null && Random.Range(0f, 100f) <= lockedDoorChance && availableSlots.Count >= 2)
            {
                // Sinh Chìa khóa ngẫu nhiên
                int kIndex = Random.Range(0, availableSlots.Count);
                eventMap.SetTile(availableSlots[kIndex], keyTileGen);
                availableSlots.RemoveAt(kIndex);

                // Sinh Cửa bị khóa (Đây chính là lối ra của tầng này)
                int dIndex = Random.Range(0, availableSlots.Count);
                eventMap.SetTile(availableSlots[dIndex], lockedDoorTileGen);
                availableSlots.RemoveAt(dIndex);

                hasLockedDoor = true;
                Debug.Log($"[DungeonGenerator] Tầng {currentFloor}: Sinh Cửa Khóa & Chìa Khóa ngẫu nhiên!");
            }
        }

        // Nếu không có cửa khóa (hoặc xúc xắc xịt), sinh Cửa Gỗ mặc định
        if (!hasLockedDoor && availableSlots.Count > 0 && exitTile != null && eventMap != null)
        {
            int eIndex = Random.Range(0, availableSlots.Count);
            Vector3Int exitPos = availableSlots[eIndex];
            eventMap.SetTile(exitPos, exitTile);
            availableSlots.RemoveAt(eIndex);
            Debug.Log($"[DungeonGenerator] Tầng {currentFloor}: Sinh Cửa Gỗ mặc định!");
        }

        // 6. SỐ LƯỢNG SPAWN DỰA TRÊN BẢNG THIẾT KẾ CAN KIEM TRA
        int enemiesToSpawn = preset.monsters;
        int eventsToSpawn = preset.events;

        // 7. SINH BOSS (Chỉ tầng Boss - Luôn spawn ở ô xa Player nhất)
        if (isBoss && availableSlots.Count > 0 && enemyMap != null)
        {
            TileBase correctBossTile = GetCorrectBossTile(currentFloor);

            if (correctBossTile != null)
            {
                // Thuật toán tìm ô xa Player nhất theo khoảng cách Manhattan
                int furthestIndex = 0;
                int maxDistance = -1;

                for (int i = 0; i < availableSlots.Count; i++)
                {
                    Vector3Int slot = availableSlots[i];
                    int dist = Mathf.Abs(slot.x - playerCellPos.x) + Mathf.Abs(slot.y - playerCellPos.y);
                    if (dist > maxDistance)
                    {
                        maxDistance = dist;
                        furthestIndex = i;
                    }
                }

                Vector3Int bossPos = availableSlots[furthestIndex];
                enemyMap.SetTile(bossPos, correctBossTile);
                availableSlots.RemoveAt(furthestIndex);
                Debug.Log($"[DungeonGenerator] CHÚ Ý: BOSS ĐÃ XUẤT HIỆN tại {bossPos} (Khoảng cách {maxDistance} từ Player) - Tầng {currentFloor}");
            }
        }

        // 8. SINH QUÁI TINH ANH (ELITE ENCOUNTER - Nếu trúng tầng Elite và không phải tầng Boss)
        if (!isBoss && eliteEncounterFloors != null && eliteEncounterFloors.Contains(currentFloor) && eliteEnemyTile != null && availableSlots.Count > 0 && enemyMap != null)
        {
            int eliteIndex = Random.Range(0, availableSlots.Count);
            Vector3Int elitePos = availableSlots[eliteIndex];
            enemyMap.SetTile(elitePos, eliteEnemyTile);
            availableSlots.RemoveAt(eliteIndex);
            Debug.Log($"[DungeonGenerator] Ô Quái Tinh Anh (Elite) đã xuất hiện tại {elitePos} - Tầng {currentFloor}");
        }

        // 9. Sinh Quái Vật thường
        int spawnedEnemies = 0;
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            if (enemyTiles.Count == 0 || enemyMap == null || availableSlots.Count == 0) break;
            int rIndex = Random.Range(0, availableSlots.Count);
            Vector3Int spawnPos = availableSlots[rIndex];
            TileBase randomEnemyTile = enemyTiles[Random.Range(0, enemyTiles.Count)];
            enemyMap.SetTile(spawnPos, randomEnemyTile);
            availableSlots.RemoveAt(rIndex);
            spawnedEnemies++;
        }

        // 10. Đặt Sự Kiện ngẫu nhiên
        int spawnedEvents = 0;
        for (int i = 0; i < eventsToSpawn; i++)
        {
            if (eventTiles.Count == 0 || eventMap == null || availableSlots.Count == 0) break;
            int rIndex = Random.Range(0, availableSlots.Count);
            Vector3Int spawnPos = availableSlots[rIndex];
            TileBase randomEventTile = eventTiles[Random.Range(0, eventTiles.Count)];
            eventMap.SetTile(spawnPos, randomEventTile);
            availableSlots.RemoveAt(rIndex);
            spawnedEvents++;
        }

        // 11. Spawn Ô Vàng ngẫu nhiên vào các ô còn trống
        int spawnedGold = 0;
        if (goldTile != null && eventMap != null)
        {
            List<Vector3Int> goldCandidates = new List<Vector3Int>(availableSlots);
            foreach (Vector3Int slot in goldCandidates)
            {
                if (Random.Range(0f, 100f) < goldTileChance)
                {
                    eventMap.SetTile(slot, goldTile);
                    availableSlots.Remove(slot);
                    spawnedGold++;
                }
            }
        }

        string bossTag = isBoss ? " [BOSS FLOOR]" : "";
        Debug.Log($"Sinh thành công Tầng {currentFloor}{bossTag} - Cỡ {width}x{height} | Quái: {spawnedEnemies} | Event: {spawnedEvents} | Ô Vàng: {spawnedGold}");
    }

    /// <summary>Lấy Tile của Boss tương ứng với tầng hiện tại.</summary>
    private TileBase GetCorrectBossTile(int floor)
    {
        // 1. Tìm trong bossFloorConfigs
        BossFloorConfig config = GetBossFloorConfig(floor);
        if (config != null && config.bossTile != null)
            return config.bossTile;

        // 2. Fallback sang enemyTiles nếu chưa có boss tile nào
        if (enemyTiles != null && enemyTiles.Count > 0)
            return enemyTiles[0];

        return null;
    }

    /// <summary>
    /// Tính số lượng quái và event cần spawn dựa trên kích thước map theo công thức:
    /// - Ô đặc biệt = 20% tổng số ô (w * h)
    /// - Quái thường = 60% ô đặc biệt (Tối thiểu 3, Tối đa Trần 9)
    /// - Sự kiện = 40% ô đặc biệt (Tối thiểu 2, Tối đa Trần 6)
    /// (Boss Tile và Elite Tile được cộng thêm riêng)
    /// </summary>
    private void GetSpawnCounts(bool isBossFloor, int currentFloor, int w, int h, out int enemies, out int events)
    {
        int totalTiles = w * h;
        int specialTiles = Mathf.RoundToInt(totalTiles * 0.20f);

        int calculatedEnemies = Mathf.RoundToInt(specialTiles * 0.60f);
        int calculatedEvents  = Mathf.RoundToInt(specialTiles * 0.40f);

        // Áp dụng giới hạn Sàn (Min) và Trần (Max Cap):
        // Quái: 3 -> 9
        // Event: 2 -> 6
        enemies = Mathf.Clamp(calculatedEnemies, 3, 9);
        events  = Mathf.Clamp(calculatedEvents,  2, 6);
    }
}


