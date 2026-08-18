# 📊 Bảng Phân Bổ Quái & Sự Kiện 20 Tầng Dungeon

## 1. Công Thức & Giới Hạn Trần (Đã Áp Dụng Vào Code)

- **Tổng ô bản đồ**: $\text{Total} = \text{Width} \times \text{Height}$
- **Ô đặc biệt (20% diện tích)**: $\text{SpecialTiles} = \text{Round}(\text{Total} \times 20\%)$
- **Quái thường (60% ô đặc biệt)**: $\text{Enemies} = \text{Clamp}(\text{Round}(\text{SpecialTiles} \times 60\%), 3, 9)$ ➔ **Tối thiểu 3, Tối đa Trần 9**
- **Sự kiện (40% ô đặc biệt)**: $\text{Events} = \text{Clamp}(\text{Round}(\text{SpecialTiles} \times 40\%), 2, 6)$ ➔ **Tối thiểu 2, Tối đa Trần 6**
- **Cộng thêm**:
  - Tầng Boss (Tầng 10, 20): $+1$ Tile Boss (ở ô xa người chơi nhất).
  - Tầng Quái Tinh Anh (Tầng 3, 7, 12, 16, 18): $+1$ Tile Quái Tinh Anh.

---

### Bảng Áp Dụng Cho Các Tầng Mốc (Biên Tier)

| Tầng | Kích thước | Tổng ô | Ô đặc biệt (20%) | Quái thường (60%) | Event (40%) | Ô Cộng Thêm |
|:---:|:---:|:---:|:---:|:---:|:---:|---|
| **1** | $5 \times 5$ | 25 | 5 | **3** | **2** | — |
| **3** | $7 \times 7$ | 49 | 10 | **6** | **4** | $+1$ Elite Tile |
| **4** | $6 \times 6$ | 36 | 7 | **4** | **3** | — |
| **7** | $8 \times 8$ | 64 | 13 | **8** | **5** | $+1$ Elite Tile |
| **9** | $9 \times 9$ | 81 | 16 | **9** *(Chạm trần)* | **6** *(Chạm trần)* | — |
| **10 (Boss 1)** | $9 \times 9$ | 81 | 16 | **9** *(Chạm trần)* | **6** *(Chạm trần)* | $+1$ Boss Tile (Nhện Nữ Vương) |
| **11** | $7 \times 8$ | 56 | 11 | **7** | **4** | — |
| **12** | $8 \times 8$ | 64 | 13 | **8** | **5** | $+1$ Elite Tile |
| **14** | $8 \times 9$ | 72 | 14 | **8** | **6** *(Chạm trần)* | — |
| **15** | $8 \times 8$ | 64 | 13 | **8** | **5** | — |
| **16** | $8 \times 9$ | 72 | 14 | **8** | **6** *(Chạm trần)* | $+1$ Elite Tile |
| **18** | $9 \times 9$ | 81 | 16 | **9** *(Chạm trần)* | **6** *(Chạm trần)* | $+1$ Elite Tile |
| **19** | $9 \times 10$ | 90 | 18 | **9** *(Chạm trần)* | **6** *(Chạm trần)* | — |
| **20 (Đại Boss)** | $9 \times 10$ | 90 | 18 | **9** *(Chạm trần)* | **6** *(Chạm trần)* | $+1$ Đại Boss Tile (Rồng Cổ Đại) |

---

## 💡 2. Gợi Ý Mở Rộng Quái Vật Mới (Theo 4 Tier)

Hiện tại game có **9 quái**: `Chuột`, `Chuột nhảy`, `Đỉa`, `Cốt Y`, `Thằn lằn đen`, `Tê tê`, `Nhện`, `Nhện hang`, `Nhện tinh anh` + 2 Boss.
Để hoàn thiện 20 tầng không bị lặp lại, cần thêm khoảng **5 – 7 quái mới**:

### Tier 1 (Tầng 1 – 5) — Khu Vực Hang Động / Nhập Môn *(Đã đủ)*
- Quái: `Enemies_chuot`, `Enemies_chuotnhay`, `Enemies_dia`, `Enemies_nhen`.

### Tier 2 (Tầng 6 – 10) — Tổ Nhện Độc / Đầm Lầy Sâu *(Đã đủ)*
- Quái: `Enemies_nhenhang`, `Enemies_CotY`, `Enemies_nhentinhanh`.
- Boss Tầng 10: `Enemies_nuvuongnhen` (Nhện Nữ Vương).

### Tier 3 (Tầng 11 – 15) — Hầm Mộ Cổ / Tàn Tích Hắc Ám *(Cần thêm 3 quái)*
1. **Bộ Xương Chiến Binh (Skeleton Warrior)**: Quái cận chiến có khiên giáp cao (chỉ số VIT cao).
2. **Dơi Hút Máu (Vampire Bat)**: Quái tốc độ cao (AGL cao), có skill hút máu hồi phục bản thân.
3. **Bóng Ma U Hồn (Specter / Ghost)**: Có khả năng né tránh (Evasion) cao và gây debuff hoảng sợ (trừ Sanity/SEN).
- *Quái hiện có hỗ trợ Tier này*: `Enemies_thanlanden`, `Enemies_Tete`.

### Tier 4 (Tầng 16 – 20) — Hang Nham Thạch / Vực Sâu Rồng *(Cần thêm 3 quái)*
1. **Hỏa Long Con (Fire Drake / Wyrmling)**: Quái rồng nhỏ có skill phun lửa gây sát thương Burn.
2. **Golem Nham Thạch (Lava Golem)**: Quái cực trâu bò, có phản sát thương khi bị đánh.
3. **Kẻ Tế Lễ Hắc Ám (Dark Cultist)**: Có skill hồi máu hoặc buff cuồng nộ cho đồng minh xung quanh.
- Boss Tầng 20: `Enemies_rongcodai` (Rồng Cổ Đại).

---

## 📜 3. Gợi Ý Mở Rộng Sự Kiện Mới (`EventData` ScriptableObject)

Hiện tại game có **3 sự kiện** (2 lựa chọn + 1 bẫy). Nên tạo thêm **7 – 9 sự kiện mới** chia làm 4 nhóm:

### Nhóm 1: Rương & Tài Nguyên (Loot & Resources)
1. **Rương Gỗ Cũ**:
   - Lựa chọn 1: Mở cẩn thận ➔ Nhận 20–40 Vàng.
   - Lựa chọn 2: Phá rương nhanh ➔ 70% nhận đồ, 30% dính bẫy gãy tay (-10 HP).
2. **Rương Khóa Cổ (Locked Chest)**:
   - Lựa chọn 1: Dùng 1 Chìa Khóa mở ➔ Nhận chắc chắn 1 Trang bị / Bảo vật xịn.
   - Lựa chọn 2: Cạy khóa mạo hiểm ➔ 40% thành công, 60% hỏng rương.
3. **Xác Kẻ Phiêu Lưu (Dead Adventurer)**:
   - Khám xét thi thể ➔ Nhận +2 Food, +1 Bình Máu (nhưng trừ 1 SEN vì cảnh tượng kinh hoàng).

### Nhóm 2: Đền Thờ & Đánh Đổi (Trade-off & Altars)
4. **Bàn Thờ Huyết Tế (Blood Altar)**:
   - Hiến tế 25 HP hiện tại ➔ Nhận vĩnh viễn +3 ATK (STR) cho lượt đi Dungeon này.
5. **Giếng Nước Cổ Xưa (Ancient Well)**:
   - Uống nước giếng ➔ Hồi phục đầy SANITY (SEN) nhưng tốn 15 HP (nhiễm độc nhẹ).
6. **Tượng Thần Cổ (Mysterious Shrine)**:
   - Cầu nguyện thành tâm ➔ 50% nhận Buff Tốc độ +5 SPD, 50% bị trừng phạt mất 30 Vàng.

### Nhóm 3: Cạm Bẫy Mới (Traps)
7. **Bẫy Khói Độc**: Cạm bẫy trực tiếp ➔ Mất 15 HP + Trừ 2 Food.
8. **Hố Sụt Lún (Sinkhole)**: Cạm bẫy trực tiếp ➔ Mất 30–50 Vàng bị rơi mất xuống vực sâu.

### Nhóm 4: Gặp Gỡ & Hồi Phục (Camp & Lore)
9. **Lửa Trại Bỏ Hoang (Abandoned Camp)**:
   - Dừng chân nghỉ ngơi ➔ Hồi phục 30% Máu + 3 SEN + Tốn 1 Food.
10. **Bia Mộ Cổ (Ancient Obelisk)**:
    - Giải mã văn tự cổ ➔ Nhận 80 EXP (hoặc nếu thất bại bị đau đầu trừ 2 SEN).