using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrainingUI : MonoBehaviour
{
    [Header("Tham chiếu Text Chỉ Số Chính")]
    public TextMeshProUGUI txtUnspentPoints;
    public TextMeshProUGUI txtSTR, txtDEX, txtVIT, txtAGL;

    [Header("Nút Cộng (+)")]
    public Button btnAddSTR;
    public Button btnAddDEX;
    public Button btnAddVIT;
    public Button btnAddAGL;

    [Header("Nút Trừ (-)")]
    public Button btnMinusSTR;
    public Button btnMinusDEX;
    public Button btnMinusVIT;
    public Button btnMinusAGL;

    [Header("Điều khiển chung")]
    public Button btnConfirm;
    public Button btnClose;

    [Header("Tham chiếu Text Chỉ Số Phụ (Bên dưới)")]
    public TextMeshProUGUI txtDamage;
    public TextMeshProUGUI txtMaxHP;
    public TextMeshProUGUI txtCrit;
    public TextMeshProUGUI txtEvasion; // THÊM BIẾN HIỂN THỊ NÉ TRÁNH
    public TextMeshProUGUI txtSpeed;

    private int tempSTR, tempDEX, tempVIT, tempAGL;
    private int tempUnspent;

    private void Start()
    {
        btnAddSTR.onClick.AddListener(() => ChangeTempStat("STR", 1));
        btnAddDEX.onClick.AddListener(() => ChangeTempStat("DEX", 1));
        btnAddVIT.onClick.AddListener(() => ChangeTempStat("VIT", 1));
        btnAddAGL.onClick.AddListener(() => ChangeTempStat("AGL", 1));

        btnMinusSTR.onClick.AddListener(() => ChangeTempStat("STR", -1));
        btnMinusDEX.onClick.AddListener(() => ChangeTempStat("DEX", -1));
        btnMinusVIT.onClick.AddListener(() => ChangeTempStat("VIT", -1));
        btnMinusAGL.onClick.AddListener(() => ChangeTempStat("AGL", -1));

        btnConfirm.onClick.AddListener(ConfirmUpgrades);
        btnClose.onClick.AddListener(CloseUI);
    }

    private void ChangeTempStat(string stat, int amount)
    {
        switch (stat)
        {
            case "STR":
                if (amount < 0 && tempSTR <= 0) return;
                tempSTR += amount; break;
            case "DEX":
                if (amount < 0 && tempDEX <= 0) return;
                tempDEX += amount; break;
            case "VIT":
                if (amount < 0 && tempVIT <= 0) return;
                tempVIT += amount; break;
            case "AGL":
                if (amount < 0 && tempAGL <= 0) return;
                tempAGL += amount; break;
        }
        tempUnspent -= amount;
        RefreshUI();
    }

    private void ConfirmUpgrades()
    {
        var p = PlayerManager.Instance;
        for (int i = 0; i < tempSTR; i++) p.UpgradeStat("STR");
        for (int i = 0; i < tempDEX; i++) p.UpgradeStat("DEX");
        for (int i = 0; i < tempVIT; i++) p.UpgradeStat("VIT");
        for (int i = 0; i < tempAGL; i++) p.UpgradeStat("AGL");

        ResetTempStats();
    }

    private void ResetTempStats()
    {
        tempSTR = tempDEX = tempVIT = tempAGL = 0;
        if (PlayerManager.Instance != null)
            tempUnspent = PlayerManager.Instance.unspentStatPoints;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (PlayerManager.Instance == null) return;
        var p = PlayerManager.Instance;

        // 1. CẬP NHẬT PHẦN CHỈ SỐ CHÍNH
        txtUnspentPoints.text = $"Điểm chưa cộng: {tempUnspent}";

        UpdateStatRow(txtSTR, "STR", p.str, tempSTR, btnAddSTR, btnMinusSTR);
        UpdateStatRow(txtDEX, "DEX", p.dex, tempDEX, btnAddDEX, btnMinusDEX);
        UpdateStatRow(txtVIT, "VIT", p.vit, tempVIT, btnAddVIT, btnMinusVIT);
        UpdateStatRow(txtAGL, "AGL", p.agl, tempAGL, btnAddAGL, btnMinusAGL);

        // 2. TÍNH TOÁN THEO ĐÚNG CÔNG THỨC TRONG PLAYER_MANAGER.CS

        // Sát thương (STR x 0.2)
        float currentDamage = p.GetTotalAttack();
        float bonusDamage = tempSTR * 0.2f;

        // Chí mạng (DEX x 0.15)
        float currentCrit = p.GetTotalCrit();
        float bonusCrit = tempDEX * 0.15f;

        // Né tránh (DEX x 0.1) - THÊM MỚI
        float currentEvasion = p.GetTotalEvasion();
        float bonusEvasion = tempDEX * 0.1f;

        // Tốc độ (AGL x 0.5)
        float currentSpeed = p.GetTotalSpeed();
        float bonusSpeed = tempAGL * 0.5f;

        // Máu tối đa (VIT x 1)
        int currentMaxHP = p.maxHP;
        int futureMaxHP = 5 + Mathf.FloorToInt((p.vit + tempVIT) * 1f) + Mathf.FloorToInt(p.equipmentHPBonus);
        int bonusHP = futureMaxHP - currentMaxHP;

        // 3. HIỂN THỊ RA UI
        UpdateDerivedStatText(txtDamage, "Sát thương", currentDamage, bonusDamage, "F2");
        UpdateDerivedStatText(txtMaxHP, "Máu tối đa", currentMaxHP, bonusHP, "F0");
        UpdateDerivedStatText(txtCrit, "Chí mạng", currentCrit, bonusCrit, "F2", "%");
        UpdateDerivedStatText(txtEvasion, "Né tránh", currentEvasion, bonusEvasion, "F1", "%"); // Cập nhật text Né tránh
        UpdateDerivedStatText(txtSpeed, "Tốc độ", currentSpeed, bonusSpeed, "F1"); // Dùng F1 vì tốc độ có số lẻ 0.5

        bool hasChanges = (tempSTR + tempDEX + tempVIT + tempAGL) > 0;
        btnConfirm.interactable = hasChanges;
    }

    private void UpdateStatRow(TextMeshProUGUI text, string statName, int baseVal, int tempVal, Button addBtn, Button minusBtn)
    {
        text.text = $"{statName}:<pos=265>{baseVal}" + (tempVal > 0 ? $" <color=green>(+{tempVal})</color>" : "");
        addBtn.interactable = tempUnspent > 0;
        minusBtn.interactable = tempVal > 0;
    }

    private void UpdateDerivedStatText(TextMeshProUGUI textObj, string statName, float baseVal, float bonusVal, string format, string suffix = "")
    {
        if (textObj == null) return;

        string baseStr = baseVal.ToString(format) + suffix;
        if (bonusVal > 0)
        {
            string bonusStr = bonusVal.ToString(format) + suffix;
            textObj.text = $"{statName}: {baseStr} <color=green>(+{bonusStr})</color>";
        }
        else
        {
            textObj.text = $"{statName}: {baseStr}";
        }
    }

    public void OpenUI()
    {
        gameObject.SetActive(true);
        ResetTempStats();
    }

    public void CloseUI()
    {
        ResetTempStats();
        gameObject.SetActive(false);
    }
}