# 📖 Hướng Dẫn Tạo Skill — OOP Skill System (Phase 5)

> Tài liệu này hướng dẫn bạn thêm **Skill Người Chơi** hoặc **Skill Kẻ Thù** mới vào game
> mà **không cần chạm vào `CombatManager.cs`** nữa.

---

## 🗂️ Cấu Trúc Thư Mục

```
Assets/Scripts/Combat/Skills/
├── SkillAction.cs          ← Base class cho skill người chơi
├── EnemySkillAction.cs     ← Base class cho skill kẻ thù
├── SkillExecutor.cs        ← Registry — đăng ký & điều phối tất cả skill
├── PlayerSkills.cs         ← Toàn bộ skill người chơi (SwordSkill1..4)
└── EnemySkills.cs          ← Toàn bộ skill kẻ thù (8 skill)
```

---

## ⚙️ Cài Đặt Trong Unity (Một Lần Duy Nhất)

**Chỉ cần làm 1 bước:**

1. Chọn GameObject chứa `CombatManager` trong Scene **Combat**.
2. **Add Component** → gõ `SkillExecutor` → Enter.

> ✅ Xong. `CombatManager` tự tìm `SkillExecutor` khi Start. Không cần kéo thả gì thêm.

---

## ➕ Cách Thêm Skill Người Chơi Mới

### Bước 1 — Mở `PlayerSkills.cs` và thêm class

```csharp
/// <summary>Sword_Skill_5 — Mô tả ngắn về skill.</summary>
public class SwordSkill5 : SkillAction
{
    public override void Execute(EnemyStats target, float flatDmg, float effectChance)
    {
        // Gây sát thương × 1.5 cho mục tiêu
        DealToEnemy(target, 1.5f, flatDmg);

        // Kết thúc lượt (BẮT BUỘC nếu skill đồng bộ)
        FinishTurn();
    }
}
```

### Bước 2 — Đăng ký trong `SkillExecutor.cs`

Mở `SkillExecutor.cs`, tìm hàm `RegisterAllSkills()` và thêm vào:

```csharp
Register("Sword_Skill_5", new SwordSkill5());
```

> ⚠️ `"Sword_Skill_5"` phải khớp **chính xác** với `skillID` trong `SkillData` ScriptableObject của bạn.

### Bước 3 — Không cần làm gì thêm ✅

---

## ➕ Cách Thêm Skill Kẻ Thù Mới

### Bước 1 — Thêm giá trị vào Enum `EnemySkillID`

Mở file định nghĩa `EnemySkillID` (thường trong `EnemyStats.cs` hoặc file riêng):

```csharp
public enum EnemySkillID
{
    // ... các skill cũ ...
    FireBall,   // ← Thêm vào đây
}
```

### Bước 2 — Mở `EnemySkills.cs` và thêm class

```csharp
/// <summary>FireBall — Ném cầu lửa gây 10 DMG + 50% Burn.</summary>
public class ESkill_FireBall : EnemySkillAction
{
    public override void Execute(EnemyStats attacker)
    {
        attacker.currentCooldown = attacker.maxCooldown;

        // Gây sát thương cho người chơi
        DealToPlayer(10f);
        ShowPlayerDmgText(10f, new Color(1f, 0.3f, 0f)); // Màu cam

        // 50% cháy (Burn) trong 3 lượt
        if (UnityEngine.Random.Range(0f, 100f) <= 50f)
            Debuffs?.AddDebuff(Player, DebuffType.Burn, 3);
    }
}
```

### Bước 3 — Đăng ký trong `SkillExecutor.cs`

```csharp
RegisterEnemy(EnemySkillID.FireBall, new ESkill_FireBall());
```

### Bước 4 — Gán skillID cho quái trong Unity

Chọn prefab kẻ thù → Inspector → `EnemyStats` → field **Skill ID** → chọn `FireBall`.

---

## 🔁 Skill Bất Đồng Bộ (Coroutine / Nhiều Frame)

Dùng khi skill cần **delay giữa các đòn** (như Phun Lửa, Multi-Hit).

```csharp
public class SwordSkill_Combo : SkillAction
{
    // ← BẮT BUỘC khai báo true nếu dùng coroutine
    public override bool IsAsync => true;

    public override void Execute(EnemyStats target, float flatDmg, float effectChance)
    {
        // Chạy coroutine qua SkillExecutor (vì SkillAction không phải MonoBehaviour)
        SkillExecutor.Instance.RunCoroutine(ComboRoutine(target, flatDmg));
    }

    private System.Collections.IEnumerator ComboRoutine(EnemyStats target, float flatDmg)
    {
        DealToEnemy(target, 1f, flatDmg);
        yield return new UnityEngine.WaitForSeconds(0.2f);

        DealToEnemy(target, 0.8f, flatDmg);
        yield return new UnityEngine.WaitForSeconds(0.2f);

        // ← BẮT BUỘC gọi FinishTurn() ở cuối coroutine
        FinishTurn();
    }
}
```

> ⚠️ **Quan trọng:** Nếu `IsAsync = true` mà quên gọi `FinishTurn()`, lượt của người chơi sẽ **bị treo vĩnh viễn**.

---

## 🧰 Danh Sách Helper Có Sẵn

### Trong `SkillAction` (Skill người chơi)

| Helper | Mô tả |
|---|---|
| `DealToEnemy(target, mult, flatDmg)` | Gây sát thương cho kẻ thù, tự tính Crit/Miss, hiện FloatingText |
| `FinishTurn()` | Kết thúc lượt người chơi (trừ AP, tick debuff, bỏ pause) |
| `CM` | Truy cập `CombatManager.Instance` |
| `UI` | Truy cập `CombatUIManager` |
| `Player` | Truy cập `PlayerManager.Instance` |
| `Debuffs` | Truy cập `DebuffManager.Instance` |
| `Buffs` | Truy cập `BuffManager.Instance` |
| `FTM` | Truy cập `FloatingTextManager.Instance` |

### Trong `EnemySkillAction` (Skill kẻ thù)

| Helper | Mô tả |
|---|---|
| `DealToPlayer(rawDmg)` | Gây sát thương cho người chơi |
| `ShowPlayerDmgText(amount, color)` | Hiện FloatingText tại HP bar người chơi |
| `CM`, `UI`, `Player`, `Debuffs`, `Buffs`, `FTM` | Tương tự như trên |

---

## 📋 Checklist Thêm Skill Mới

### Skill Người Chơi
- [ ] Thêm class kế thừa `SkillAction` trong `PlayerSkills.cs`
- [ ] Override `Execute(target, flatDmg, effectChance)`
- [ ] Gọi `FinishTurn()` ở cuối (hoặc cuối coroutine nếu async)
- [ ] Khai báo `IsAsync => true` nếu dùng coroutine
- [ ] Đăng ký trong `SkillExecutor.RegisterAllSkills()`
- [ ] Tạo/cập nhật `SkillData` ScriptableObject với đúng `skillID`

### Skill Kẻ Thù
- [ ] Thêm giá trị mới vào enum `EnemySkillID`
- [ ] Thêm class kế thừa `EnemySkillAction` trong `EnemySkills.cs`
- [ ] Override `Execute(attacker)`
- [ ] Set `attacker.currentCooldown = attacker.maxCooldown` bên trong Execute
- [ ] Khai báo `IsAsync => true` nếu dùng coroutine
- [ ] Đăng ký trong `SkillExecutor.RegisterAllSkills()`
- [ ] Gán `skillID` đúng trên prefab kẻ thù trong Unity Inspector

---

## 🐛 Troubleshooting

**Lỗi: "[SkillExecutor] Player skill không tìm thấy: Sword_Skill_X"**
→ Kiểm tra `skillID` trong `SkillData` có khớp chính xác với chuỗi bạn dùng trong `Register("Sword_Skill_X", ...)`.

**Lỗi: Lượt người chơi bị treo sau khi dùng skill**
→ Đảm bảo skill async có `IsAsync => true` VÀ gọi `FinishTurn()` ở cuối coroutine.

**Lỗi: Skill kẻ thù không kích hoạt**
→ Kiểm tra `EnemyStats.skillID` trên prefab quái có đúng giá trị enum không.
→ Kiểm tra `SkillExecutor.RegisterAllSkills()` đã có `RegisterEnemy(EnemySkillID.TenSkill, ...)`.

**Lỗi: NullReferenceException trong Execute()**
→ Dùng `?.` khi gọi `Debuffs?.AddDebuff(...)` thay vì `Debuffs.AddDebuff(...)` để tránh crash khi manager chưa khởi tạo.
