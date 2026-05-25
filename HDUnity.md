# 🎮 HDUnity.md — Hướng Dẫn Thêm Nội Dung Mới Trong Unity

> Tài liệu này hướng dẫn cách cài đặt trong Unity Editor để thêm mới: Kĩ năng, Trang bị, Vật phẩm, Sự kiện Dungeon, Ủy thác Guild, Item/Trang bị Shop, và Kẻ thù.  
> Đọc cùng với `HuongDanTaoSkill.md` để có hướng dẫn chi tiết về kỹ thuật code.

---

## 📚 Mục Lục

1. [Thêm Kĩ Năng Mới](#1-thêm-kĩ-năng-mới)
2. [Thêm Trang Bị Mới](#2-thêm-trang-bị-mới)
3. [Thêm Vật Phẩm Tiêu Hao Mới](#3-thêm-vật-phẩm-tiêu-hao-mới)
4. [Thêm Sự Kiện Dungeon Mới](#4-thêm-sự-kiện-dungeon-mới)
5. [Thêm Ủy Thác Guild Mới](#5-thêm-ủy-thác-guild-mới)
6. [Thêm Item/Trang Bị Vào Shop](#6-thêm-itemtrang-bị-vào-shop)
7. [Thêm Kẻ Thù Mới](#7-thêm-kẻ-thù-mới)

---

## 1. Thêm Kĩ Năng Mới

> Xem chi tiết code tại `HuongDanTaoSkill.md`. Phần này chỉ hướng dẫn bước cài đặt trong **Unity Editor**.

### Bước 1 — Viết code kĩ năng (trong VS Code / Rider)

- **Skill Người Chơi**: Thêm class kế thừa `SkillAction` trong `PlayerSkills.cs`, đăng ký trong `SkillExecutor.RegisterAllSkills()`.
- **Skill Kẻ Thù**: Thêm enum vào `EnemySkillID`, thêm class kế thừa `EnemySkillAction` trong `EnemySkills.cs`, đăng ký trong `SkillExecutor.RegisterAllSkills()`.

### Bước 2 — Tạo SkillData ScriptableObject trong Unity

1. **Project Window** → Chuột phải vào folder `Assets/ScriptableObjects/Skills/` (hoặc nơi bạn lưu).
2. **Create → Game → Skill** (tìm menu `Game/Skill`).
3. Đặt tên file = tên skill (vd: `Sword_Skill_5`).
4. Điền vào **Inspector**:
   | Field | Giá Trị |
   |-------|---------|
   | `Skill ID` | Phải **khớp chính xác** chuỗi đăng ký trong `Register("Sword_Skill_5", ...)` |
   | `Skill Name` | Tên hiển thị (vd: "Đại Chém") |
   | `Description` | Mô tả khi hover |
   | `Icon` | Kéo Sprite icon vào |
   | `Cooldown` | Số lượt cooldown (0 = không CD) |
   | `Target Type` | `SingleEnemy` / `Random` / `AllEnemy` / `SelfBuff` |
   | `Damage Bonus Per Level` | Mảng float, mỗi phần tử = 1 cấp độ (vd: [10, 15, 20]) |
   | `Effect Chance Per Level` | Mảng float % hiệu ứng (vd: [30, 40, 50]) |

### Bước 3 — Gán vào SkillManager (Skill Tree)

1. Chọn **GameObject có SkillManager** trong Scene **Town**.
2. Inspector → `SkillManager` → mảng `All Skills` → kéo `SkillData` vừa tạo vào.

### Bước 4 — Không cần làm gì thêm ✅

`SkillExecutor` tự nhận và chạy khi player dùng kĩ năng trong combat.

---

## 2. Thêm Trang Bị Mới

### Bước 1 — Tạo ItemData ScriptableObject

1. **Project Window** → Chuột phải → **Create → Game → Item**.
2. Đặt tên file = tên trang bị (vd: `DaoNganHan`).
3. Điền **Inspector** — phần **Thông tin cơ bản**:
   | Field | Giá Trị |
   |-------|---------|
   | `Item Name` | Tên hiển thị (vd: "Dao Ngắn Hàn") |
   | `Description` | Mô tả trang bị |
   | `Item Type` | **Equipment** |
   | `Icon` | Kéo Sprite vào |
   | `Buy Price` | Giá mua |
   | `Sell Price` | Giá bán |
   | `Can Be Sold` | ✅ |

4. Điền **phần Equipment**:
   | Field | Giá Trị |
   |-------|---------|
   | `Equipment Slot` | `Weapon` / `Armor` / `Accessory1` / `Accessory2` |
   | `Damage Bonus` | Cộng ATK |
   | `Defense Bonus` | Cộng DEF |
   | `Speed Bonus` | Cộng SPD |
   | `HP Bonus` | Cộng HP tối đa |
   | `Crit Bonus` | Cộng % crit |
   | `Evasion Bonus` | Cộng % né |
   | `Armor Penetration` | Xuyên giáp |
   | `Bleed Chance` | % gây Chảy Máu khi đánh thường |
   | `Stun Chance` | % gây Choáng khi đánh thường |
   | `Bonus Damage Vs Human` | Thêm DMG với kẻ thù Human |
   | `Bonus Damage Vs Non Human` | Thêm DMG với kẻ thù NonHuman |

5. **Stack & Sử dụng**:
   - `Is Stackable`: ❌ (trang bị không stack)
   - `Usable In Combat`: ❌

### Bước 2 — Thêm vào Shop (nếu cần)

> Xem mục [6. Thêm Item/Trang Bị Vào Shop](#6-thêm-itemtrang-bị-vào-shop).

---

## 3. Thêm Vật Phẩm Tiêu Hao Mới

### Bước 1 — Tạo ItemData ScriptableObject

1. **Create → Game → Item**.
2. Điền **phần Thông tin cơ bản**:
   - `Item Type` = **Consumable**
   - `Icon`, `Buy Price`, `Sell Price`, `Can Be Sold`.

3. Điền **phần Cho Consumable**:

   | `Consumable Type` | Tác dụng | Field cần điền |
   |---|---|---|
   | `HP` | Hồi máu cố định | `Heal Amount` |
   | `HP` (%) | Hồi máu theo % | `Heal Percentage` (0.4 = 40%) |
   | `SEN` | Hồi sanity | `Heal Amount` |
   | `Food` | Hồi lương thực | `Food Restore Amount` |
   | `Buff_ATK` | Tăng ATK | `Buff Value` + `Buff Duration` |
   | `Buff_DEF` | Tăng DEF | `Buff Value` + `Buff Duration` |
   | `Buff_SPD` | Tăng SPD | `Buff Value` + `Buff Duration` |
   | `Buff_CRIT` | Tăng Crit% | `Buff Value` + `Buff Duration` |
   | `Buff_EVA` | Tăng Eva% | `Buff Value` + `Buff Duration` |
   | `WeaponCoating` | Tẩm vũ khí | `Coating Type` + `Coating Chance` |
   | `Cleanse` | Giải debuff | (không cần điền thêm) |

4. **Stack & Sử dụng**:
   - `Is Stackable`: ✅
   - `Max Stack Size`: 99 (hoặc ít hơn nếu muốn giới hạn)
   - `Usable In Combat`: ✅ (nếu dùng được trong chiến đấu)

---

## 4. Thêm Sự Kiện Dungeon Mới

### Bước 1 — Tạo EventData ScriptableObject

1. **Project Window** → Chuột phải → **Create → RPG → Dungeon Event**.
2. Đặt tên file.
3. Điền **Inspector**:
   | Field | Giá Trị |
   |-------|---------|
   | `Event Name` | Tên sự kiện (hiển thị trên panel) |
   | `Event Icon` | Sprite icon sự kiện |
   | `Description` | Mô tả tình huống (dài) |

4. Điền **mảng Choices** (mỗi phần tử = 1 lựa chọn):
   | Field | Giá Trị |
   |-------|---------|
   | `Choice Text` | Văn bản nút chọn |
   | `Base Success Chance` | % thành công (0-100) |
   | `Reward Gold/Exp/HP/Food/Sanity` | Phần thưởng nếu thành công |
   | `Reward Item` | Kéo ItemData vào (nếu có) |
   | `Buff Stat Type` | Tên chỉ số muốn buff tạm ("ATK"/"DEF"...) |
   | `Buff Stat Amount` | Lượng buff |
   | `Success Message` | Văn bản khi thành công |
   | `Penalty HP/Food/Sanity/Gold` | Hình phạt nếu thất bại |
   | `Fail Message` | Văn bản khi thất bại |

   > **Cạm bẫy (Trap)**: Nếu để `Choices` = 0 phần tử, dùng các field `Trap Penalty *` để cài đặt cạm bẫy kích hoạt tự động.

### Bước 2 — Tạo Tile mới cho sự kiện

1. Chuẩn bị **Sprite** cho tile này.
2. Tạo **Tile Asset**: **Create → 2D → Tiles → Tile** → gán Sprite vào.

### Bước 3 — Gán vào EventManager (hoặc PlayerMovement)

1. Chọn **GameObject PlayerMovement** trong Scene **Dungeon**.
2. Inspector → `Player Movement` → mảng **Event Mappings** → nhấn `+`.
3. Kéo **Tile Asset** vào field `Event Tile`.
4. Kéo **EventData** vào field `Event Data`.

### Bước 4 — Thêm tile vào pool sinh ngẫu nhiên

1. Chọn **GameObject DungeonGenerator** trong Scene **Dungeon**.
2. Inspector → `Event Tiles` → nhấn `+` → kéo **Tile Asset** vào.

> ✅ Từ giờ khi sinh Dungeon, tile sự kiện mới có thể xuất hiện và khi đạp vào sẽ kích hoạt EventData đã ghép.

---

## 5. Thêm Ủy Thác Guild Mới

### Bước 1 — Chọn GameObject GuildManager

1. Mở Scene **Town**.
2. Chọn **GameObject có GuildManager** (thường là `GameManagers` hoặc `GuildManager`).

### Bước 2 — Thêm QuestConfig mới

1. Inspector → `Guild Manager` → mảng **Predefined Quests** → nhấn `+`.
2. Điền các field:

   | Field | Giá Trị |
   |-------|---------|
   | `Quest ID` | Chuỗi định danh duy nhất (vd: `hunt_spider_10`) |
   | `Type` | `Hunt` (tiêu diệt quái) hoặc `Gather` (thu thập vật phẩm) |
   | `Target Name` | Tên quái (`enemyName` trong EnemyStats) hoặc tên vật phẩm (`itemName`) |
   | `Quest Icon` | Kéo Sprite icon vào |
   | `Required Amount` | Số lượng cần (vd: 5 con nhện) |
   | `Reward Gold` | Vàng thưởng |
   | `Reward Exp` | EXP thưởng |
   | `Reward Item` | Kéo ItemData vào (nếu thưởng vật phẩm) |
   | `Reward Item Amount` | Số lượng vật phẩm thưởng |
   | `Reward Equipment` | Kéo ItemData (Equipment) vào (nếu thưởng trang bị) |

> ⚠️ **Quan trọng**: `Target Name` cho **Hunt** quest phải khớp **chính xác** với `enemyName` trên prefab EnemyStats của quái đó (phân biệt hoa/thường).

---

## 6. Thêm Item/Trang Bị Vào Shop

> Shop lấy ngẫu nhiên từ pool `allPossibleItems`. Chỉ cần thêm item vào pool là xong.

### Cách thêm vào pool ngẫu nhiên

1. Đảm bảo đã tạo **ItemData ScriptableObject** (xem mục 2 hoặc 3).
2. Mở Scene **Town** → Chọn **GameObject ShopManager**.
3. Inspector → `Shop Manager` → mảng **All Possible Items** → nhấn `+`.
4. Kéo **ItemData** vừa tạo vào ô mới.

> **Lưu ý**: Shop không bán bình máu (`ConsumableType.HP`) theo thiết kế hiện tại. Nếu muốn bỏ giới hạn này, sửa logic trong `ShopManager.RefreshShop()`.

### Điều chỉnh số lượng và vàng shop

- Chọn **GameObject ShopManager** → Inspector:
  - `Shop Gold Min` / `Shop Gold Max`: Khoảng vàng ngẫu nhiên cho chủ shop.
  - Shop hiển thị **12 item** ngẫu nhiên mỗi lần refresh.

---

## 7. Thêm Kẻ Thù Mới

### Bước 1 — Tạo EnemyStats Prefab

1. Tạo **GameObject mới** trong Hierarchy.
2. **Add Component → EnemyStats**.
3. Điền **Inspector**:

   | Field | Giá Trị |
   |-------|---------|
   | `Enemy Name` | Tên hiển thị (và dùng để track kill cho Guild) |
   | `Enemy Sprite` | Kéo Sprite vào |
   | `Enemy Type` | `Human` hoặc `NonHuman` |
   | `Max HP` | HP tối đa |
   | `Base Defense` | DEF cơ bản |
   | `Base Speed` | SPD — ảnh hưởng đến lượt trong ATB |
   | `Attack` | ATK cơ bản |
   | `Crit Chance` | % chí mạng |
   | `Evasion` | % né tránh |
   | `Gold Drop` | Vàng rơi khi chết |
   | `Exp Drop` | EXP rơi khi chết |

4. Điền **phần Kỹ năng** (nếu có skill):
   | Field | Giá Trị |
   |-------|---------|
   | `Skill ID` | Chọn enum (phải đã đăng ký trong SkillExecutor) |
   | `Skill Name` | Tên kĩ năng hiển thị |
   | `Skill Description` | Mô tả kĩ năng |
   | `Max Cooldown` | Số lượt giữa các lần dùng skill |
   | `Initial Cooldown` | Số lượt chờ trước khi dùng skill lần đầu |

   > Nếu quái **không có skill**: để `Skill ID = None`.

5. **Save thành Prefab**: Kéo GameObject vào folder `Assets/Prefabs/Enemies/`.

### Bước 2 — Tạo Tile cho kẻ thù

1. Chuẩn bị **Sprite** cho tile quái trên map dungeon.
2. **Create → 2D → Tiles → Tile** → gán Sprite.

### Bước 3 — Gán vào EncounterSpawner

1. Chọn **GameObject CombatManager** trong Scene **Combat**.
2. Chọn **Component EncounterSpawner** (hoặc tìm `EncounterSpawner` trong Hierarchy).
3. Mảng **Normal Enemy Prefabs** → nhấn `+` → kéo **Prefab** vừa tạo vào.

   > ⚠️ Tên prefab (file name) phải là **chữ thường, không dấu** (vd: `nhenhang`, `cotyi`) vì `FindNormalPrefabByName()` so sánh lowercase.

### Bước 4 — Thêm tile vào DungeonGenerator

1. Chọn **GameObject DungeonGenerator** trong Scene **Dungeon**.
2. Inspector → mảng **Enemy Tiles** → nhấn `+` → kéo **Tile Asset** vào.

### Bước 5 (Tùy chọn) — Thêm skill mới cho quái

> Nếu quái có skill mới (chưa có trong enum), xem `HuongDanTaoSkill.md` mục **"Thêm Skill Kẻ Thù Mới"**.

---

## ⚡ Checklist Nhanh

| Nội dung | Nơi tạo | Nơi gán |
|----------|---------|---------|
| Skill người chơi | `PlayerSkills.cs` + `SkillExecutor` | `SkillData` SO → `SkillManager` |
| Skill kẻ thù | `EnemySkills.cs` + `EnemySkillID` enum | Prefab `EnemyStats.skillID` |
| Trang bị | `ItemData` SO (`Equipment`) | `ShopManager.allPossibleItems` (nếu bán) |
| Vật phẩm tiêu hao | `ItemData` SO (`Consumable`) | `ShopManager.allPossibleItems` (nếu bán) |
| Sự kiện Dungeon | `EventData` SO | `PlayerMovement.eventMappings` + `DungeonGenerator.eventTiles` |
| Ủy thác Guild | `QuestConfig` trong Inspector | `GuildManager.predefinedQuests` |
| Shop item | ItemData đã tạo | `ShopManager.allPossibleItems` |
| Kẻ thù mới | `EnemyStats` Prefab + Tile | `EncounterSpawner.normalEnemyPrefabs` + `DungeonGenerator.enemyTiles` |
