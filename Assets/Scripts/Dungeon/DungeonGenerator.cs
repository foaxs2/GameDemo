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
    
    [Header("Boss Bosses (Sinh ở Tầng 5, 10...)")]
    public List<TileBase> bossTiles = new List<TileBase>();

    [Header("Random Event Tiles (Rương, Bẫy...)")]
    public System.Collections.Generic.List<UnityEngine.Tilemaps.TileBase> eventTiles = new System.Collections.Generic.List<UnityEngine.Tilemaps.TileBase>();

    [Header("Ô Vàng (Nhặt vàng trực tiếp)")]
    [Tooltip("Tile hiển thị ô vàng. Kéo Tile Asset vào đây trong Inspector.")]
    public UnityEngine.Tilemaps.TileBase goldTile;
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

    public void GenerateDungeon(int currentFloor, int seed)
    {
        // Fix cứng trạng thái ngẫu nhiên bằng Seed để Map không đổi khi đánh quái xong quay lại
        Random.InitState(seed);

        // 1. XÁC ĐỊNH KÍCH THƯỚC MAP THEO TẦNG
        bool isBossFloor = (currentFloor == 5 || currentFloor == 10);
        int width, height;

        if (isBossFloor)
        {
            // Tầng Boss (5, 10): LUÔN 9x9
            width = 9;
            height = 9;
        }
        else if (currentFloor <= 3)
        {
            // Tầng 1-3: Hình chữ nhật, mỗi chiều từ 5 đến 7
            width  = Random.Range(5, 8);  // 5, 6, hoặc 7
            height = Random.Range(5, 8);  // 5, 6, hoặc 7
        }
        else
        {
            // Tầng 4+: Hình chữ nhật, mỗi chiều từ 6 đến 9
            width  = Random.Range(6, 10); // 6, 7, 8, hoặc 9
            height = Random.Range(6, 10);
        }

        // 2. Xóa sạch mọi thứ tàn dư của map cũ/vẽ tay
        if (groundMap != null) groundMap.ClearAllTiles();
        if (fogMap != null) fogMap.ClearAllTiles();
        if (enemyMap != null) enemyMap.ClearAllTiles();
        if (eventMap != null) eventMap.ClearAllTiles();

        List<Vector3Int> availableSlots = new List<Vector3Int>();

        // 4. Fill Ground và Fog (Căn giữa map thay vì vẽ hết lên góc trên phải)
        int startX = -width / 2;
        int endX = width / 2;
        int startY = -height / 2;
        int endY = height / 2;

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (groundTile != null && groundMap != null) groundMap.SetTile(pos, groundTile);
                if (fogTile != null && fogMap != null) fogMap.SetTile(pos, fogTile);
                
                availableSlots.Add(pos);
            }
        }

        // 5. Đặt Player vào vị trí ngẫu nhiên
        if (availableSlots.Count > 0 && playerTransform != null && groundMap != null)
        {
            int pIndex = Random.Range(0, availableSlots.Count);
            Vector3Int pPos = availableSlots[pIndex];
            
            // PlayerMovement sẽ tự cập nhật currentCellPosition khi Start() chạy
            playerTransform.position = groundMap.GetCellCenterWorld(pPos);
            //Đặt cửa ngay dưới chân Player
            if (entryTileGen != null && eventMap != null)
            {
                eventMap.SetTile(pPos, entryTileGen);
            }
            availableSlots.RemoveAt(pIndex);
        }

        // 6. HỆ THỐNG LỐI RA (Chỉ chọn 1 trong 2 loại)
        bool hasLockedDoor = false;

        // Đổ xúc xắc xem tầng này có dùng Cửa Khóa làm lối ra không
        if (keyTileGen != null && lockedDoorTileGen != null && Random.Range(0f, 100f) <= lockedDoorChance && availableSlots.Count >= 2)
        {
            // Sinh Chìa khóa
            int kIndex = Random.Range(0, availableSlots.Count);
            eventMap.SetTile(availableSlots[kIndex], keyTileGen);
            availableSlots.RemoveAt(kIndex);

            // Sinh Cửa bị khóa (Đây chính là lối ra của tầng này)
            int dIndex = Random.Range(0, availableSlots.Count);
            eventMap.SetTile(availableSlots[dIndex], lockedDoorTileGen);
            availableSlots.RemoveAt(dIndex);

            hasLockedDoor = true;
            Debug.Log("Tầng này dùng Cửa Khóa làm lối ra!");
        }

        // Nếu xúc xắc xịt (không có cửa khóa), thì mới sinh Cửa gỗ mặc định
        if (!hasLockedDoor && availableSlots.Count > 0 && exitTile != null && eventMap != null)
        {
            int eIndex = Random.Range(0, availableSlots.Count);
            Vector3Int exitPos = availableSlots[eIndex];
            eventMap.SetTile(exitPos, exitTile);
            availableSlots.RemoveAt(eIndex);
            Debug.Log("Tầng này dùng Cửa Gỗ mặc định làm lối ra!");
        }

        // 7. TÍNH SỐ LƯỢNG SPAWN DỰA TRÊN KÍCH THƯỚC MAP
        GetSpawnCounts(isBossFloor, width, height, out int enemiesToSpawn, out int eventsToSpawn);

        // 8. Sinh Boss (chỉ tầng Boss)
        if (isBossFloor && bossTiles.Count > 0 && enemyMap != null)
        {
            // Lựa chọn Boss Tile dựa trên Tầng
            TileBase correctBossTile = bossTiles[0]; // Mặc định là Nhện
            if (currentFloor == 10 && bossTiles.Count > 1) 
                correctBossTile = bossTiles[1]; // Rồng
                
            // Sinh 1 tile Boss
            int bIndex = Random.Range(0, availableSlots.Count);
            Vector3Int bossPos = availableSlots[bIndex];
            enemyMap.SetTile(bossPos, correctBossTile);
            availableSlots.RemoveAt(bIndex);
            Debug.Log($"CHÚ Ý: TẦNG BOSS ĐÃ XUẤT HIỆN! (Tầng {currentFloor})");
        }

        // 9. Sinh Quái Vật thường (cả tầng Boss lẫn tầng thường đều có enemiesToSpawn quái)
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
            // Duyệt toàn bộ slot còn lại, mỗi slot có goldTileChance% xuất hiện
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

        string bossTag = isBossFloor ? " [BOSS FLOOR]" : "";
        Debug.Log($"Sinh thành công Tầng {currentFloor}{bossTag} - Cỡ {width}x{height} | Quái: {spawnedEnemies} | Event: {spawnedEvents} | Ô Vàng: {spawnedGold}");
    }

    /// <summary>
    /// Tính số lượng quái và event cần spawn dựa trên kích thước map.
    /// Ngưỡng tối thiểu: 3 quái, 2 event.
    /// Tầng Boss (isBossFloor): 3 quái + 3 event (Boss tile được xử lý riêng).
    /// </summary>
    private void GetSpawnCounts(bool isBossFloor, int w, int h, out int enemies, out int events)
    {
        // --- Tầng Boss: cố định ---
        if (isBossFloor)
        {
            enemies = 3;
            events  = 3;
            return;
        }

        // --- Tầng thường: dựa vào diện tích ---
        // Lấy chiều lớn hơn để phân loại nhóm kích thước
        int maxDim = Mathf.Max(w, h);
        int minDim = Mathf.Min(w, h);

        if (maxDim <= 7)
        {
            // Map nhỏ: 5x5 → 7x7 (bao gồm 5x7, 6x7...)
            enemies = 3;
            events  = 2;
        }
        else if (maxDim == 8)
        {
            // Map vừa: 8x8, 7x8, 6x8...
            enemies = 3;
            events  = 3;
        }
        else
        {
            // Map lớn: 9x9, 8x9, 7x9...
            enemies = 4;
            events  = 3;
        }

        // Đảm bảo ngưỡng tối thiểu tuyệt đối
        enemies = Mathf.Max(enemies, 3);
        events  = Mathf.Max(events,  2);
    }
}
