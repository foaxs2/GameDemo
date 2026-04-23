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
    public List<TileBase> eventTiles = new List<TileBase>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void GenerateDungeon(int currentFloor, int seed)
    {
        // Fix cứng trạng thái ngẫu nhiên bằng Seed để Map không đổi khi đánh quái xong quay lại
        Random.InitState(seed);

        // 1. Phân tích cấu hình Tầng
        DungeonFloorData config;
        if (floorConfigs.Count == 0 || currentFloor > floorConfigs.Count)
        {
            // Tầng sinh mặc định nếu quên chưa điền hoặc vô tận
            config = new DungeonFloorData();
            config.minSize = 5 + (currentFloor / 3);
            config.maxSize = 8 + (currentFloor / 3);
            config.maxEnemies = 2 + (currentFloor / 2);
            config.maxEvents = 1 + (currentFloor / 4);
        }
        else
        {
            config = floorConfigs[currentFloor - 1];
        }

        // 2. Xóa sạch mọi thứ tàn dư của map cũ/vẽ tay
        if (groundMap != null) groundMap.ClearAllTiles();
        if (fogMap != null) fogMap.ClearAllTiles();
        if (enemyMap != null) enemyMap.ClearAllTiles();
        if (eventMap != null) eventMap.ClearAllTiles();

        // 3. Gieo xúc xắc kích cỡ map (ÉP VUÔNG để không bị dẹt)
        int size = Random.Range(config.minSize, config.maxSize + 1);
        int width = size;
        int height = size;

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
            availableSlots.RemoveAt(pIndex);
        }

        // 6. Đặt Exit (Cửa Ra)
        if (availableSlots.Count > 0 && exitTile != null && eventMap != null)
        {
            int eIndex = Random.Range(0, availableSlots.Count);
            Vector3Int exitPos = availableSlots[eIndex];
            eventMap.SetTile(exitPos, exitTile);
            availableSlots.RemoveAt(eIndex);
        }

        // 7. Sinh Quái Vật HOẶC Sinh Boss
        bool isBossFloor = (currentFloor % 5 == 0); // Ví dụ Tầng 5, 10, 15
        int enemiesToSpawn = 0; // Khai báo trước để dùng cho Debug.Log ở cuối

        if (isBossFloor && bossTiles.Count > 0 && enemyMap != null)
        {
            // Nếu là tầng Boss: Không đẻ quái nhãi, chỉ đẻ 1 cục Boss duy nhất ở rốn bản đồ (0,0)
            // Tìm ô gần tâm nhất để nhét boss vào:
            int bIndex = Random.Range(0, availableSlots.Count);
            Vector3Int bossPos = availableSlots[bIndex];
            TileBase randomBossTile = bossTiles[Random.Range(0, bossTiles.Count)];
            enemyMap.SetTile(bossPos, randomBossTile);
            availableSlots.RemoveAt(bIndex);

            enemiesToSpawn = 1; // Tính con Boss là 1 quái
            Debug.Log("CHÚ Ý: TẦNG BOSS ĐÃ XUẤT HIỆN!");
        }
        else
        {
            // Tầng bình thường: Rải quái ngẫu nhiên
            enemiesToSpawn = Mathf.Min(config.maxEnemies, availableSlots.Count);
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                if (enemyTiles.Count == 0 || enemyMap == null) break;
                int rIndex = Random.Range(0, availableSlots.Count);
                Vector3Int spawnPos = availableSlots[rIndex];
                
                TileBase randomEnemyTile = enemyTiles[Random.Range(0, enemyTiles.Count)];
                enemyMap.SetTile(spawnPos, randomEnemyTile);
                
                availableSlots.RemoveAt(rIndex);
            }
        }

        // 8. Đặt Sự Kiện ngẫu nhiên
        int eventsToSpawn = Mathf.Min(config.maxEvents, availableSlots.Count);
        for (int i = 0; i < eventsToSpawn; i++)
        {
            if (eventTiles.Count == 0 || eventMap == null) break;
            int rIndex = Random.Range(0, availableSlots.Count);
            Vector3Int spawnPos = availableSlots[rIndex];
            
            TileBase randomEventTile = eventTiles[Random.Range(0, eventTiles.Count)];
            eventMap.SetTile(spawnPos, randomEventTile);
            
            availableSlots.RemoveAt(rIndex);
        }

        Debug.Log($"Sinh thành công Tầng {currentFloor} - Cỡ {width}x{height} với {enemiesToSpawn} quái và {eventsToSpawn} Event!");
    }
}
