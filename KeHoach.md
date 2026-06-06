# 📅 KeHoach.md — Lộ Trình Tính Năng & Kế Hoạch Chi Tiết

> Cập nhật: 2026-06-04.
> Lập kế hoạch dựa trên **Mục Tiêu Hiện Tại.md**

---

## 🎯 Bảng Ưu Tiên — Việc Cần Làm Tiếp Theo

| STT | Tính năng | Ưu tiên | Trạng thái | Ghi chú |
|-----|-----------|---------|------------|---------|
| 1 | **Kho Storage 60 ô** | 🟡 Trung | ⏸️ Tạm hoãn | Chờ có vật phẩm loại 2 (Material/KeyItem). |
| 2 | **Localization VN/EN** | 🟢 Thấp | ⏸️ Tạm hoãn | Thực hiện sau khi nội dung game ổn định. |

---

## ✅ Đã Hoàn Thành

| Tính năng | Phiên hoàn thành | Chi tiết |
|-----------|------------------|----------|
| **Nút Cài Đặt (Bánh Răng) & Thoát Game tại Town** | 2026-06-04 | Thêm nút bánh răng mở panel cài đặt cấu hình (âm lượng, màn hình, ngôn ngữ...) và nút lưu game tự động trước khi thoát về màn hình chính. |
| **Hệ Thống Sự Kiện Town (Town Events)** | 2026-06-03 | Tích hợp hệ thống roll sự kiện ngẫu nhiên 50% sau khi hoàn thành tầng Dungeon (Giảm 25% mua shop/guild, tăng 50% giá bán, miễn phí rượu guild, giảm 25% học kĩ năng). Hiển thị sự kiện trên HUD và hỗ trợ Save/Load. |
| **Cải Thiện & Sửa Lỗi UI Town** | 2026-06-02 | Sửa lỗi màn hình xanh (bảo vệ các manager khi Awake), tạo TownUIController quản lý phím tắt, tạo TownHUD đồng bộ chỉ số trên cùng. |
| **Tái Thiết Hệ Thống EXP & Điểm Kỹ Năng** | 2026-05-31 | EXP cố định 50 cho mọi cấp, nâng cấp thuộc tính nhận +1 điểm, không tăng maxFood, tích hợp thanh EXP Slider và HUD hiển thị điểm kỹ năng. |
| **Scene Start (Menu & 3 Save Slots)** | 2026-05-31 | Menu 3 nút (Bắt đầu, Cài đặt, Thoát). Thiết lập 3 file save slot độc lập, trích xuất metadata thời gian chơi/level lên slot trống/đã có, bảng xác nhận xóa và Auto-save 1 phút. |
| **Điều Chỉnh Lưu Trữ Dungeon Khi Chết** | 2026-05-31 | Khi người chơi chết/điên tại Dungeon, dungeon map tiến trình cũ sẽ được reset ngẫu nhiên map mới (seed mới, fog mới, quái mới) thay vì giữ nguyên map cũ bị kẹt. |
| STR/DEX/VIT/AGL default = 10 | 2026-05-01 | Thiết lập chỉ số mặc định ban đầu là 10. |
| Flee formula: `80 - senLost×4` | 2026-05-01 | Áp dụng công thức chạy trốn dựa theo Sen mất. |
| Skill 4 Bleed — duration tracking 4 lượt | 2026-05-01 | Thiết lập hiệu ứng chảy máu kéo dài 4 lượt. |
| SEN ≤ 4 — ẩn info kẻ thù + icon `?` | 2026-05-01 | Che thông tin kẻ địch khi lý trí quá thấp. |
| SEN = 0 — về Town, reset = 2 | 2026-05-01 | Tự động quay về Town khi phát điên. |
| Save/Load JSON (PlayerStats, Inventory, Dungeon, Skills) | 2026-05-01 | Cơ chế cơ bản lưu trữ dữ liệu sang JSON. |
| EnemySkillID enum — thay thế string.Contains | 2026-05-01 | Tối ưu hóa định danh kỹ năng kẻ địch. |
| Nhện Tinh Anh — entity-count logic (alive HP>0) | 2026-05-01 | Đếm số lượng kẻ địch tinh anh còn sống. |
| Shop gold `70–250` SerializeField | 2026-05-01 | Điều chỉnh lượng vàng trong shop. |
| Magic number → const/SerializeField | 2026-05-01 | Dọn dẹp magic numbers trong code. |
| Coroutine leak guard (yield break) | 2026-05-01 | Ngăn ngừa rò rỉ coroutine. |
| InventoryManager Start() test items → ContextMenu | 2026-05-01 | Thêm tính năng test đồ nhanh trong editor. |
| Combat inventory không giới hạn, Storage = 20 ô | 2026-05-01 | Quy định dung lượng túi đồ và kho. |
| Enemy death animation (slide + fade 0.5s) | 2026-05-01 | Thêm hiệu ứng trượt mờ khi quái chết. |
| **Death Screen (Combat + Dungeon)** | 2026-05-04 | Hiển thị màn hình khi người chơi HP = 0. |
| **DeathContext system** | 2026-05-04 | Hệ thống truyền trạng thái chết giữa các Scene. |
| **Madness Panel** | 2026-05-04 | Hiện bảng phát điên khi Sen = 0. |
| **TownDeathNoticeUI** | 2026-05-04 | Bảng thông báo chết/điên khi xuất hiện ở Town. |
| **DungeonDeathUI** | 2026-05-05 | Quản lý thông báo chết/điên khi ở Dungeon. |
| **Mouse tile movement + hover highlight** | 2026-05-05 | Di chuyển bằng chuột và vẽ ô hover. |
| **ExitDialog bug fix** | 2026-05-05 | Sửa lỗi dialog thoát bị Canvas chặn raycast. |
| **Buff Item Runtime** | 2026-05-07 | Hệ thống quản lý buff và icon buff theo thời gian thực. |
| **Weapon Coating (Skill 4 + Item)** | 2026-05-07 | Tẩm độc/máu lên vũ khí và hiển thị icon tương ứng. |
| **BuffManager tick + event** | 2026-05-07 | Đồng bộ đếm ngược thời gian kéo dài của buff. |
| **Cleanse Item** | 2026-05-07 | Đã thiết lập giải trừ hiệu ứng xấu ConsumableType.Cleanse. |
| **Boss LV5 — Nhện Nữ Vương** | 2026-05-10 | Boss Nhện 3 passives và gọi nhện con. |
| **Boss LV10 — Rồng Cổ Đại** | 2026-05-10 | Boss Rồng khè lửa, stun và nộ. |
| **Giới hạn Túi Đồ (3/3 Bình Máu)** | 2026-05-10 | Giới hạn số lượng máu mang theo và tặng quà khởi đầu. |
| **Guild UI Hoàn Chỉnh** | 2026-05-11 | Bảng nhiệm vụ tùy chỉnh, nhận lương thưởng và nâng cấp tiến độ. |
| **Ô Vàng trên Map** | 2026-05-16 | Tỷ lệ xuất hiện ô vàng nhận tiền ngẫu nhiên trên bản đồ. |
| **Enemy AI Heal (HealEnemy)** | 2026-05-16 | Quái hỗ trợ hồi máu đồng minh thấp máu nhất. |
| **Phí Rút Lui Về Town** | 2026-05-16 | Thu phí rút lui 30 vàng khi đi qua Cửa Vào. |
| **Hình Phạt Chết/Điên** | 2026-05-16 | Mất nửa số vàng và EXP kiếm được trong tầng khi thất bại. |
