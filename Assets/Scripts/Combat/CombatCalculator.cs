using UnityEngine;

/// <summary>
/// [Phase 1 Refactor] Class tĩnh chứa toàn bộ công thức toán học chiến đấu.
/// Không kế thừa MonoBehaviour — chỉ là tập hợp các hàm tính toán thuần túy.
/// CombatManager sẽ gọi các hàm này thay vì tự tính nội bộ.
/// </summary>
public static class CombatCalculator
{
    // ─────────────────────────────────────────────────────────────────────────
    //  KẾT QUẢ TẤN CÔNG — trả về struct để caller xử lý hiệu ứng sau đó
    // ─────────────────────────────────────────────────────────────────────────

    public struct AttackResult
    {
        public bool missed;       // true nếu né tránh thành công
        public bool isCrit;       // true nếu đòn chí mạng
        public int  finalDamage;  // sát thương thực sau khi trừ DEF
        public float rawDamage;   // sát thương thô (trước trừ DEF, sau nhân crit)
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  EVASION CHECK
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Kiểm tra xem Unit mục tiêu có né tránh đòn tấn công không.
    /// </summary>
    /// <param name="target">Unit bị tấn công (PlayerManager hoặc EnemyStats).</param>
    /// <returns>true nếu né tránh thành công.</returns>
    public static bool CheckEvasion(Unit target)
    {
        float evasionChance = 0f;

        if (target is PlayerManager pm)
            evasionChance = pm.GetTotalEvasion();
        else if (target is EnemyStats es)
            evasionChance = es.evasion;

        return UnityEngine.Random.Range(0f, 100f) <= evasionChance;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PLAYER → ENEMY DAMAGE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tính toán một đòn đánh từ Player lên EnemyStats.
    /// Bao gồm: bonus vs Human/NonHuman từ trang bị, nhân crit, trừ DEF.
    /// </summary>
    /// <param name="attacker">PlayerManager (kẻ tấn công).</param>
    /// <param name="target">EnemyStats (kẻ bị tấn công).</param>
    /// <param name="damageMultiplier">Hệ số nhân lên ATK cơ bản (ví dụ: 0.85f cho hit 2 của combo).</param>
    /// <param name="flatBonus">Cộng thẳng vào sát thương thô từ cấp độ skill.</param>
    /// <param name="extraCritChance">Crit thêm từ skill (tính cộng dồn).</param>
    /// <returns>AttackResult chứa kết quả tính toán.</returns>
    public static AttackResult CalculatePlayerDamage(
        PlayerManager attacker,
        EnemyStats    target,
        float         damageMultiplier = 1f,
        float         flatBonus        = 0f,
        float         extraCritChance  = 0f)
    {
        var result = new AttackResult();

        // 1. Evasion check
        if (CheckEvasion(target))
        {
            result.missed = true;
            return result;
        }

        // 2. Crit check
        float totalCritChance = attacker.GetTotalCrit() + extraCritChance;
        result.isCrit = UnityEngine.Random.Range(0f, 100f) <= totalCritChance;

        // 3. Tính sát thương thô
        float rawDamage = (attacker.GetTotalAttack() * damageMultiplier) + flatBonus;

        // 4. Bonus theo loại kẻ thù (Human / Non-human)
        if (target.enemyType == EnemyType.Human)
            rawDamage += attacker.equipmentBonusDamageVsHuman;
        else
            rawDamage += attacker.equipmentBonusDamageVsNonHuman;

        // 5. Nhân crit
        if (result.isCrit) rawDamage *= 1.5f;
        result.rawDamage = rawDamage;

        // 6. Trừ DEF và làm tròn
        result.finalDamage = Mathf.FloorToInt(Mathf.Max(1f, rawDamage - target.currentDefense));

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PLAYER NORMAL ATTACK (wrapper đơn giản hơn cho đòn thường)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tính một đòn tấn công thường của Player (damageMultiplier = 1, flatBonus = 0).
    /// Tiện gọi hơn CalculatePlayerDamage trong trường hợp không dùng Skill.
    /// </summary>
    public static AttackResult CalculateNormalAttack(PlayerManager attacker, EnemyStats target)
        => CalculatePlayerDamage(attacker, target, 1f, 0f, 0f);

    // ─────────────────────────────────────────────────────────────────────────
    //  ENEMY NORMAL ATTACK → PLAYER
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tính đòn tấn công thường của Enemy lên Player.
    /// Trả về sát thương hiển thị (sau DEF + Defending bonus).
    /// </summary>
    public struct EnemyAttackResult
    {
        public bool missed;
        public bool isCrit;
        public int  displayDamage; // Số hiện trên màn hình (sau DEF)
        public float rawDamage;   // Sát thương thô (trước DEF)
    }

    public static EnemyAttackResult CalculateEnemyAttack(EnemyStats attacker, PlayerManager target)
    {
        var result = new EnemyAttackResult();

        if (CheckEvasion(target))
        {
            result.missed = true;
            return result;
        }

        result.isCrit  = UnityEngine.Random.Range(0f, 100f) <= attacker.critChance;
        result.rawDamage = attacker.GetTotalAttack();
        if (result.isCrit) result.rawDamage *= 1.5f;

        // Áp dụng extraDamageTakenPercent từ trang bị (ví dụ Skull Ring)
        if (target != null && target.equipmentExtraDamageTakenPercent > 0f)
            result.rawDamage *= (1f + target.equipmentExtraDamageTakenPercent / 100f);

        bool hasDefBuff = BuffManager.Instance != null && BuffManager.Instance.HasBuff(target, BuffType.DEF_Up);
        if (hasDefBuff || target.isDefending)
            result.rawDamage *= 0.5f; // 50% giảm sát thương nhận vào khi Phòng Thủ

        float defToUse = target.currentDefense + target.equipmentDefenseBonus;
        if (hasDefBuff || target.isDefending)
            defToUse += 1f; // + 1 DEF

        result.displayDamage = Mathf.FloorToInt(Mathf.Max(1f, result.rawDamage - defToUse));
        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  BOSS SKILL: NhenNuVuong ClawRip
    // ─────────────────────────────────────────────────────────────────────────

    public struct BossClawResult
    {
        public bool missed;
        public bool isCrit;
        public int  displayDamage;
        public float rawDamage;
    }

    /// <summary>
    /// Công thức đặc biệt của Cào Xé Thịt: damage = % maxHP thay vì ATK thường.
    /// Crit: 32% maxHP / Thường: 20% maxHP. Bị ảnh hưởng bởi DEF và EVA.
    /// </summary>
    public static BossClawResult CalculateClawRip(PlayerManager target)
    {
        var result = new BossClawResult();

        if (CheckEvasion(target))
        {
            result.missed = true;
            return result;
        }

        result.isCrit    = UnityEngine.Random.Range(0f, 100f) <= 32f;
        result.rawDamage = result.isCrit
            ? target.maxHP * 0.32f   // Crit = 32% maxHP
            : target.maxHP * 0.20f;  // Bình thường = 20% maxHP

        // Áp dụng extraDamageTakenPercent từ trang bị (ví dụ Skull Ring)
        if (target != null && target.equipmentExtraDamageTakenPercent > 0f)
            result.rawDamage *= (1f + target.equipmentExtraDamageTakenPercent / 100f);

        bool hasDefBuff = BuffManager.Instance != null && BuffManager.Instance.HasBuff(target, BuffType.DEF_Up);
        if (hasDefBuff || target.isDefending)
            result.rawDamage *= 0.5f; // 50% giảm sát thương nhận vào khi Phòng Thủ

        float defToUse = target.currentDefense + target.equipmentDefenseBonus;
        if (hasDefBuff || target.isDefending)
            defToUse += 1f; // + 1 DEF

        result.displayDamage = Mathf.FloorToInt(Mathf.Max(1f, result.rawDamage - defToUse));
        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  FLEE CHANCE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tính xác suất chạy trốn: 80% − (SEN đã mất × 4%), tối thiểu 0%.
    /// </summary>
    public static float CalculateFleeChance(PlayerManager player)
    {
        int senLost = player.maxSen - player.sen;
        return Mathf.Max(0f, 80f - (senLost * 4f));
    }
}
