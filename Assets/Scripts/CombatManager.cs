using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatManager : MonoBehaviour
{
    [Header("Dữ liệu Kẻ thù")]
    public GameObject[] normalEnemyPrefabs;
    public GameObject[] bossPrefabs;
    private EnemyStats currentEnemyStats;

    [Header("Hệ thống ATB")]
    private float playerAP = 0f;
    private float enemyAP = 0f;
    private float maxAP = 100f;
    private bool isCombatPaused = false;
    public float tickSpeedMultiplier = 5f;

    [Header("UI Người chơi")]
    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI playerAPText;
    public TextMeshProUGUI playerSanityText; // Thêm Text Sanity
    public Slider playerHPBar;
    public Slider playerAPBar;
    public Slider playerSanityBar; // Thêm Slider Sanity


    [Header("UI Kẻ thù")]
    public Image enemyIcon;
    public Slider enemyHPBar;
    public Slider enemyAPBar;

    [Header("UI Điều khiển")]
    public GameObject actionMenu;

    [Header("UI Bảng thông tin Kẻ thù")]
    public GameObject enemyInfoPanel;
    public TextMeshProUGUI infoStatsText;
    public TextMeshProUGUI infoSkillText;
    void Start()
    {
        SpawnRandomEnemy();
        UpdatePlayerUI();
        actionMenu.SetActive(false);
    }

    void Update()
    {
        if (isCombatPaused || currentEnemyStats == null || PlayerManager.Instance == null) return;

        if (currentEnemyStats.currentSpeed == 0)
        {
            currentEnemyStats.currentSpeed = currentEnemyStats.baseSpeed;
            currentEnemyStats.currentDefense = currentEnemyStats.baseDefense;
            currentEnemyStats.currentHP = currentEnemyStats.maxHP;
        }
        // Cập nhật tốc độ dựa trên CurrentSpeed thay vì GetTotalSpeed
        playerAP += PlayerManager.Instance.currentSpeed * Time.deltaTime * tickSpeedMultiplier;
        enemyAP += currentEnemyStats.currentSpeed * Time.deltaTime * tickSpeedMultiplier;

        // Đồng bộ dữ liệu AP cho DebuffManager
        PlayerManager.Instance.currentAP = playerAP;
        currentEnemyStats.currentAP = enemyAP;

        UpdateAPUI();

        if (playerAP >= maxAP || enemyAP >= maxAP)
        {
            isCombatPaused = true;
            Unit activeUnit = playerAP >= enemyAP ? (Unit)PlayerManager.Instance : (Unit)currentEnemyStats;

            // 1. Kiểm tra Choáng
            if (DebuffManager.Instance.HasDebuff(activeUnit, DebuffType.Stun))
            {
                if (activeUnit == PlayerManager.Instance) playerAP -= maxAP;
                else enemyAP -= maxAP;

                activeUnit.currentAP -= maxAP;
                DebuffManager.Instance.ProcessTurnEnd(activeUnit);
                isCombatPaused = false;
                return;
            }

            // 2. Xử lý sát thương DoT (Độc/Bỏng/Chảy máu)
            DebuffManager.Instance.ProcessTurnStart(activeUnit);
            UpdatePlayerUI();
            UpdateEnemyUI();

            // 3. Kiểm tra tử vong do DoT
            if (PlayerManager.Instance.currentHP <= 0)
            {
                CheckPlayerDeath();
                return;
            }
            if (currentEnemyStats.currentHP <= 0)
            {
                Debug.Log(currentEnemyStats.enemyName + " gục ngã vì hiệu ứng trạng thái!");
                return;
            }

            // 4. Vào lượt hành động
            if (activeUnit == PlayerManager.Instance) PlayerTurn();
            else EnemyTurn();
        }
    }

    void SpawnRandomEnemy()
    {
        if (normalEnemyPrefabs.Length == 0) return;

        int randomIndex = Random.Range(0, normalEnemyPrefabs.Length);
        GameObject enemyObject = Instantiate(normalEnemyPrefabs[randomIndex]);
        SetupEnemy(enemyObject);
    }

    public void SpawnBoss(int bossIndex)
    {
        if (bossIndex < 0 || bossIndex >= bossPrefabs.Length) return;

        GameObject enemyObject = Instantiate(bossPrefabs[bossIndex]);
        SetupEnemy(enemyObject);
    }

    void SetupEnemy(GameObject enemyObject)
    {
        currentEnemyStats = enemyObject.GetComponent<EnemyStats>();

        if (currentEnemyStats.enemySprite != null)
        {
            enemyIcon.sprite = currentEnemyStats.enemySprite;
        }

        enemyObject.SetActive(false);

        enemyHPBar.maxValue = currentEnemyStats.maxHP;
        enemyHPBar.value = currentEnemyStats.currentHP;
        enemyAPBar.maxValue = maxAP;
        enemyAPBar.value = 0;
    }

    void UpdatePlayerUI()
    {
        if (PlayerManager.Instance == null) return;

        playerHPBar.maxValue = PlayerManager.Instance.maxHP;
        playerHPBar.value = PlayerManager.Instance.currentHP;
        playerHPText.text = PlayerManager.Instance.currentHP + "/" + PlayerManager.Instance.maxHP;

        // Cập nhật UI Sanity
        if (playerSanityBar != null)
        {
            playerSanityBar.maxValue = PlayerManager.Instance.maxSen;
            playerSanityBar.value = PlayerManager.Instance.sen;
        }

        if (playerSanityText != null)
        {
            playerSanityText.text = PlayerManager.Instance.sen.ToString();
        }

    }

    void UpdateAPUI()
    {
        playerAPBar.value = playerAP;
        playerAPText.text = Mathf.FloorToInt(playerAP) + "%";
        enemyAPBar.value = enemyAP;
    }

    void PlayerTurn()
    {
        playerAP = maxAP;
        UpdateAPUI();
        actionMenu.SetActive(true);
        PlayerManager.Instance.isDefending = false; 
        actionMenu.SetActive(true);
    }

    // --- CÂY QUYẾT ĐỊNH AI CỦA KẺ THÙ ---
    void EnemyTurn()
    {
        enemyAP = maxAP;
        UpdateAPUI();

        // 1. Giảm thời gian hồi chiêu
        if (currentEnemyStats.currentCooldown > 0)
            currentEnemyStats.currentCooldown--;

        bool usedSkill = false;

        // 2. Kiểm tra điều kiện dùng Kỹ năng (AI)
        if (currentEnemyStats.maxCooldown > 0 && currentEnemyStats.currentCooldown == 0)
        {
            // Tỉ lệ 70% thực thi kỹ năng
            if (Random.Range(0f, 100f) <= 70f)
            {
                ExecuteEnemySkill();
                usedSkill = true;
            }
        }

        // 3. Nếu không dùng kỹ năng thì Đánh thường
        if (!usedSkill)
        {
            ExecuteEnemyNormalAttack();
        }

        UpdatePlayerUI();
        CheckPlayerDeath();

        enemyAP -= maxAP;
        isCombatPaused = false;
    }
    public void ToggleEnemyInfo()
    {
        if (currentEnemyStats == null) return;

        // Bật/tắt trạng thái hiển thị
        enemyInfoPanel.SetActive(!enemyInfoPanel.activeSelf);

        if (enemyInfoPanel.activeSelf)
        {
            infoStatsText.text = $"HP: {currentEnemyStats.maxHP}\nATK: {currentEnemyStats.attack}\nDEF: {currentEnemyStats.currentDefense}\nSPD: {currentEnemyStats.currentSpeed}\nCRIT: {currentEnemyStats.critChance}%\nEVA: {currentEnemyStats.evasion}%";

            if (currentEnemyStats.maxCooldown > 0)
                infoSkillText.text = $"<color=yellow>Kỹ năng: {currentEnemyStats.skillName}</color>\n{currentEnemyStats.skillDescription}\n(Hồi chiêu: {currentEnemyStats.maxCooldown} lượt)";
            else
                infoSkillText.text = "Kẻ thù này không có kỹ năng đặc biệt.";
        }
    }



    // --- XỬ LÝ ĐÁNH THƯỜNG ---
    void ExecuteEnemyNormalAttack()
    {
        if (CheckEvasion(PlayerManager.Instance))
        {
            Debug.Log("Bạn đã né được đòn đánh của " + currentEnemyStats.enemyName + "!");
            return;
        }
        //Công thức tính sát thương
        bool isCrit = UnityEngine.Random.Range(0f, 100f) <= currentEnemyStats.critChance;
        float rawDamage = currentEnemyStats.attack;
        if (isCrit) rawDamage *= 1.5f;

        int finalDamage = Mathf.FloorToInt(Mathf.Max(1, rawDamage - PlayerManager.Instance.currentDefense));
        PlayerManager.Instance.currentHP -= finalDamage;
        if (PlayerManager.Instance.currentHP < 0) PlayerManager.Instance.currentHP = 0;

        Color dmgColor = isCrit ? Color.yellow : Color.white;
        FloatingTextManager.Instance.SpawnText(playerHPBar.transform.position, finalDamage.ToString(), dmgColor);

        Debug.Log($"{currentEnemyStats.enemyName} đánh thường! Sát thương: {finalDamage}" + (isCrit ? " (Chí mạng!)" : ""));

        if (isCrit && UnityEngine.Random.Range(0f, 100f) <= 12f)
        {
            PlayerManager.Instance.ReduceSanity(1);
            Debug.Log("Bị đòn chí mạng! Trừ 1 Sanity.");
        }
    }

    // --- XỬ LÝ TỪNG KỸ NĂNG RIÊNG BIỆT ---
    void ExecuteEnemySkill()
    {
        currentEnemyStats.currentCooldown = currentEnemyStats.maxCooldown;
        Debug.Log($"<color=red>{currentEnemyStats.enemyName} dùng kỹ năng: {currentEnemyStats.skillName}!</color>");

        switch (currentEnemyStats.enemyName)
        {
            case "Đỉa":
                // Hút Máu: 1 ATK dmg, 30% Bleed, Hồi 5 HP
                float diaRawDamage = currentEnemyStats.attack;
                PlayerManager.Instance.TakeDamage(diaRawDamage, false, false);

                if (UnityEngine.Random.Range(0f, 100f) <= 30f)
                {
                    DebuffManager.Instance.AddDebuff(PlayerManager.Instance, DebuffType.Bleed, 1, 1, diaRawDamage);
                    Debug.Log("Đỉa gây Chảy máu!");
                }

                currentEnemyStats.currentHP = Mathf.Min(currentEnemyStats.maxHP, currentEnemyStats.currentHP + 5);
                UpdateEnemyUI();
                break;

            case "Thằn lằn đen":
                // Cắn Độc: 1 ATK dmg, 30% Poison
                float tldDmg = currentEnemyStats.attack;
                PlayerManager.Instance.TakeDamage(tldDmg, false, false);

                if (UnityEngine.Random.Range(0f, 100f) <= 30f)
                {
                    DebuffManager.Instance.AddDebuff(PlayerManager.Instance, DebuffType.Poison, 3);
                    Debug.Log("Thằn lằn đen tiêm Độc!");
                }
                break;

            case "Nhện":
                // Tơ Dính: Buff +2 ATK, 30% Fracture
                // (Tạm thời cộng thẳng ATK, sau này có hệ thống Buff sẽ đổi thành tồn tại 2 lượt)
                currentEnemyStats.attack += 2;

                if (UnityEngine.Random.Range(0f, 100f) <= 30f)
                {
                    DebuffManager.Instance.AddDebuff(PlayerManager.Instance, DebuffType.Fracture, 3);
                    Debug.Log("Nhện phóng tơ gây Đập vỡ xương!");
                }
                break;
            case "Nhện Hang":
                // Cắn: Tấn công +3 ATK (Chỉ 1 lượt)
                float nhenHangDmg = currentEnemyStats.attack + 3f;
                PlayerManager.Instance.TakeDamage(nhenHangDmg, false, false);
                Debug.Log($"Nhện Hang dùng kỹ năng Cắn hiểm! Gây {nhenHangDmg} sát thương.");
                break;

            case "Chuột nhảy":
                // Tăng Tốc: +5 SPD (Tạm thời cộng thẳng, sau này có hệ thống Buff sẽ giới hạn 2 lượt)
                currentEnemyStats.currentSpeed += 5f;
                Debug.Log("Chuột nhảy dùng Tăng Tốc! SPD tăng thêm 5.");
                break;

            case "Nhện tinh anh":
                // Gọi Đàn: Triệu hồi 1 Nhện hang
                // (Cơ chế này đòi hỏi UI hiển thị nhiều quái vật, tạm thời in ra Log)
                Debug.Log("Nhện tinh anh rít lên Gọi Đàn! (Cần cập nhật hệ thống Multi-Enemy để triệu hồi Nhện Hang).");
                break;
            case "Rồng cổ đại":
                // Kiểm tra nội tại Cuồng Nộ
                bool isEnraged = currentEnemyStats.currentHP < 100;

                // Random 50/50 chọn 1 trong 2 kỹ năng
                if (UnityEngine.Random.Range(0f, 100f) <= 50f)
                {
                    // Skill 1: Vuốt Rồng (23 base dmg, 28 nếu cuồng nộ)
                    float clawBaseDmg = isEnraged ? 28f : 23f;
                    PlayerManager.Instance.TakeDamage(clawBaseDmg, false, false);

                    if (UnityEngine.Random.Range(0f, 100f) <= 30f)
                    {
                        DebuffManager.Instance.AddDebuff(PlayerManager.Instance, DebuffType.Fracture, 3);
                    }
                    Debug.Log("Rồng quẹt Vuốt Rồng!");
                }
                else
                {
                    // Skill 2: Hơi Thở Lửa (Đánh 3 nhịp, mỗi nhịp 8 True Dmg, 40% Bỏng)
                    Debug.Log("Rồng khạc Hơi Thở Lửa liên tục 3 lần!");
                    for (int i = 0; i < 3; i++)
                    {
                        PlayerManager.Instance.TakeDamage(8f, true, false); // True Damage
                        if (UnityEngine.Random.Range(0f, 100f) <= 40f)
                        {
                            DebuffManager.Instance.AddDebuff(PlayerManager.Instance, DebuffType.Burn, 3);
                        }
                    }
                }
                break;

            default:
                ExecuteEnemyNormalAttack();
                break;
        }
    }
    void CheckPlayerDeath()
    {
        if (PlayerManager.Instance.currentHP <= 0)
        {
            Debug.Log("Người chơi đã tử vong. Game Over!");
            isCombatPaused = true;
        }
    }
    public void OnAttackButton()
    {
        actionMenu.SetActive(false);

        if (CheckEvasion(currentEnemyStats))
        {
            Debug.Log(currentEnemyStats.enemyName + " đã né được đòn tấn công của bạn!");
        }
        else
        {
            bool isCrit = UnityEngine.Random.Range(0f, 100f) <= PlayerManager.Instance.GetTotalCrit();
            float rawDamage = PlayerManager.Instance.GetTotalAttack();
            if (isCrit) rawDamage *= 1.5f;

            int finalDamage = Mathf.FloorToInt(Mathf.Max(1, rawDamage - currentEnemyStats.currentDefense));
            currentEnemyStats.currentHP -= finalDamage;
            if (currentEnemyStats.currentHP < 0) currentEnemyStats.currentHP = 0;

            Color dmgColor = isCrit ? Color.yellow : Color.white;
            FloatingTextManager.Instance.SpawnText(enemyIcon.transform.position, finalDamage.ToString(), dmgColor);
        }

        UpdateEnemyUI();

        if (currentEnemyStats.currentHP <= 0)
        {
            Debug.Log(currentEnemyStats.enemyName + " đã bị tiêu diệt!");
            isCombatPaused = true;
            return;
        }

        playerAP -= maxAP;
        isCombatPaused = false;
    }

    public void OnDefendButton()
    {
        actionMenu.SetActive(false);
        PlayerManager.Instance.isDefending = true; // Bật cờ phòng thủ
        Debug.Log("Người chơi Phòng thủ! Tăng DEF và +25% Né tránh trong lượt này.");

        playerAP -= maxAP;
        isCombatPaused = false;
    }
    void UpdateEnemyUI()
    {
        if (enemyHPBar != null)
        {
            enemyHPBar.value = currentEnemyStats.currentHP;
        }
    }

    // --- CƠ CHẾ NÉ TRÁNH ---
    private bool CheckEvasion(Unit target)
    {
        float evasionChance = 0f;

        // Lấy chỉ số EVA tương ứng của mục tiêu
        if (target == PlayerManager.Instance)
            evasionChance = PlayerManager.Instance.GetTotalEvasion();
        else if (target == currentEnemyStats)
            evasionChance = currentEnemyStats.evasion;

        // Đổ xúc xắc 100 mặt
        if (UnityEngine.Random.Range(0f, 100f) <= evasionChance)
        {
            // Nếu số đổ ra <= Tỉ lệ né -> Né thành công!
            if (FloatingTextManager.Instance != null)
            {
                FloatingTextManager.Instance.SpawnText(target.transform.position, "Miss!", Color.cyan);
            }
            return true;
        }

        return false; // Đánh trúng
    }
}