\# 📜 ReadMe.md - Yêu Cầu Thực Hiện Đồ Án Game 2D RPG Theo Lượt

\---



\## 🎯 1. Tổng Quan Dự Án

\- \*\*Thể loại:\*\* 2D Turn-based RPG (Demo đồ án cơ sở)

\- \*\*Ngôn ngữ:\*\* Hỗ trợ song ngữ Tiếng Việt \& Tiếng Anh (tích hợp Localization)

\- \*\*Asset:\*\* Sử dụng asset free + AI generate + công cụ chỉnh sửa ảnh

\- \*\*Phạm vi hoàn thiện:\*\* Combat, Skill Tree, Item, Shop, Inventory, Enemies, Town, Guild, Dungeon (10 tầng đầu), UI đầy đủ, hệ thống SEN, cơ chế AP Gauge.

\- \*\*Mục tiêu:\*\* Xây dựng nền tảng combat chiến thuật dựa trên tốc độ (SPD), hệ thống trạng thái phức tạp, khám phá dungeon dạng lưới, và kinh tế game cân bằng.



\---



\## 📊 2. Hệ Thống Chỉ Số \& Công Thức Toán Học

\### 2.1 Chỉ số mặc định Người Chơi (Player Base Stats)

| Chỉ số | Giá trị mặc định | Ghi chú |

|--------|------------------|---------|

| `maxHP` | 20 | `currentHP` cập nhật động |

| `attack` | 1 | Sát thương nền tảng |

| `defense` | 0 | Chỉ tăng qua trang bị/buff |

| `speed` | 5 | Cơ sở tính SPD |

| `gold` | 0 | Tiền tệ |

| `sen` | 10 | Giới hạn cứng = 10 |

| `food` | 50 | Nhu yếu phẩm |

| `Crit` | 5% | Tỉ lệ chí mạng |

| `Eva` | 5% | Tỉ lệ né đòn |

| `STR/DEX/VIT/AGL/DEF/MDF/RES/INT/FAITH` | 0.0 | Tăng dần qua level/item |



\### 2.2 Công Thức Tính Toán

1\. \*\*Sát thương tổng (Damage Output):\*\*

&#x20;  ```

&#x20;  Damage = (1 (Base) + WeaponDmg + STR×0.1 + EquipBonus + SkillBonus + BuffBonus) × Kháng

&#x20;  ```

2\. \*\*Giảm trừ phòng thủ (DEF Reduction):\*\*

&#x20;  ```

&#x20;  DamageTaken = Max(1, InputDamage - (EnemyDEF - ArmorPenetration))

&#x20;  ```

&#x20;  \*Lưu ý:\* DEF không thể < 0. Xuyên giáp giảm hiệu quả DEF của mục tiêu.

3\. \*\*Tốc độ tổng (Total SPD):\*\*

&#x20;  ```

&#x20;  TotalSPD = BaseSPD + (AGL × 0.5)

&#x20;  ```

4\. \*\*Hệ thống SEN (Sanity/Tỉnh Táo):\*\*

&#x20;  - `SEN` mặc định = 10, không thể vượt quá 10.

&#x20;  - Tỉ lệ thành công sự kiện: `Base% + (SEN × 3%)`. (Ví dụ: 10 SEN = +30%)

&#x20;  - Giảm SEN: `-5%` mỗi ô dungeon, `-18%` nếu `food = 0`, giảm khi bị chí mạng.

&#x20;  - `SEN ≤ 4`: Ẩn thông tin kẻ thù, mã hóa chỉ số/skill, NPC có thể tấn công hoặc thất bại nhiệm vụ.

&#x20;  - `SEN = 0`: Thám hiểm thất bại, quay về Town, SEN reset về 2. Hết tiền → Xóa save, chơi lại.

&#x20;  - Hồi SEN: Item (Thuốc an thần), Rượu tại quán trọ, Sự kiện đặc biệt.

5\. \*\*Nâng cấp nhân vật:\*\* Mỗi level lên +3 điểm chỉ số tự phân bổ. Công thức scaling:

&#x20;  - `10 STR` → +1 ATK, `15 STR` → +1.5 ATK, `20 STR` → +2 ATK (tương tự DEX, VIT, AGL)

&#x20;  - `10 VIT` → +5 HP, `15 VIT` → +10 HP, `20 VIT` → +15 HP

&#x20;  - `10 DEX` → +1% Crit, +1% Eva



\---



\## ⚔️ 3. Cơ Chế Chiến Đấu (Combat System)

\### 3.1 Thanh Hành Động (Action Gauge / AP System)

\- \*\*Ngưỡng kích hoạt:\*\* Cố định `100 điểm`.

\- \*\*Tick System:\*\* Mỗi tick, tất cả thực thể cộng `AP += TotalSPD`.

\- \*\*Kiểm tra lượt:\*\* Thực thể nào `AP ≥ 100` → được quyền hành động.

\- \*\*Giải quyết xung đột:\*\*

&#x20; 1. Ưu tiên `AP` cao hơn.

&#x20; 2. Nếu bằng nhau → \*\*Người chơi\*\* ưu tiên trước Kẻ thù.

\- \*\*Cơ chế trừ điểm:\*\* Sau khi hành động, `AP = AP - 100` (giữ lại phần dư, không reset về 0).

\- \*\*Tương tác:\*\* Item không tiêu tốn AP. Buff/Debuff SPD cập nhật ngay tại tick tiếp theo.

\- \*\*Hiển thị AP:\*\* Thanh hiển thị `Tick cuối cùng`. Ví dụ: Player đủ 100, Enemy 68 → thanh hiển thị `(68/100)`. Enemy hành động, Player tick được 30 → `(30/100)`.



\### 3.2 Cấu Trúc Lượt \& Nút Bấm

\- \*\*Thứ tự:\*\* Chọn Skill → Chọn mục tiêu (1 lần) → Xác nhận (lần 2) → Kiểm tra AP → Thực thi.

\- \*\*Nút lệnh:\*\* Tấn công, Kỹ năng (hiện CD), Phép (tương lai), Hành động (Giám định/Phòng thủ), Vật phẩm, Bỏ chạy.

\- \*\*Bỏ chạy:\*\* Tỉ lệ mặc định `80%`, giảm `-6%` mỗi điểm SEN mất. Thất bại → AP reset `0%`, kẻ thù tấn công dồn.

\- \*\*Số lượng thực thể:\*\* Hệ thống hỗ trợ tối đa \*\*5 thực thể\*\*. Tuy nhiên, trong các tình huống đặc biệt (ví dụ: Skill Nhện Tinh Anh) giới hạn hiển thị là \*\*3\*\*. Cần lập trình linh động.



\---



\## 🧪 4. Hệ Thống Trạng Thái (Debuff/Buff)

\*Ưu tiên áp dụng quy tắc từ `game\_design\_demo.txt`\*

| Trạng thái | Cơ chế | Sát thương/Giảm | Thời gian | Stack |

|------------|--------|----------------|-----------|-------|

| \*\*Chảy máu (Bleed)\*\* | Tỉ lệ cố định (<30% từ weapon/skill). Không thể kháng. Một số quái miễn nhiễm. | `8% MaxHP + BaseDmg` (12% nếu yếu với Bleed) | 1 lần | Không stack |

| \*\*Đập vỡ xương\*\* | Gây tổn thương, giảm tốc | +30% sát thương phải nhận, `-20% BaseSPD` | 3 lượt | Không stack |

| \*\*Choáng (Stun)\*\* | Mất lượt, AP vẫn tăng đến 100% nhưng không hành động. Refresh duration nếu trúng lại. | - | Theo skill | Refresh thời gian |

| \*\*Độc (Poison)\*\* | Cộng dồn, reset duration về 3 nếu stack khi còn >1 turn | Player: `3/5/8` dmg/tầng. Enemy: `+5% HP` nếu yếu | 3 lượt (reset) | Tối đa 3 |

| \*\*Bỏng (Burn)\*\* | Cộng dồn, giảm DEF | Player: `1/3/5` dmg/tầng. Enemy: `+5% HP` nếu yếu. Giảm DEF: `1/2/4` (min 0) | 3 lượt | Tối đa 3 |



\### 🔗 Tương Tác Trạng Thái

\- `Bỏng + Độc`: Gây cả 2 loại sát thương đồng thời.

\- `Chảy máu + Đập vỡ xương`: Sát thương %HP không bị ảnh hưởng bởi trọng thương.

\- `Độc + Choáng`: Kẻ thù bị choáng chịu \*\*2x sát thương độc\*\*, nhưng độc mất \*\*2 turn\*\*.

\- \*\*Kháng trạng thái:\*\* Trang bị có thể có `Poison/Burn Resistance` (giảm 50-100% sát thương trạng thái). Choáng không thể kháng, một số quái miễn nhiễm.

\- \*\*Xóa trạng thái:\*\* Tất cả đều có thể giải bằng Item tiêu hao.



\---



\## 🌳 5. Kỹ Năng \& Cây Kỹ Năng (Skill System)

\### 5.1 Kỹ Năng Kiếm (Sword Skills)

\- \*\*Skill 1:\*\* Chém 4 đòn liên tiếp vào địch ngẫu nhiên. Giảm 15% sát thương cộng dồn từ đòn 2 (Đ2:85%, Đ3:70%, Đ4:55%). `+2` bonus dmg. Áp dụng hiệu ứng đòn đánh cho mỗi đòn.

\- \*\*Skill 2:\*\* 1 đòn mạnh vào mục tiêu chọn. Có thể crit. `+5` bonus dmg, `+30%` crit chance.

\- \*\*Skill 3:\*\* 2 đòn vào 1 mục tiêu. Đòn 2 giảm `40%` sát thương. `+3` bonus dmg. Áp dụng hiệu ứng đòn đánh.

\- \*\*Skill 4:\*\* Phủ Bleed lên kiếm trong 4 lượt. `20%` chance gây Bleed khi chém. \*(Bắt buộc ghi dòng Debug combat để kiểm tra HP loss)\*.



\### 5.2 Skill Tree \& Học Kỹ Năng

\- Phân nhánh: Vũ khí chém, đâm, tầm xa, thể chất nội tại.

\- Học tại "Khu vực huấn luyện" bằng tiền vàng.

\- UI: Tạo `skillDetailPanel` (Popup). Nền: `btnCloseDetailBackground` (full màn hình mờ). Hiển thị: Tên, Cấp, Mô tả, Nút Học/Nâng cấp.



\### 5.3 Quy tắc CD \& Điều kiện đặc biệt

\- Skill tiêu tốn 1 lượt trừ khi là bị động.

\- `Nhện Tinh Anh`: Nếu `Entity count == 3` → Không dùng skill, đánh thường, CD không đổi. Nếu 1 quái (không phải Nhện Tinh Anh) chết → Kích hoạt skill ngay. Thất bại → Không triệu hồi, mất lượt. Thành công → CD hồi sau 5 turn. CD mặc định ban đầu: 2 turn, sau đó áp dụng CD cụ thể.



\---



\## 🎒 6. Vật Phẩm \& Trang Bị

\### 6.1 Phân loại Item

\- \*\*Loại 1 (Ngoài combat):\*\* 20 ô, không stack, dùng để bán/craft (tạm chưa triển khai).

\- \*\*Loại 2 (Trong combat):\*\* Stack tối đa `99`, không giới hạn ô. Buff, hồi máu, debuff, bom.

\- \*\*Bình máu khởi đầu:\*\* 3 lọ, hồi `33% maxHP`, dùng 1 lần/lượt. Hồi tại Guild `10g/lọ`. Tối đa 3 lọ (có thể nâng giới hạn qua lò rèn).

\- \*\*Buff chỉ số:\*\* Stack tối đa 3 tầng, duy trì 5 turn. Ví dụ: Thuốc tiên kích hỏa `+1/2/5` dmg.

\- \*\*Bom:\*\* Gây dmg cố định hoặc debuff, dùng 1 lần/lượt.



\### 6.2 Trang Bị (Equipment)

\- Không yêu cầu chỉ số để mặc. 4 slot: Vũ khí chính, Giáp (1 cái), Phụ kiện 1, Phụ kiện 2.

\- Vũ khí + Giáp: Tối đa 3 chỉ số cộng thêm. Phụ kiện: Tối đa 2 chỉ số.

\- \*\*Chỉ số tham khảo:\*\* (Coiled-nail: 1dmg, 3 AP | Machete: 2dmg, 40 bleed | Leather-armor: +5HP | Power-ring: +5STR | Tribal-pendant: -SEN drop,...)

\- \*\*Công thức damage trang bị:\*\* Cộng trực tiếp vào `(1 + WeaponDmg + STR×0.1 + ...)`



\---



\## 🗺️ 7. Khám Phá Dungeon \& Sự Kiện

\- \*\*Bản đồ lưới:\*\* `5x5`, `6x7`, `7x7`, `9x9`, `10x10` tùy tầng. Di chuyển chuột/phím mũi tên, chỉ đi ô liền kề.

\- \*\*Spawn ngẫu nhiên:\*\* Quái, Sự kiện, Ô Vàng (5%), Chìa khóa, Cửa vào/ra. \*\*Không được chồng lấn/ghi đè\*\*.

\- \*\*Tiêu hao:\*\* Mỗi ô `-1 food`. Nếu `food=0` → `-2 HP/ô`, tăng tỉ lệ giảm SEN. Hồi food tại Guild `30g` hoặc item.

\- \*\*Tiến trình:\*\* Hoàn thành 1 tầng → Tìm cửa ra (có thể khóa, cần chìa). Ra ngoài → Confirm hoàn thành, không thể vào lại tầng cũ. Tầng mới độ khó \& độ đa dạng tăng dần.

\- \*\*Boss:\*\* Miniboss mỗi 5 tầng, Boss chính mỗi 10 tầng.

\- \*\*Sự kiện:\*\* 2-3 lựa chọn, tỉ lệ thành công `% + (SEN×3%)`. Thành công: Vàng/EXP/Đồ. Thất bại: Trừ HP/Tiền/SEN/Giảm chỉ số.



\---



\## 🏘️ 8. Thị Trấn \& Hội Mạo Hiểm

\- \*\*Guild:\*\* UI mua máu/đồ ăn, hiển thị vàng. Nút nhận lương `40g` (sau khi hoàn thành tầng, mặc định `30g`). Bảng nhiệm vụ treo thưởng (nộp đồ/săn quái → nhận thưởng).

\- \*\*Khu vực khác:\*\* Cửa hàng, Thợ giáp/vũ khí (tạm khóa), Nhà giả kim (tạm khóa), Khu huấn luyện, Trường phép (tạm khóa), Trang bị/Kho (60 ô), Rời thị trấn.

\- \*\*Cửa hàng/Thương nhân:\*\* Gold shop random `70-150g` mỗi lần về town. 12 item không stack random. Logic mua/bán: Shop tăng/giảm gold theo giao dịch. Hết tiền → Không bán được.



\---



\## 🤖 9. Trí Tuệ Nhân Tạo Kẻ Thù (Enemy AI)

\*Hoạt động theo Cây Quyết Định (Decision Tree) tuần tự khi AP ≥ 100:\*

1\. \*\*Kiểm tra Choáng:\*\* Nếu dính → Trừ 100 AP, kết thúc lượt.

2\. \*\*Sinh tồn:\*\* Nếu `HP < 40%` \& có skill hồi → `70%` kích hoạt hồi máu.

3\. \*\*Skill Check:\*\* Nếu `CD == 0` → `70%` dùng skill, bắt đầu tính CD, trừ 100 AP. Ưu tiên tự heal hoặc đồng minh `HP < 40%`.

4\. \*\*Hành động mặc định:\*\* Tấn công cơ bản, trừ 100 AP.

\- \*Lưu ý:\* AI bỏ qua bước chọn mục tiêu (vì 1v1 hoặc nhóm cố định). Skill tăng SPD áp dụng ngay vào tick tiếp theo.



\---



\## 🖥️ 10. Giao Diện (UI) \& Điều Khiển

\### 10.1 Màn Hình Combat

\- \*\*Player Card:\*\* HP, SEN, MP(tương lai), AP%.

\- \*\*Enemy Card:\*\* Tên, HP, AP. Click icon → Hiện chỉ số \& skill. Nếu `SEN ≤ 4` → Icon đổi cố định, chỉ số/skill bị mã hóa. Khôi phục khi `SEN ≥ 5`.

\- \*\*Action Menu:\*\* Copy `actionMenu` → đổi tên `skillMenuPanel`. Nút gốc "Kỹ năng" → `OnClick` ẩn actionMenu, hiện skillMenuPanel.

\- \*\*Nút bấm Skill:\*\* Gắn script gọi `CombatManager.Instance.SelectSkillToUse(skillData)`. Trong `Start()`, kiểm tra `skillLevel > 0` mới `Instantiate` nút.



\### 10.2 Điều Khiển

\- Phím `↑↓←→`: Di chuyển menu/chọn mục tiêu.

\- `Z`: Xác nhận. `X`: Hủy/Quay lại.

\- Chuột: Hỗ trợ click trực tiếp trên UI.



\---



\## 🛠️ 11. Hướng Dẫn Triển Khai Kỹ Thuật (Cho AI/Dev)

1\. \*\*Kiến trúc Core:\*\* Sử dụng Singleton cho `GameManager`, `CombatManager`, `InventoryManager`, `SaveSystem`.

2\. \*\*AP System:\*\* Chạy `Coroutine` hoặc `Update()` với `Time.deltaTime` quy đổi sang `Tick`. Dùng `Queue` hoặc `PriorityQueue` sắp xếp thứ tự hành động dựa trên AP.

3\. \*\*Status Manager:\*\* Dùng `Dictionary<StatusType, StatusData>` để quản lý duration, stacks, damage calculation. Áp dụng công thức reset duration khi stack.

4\. \*\*UI Dynamic Loading:\*\* `skillMenuPanel` phải `Instantiate` button từ prefab dựa trên `SkillData.level`. Dùng `EventSystem` cho điều khiển phím.

5\. \*\*Debug \& Logging:\*\* Thêm dòng `Debug.Log($"\[Skill4] Bleed applied. Target HP loss: {damage}");` tại combat log.

6\. \*\*Localization:\*\* Dùng `ScriptableObject` hoặc JSON lưu chuỗi VN/EN. Gắn vào `Text` component.

7\. \*\*Save/Load:\*\* Serialize JSON cho PlayerStats, Inventory, DungeonProgress, SEN, Gold.

8\. \*\*Xử lý xung đột file:\*\* Ưu tiên `game\_design\_demo.txt` cho Debuff, `Skill.txt` cho UI/Boss override, `Ý tưởng.txt` cho nền tảng.



\---



\## 📖 12. Bảng Dữ Liệu Tham Chiếu (Enemies \& Bosses)

\### 12.1 Quái Thường

| Tên | HP | ATK | DEF | SPD | Crit | Eva | Skill |

|-----|----|-----|-----|-----|------|-----|-------|

| Chuột | 12 | 1 | 0 | 5 | 5% | 5% | Không |

| Nhện | 16 | 1 | 0 | 7 | 5% | 5% | Tơ dính: +2 ATK, 30% -2 SPD (CD3, không stack) |

| Nhện Hang | 20 | 2 | 1 | 10 | 7% | 8% | Cắn: +3 ATK 1 lượt (CD3) |

| Chuột nhảy | 18 | 2 | 0 | 15 | 8% | 10% | Tăng Tốc: +5 SPD 2 lượt (CD5) |

| Đỉa | 14 | 1 | 0 | 8 | 5% | 5% | Hút Máu: 30% Bleed, hồi 5HP (CD5) |

| Thằn lằn đen | 20 | 1 | 0 | 12 | 8% | 8% | Cắn độc: 30% Poison 3 turn |

| Nhện Tinh Anh | 28 | 3 | 2 | 14 | 12% | 5% | Gọi Đàn: Summon Nhện Hang (CD7) |



\### 12.2 Boss (Đã cập nhật override)

\- \*\*Boss LV5: Nhện Nữ Vương\*\* (Xuất hiện cùng 2 Nhện Tinh Anh)

&#x20; - `60HP, 4ATK, 3DEF, 110RB, 12% Crit, 8% Eva, 16SPD`

&#x20; - \*Skill 1 (Cào xé thịt):\* Sát thương chuẩn `18% HP player`, Crit `26%`. Không cộng dồn với buff dmg. CD 5 turn, không dùng ngay.

&#x20; - \*Nội tại 1:\* Nếu 2 Nhện Tinh Anh chết → Sinh thêm 2 Nhện Hang.

&#x20; - \*Nội tại 2:\* `HP < 40%` → Hóa điên, tăng `? ATK \& ? SPD` (biến riêng).

&#x20; - \*Nội tại 3:\* Đòn đánh có tỉ lệ nhỏ gây 1 tầng Độc (stack, 4 turn).

\- \*\*Boss LV10: Rồng Cổ Đại Bị Thương\*\* (Xuất hiện 1 mình)

&#x20; - `560/3060 HP, 16ATK, 5DEF, Miễn nhiễm Bleed/Stun, Yếu với Độc, 7% Crit, 5% Eva, 16SPD`

&#x20; - \*Skill 1 (Phun lửa):\* 3 đòn, mỗi đòn `6 damage`, `40%` gây Bỏng (stack, kéo dài 3 lượt). CD 5 turn, không dùng ngay.

&#x20; - \*Nội tại 1 (Tiếng Rống Suy Nhược):\* Mỗi 4 turn → Gầm, `30%` gây Choáng 1 turn. Có hiệu ứng khựng lấy hơi cảnh báo.

&#x20; - \*Nội tại 2:\* `HP < 40%` (theo HP cơ bản, không phải max) → Hóa điên, tăng `? ATK \& ? SPD` (biến riêng).

&#x20; - \*Nội tại 3 (Vảy Rạn Nứt):\* Khi bị Crit → Rồng giảm `10% DEF` trong 2 turn (stack). Nếu dính Độc → Sát thương độc nhận `+300%`.
\---

