# 📅 KeHoach.md — Lộ Trình Tính Năng Còn Lại

> Cập nhật: 2026-05-16.

---

## 🎯 Bảng Ưu Tiên — Việc Còn Lại

| STT | Tính năng | Ưu tiên | Ghi chú |
|-----|-----------|---------|---------|
| 1 | **Kho Storage 60 ô** | 🟡 Trung | Hiện `storageMaxSlots = 20`, chưa có vật phẩm loại 2 |
| 2 | **Localization VN/EN** | 🟢 Thấp | Tạm hoãn đến sau khi nội dung game ổn định |

---

## 🧩 Chi Tiết Logic & Hướng Triển Khai

---

### 🟡 2. Kho Storage 60 Ô

> **Tạm hoãn** — chưa có vật phẩm loại 2 (Material/KeyItem).

---

### 🟢 3. Localization VN/EN

> Tạm hoãn đến sau khi nội dung game ổn định.

---

## ✅ Đã Hoàn Thành (Tham Khảo chưa chắc chính sát 100%)

| Tính năng | Phiên |
|-----------|-------|
| STR/DEX/VIT/AGL default = 10 | 2026-05-01 |
| Flee formula: `80 - senLost×4` | 2026-05-01 |
| Skill 4 Bleed — duration tracking 4 lượt | 2026-05-01 |
| SEN ≤ 4 — ẩn info kẻ thù + icon `?` | 2026-05-01 |
| SEN = 0 — về Town, reset = 2 | 2026-05-01 |
| Save/Load JSON (PlayerStats, Inventory, Dungeon, Skills) | 2026-05-01 |
| EnemySkillID enum — thay thế string.Contains | 2026-05-01 |
| Nhện Tinh Anh — entity-count logic (alive HP>0) | 2026-05-01 |
| Shop gold `70–250` SerializeField | 2026-05-01 |
| Magic number → const/SerializeField | 2026-05-01 |
| Coroutine leak guard (yield break) | 2026-05-01 |
| InventoryManager Start() test items → ContextMenu | 2026-05-01 |
| EXP/Level display trên HUD | 2026-05-01 |
| Combat inventory không giới hạn, Storage = 20 ô | 2026-05-01 |
| Enemy death animation (slide + fade 0.5s) | 2026-05-01 |
| ContextMenu thêm nhiều đồ test cùng lúc | 2026-05-01 |
| **Death Screen (Combat + Dungeon)** | 2026-05-04 |
| **DeathContext system** — truyền trạng thái chết giữa scenes | 2026-05-04 |
| **Madness Panel** — SEN=0 hiện bảng phát điên (Combat + Dungeon) | 2026-05-04 |
| **TownDeathNoticeUI** — thông báo tại Town khi quay về | 2026-05-04 |
| **DungeonDeathUI** — bảng chết/phát điên trong Dungeon | 2026-05-05 |
| **Mouse tile movement + hover highlight** — click ô kề cạnh | 2026-05-05 |
| **ExitDialog bug fix** — HUDCanvas chặn raycast | 2026-05-05 |
| **Buff Item Runtime** — BuffManager, IconLibrary, StatusIconContainer, Defending icon | 2026-05-07 |
| **Weapon Coating (Skill 4 + Item)** — ghi đè coating, icon hiển thị | 2026-05-07 |
| **BuffManager tick + event** — OnBuffTick, duration đếm ngược đúng | 2026-05-07 |
| **Cleanse Item** — ConsumableType.Cleanse đã implement | 2026-05-07 |
| **Boss LV5 — Nhện Nữ Vương** — 3 Innate passives, spawn ally logic | 2026-05-10 |
| **Boss LV10 — Rồng Cổ Đại** — Stun, Fire breath, Enrage | 2026-05-10 |
| **Giới hạn Túi Đồ (3/3 Bình Máu)** — Tặng 50 vàng, 3 bình máu khởi đầu | 2026-05-10 |
| **Guild UI Hoàn Chỉnh** — Lương thưởng, Shop máu/thức ăn, Nhiệm vụ tùy chỉnh, Cập nhật tiến độ | 2026-05-11 |
| **Ô Vàng trên Map** — 5% spawn mỗi ô còn trống, nhận 20-60 vàng, hiện floating text vàng | 2026-05-16 |
| **Enemy AI Heal (HealEnemy)** — Cốt Y hồi đồng minh HP thấp nhất, tự hồi khi không có đồng minh, hiện số xanh lá | 2026-05-16 |
| **Phí Rút Lui Về Town** — Trừ 30 vàng khi quay về qua Cửa Vào; dialog hiện số vàng sẽ mất | 2026-05-16 |
| **Hình Phạt Chết/Điên** — Mất nửa vàng & EXP kiếm trong tầng; không ảnh hưởng vật phẩm | 2026-05-16 |
| **Công thức EXP Đa Thức** — Dễ lên cấp lúc đầu, khó dần về sau (Base + Linear + Quad) | 2026-05-16 |
