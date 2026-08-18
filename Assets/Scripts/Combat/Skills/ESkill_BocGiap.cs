using UnityEngine;

/// <summary>
/// Skill Tê Tê — Bọc Giáp (Armored Shell):
/// Kích hoạt buff Bọc Giáp trong 3 lượt (giảm 50% sát thương trực tiếp + phản sát thương lại Player).
/// Tê Tê hiển thị Icon Buff Phòng Thụ, không tấn công người chơi khi đang Bọc Giáp.
/// Mỗi đòn đánh của Player hoặc lượt AP sẽ giảm 1 lượt của Bọc Giáp.
/// </summary>
public class ESkill_BocGiap : EnemySkillAction
{
    public override void Execute(EnemyStats attacker)
    {
        if (attacker == null || attacker.currentHP <= 0) return;

        attacker.currentCooldown = attacker.maxCooldown;

        // Thêm Buff BocGiap trong 3 lượt (Max 1 stack, hiển thị Icon phòng thủ)
        Buffs?.AddBuff(attacker, BuffType.BocGiap, 0.5f, 3, 1);
    }
}
