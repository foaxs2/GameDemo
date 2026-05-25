/// <summary>
/// Lưu loại death để TownDeathNoticeUI biết hiển thị thông báo nào khi vào scene Town.
/// Được đặt trước khi LoadScene("Town"), đọc một lần rồi xóa.
/// </summary>
public static class DeathContext
{
    public enum DeathType
    {
        None,
        /// <summary>HP = 0 trong Combat → về Town hồi full HP, SEN giữ nguyên</summary>
        CombatDeath,
        /// <summary>SEN = 0 trong Combat → về Town HP giữ như lúc chết, SEN = 2</summary>
        CombatMadness,
        /// <summary>HP = 0 trong Dungeon (bẫy/đói) → về Town hồi full HP, SEN giữ nguyên</summary>
        DungeonDeath,
        /// <summary>SEN = 0 trong Dungeon → về Town HP giữ như lúc chết, SEN = 2</summary>
        DungeonMadness,
    }

    public static DeathType Pending = DeathType.None;

    /// <summary>Đọc và xóa ngay sau khi dùng.</summary>
    public static DeathType Consume()
    {
        DeathType result = Pending;
        Pending = DeathType.None;
        return result;
    }
}
