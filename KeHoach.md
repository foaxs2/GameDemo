# 📅 KeHoach.md — Lộ Trình Tính Năng \& Kế Hoạch Chi Tiết

> Cập nhật: 2026-07-07.
> Lập kế hoạch dựa trên \*\*Mục Tiêu Hiện Tại.md\*\* + phân tích tại \*\*Analysis Results\*\*.

\---

## 🎯 Bảng Ưu Tiên Tổng Quan

|Ưu tiên|Mục|Tính năng|Trạng thái|
|-|-|-|-|
|🥇 1|Mục 5|Logic skill kẻ thù + Bong bóng cảnh báo + Tê tê|✅ Hoàn thành|
|🥈 2|Mục 3|Chỉnh Guild đếm lên (0/5 thay vì 5/5)|✅ Hoàn thành|
|🥉 3|Mục 4|Scene Test (TestCombat + TestDungeon)|✅ Hoàn thành|
|4|Mục 1+1.1|VFX combat + Screen Shake|✅ Hoàn thành|
|5|Mục 8|Dungeon 20 tầng + Boss config linh hoạt|✅ Hoàn thành|
|6|Mục 2|Hệ thống vật phẩm loại 2 + Slot dungeon + Drop item|⬜ Chưa làm|
|7|Mục 2.1|Tiệm Rèn + Tiệm Thuốc (chế tạo)|⬜ Chưa làm|
|8|Mục 6|Logic Sanity nâng cao (ẩn icon, bong bóng)|⬜ Chưa làm|
|9|Mục 7|Sự kiện va chạm + Sự kiện đánh đổi|⬜ Chưa làm|
|10|Mục 10 (M�### 6 — MỤC 2 — Hệ Thống Vật Phẩm Loại 2 (Ưu tiên 6)
 hơn vào `advancedEnemyPrefabs` trong Inspector.

\---

### 6 — MỤC 2 — Hệ Thống Vật Phẩm Loại 2 (Ưu tiên 6)

#### 2.1 — Refactor ItemType

|Loại|File|Thay đổi|
|-|-|-|
|✏️ Sửa|`Assets/Scripts/ITEM/ItemData.cs`|Thêm vào enum `ItemType`: `Artifact`, `ActivatableItem`. Thêm property `public bool IsType2Item`|

```csharp
// Thêm vào enum ItemType:
Artifact,         // Chỉ bán lấy vàng
ActivatableItem,  // Kích hoạt trong dungeon

// Thêm property vào class ItemData:
public bool IsType2Item =>
    itemType == ItemType.Material ||
    itemType == ItemType.Artifact ||
    itemType == ItemType.ActivatableItem;
```

#### 2.2 — DungeonSlots (thay storageInventory)

|Loại|File|Thay đổi|
|-|-|-|
|✏️ Sửa|`Assets/Scripts/ITEM/InventoryManager.cs`|Đổi tên `storageInventory` → `dungeonSlots`. Đổi `storageMaxSlots = 20` → `dungeonSlotCount = 6`. Thêm `public int maxDungeonSlots = 6` (nâng cấp được). Sửa hàm `AddEquipmentToStorage()` → `AddToCombatInventory()`. Sửa tất cả logic Item Loại 2 → vào `dungeonSlots`|
|✏️ Sửa|`Assets/Scripts/SaveSystem.cs`|Cập nhật save/load field tên mới|

**Logic AddItem mới**:

```csharp
public bool AddItem(ItemData item, int qty = 1)
{
    if (item.itemType == ItemType.Equipment || !item.IsType2Item)
        return AddToCombatInventory(item, qty);   // Loại 1 + Equipment → không giới hạn
    else
        return AddToDungeonSlots(item, qty);       // Loại 2 → 6 ô
}
```

**Khi dungeonSlots đầy**:

```csharp
// Trong AddToDungeonSlots():
if (slots đầy)
{
    // Lấy vị trí tile hiện tại của player
    Vector3Int dropPos = PlayerMovement.Instance.currentCellPosition;
    DungeonDropManager.Instance.DropItem(item, dropPos);
    return false;
}
```

#### 2.3 — Item Rơi Trên Map Dungeon

|Loại|File|Mô tả|
|-|-|-|
|🆕 Tạo|`Assets/Scripts/Dungeon/DungeonDropManager.cs`|Quản lý danh sách item đã rớt trên map. Khi player bước vào ô có item → hiện popup chọn nhặt/bỏ qua|
|✏️ Sửa|`Assets/Scripts/Dungeon/DungeonGenerator.cs`|Thêm `public TileBase droppedItemTile` → dùng để đánh dấu ô có item rớt|

#### 2.4 — Drop Item Từ Quái

|Loại|File|Thay đổi|
|-|-|-|
|✏️ Sửa|`Assets/Scripts/Combat/EnemyStats.cs`|Thêm `public List<ItemDropEntry> lootTable`|
|🆕 Tạo|`Assets/Scripts/Combat/ItemDropEntry.cs`|Class `\[Serializable]` với `ItemData item` và `float dropChance`|
|✏️ Sửa|`Assets/Scripts/Combat/CombatManager.cs`|Trong `CheckBattleEnd()` → sau khi quái chết, roll loot table và gọi `InventoryManager.AddItem()`|

```csharp
\[Serializable]
public class ItemDropEntry
{
    public ItemData item;
    \[Range(0f, 100f)] public float dropChance = 10f;
}
```

\---

### 7 — MỤC 2.1 — Tiệm Rèn + Tiệm Thuốc (Ưu tiên 7)

#### 7.1 — Tiệm Rèn (2 Tab)

|Loại|File|Mô tả|
|-|-|-|
|🆕 Tạo|`Assets/Scripts/Town/SmithyUI.cs`|UI 2 tab: Nâng vũ khí/giáp và Mở rộng slot|
|🆕 Tạo|`Assets/Scripts/Town/SmithyManager.cs`|Logic nâng cấp, lưu cấp độ nâng vũ khí/giáp/slot|
|✏️ Sửa|`Assets/Scripts/Combat/Player\_Manager.cs`|Thêm `weaponUpgradeLevel` (0-3), `armorUpgradeLevel` (0-3). Tính bonus ATK/HP từ cấp nâng|
|✏️ Sửa|`Assets/Scripts/SaveSystem.cs`|Save/load các field nâng cấp mới|

**Tab 1 — Nâng vũ khí/giáp**:

```
Nâng vũ khí (dùng Thép): cấp 1/2/3 → +1/+2/+5 ATK. Tốn 100/180/320 vàng + 1/2/3 Thép
Nâng vũ khí (dùng Orichalcum): cấp 1/2/3 → +1/+2/+5 ATK + 2% CritRate mỗi lần dùng Orichalcum để nâng. Miễn phí vàng.
Nâng giáp (dùng Thép): cấp 1/2/3 → +3/+6/+12 maxHP. Tốn 100/180/320 vàng + 1/2/3 Thép
Nâng giáp (dùng Orichalcum): cấp 1/2/3 → +3/+6/+12 maxHP + 2% Evasion mỗi lần dùng Orichalcum để nâng. Miễn phí vàng.
```

**Tab 2 — Mở rộng slot túi đồ**:

```
Slot hiện tại: 6 → 8 → 10 → 12 (tối đa 12). Tốn vàng mỗi lần mở rộng.
```

#### 7.2 — Tiệm Thuốc (Bán + Chế Tạo)

|Loại|File|Mô tả|
|-|-|-|
|🆕 Tạo|`Assets/Scripts/Town/PotionShopUI.cs`|UI 2 phần: danh sách mua + UI chế tạo|
|🆕 Tạo|`Assets/Scripts/Town/CraftingManager.cs`|Quản lý công thức chế tạo, kiểm tra nguyên liệu, thực hiện chế tạo|
|🆕 Tạo|`Assets/Scripts/Town/CraftingRecipe.cs`|Class `\[Serializable]` lưu công thức A + B → C|

**UI Chế Tạo**:

* Slot A và Slot B: bấm vào → mở list chọn vật phẩm từ inventory
* Nếu A + B khớp 1 công thức → hiện sản phẩm C bên cạnh + enable nút "Chế Tạo"
* Nếu không khớp → sản phẩm C trống + disable nút
* Nút trái góc: "Xem Công Thức" → hiện panel danh sách tất cả công thức `A + B → C`
* Nếu 1 ô trống → disable nút "Chế Tạo"

**Công thức chế tạo** (cấu hình trong Inspector):

|Slot A|Slot B|Sản phẩm|
|-|-|-|
|Bình máu|Thảo dược|Bình máu lớn (hồi 80% HP)|
|Thảo dược|Thảo dược|Thuốc giải độc (Cleanse)|
|Thép|Tinh chất|Orichalcum|
|Đuốc|Dầu|Đèn soi sáng|
|Nọc độc|Bình rỗng|Dầu tẩm độc|
|Khoáng thạch|Bình rỗng|Bình hồi SEN|

**Vật phẩm mới cần tạo ItemData**:

* `Bình máu lớn` — ConsumableType.HP, healAmount = 80% maxHP, Loại 1
* `Tinh chất` — ItemType.Material, Loại 2, dropChance 50% từ quái tinh anh, giá mua 200 vàng
* `Orichalcum` — ItemType.Material, Loại 2, giá mua 380 vàng tại shop
* `Đèn soi sáng` — ItemType.ActivatableItem, Loại 2
* `Bình hồi SEN` — ConsumableType.SEN, Loại 1

\---

### 8 — MỤC 6 — Logic Sanity Nâng Cao (Ưu tiên 8)

**Ghi chú**: Cần làm sau mục 5 vì cần có UI bong bóng kỹ năng để ẩn.

|Loại|File|Thay đổi|
|-|-|-|
|✏️ Sửa|`Assets/Scripts/Combat/EnemyUI.cs`|Thêm `public Sprite hiddenEnemySprite`. Trong `RefreshSENState()`: khi `sen < 4` → `icon.sprite = hiddenEnemySprite` (trừ boss). Thêm logic ẩn `warningBubble` khi `sen < 4`|
|✏️ Sửa|`Assets/Scripts/Combat/CombatUIManager.cs`|`ToggleEnemyInfo()` đã ẩn text khi `sen <= 4` — chỉ cần đảm bảo logic đồng bộ|

**Thiết lập trong Unity**:

* Tạo 1 Sprite chung "enemy\_unknown" (hình bóng tối/dấu hỏi)
* Kéo sprite này vào field `hiddenEnemySprite` của từng prefab EnemyUI

\---

### 9 — MỤC 7 — Sự Kiện Mới (Ưu tiên 9)

**Ghi chú**: Cần mục 8 (dungeon 20 tầng) xong trước để có nền tảng.

#### 9.1 — Sự Kiện Va Chạm (Elite Encounter)

Đã mô tả tại Mục 8.4. Thêm loại `EventTile` mới là `EliteEncounterTile` → khi bước vào hiển thị text và lựa chọn, nhiều khả năng mở màn combat với quái tinh anh mạnh hơn bình thường (có thể có thêm buff passive).

#### 9.2 — Sự Kiện Đánh Đổi

|Loại|File|Mô tả|
|-|-|-|
|✏️ Sửa|`Assets/Scripts/Dungeon/EventData.cs`|Thêm `bool isTradeoff`. Thêm fields cho buff tạm thời (stat tăng) và cost (HP/resource)|
|✏️ Sửa|`Assets/Scripts/Dungeon/EventManager.cs`|Xử lý sự kiện đánh đổi: trừ cost ngay, áp buff tạm thời|
|🆕 Tạo|`Assets/Scripts/Combat/TemporaryRunBuff.cs`|Quản lý buff tạm thời trong run dungeon (clear khi về Town)|
|✏️ Sửa|`Assets/Scripts/Town/PlayerMovement.cs` hoặc scene load|Khi load scene Town: gọi `TemporaryRunBuff.ClearAll()`|

**Ví dụ sự kiện đánh đổi**:

```
"Bàn thờ cổ" — Hiến tế 20 HP (currentHP, không maxHP) → nhận +5 ATK cho run này
"Hợp đồng quỷ" — Mất 10 Food → nhận +3 SPD cho run này
```

\---

## ⏸️ Tạm Hoãn

|Tính năng|Lý do|
|-|-|
|**Kho Storage 6 ô**|Đã tích hợp vào mục 2 (dungeonSlots), sẽ làm cùng lúc|
|**Localization VN/EN**|Thực hiện sau khi nội dung game ổn định|

\---

## ✅ Đã Hoàn Thành

|Tính năng|Phiên hoàn thành|Chi tiết|
|-|-|-|
|**Nút Cài Đặt (Bánh Răng) \& Thoát Game tại Town**|2026-06-04|Thêm nút bánh răng mở panel cài đặt cấu hình (âm lượng, màn hình, ngôn ngữ...) và nút lưu game tự động trước khi thoát về màn hình chính.|
|**Hệ Thống Sự Kiện Town (Town Events)**|2026-06-03|Tích hợp hệ thống roll sự kiện ngẫu nhiên 50% sau khi hoàn thành tầng Dungeon (Giảm 25% mua shop/guild, tăng 50% giá bán, miễn phí rượu guild, giảm 25% học kĩ năng). Hiển thị sự kiện trên HUD và hỗ trợ Save/Load.|
|**Cải Thiện \& Sửa Lỗi UI Town**|2026-06-02|Sửa lỗi màn hình xanh (bảo vệ các manager khi Awake), tạo TownUIController quản lý phím tắt, tạo TownHUD đồng bộ chỉ số trên cùng.|
|**Tái Thiết Hệ Thống EXP \& Điểm Kỹ Năng**|2026-05-31|EXP cố định 50 cho mọi cấp, nâng cấp thuộc tính nhận +1 điểm, không tăng maxFood, tích hợp thanh EXP Slider và HUD hiển thị điểm kỹ năng.|
|**Scene Start (Menu \& 3 Save Slots)**|2026-05-31|Menu 3 nút (Bắt đầu, Cài đặt, Thoát). Thiết lập 3 file save slot độc lập, trích xuất metadata thời gian chơi/level lên slot trống/đã có, bảng xác nhận xóa và Auto-save 1 phút.|
|**Điều Chỉnh Lưu Trữ Dungeon Khi Chết**|2026-05-31|Khi người chơi chết/điên tại Dungeon, dungeon map tiến trình cũ sẽ được reset ngẫu nhiên map mới (seed mới, fog mới, quái mới) thay vì giữ nguyên map cũ bị kẹt.|
|STR/DEX/VIT/AGL default = 10|2026-05-01|Thiết lập chỉ số mặc định ban đầu là 10.|
|Flee formula: `80 - senLost×4`|2026-05-01|Áp dụng công thức chạy trốn dựa theo Sen mất.|
|Skill 4 Bleed — duration tracking 4 lượt|2026-05-01|Thiết lập hiệu ứng chảy máu kéo dài 4 lượt.|
|SEN ≤ 4 — ẩn info kẻ thù + icon `?`|2026-05-01|Che thông tin kẻ địch khi lý trí quá thấp.|
|SEN = 0 — về Town, reset = 2|2026-05-01|Tự động quay về Town khi phát điên.|
|Save/Load JSON (PlayerStats, Inventory, Dungeon, Skills)|2026-05-01|Cơ chế cơ bản lưu trữ dữ liệu sang JSON.|
|EnemySkillID enum — thay thế string.Contains|2026-05-01|Tối ưu hóa định danh kỹ năng kẻ địch.|
|Nhện Tinh Anh — entity-count logic (alive HP>0)|2026-05-01|Đếm số lượng kẻ địch tinh anh còn sống.|
|Shop gold `70–250` SerializeField|2026-05-01|Điều chỉnh lượng vàng trong shop.|
|Magic number → const/SerializeField|2026-05-01|Dọn dẹp magic numbers trong code.|
|Coroutine leak guard (yield break)|2026-05-01|Ngăn ngừa rò rỉ coroutine.|
|InventoryManager Start() test items → ContextMenu|2026-05-01|Thêm tính năng test đồ nhanh trong editor.|
|Combat inventory không giới hạn, Storage = 20 ô|2026-05-01|Quy định dung lượng túi đồ và kho.|
|Enemy death animation (slide + fade 0.5s)|2026-05-01|Thêm hiệu ứng trượt mờ khi quái chết.|
|**Death Screen (Combat + Dungeon)**|2026-05-04|Hiển thị màn hình khi người chơi HP = 0.|
|**DeathContext system**|2026-05-04|Hệ thống truyền trạng thái chết giữa các Scene.|
|**Madness Panel**|2026-05-04|Hiện bảng phát điên khi Sen = 0.|
|**TownDeathNoticeUI**|2026-05-04|Bảng thông báo chết/điên khi xuất hiện ở Town.|
|**DungeonDeathUI**|2026-05-05|Quản lý thông báo chết/điên khi ở Dungeon.|
|**Mouse tile movement + hover highlight**|2026-05-05|Di chuyển bằng chuột và vẽ ô hover.|
|**ExitDialog bug fix**|2026-05-05|Sửa lỗi dialog thoát bị Canvas chặn raycast.|
|**Buff Item Runtime**|2026-05-07|Hệ thống quản lý buff và icon buff theo thời gian thực.|
|**Weapon Coating (Skill 4 + Item)**|2026-05-07|Tẩm độc/máu lên vũ khí và hiển thị icon tương ứng.|
|**BuffManager tick + event**|2026-05-07|Đồng bộ đếm ngược thời gian kéo dài của buff.|
|**Cleanse Item**|2026-05-07|Đã thiết lập giải trừ hiệu ứng xấu ConsumableType.Cleanse.|
|**Boss LV5 — Nhện Nữ Vương**|2026-05-10|Boss Nhện 3 passives và gọi nhện con.|
|**Boss LV10 — Rồng Cổ Đại**|2026-05-10|Boss Rồng khè lửa, stun và nộ.|
|**Giới hạn Túi Đồ (3/3 Bình Máu)**|2026-05-10|Giới hạn số lượng máu mang theo và tặng quà khởi đầu.|
|**Guild UI Hoàn Chỉnh**|2026-05-11|Bảng nhiệm vụ tùy chỉnh, nhận lương thưởng và nâng cấp tiến độ.|
|**Ô Vàng trên Map**|2026-05-16|Tỷ lệ xuất hiện ô vàng nhận tiền ngẫu nhiên trên bản đồ.|
|**Enemy AI Heal (HealEnemy)**|2026-05-16|Quái hỗ trợ hồi máu đồng minh thấp máu nhất.|
|**Phí Rút Lui Về Town**|2026-05-16|Thu phí rút lui 30 vàng khi đi qua Cửa Vào.|
|**Hình Phạt Chết/Điên**|2026-05-16|Mất nửa số vàng và EXP kiếm được trong tầng khi thất bại.|
|**Thuộc tính Trang Bị Mới (Tribal Pendant & Skull Ring)**|2026-08-01|Thêm `senLossReduction` (giảm % rớt SEN di chuyển/đói/crit) và `extraDamageTakenPercent` (debuff nhận thêm damage) vào `ItemData`, `Player_Manager`, `PlayerMovement`, `CombatManager` & `CombatCalculator`.|
|**Cải Tiến Logic Shop (Lọc Item 0G & Đồ Duy Nhất)**|2026-08-01|Lọc bỏ các item có `buyPrice <= 0`, `canBeSold == false` hoặc Bình máu. Vật phẩm ngoài loại Consumable luôn xuất hiện duy nhất không trùng lặp.|
|**Tối Ưu Stacking Inventory**|2026-08-01|Tích hợp kiểm tra cờ `isStackable` trong `InventoryManager` để quản lý số lượng stack chính xác.|
|**Logic Skill Kẻ Thù + Bong Bóng Cảnh Báo + Quái Tê Tê**|2026-08-02|Chuyển cooldown quái về `maxCooldown`, skill trigger 100%, hiển thị UI Bong Bóng Cảnh Báo trước 1 lượt (tự ẩn khi SEN ≤ 4), tăng tỷ lệ debuff kỹ năng quái lên 80%, hoàn thiện Tê Tê (Bọc Giáp 3 lượt/hit, phản sát thương dội lại Player, hiện Icon DEF, tạm dừng CD, không đánh khi bọc giáp), và tự động reset HP + clear Buff/Debuff khi về Town (giữ nguyên SEN).|
|**Chỉnh UI Guild Đếm Lên**|2026-08-02|Đảo ngược logic hiển thị tiến độ nhiệm vụ Săn Quái từ đếm xuống (5/5) thành đếm lên (0/5 ➔ 1/5 ➔ ... ➔ 5/5 khi hoàn thành).|
|**Cập nhật buff Defend**|2026-08-03|Tăng buff phòng thủ lên 50% giảm sát thương + 1 DEF cho 2 lượt khi phòng thủ, Sửa lỗi hiển thị icon buff của player|  
|**Tạo 2 scenes test các chức năng**|2026-08-11|Tạo scenes TestCombat và TestDungeon để test các chức năng của game.|
|**VFX Combat \& Screen Shake (Mục 1 + 1.1)**|2026-08-12|Triển khai hệ thống rung màn hình toàn diện UIShake (rung toàn bộ panel con Canvas + Camera), quản lý VFX tự động CombatVFX (tự nạp Prefab theo chuẩn UI từ Resources), hỗ trợ đa góc chém combo hit (Đa Kích 4 góc, Trọng Kích cắt chữ X, Chém Mạnh dứt điểm), hiệu ứng quái đánh, boss khè lửa, và sửa nút Reset TestCombat giữ nguyên chỉ số tùy chỉnh.|
|**Tối Ưu Hóa Icon Buff/Debuff Trong Suốt**|2026-08-13|Cập nhật toàn bộ sprite IconBuffDebuff sang định dạng PNG đã tách nền, tự động kết nối lại 16 GUID trong Icon Library.asset và tắt Image nền trắng trong StatusIconPrefab để hiển thị trong suốt hoàn hảo trên UI.|
|**Cải Tiến Skill Trọng Kích Chém Đôi (SwordSkill3)**|2026-08-13|Đổi đòn chém đầu tiên sang chém thường (-35°) không rung màn hình, hợp với đòn 2 (+35°) thành vệt chém chữ X. Tự động chuyển mục tiêu sang kẻ địch ngẫu nhiên còn lại nếu nhát chém đầu đã kết liễu mục tiêu ban đầu.|
|**Cải Thiện Hệ Thống Combat & Pacing (Mục 10)**|2026-08-13|Bổ sung nhịp độ chờ kết thúc hoạt ảnh chém (0.35s cho Player, 0.25s sau đòn quái). Hệ thống ánh sáng chuẩn xác ôm sát EnemyIcon: hiệu ứng báo hiệu quái ra đòn (sáng ấm + nền trắng 0.35s), hiệu ứng trúng đòn Darkest Dungeon (nháy đỏ rực 0.4s khi bị Player chém trúng) và hiệu ứng chết nháy đỏ trước khi trượt xuống.|
|**Dungeon 20 Tầng + Boss Config Linh Hoạt (Mục 8)**|2026-08-14|Mở rộng map 20 tầng với các cấp kích thước tăng dần (5x5 đến 9x10); áp dụng công thức phân bổ 20% diện tích (60% quái, 40% event) có trần (Max 9 Quái / Max 6 Event); cấu hình BossFloorConfig linh hoạt (Tầng 10 Boss Nhện + 2 đệ hỗ trợ và cơ chế Sinh Đàn triệu hồi 2 con khi đệ chết, Tầng 20 Đại Boss Rồng); thuật toán Boss spawn xa Player nhất; cửa ra tầng Boss là Cửa Khóa và rơi chìa khóa khi diệt Boss; hệ thống Quái Tinh Anh (Elite) với tile riêng và đội hình 3 quái; quái nâng cao tầng 11+; đồng bộ công cụ Test.|
|**Tách Biệt Save File Test & Auto-Save Toàn Diện**|2026-08-15|Tách biệt hoàn toàn file lưu `save_test_mode.json` cho các Scene Test (`TestDungeon`, `TestCombat`, `Combat_Test`) không ảnh hưởng 3 slot game thật. Tự động lưu game khi trang bị/thay đổi item, thắng trận, qua tầng, chỉnh sửa chỉ số trong Test Menu.|
|**Dungeon UI Overhaul (3 Panel + RenderTexture Viewport)**|2026-08-16|Tái cấu trúc toàn bộ UI Dungeon: Khung giữa chiếu qua RenderTexture từ Camera độc lập (`DungeonCameraController`), Header đổi tên theo tầng ("Cống ngầm - Tầng 1-10", "Lăng mộ - Tầng 11-20"), Left Panel với 4 thanh Bar màu Image Fill (HP, Food, SEN, EXP), lưới 6 chỉ số 2x3, 4 ô trang bị, `txtHeroLevel` hiển thị Lever, Right Panel hiển thị Quest & Thống kê tầng, Bottom Panel 8 ô item.|
|**Popup Chi Tiết Vật Phẩm (Panel_ItemDetail)**|2026-08-17|Click vào ô vật phẩm ở đáy màn hình mở popup chi tiết (Icon, Tên, Mô tả, Công dụng sạch không ký tự & và không dùng mã màu HTML dạng `+10 Lương thực +3 HP`, `+2 SEN`), có 2 nút Sử Dụng & Vứt Bỏ, hỗ trợ click nền ngoài để đóng popup.|
|**Hoãn Trừ Chỉ Số Sự Kiện Cạm Bẫy**|2026-08-18|Sự kiện cạm bẫy (0 lựa chọn) hoãn trừ HP & SEN, chỉ chính thức trừ và cập nhật thanh bar sau khi người chơi bấm nút "Tiếp Tục / Xác Nhận".|
|**Nâng Cấp Hệ Thống Nhiệm Vụ Guild & Nhận Thưởng Trong Dungeon**|2026-08-18|Tạo Prefab thanh ngang mỏng riêng cho Dungeon (`Quest_Dungeon_Row.prefab`), giới hạn nhận và hiển thị tối đa 3 nhiệm vụ đã nhận (`isAccepted == true`), hỗ trợ quà tặng kèm (Vật phẩm/Trang bị + text `x1`/`x2`, tự ẩn nếu không có), hoàn thành tự tối `darkOverlay` + hiện nút `btnClaim` để nhận thưởng trực tiếp trong Dungeon kèm Floating Text, sửa quét vật phẩm thu thập trực tiếp theo tên `itemName` từ `InventoryManager`, thêm Dropdown chọn quest và nút Clear Quests trong Test Menu.|

---

## 🎨 Hướng Dẫn Chuẩn Bị Assets VFX Còn Lại (Làm Sau)

### A. Thư mục đặt Prefab bắt buộc
```
Assets/Resources/VFX/
```

### B. Danh sách các Prefab cần chuẩn bị

| # | Loại Hiệu Ứng | Tên Prefab Bắt Buộc | Dùng Cho | Gợi Ý Kích Thước & Animation |
|---|---|---|---|---|
| 1 | **Chém thường** | `Assets/Resources/VFX/slash_normal.prefab` | Đòn đánh thường, skill Đa Kích, Trọng Kích đòn 2 | *(Đã hoàn thành)* |
| 2 | **Chém mạnh** | `Assets/Resources/VFX/slash_heavy.prefab` | Skill Chém Mạnh (Skill 2), Trọng Kích (Skill 3 đòn 1) | Vệt chém to hơn, uy lực hơn (6–8 frames) | *(Đã hoàn thành)* |
| 3 | **Hào quang Buff** | `Assets/Resources/VFX/aura_buff.prefab` | Tẩm Bleed (Skill 4), Quái Tăng Tốc, Quái Giăng Tơ | Vòng sáng / hào quang tròn bao quanh mục tiêu (`100x100` ~ `120x120`) | *(Đã hoàn thành)* |
| 4 | **Trúng đòn mạnh** | `Assets/Resources/VFX/hit_heavy.prefab` | Quái đánh Player, Player bị Crit, Skill quái | Hiệu ứng vỡ impact / tia nổ trúng đòn (`80x80` ~ `100x100`) |*(Đã hoàn thành)* |
| 5 | **Phun lửa** | `Assets/Resources/VFX/flame_breath.prefab` | Rồng Cổ Đại khè lửa diện rộng | Luồng lửa phun kéo dài (`250x120` ~ `300x150`, 8–12 frames) |

---

### C. Quy trình 5 bước tạo Prefab đúng chuẩn UI trong Unity
1. **Import Sprite Sheet**: Chọn file ảnh trong Project ➔ Inspector: chuyển `Sprite Mode = Multiple` ➔ Bấm `Sprite Editor` ➔ `Slice` ➔ `Apply`.
2. **Tạo GameObject UI**: Trong cửa sổ Hierarchy, **Chuột phải ➔ UI ➔ Image** (tạo GameObject có sẵn `RectTransform` + `Image`).
3. **Tạo Animation Clip**: Kéo các frame Sprite vừa cắt vào cửa sổ **Animation (Ctrl + 6)** để tạo Animation Clip + Animator Controller.
4. **Cấu hình Component**:
   - `RectTransform`: Set `Anchor` và `Pivot` = **(0.5, 0.5)** (ở giữa). Chỉnh `Width` & `Height` theo tỉ lệ tự nhiên của Sprite.
   - `Image`: **Bỏ tích ô `Raycast Target`** (để khi hiệu ứng hiện lên không chặn click chuột vào quái hoặc các nút bấm bên dưới).
5. **Lưu thành Prefab**: Kéo GameObject từ Hierarchy vào thư mục `Assets/Resources/VFX/` với đúng tên bắt buộc ở bảng trên ➔ Xóa GameObject tạm trong Hierarchy scene.

---

## 🏰 Tổng Kết Nâng Cấp Hệ Thống Dungeon & UI Mới (Tháng 08/2026)

### 1. Tách Biệt Hoàn Toàn Hệ Thống Lưu Dữ Liệu (Save System):
* **Chế độ chơi thật:** Sử dụng 3 slot lưu độc lập (`save_slot_1.json`, `save_slot_2.json`, `save_slot_3.json`) quản lý tại màn hình Start.
* **Chế độ Test (`TestDungeon`, `TestCombat`, `Combat_Test`):** Tự động nhận diện và ghi/đọc riêng trên file `save_test_mode.json`. Mọi thay đổi trong các Scene Test không bao giờ ảnh hưởng đến 3 file lưu của game thật.
* **Cơ chế Auto-Save:** Tự động nạp khi vào màn test, tự động lưu khi: thay đổi trang bị/vật phẩm trong túi đồ, khi thắng trận quái, khi hoàn thành/qua tầng hầm ngục, và khi tùy chỉnh chỉ số trong bảng Test Menu.

### 2. Tái Cấu Trúc Toàn Diện UI Dungeon (`Canvas_Dungeon_Main`):
* **Khung Trung Tâm (Center Viewport):** Camera độc lập (`DungeonCameraController`) render hình ảnh hầm ngục lên `RenderTexture` và chiếu vào `RawImage_DungeonViewport`. Panel sự kiện (`Panel_Event`) nằm đè trực tiếp lên khung trung tâm.
* **Header Động Theo Tầng:** Tự động đổi tên khu vực theo tầng:
  * Tầng 1 ➔ 10: `Cống ngầm - Tầng X`.
  * Tầng 11 ➔ 20: `Lăng mộ - Tầng X`.
* **Left Panel (Anh Hùng & Chỉ Số):**
  * `txtHeroName` và `txtHeroLevel` (tự động cập nhật `Lever X`).
  * 4 thanh Bar màu sinh tồn (HP đỏ `#C41E3A`, Food xanh `#2E8B57`, SEN xám `#A9A9A9`, EXP vàng `#DAA520`) dùng kỹ thuật `Image Fill` (loại bỏ Slider cũ để tối ưu hiệu năng).
  * Lưới 6 chỉ số chiến đấu dạng 2x3 có icon và tiền tố đầy đủ.
  * 4 ô trang bị (Vũ khí, Giáp, Phụ kiện 1, Phụ kiện 2) có icon và hover chi tiết.
* **Popup Chi Tiết Vật Phẩm (`Panel_ItemDetail`):**
  * Click vào bất kỳ ô item ở thanh đáy 8 ô để mở bảng chi tiết (Icon, Tên, Mô tả).
  * Dòng hiệu ứng hiển thị chuỗi sạch, tinh gọn (loại bỏ ký tự `&` và mã màu HTML, hiển thị dạng `+10 Lương thực +3 HP`, `+2 SEN`) giữ nguyên màu mặc định của TMP.
  * Hỗ trợ 2 nút tương tác: "Sử Dụng" (`btnDetailUse`) và "Vứt Bỏ" (`btnDetailDiscard`), cùng nút đóng phủ toàn màn hình (`Overlay_CloseBtn`).
* **Sự Kiện Cạm Bẫy (Hoãn Trừ HP & SEN):**
  * Khi dẫm vào ô sự kiện bẫy (0 lựa chọn), thanh HP và SEN **chưa bị trừ ngay**.
  * Chỉ chính thức trừ chỉ số và cập nhật thanh Bar sau khi người chơi đọc xong và bấm nút "Tiếp Tục / Xác Nhận".

### 3. Nâng Cấp Hệ Thống Nhiệm Vụ Guild (Quest System):
* **Prefab Thanh Ngang Dungeon (`Quest_Dungeon_Row.prefab`):**
  * Tạo riêng prefab thanh ngang mỏng, tinh gọn cho Dungeon mà không làm thay đổi hay vỡ giao diện thẻ bài lớn ở Town.
  * Hỗ trợ hiển thị quà tặng kèm (Vật phẩm hoặc Trang bị) với Icon và Text số lượng `x1`, `x2`... tự động ẩn nếu nhiệm vụ không có quà.
* **Quy Chuẩn Giới Hạn & Lọc Nhiệm Vụ:**
  * Giới hạn tối đa nhận 3 nhiệm vụ cùng lúc (`3/3`).
  * Trên bảng `Quest_ScrollView` trong Dungeon chỉ hiển thị các nhiệm vụ **đã nhận** (`isAccepted == true`) và tối đa 3 nhiệm vụ.
* **Cơ Chế Nhận Thưởng Trực Tiếp Trong Dungeon:**
  * Khi hoàn thành mục tiêu: Thẻ nhiệm vụ tự động bật `darkOverlay` và nổi bật nút `btnClaim` ("Nhận thưởng").
  * Click vào nút hoặc thẻ để nhận thưởng ngay tại chỗ (cộng Vàng, EXP, Item, hiện Floating Text `+150g` trên đầu Player và lưu game) mà không cần quay về Town.
* **Quét Vật Phẩm Thu Thập Trực Tiếp Theo Tên:**
  * Sửa hàm kiểm tra nhiệm vụ thu thập (`QuestType.Gather`) quét trực tiếp theo tên vật phẩm (`itemName`) từ `InventoryManager` (cả túi mang theo và kho), không phân biệt hoa/thường, không còn phụ thuộc vào việc kéo thả mảng `allItems` trong `SaveSystem`.
* **Công Cụ Test Menu:**
  * Dropdown chọn Quest + Nút "Nhận Quest" để nhận ngay bất kỳ quest nào để test trong Dungeon.
  * Nút "Clear Quests" để xóa sạch toàn bộ nhiệm vụ khi cần.
