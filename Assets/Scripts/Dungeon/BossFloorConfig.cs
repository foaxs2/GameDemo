using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Cấu hình Boss theo tầng linh hoạt cho Dungeon và Combat.
/// Cho phép thiết lập số tầng, tile hiển thị trên bản đồ, Boss chính ở giữa, và tối đa 2 quái hỗ trợ 2 bên.
/// </summary>
[System.Serializable]
public class BossFloorConfig
{
    [Tooltip("Số tầng xuất hiện Boss (ví dụ: 10, 20)")]
    public int floorNumber;

    [Tooltip("Cục gạch đại diện cho Boss trên Tilemap Dungeon (Scene Dungeon)")]
    public TileBase bossTile;

    [Header("Đội Hình Trận Đấu (Scene Combat)")]
    [Tooltip("Prefab của Boss chính (xuất hiện ở ô Giữa)")]
    public GameObject bossPrefab;

    [Tooltip("Danh sách tối đa 2 quái hỗ trợ xuất hiện cùng Boss lúc đầu (Slot 0 = Trái, Slot 1 = Phải)")]
    public GameObject[] supportEnemyPrefabs = new GameObject[2];

    [Header("Cơ Chế Triệu Hồi Khi Quái Con Chết")]
    [Tooltip("Prefab quái đệ sẽ triệu hồi lên khi 2 quái hỗ trợ bị tiêu diệt (Dành cho Boss có passive Sinh Đàn như Nhện Nữ Vương. Để trống sẽ tự tìm quái con phù hợp)")]
    public GameObject summonOnAlliesDeadPrefab;
}
