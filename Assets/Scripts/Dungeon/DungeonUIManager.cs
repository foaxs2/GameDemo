using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Quản lý toàn bộ giao diện của Scene Dungeon (phong cách Grim Quest / Dark Fantasy):
/// - Header: Tên Dungeon + Tầng hiện tại.
/// - Left Panel: 4 Thanh Bar màu riêng biệt (HP: Đỏ, Food: Xanh lá, SEN: Xám bạc, EXP: Vàng), Chỉ số & Trang bị.
/// - Right Panel: Danh sách Nhiệm vụ Guild đang nhận & Thống kê tầng thời gian thực.
/// - Bottom Panel: Tổng số Vàng & Lưới các ô túi đồ Dungeon (DungeonSlots).
/// </summary>
public class DungeonUIManager : MonoBehaviour
{
    public static DungeonUIManager Instance { get; private set; }

    [Header("Top Header")]
    public TextMeshProUGUI txtFloorTitle;

    [Header("Left Panel - 4 Thanh Bar Màu (Image Filled)")]
    public Image hpBarFill;
    public TextMeshProUGUI txtHP;
    public Image foodBarFill;
    public TextMeshProUGUI txtFood;
    public Image senBarFill;
    public TextMeshProUGUI txtSEN;
    public Image expBarFill;
    public TextMeshProUGUI txtEXP;

    [Header("Left Panel - Thông Tin & Chỉ Số Anh Hùng")]
    public Image imgHeroAvatar;
    public TextMeshProUGUI txtHeroName;
    public TextMeshProUGUI txtHeroLevel;
    [Tooltip("Ô Text tổng hợp 6 chỉ số (tự động ghép màu và icon)")]
    public TextMeshProUGUI txtHeroStats;
    [Header("Tùy chọn Text Chỉ Số Riêng Biệt (Nếu muốn xếp lưới 2x3)")]
    public TextMeshProUGUI txtDamage;
    public TextMeshProUGUI txtDefense;
    public TextMeshProUGUI txtCrit;
    public TextMeshProUGUI txtEvasion;
    public TextMeshProUGUI txtSpeed;
    public TextMeshProUGUI txtArmorPen;

    [Header("Trang Bị (4 Ô) - Hình Ảnh Icon")]
    public Image imgWeapon;
    public Image imgArmor;
    public Image imgAccessory1;
    public Image imgAccessory2;

    [Header("Trang Bị (4 Ô) - Tùy Chọn Tên Trang Bị")]
    public TextMeshProUGUI txtWeaponName;
    public TextMeshProUGUI txtArmorName;
    public TextMeshProUGUI txtAccessory1Name;
    public TextMeshProUGUI txtAccessory2Name;

    [Header("Right Panel - Nhiệm Vụ Guild & Thống Kê Tầng")]
    public Transform questListContainer;
    public GameObject questCardPrefab;
    public TextMeshProUGUI txtExploredTiles;
    public TextMeshProUGUI txtFloorGold;
    public TextMeshProUGUI txtKilledMonsters;
    public TextMeshProUGUI txtFoundEvents;

    [Header("Bottom Panel - Túi Đồ Dungeon & Vàng")]
    public TextMeshProUGUI txtTotalGold;
    public Transform bottomSlotsContainer;
    public GameObject dungeonSlotPrefab;

    [Header("Item Detail Popup (Bảng Chi Tiết & Tương Tác Vật Phẩm)")]
    public GameObject panelItemDetail;
    public Image imgDetailIcon;
    public TextMeshProUGUI txtDetailName;
    public TextMeshProUGUI txtDetailDescription;
    public TextMeshProUGUI txtDetailEffect;
    public Button btnDetailUse;
    public Button btnDetailDiscard;
    public Button btnDetailCloseOverlay;

    private ItemData currentSelectedItem;

    private string atkPrefix = "Sát thương: ";
    private string defPrefix = "Phòng ngự: ";
    private string critPrefix = "Chí mạng: ";
    private string evaPrefix = "Né tránh: ";
    private string spdPrefix = "Tốc độ: ";
    private string penPrefix = "Xuyên giáp: ";

    private string exploredPrefix = "";
    private string floorGoldPrefix = "";
    private string killedMonstersPrefix = "";
    private string foundEventsPrefix = "";

    private Sprite defaultWeaponSprite, defaultArmorSprite, defaultAcc1Sprite, defaultAcc2Sprite;
    private Color defaultWeaponColor = Color.white, defaultArmorColor = Color.white, defaultAcc1Color = Color.white, defaultAcc2Color = Color.white;
    private bool hasCachedDefaults = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        CacheDefaultEquipSlots();
    }

    private void Start()
    {
        if (panelItemDetail != null)
            panelItemDetail.SetActive(false);

        CacheDefaultEquipSlots();
        ExtractPrefixes();
        UpdateAllDungeonHUD();
    }

    private void CacheDefaultEquipSlots()
    {
        if (hasCachedDefaults) return;
        hasCachedDefaults = true;

        if (imgWeapon != null) { defaultWeaponSprite = imgWeapon.sprite; defaultWeaponColor = imgWeapon.color; }
        if (imgArmor != null) { defaultArmorSprite = imgArmor.sprite; defaultArmorColor = imgArmor.color; }
        if (imgAccessory1 != null) { defaultAcc1Sprite = imgAccessory1.sprite; defaultAcc1Color = imgAccessory1.color; }
        if (imgAccessory2 != null) { defaultAcc2Sprite = imgAccessory2.sprite; defaultAcc2Color = imgAccessory2.color; }
    }

    private void ExtractPrefixes()
    {
        if (txtDamage != null && !string.IsNullOrEmpty(txtDamage.text))
        {
            if (txtDamage.text.Contains(":"))
                atkPrefix = txtDamage.text.Substring(0, txtDamage.text.IndexOf(':') + 1) + " ";
            else if (!char.IsDigit(txtDamage.text.Trim()[0]))
                atkPrefix = txtDamage.text.Trim() + " ";
            else
                atkPrefix = "";
        }

        if (txtDefense != null && !string.IsNullOrEmpty(txtDefense.text))
        {
            if (txtDefense.text.Contains(":"))
                defPrefix = txtDefense.text.Substring(0, txtDefense.text.IndexOf(':') + 1) + " ";
            else if (!char.IsDigit(txtDefense.text.Trim()[0]))
                defPrefix = txtDefense.text.Trim() + " ";
            else
                defPrefix = "";
        }

        if (txtCrit != null && !string.IsNullOrEmpty(txtCrit.text))
        {
            if (txtCrit.text.Contains(":"))
                critPrefix = txtCrit.text.Substring(0, txtCrit.text.IndexOf(':') + 1) + " ";
            else if (!char.IsDigit(txtCrit.text.Trim()[0]))
                critPrefix = txtCrit.text.Trim() + " ";
            else
                critPrefix = "";
        }

        if (txtEvasion != null && !string.IsNullOrEmpty(txtEvasion.text))
        {
            if (txtEvasion.text.Contains(":"))
                evaPrefix = txtEvasion.text.Substring(0, txtEvasion.text.IndexOf(':') + 1) + " ";
            else if (!char.IsDigit(txtEvasion.text.Trim()[0]))
                evaPrefix = txtEvasion.text.Trim() + " ";
            else
                evaPrefix = "";
        }

        if (txtSpeed != null && !string.IsNullOrEmpty(txtSpeed.text))
        {
            if (txtSpeed.text.Contains(":"))
                spdPrefix = txtSpeed.text.Substring(0, txtSpeed.text.IndexOf(':') + 1) + " ";
            else if (!char.IsDigit(txtSpeed.text.Trim()[0]))
                spdPrefix = txtSpeed.text.Trim() + " ";
            else
                spdPrefix = "";
        }

        if (txtArmorPen != null && !string.IsNullOrEmpty(txtArmorPen.text))
        {
            if (txtArmorPen.text.Contains(":"))
                penPrefix = txtArmorPen.text.Substring(0, txtArmorPen.text.IndexOf(':') + 1) + " ";
            else if (!char.IsDigit(txtArmorPen.text.Trim()[0]))
                penPrefix = txtArmorPen.text.Trim() + " ";
            else
                penPrefix = "";
        }

        if (txtExploredTiles != null && !string.IsNullOrEmpty(txtExploredTiles.text))
        {
            if (txtExploredTiles.text.Contains(":"))
                exploredPrefix = txtExploredTiles.text.Substring(0, txtExploredTiles.text.IndexOf(':') + 1) + " ";
            else if (!char.IsDigit(txtExploredTiles.text.Trim()[0]))
                exploredPrefix = txtExploredTiles.text.Trim() + " ";
            else
                exploredPrefix = "";
        }

        if (txtFloorGold != null && !string.IsNullOrEmpty(txtFloorGold.text))
        {
            if (txtFloorGold.text.Contains(":"))
                floorGoldPrefix = txtFloorGold.text.Substring(0, txtFloorGold.text.IndexOf(':') + 1) + " ";
            else if (!char.IsDigit(txtFloorGold.text.Trim()[0]))
                floorGoldPrefix = txtFloorGold.text.Trim() + " ";
            else
                floorGoldPrefix = "";
        }

        if (txtKilledMonsters != null && !string.IsNullOrEmpty(txtKilledMonsters.text))
        {
            if (txtKilledMonsters.text.Contains(":"))
                killedMonstersPrefix = txtKilledMonsters.text.Substring(0, txtKilledMonsters.text.IndexOf(':') + 1) + " ";
            else if (!char.IsDigit(txtKilledMonsters.text.Trim()[0]))
                killedMonstersPrefix = txtKilledMonsters.text.Trim() + " ";
            else
                killedMonstersPrefix = "";
        }

        if (txtFoundEvents != null && !string.IsNullOrEmpty(txtFoundEvents.text))
        {
            if (txtFoundEvents.text.Contains(":"))
                foundEventsPrefix = txtFoundEvents.text.Substring(0, txtFoundEvents.text.IndexOf(':') + 1) + " ";
            else if (!char.IsDigit(txtFoundEvents.text.Trim()[0]))
                foundEventsPrefix = txtFoundEvents.text.Trim() + " ";
            else
                foundEventsPrefix = "";
        }
    }

    /// <summary>Alias đồng bộ để tương thích với tất cả các script gọi đến.</summary>
    public void UpdateDungeonHUD() => UpdateAllDungeonHUD();

    /// <summary>Cập nhật toàn bộ các phân vùng trên giao diện Dungeon.</summary>
    public void UpdateAllDungeonHUD()
    {
        UpdateHeader();
        UpdateBars();
        UpdateHeroStats();
        UpdateQuestList();
        UpdateFloorStats();
        UpdateBottomInventory();
    }

    public void UpdateHeader()
    {
        if (txtFloorTitle != null)
        {
            string areaName = PlayerMovement.currentFloor <= 10 ? "Cống ngầm" : "Lăng mộ";
            txtFloorTitle.text = $"{areaName} - Tầng {PlayerMovement.currentFloor}";
        }
    }

    public void UpdateBars()
    {
        var pm = PlayerManager.Instance;
        if (pm == null) return;

        // 1. HP Bar (Máu Đỏ)
        if (hpBarFill != null) hpBarFill.fillAmount = pm.maxHP > 0 ? (float)pm.currentHP / pm.maxHP : 0f;
        if (txtHP != null) txtHP.text = $"{pm.currentHP} / {pm.maxHP}";

        // 2. Food Bar (Lương Thực Xanh Lá)
        if (foodBarFill != null) foodBarFill.fillAmount = pm.maxFood > 0 ? (float)pm.food / pm.maxFood : 0f;
        if (txtFood != null) txtFood.text = $"{pm.food} / {pm.maxFood}";

        // 3. SEN Bar (Tâm Trí Xám Bạc)
        if (senBarFill != null) senBarFill.fillAmount = pm.maxSen > 0 ? (float)pm.sen / pm.maxSen : 0f;
        if (txtSEN != null) txtSEN.text = $"{pm.sen} / {pm.maxSen}";

        // 4. EXP Bar (Kinh Nghiệm Vàng)
        if (expBarFill != null) expBarFill.fillAmount = pm.expToNextLevel > 0 ? (float)pm.currentExp / pm.expToNextLevel : 0f;
        if (txtEXP != null) txtEXP.text = $"{pm.currentExp} / {pm.expToNextLevel}";
    }

    public void UpdateHeroStats()
    {
        var pm = PlayerManager.Instance;
        var inv = InventoryManager.Instance;
        if (pm == null) return;

        if (txtHeroLevel != null) txtHeroLevel.text = $"Lever {pm.level}";
        if (txtHeroName != null) txtHeroName.text = txtHeroLevel != null ? pm.playerName : $"{pm.playerName} (Lv.{pm.level})";

        float totalDmg = pm.baseAttack + pm.equipmentDamageBonus;
        float totalDef = pm.baseDefense + pm.equipmentDefenseBonus;
        float totalCrit = pm.baseCrit + pm.equipmentCritBonus;
        float totalEva = pm.baseEvasion + pm.equipmentEvasionBonus;
        float totalSpd = pm.GetTotalSpeed();
        float totalPen = pm.equipmentArmorPenetration;

        // 1. Ô Text tổng hợp (nếu dùng 1 ô text duy nhất)
        if (txtHeroStats != null)
        {
            txtHeroStats.text = 
                $"⚔️ Sát thương: <color=#FFA07A>{totalDmg}</color>\n" +
                $"🛡️ Phòng ngự: <color=#87CEFA>{totalDef}</color>\n" +
                $"🎯 Chí mạng: <color=#FFD700>{totalCrit}%</color>\n" +
                $"💨 Né tránh: <color=#98FB98>{totalEva}%</color>\n" +
                $"⚡ Tốc độ: <color=#FFFFE0>{totalSpd:F1}</color>\n" +
                $"🏹 Xuyên giáp: <color=#E6E6FA>{totalPen}</color>";
        }

        // 2. Từng ô Text riêng biệt (khớp với txtATK, txtDEF, txtCRIT, txtEVA, txtSPEED, txtARMORPIEC trong Canvas)
        if (txtDamage != null) txtDamage.text = $"{atkPrefix}{totalDmg}";
        if (txtDefense != null) txtDefense.text = $"{defPrefix}{totalDef}";
        if (txtCrit != null) txtCrit.text = $"{critPrefix}{totalCrit}%";
        if (txtEvasion != null) txtEvasion.text = $"{evaPrefix}{totalEva}%";
        if (txtSpeed != null) txtSpeed.text = $"{spdPrefix}{totalSpd:F1}";
        if (txtArmorPen != null) txtArmorPen.text = $"{penPrefix}{totalPen}";

        if (inv != null)
        {
            CacheDefaultEquipSlots();
            UpdateEquipSlot(imgWeapon, txtWeaponName, inv.equippedWeapon, defaultWeaponSprite, defaultWeaponColor, "Vũ khí trống");
            UpdateEquipSlot(imgArmor, txtArmorName, inv.equippedArmor, defaultArmorSprite, defaultArmorColor, "Giáp trống");
            UpdateEquipSlot(imgAccessory1, txtAccessory1Name, inv.equippedAccessory1, defaultAcc1Sprite, defaultAcc1Color, "Trang sức 1");
            UpdateEquipSlot(imgAccessory2, txtAccessory2Name, inv.equippedAccessory2, defaultAcc2Sprite, defaultAcc2Color, "Trang sức 2");
        }
    }

    private void UpdateEquipSlot(Image img, TextMeshProUGUI txtName, ItemData item, Sprite defaultSprite, Color defaultColor, string defaultEmptyName = "Trống")
    {
        if (img != null)
        {
            if (item != null && item.icon != null)
            {
                img.sprite = item.icon;
                img.color = Color.white;
            }
            else
            {
                // Khôi phục lại đúng hình ảnh và màu sắc của Slot mặc định mà bạn đã gán trong Editor
                img.sprite = defaultSprite;
                img.color = defaultColor;
            }
        }

        if (txtName != null)
        {
            if (item != null && !string.IsNullOrEmpty(item.itemName))
            {
                txtName.text = item.itemName;
                txtName.color = Color.white;
            }
            else
            {
                txtName.text = defaultEmptyName;
                txtName.color = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            }
        }
    }

    public void UpdateFloorStats()
    {
        int total = DungeonGenerator.Instance != null ? DungeonGenerator.Instance.TotalTiles : 49;
        int cleared = PlayerMovement.clearedFogTiles != null ? PlayerMovement.clearedFogTiles.Count : 0;
        int gold = PlayerMovement.floorGoldEarned;
        int monsters = PlayerMovement.floorMonstersKilled;
        int events = PlayerMovement.activatedEventTiles != null ? PlayerMovement.activatedEventTiles.Count : 0;

        if (txtExploredTiles != null) txtExploredTiles.text = $"{exploredPrefix}{cleared}/{total}";
        if (txtFloorGold != null) txtFloorGold.text = $"{floorGoldPrefix}{gold}";
        if (txtKilledMonsters != null) txtKilledMonsters.text = $"{killedMonstersPrefix}{monsters}";
        if (txtFoundEvents != null) txtFoundEvents.text = $"{foundEventsPrefix}{events}";
    }

    public void UpdateQuestList()
    {
        if (questListContainer == null || questCardPrefab == null || GuildManager.Instance == null) return;

        foreach (Transform child in questListContainer) Destroy(child.gameObject);

        var activeQuests = GuildManager.Instance.GetActiveQuests();
        if (activeQuests != null)
        {
            int displayedCount = 0;
            foreach (var quest in activeQuests)
            {
                // Chỉ hiển thị nhiệm vụ người chơi ĐÃ NHẬN (isAccepted == true) và tối đa 3 nhiệm vụ
                if (quest == null || !quest.isAccepted) continue;
                if (displayedCount >= 3) break;

                GameObject card = Instantiate(questCardPrefab, questListContainer);
                QuestSlotUI slot = card.GetComponent<QuestSlotUI>();
                if (slot != null) slot.Setup(quest, null);
                displayedCount++;
            }
        }
    }

    public void UpdateBottomInventory()
    {
        var pm = PlayerManager.Instance;
        var inv = InventoryManager.Instance;
        if (pm != null && txtTotalGold != null) txtTotalGold.text = pm.gold.ToString();

        if (bottomSlotsContainer == null || inv == null) return;

        // 1. Chỉ lọc ra các vật phẩm thuộc dạng Hồi Phục (HP, SEN, Food)
        List<InventorySlot> recoveryItems = new List<InventorySlot>();

        // Quét từ túi đồ chiến đấu (combatInventory)
        if (inv.combatInventory != null)
        {
            foreach (var slot in inv.combatInventory)
            {
                if (slot != null && slot.item != null && slot.item.itemType == ItemType.Consumable)
                {
                    if (slot.item.consumableType == ConsumableType.HP || 
                        slot.item.consumableType == ConsumableType.SEN || 
                        slot.item.consumableType == ConsumableType.Food)
                    {
                        recoveryItems.Add(slot);
                    }
                }
            }
        }

        // Nếu combatInventory chưa có, quét thêm từ storageInventory
        if (recoveryItems.Count == 0 && inv.storageInventory != null)
        {
            foreach (var slot in inv.storageInventory)
            {
                if (slot != null && slot.item != null && slot.item.itemType == ItemType.Consumable)
                {
                    if (slot.item.consumableType == ConsumableType.HP || 
                        slot.item.consumableType == ConsumableType.SEN || 
                        slot.item.consumableType == ConsumableType.Food)
                    {
                        recoveryItems.Add(slot);
                    }
                }
            }
        }

        // 2. Lấp đầy tối đa 8 ô (ưu tiên sử dụng 8 ô có sẵn bạn đã tạo trong Editor)
        int preCreatedCount = bottomSlotsContainer.childCount;
        int maxSlots = preCreatedCount > 0 ? preCreatedCount : 8;

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObj = null;

            if (i < bottomSlotsContainer.childCount)
            {
                // Dùng trực tiếp các ô bạn đã xếp trong Editor
                slotObj = bottomSlotsContainer.GetChild(i).gameObject;
            }
            else if (dungeonSlotPrefab != null)
            {
                slotObj = Instantiate(dungeonSlotPrefab, bottomSlotsContainer);
            }

            if (slotObj == null) continue;

            // Tìm hoặc tự tạo Icon Image con để hiển thị ảnh item
            Transform iconTrans = slotObj.transform.Find("Icon");
            Image iconImg = iconTrans != null ? iconTrans.GetComponent<Image>() : null;
            if (iconImg == null)
            {
                Image[] imgs = slotObj.GetComponentsInChildren<Image>(true);
                foreach (var im in imgs)
                {
                    if (im.gameObject != slotObj) { iconImg = im; break; }
                }
            }
            if (iconImg == null)
            {
                GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconGo.transform.SetParent(slotObj.transform, false);
                RectTransform rt = iconGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.12f, 0.12f);
                rt.anchorMax = new Vector2(0.88f, 0.88f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                iconImg = iconGo.GetComponent<Image>();
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            // Tìm hoặc tự tạo Quantity Text con để hiển thị số lượng
            Transform qtyTrans = slotObj.transform.Find("Quantity");
            TextMeshProUGUI qtyText = qtyTrans != null ? qtyTrans.GetComponent<TextMeshProUGUI>() : slotObj.GetComponentInChildren<TextMeshProUGUI>(true);
            if (qtyText == null)
            {
                GameObject qtyGo = new GameObject("Quantity", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                qtyGo.transform.SetParent(slotObj.transform, false);
                RectTransform rt = qtyGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.4f, 0f);
                rt.anchorMax = new Vector2(0.95f, 0.45f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                qtyText = qtyGo.GetComponent<TextMeshProUGUI>();
                qtyText.fontSize = 18;
                qtyText.fontStyle = FontStyles.Bold;
                qtyText.alignment = TextAlignmentOptions.BottomRight;
                qtyText.color = Color.white;
                qtyText.raycastTarget = false;
            }

            Button btn = slotObj.GetComponent<Button>();
            if (btn == null) btn = slotObj.AddComponent<Button>();

            // 3. Kiểm tra xem ô này có Item hồi phục hay không
            if (i < recoveryItems.Count && recoveryItems[i] != null && recoveryItems[i].item != null)
            {
                var slotData = recoveryItems[i];
                if (iconImg != null)
                {
                    iconImg.gameObject.SetActive(true);
                    iconImg.sprite = slotData.item.icon;
                    iconImg.color = Color.white;
                    iconImg.preserveAspect = true;
                }
                if (qtyText != null)
                {
                    qtyText.gameObject.SetActive(true);
                    qtyText.text = slotData.quantity > 1 ? slotData.quantity.ToString() : "";
                }
                if (btn != null)
                {
                    btn.interactable = true;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => ShowItemDetailPopup(slotData.item));
                }
            }
            else
            {
                // Ô trống: Giữ nguyên khung Khung_0 của bạn, chỉ ẩn icon và text con
                if (iconImg != null)
                {
                    iconImg.sprite = null;
                    iconImg.gameObject.SetActive(false);
                }
                if (qtyText != null)
                {
                    qtyText.text = "";
                    qtyText.gameObject.SetActive(false);
                }
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.interactable = false;
                }
            }
        }
    }

    /// <summary>Hiển thị bảng chi tiết vật phẩm khi click vào ô item ở đáy màn hình.</summary>
    public void ShowItemDetailPopup(ItemData item)
    {
        if (item == null) return;
        currentSelectedItem = item;

        if (panelItemDetail != null)
        {
            panelItemDetail.SetActive(true);
            panelItemDetail.transform.SetAsLastSibling();
        }

        if (imgDetailIcon != null)
        {
            imgDetailIcon.sprite = item.icon;
            imgDetailIcon.preserveAspect = true;
            imgDetailIcon.color = Color.white;
        }

        if (txtDetailName != null)
        {
            txtDetailName.text = item.itemName;
        }

        if (txtDetailDescription != null)
        {
            txtDetailDescription.text = item.description;
        }

        if (txtDetailEffect != null)
        {
            txtDetailEffect.text = GetCleanItemEffectText(item);
        }

        if (btnDetailUse != null)
        {
            btnDetailUse.onClick.RemoveAllListeners();
            btnDetailUse.onClick.AddListener(UseSelectedItem);
        }

        if (btnDetailDiscard != null)
        {
            btnDetailDiscard.onClick.RemoveAllListeners();
            btnDetailDiscard.onClick.AddListener(DiscardSelectedItem);
        }

        if (btnDetailCloseOverlay != null)
        {
            btnDetailCloseOverlay.onClick.RemoveAllListeners();
            btnDetailCloseOverlay.onClick.AddListener(CloseItemDetailPopup);
        }
    }

    private string GetCleanItemEffectText(ItemData item)
    {
        if (item == null) return "";

        List<string> effects = new List<string>();

        if (item.itemType == ItemType.Consumable)
        {
            if (item.foodRestoreAmount > 0)
            {
                effects.Add($"+{item.foodRestoreAmount} Lương thực");
            }

            if (item.consumableType == ConsumableType.HP || (item.consumableType == ConsumableType.Food && item.healAmount > 0))
            {
                int hpVal = item.healAmount > 0 ? item.healAmount : Mathf.FloorToInt((PlayerManager.Instance?.maxHP ?? 100) * item.healPercentage);
                if (hpVal > 0) effects.Add($"+{hpVal} HP");
            }
            else if (item.consumableType == ConsumableType.SEN)
            {
                int senVal = item.healAmount > 0 ? item.healAmount : Mathf.FloorToInt((PlayerManager.Instance?.maxSen ?? 10) * item.healPercentage);
                if (senVal > 0) effects.Add($"+{senVal} SEN");
            }
            else if (item.consumableType == ConsumableType.Buff_ATK)
            {
                effects.Add($"+{item.buffValue} Sát thương ({item.buffDuration} lượt)");
            }
            else if (item.consumableType == ConsumableType.Buff_DEF)
            {
                effects.Add($"+{item.buffValue} Phòng ngự ({item.buffDuration} lượt)");
            }
            else if (item.consumableType == ConsumableType.Buff_SPD)
            {
                effects.Add($"+{item.buffValue} Tốc độ ({item.buffDuration} lượt)");
            }
            else if (item.consumableType == ConsumableType.Buff_CRIT)
            {
                effects.Add($"+{item.buffValue}% Chí mạng ({item.buffDuration} lượt)");
            }
            else if (item.consumableType == ConsumableType.Buff_EVA)
            {
                effects.Add($"+{item.buffValue}% Né tránh ({item.buffDuration} lượt)");
            }
            else if (item.consumableType == ConsumableType.Cleanse)
            {
                effects.Add("Hóa giải mọi hiệu ứng xấu");
            }
            else if (item.consumableType == ConsumableType.WeaponCoating)
            {
                effects.Add($"Tẩm vũ khí: {item.coatingType} ({item.coatingChance}%)");
            }
        }
        else if (item.itemType == ItemType.Equipment)
        {
            if (item.damageBonus > 0) effects.Add($"+{item.damageBonus} Sát thương");
            if (item.defenseBonus > 0) effects.Add($"+{item.defenseBonus} Phòng ngự");
            if (item.critBonus > 0) effects.Add($"+{item.critBonus}% Chí mạng");
            if (item.evasionBonus > 0) effects.Add($"+{item.evasionBonus}% Né tránh");
            if (item.speedBonus > 0) effects.Add($"+{item.speedBonus} Tốc độ");
            if (item.armorPenetration > 0) effects.Add($"+{item.armorPenetration} Xuyên giáp");
        }

        if (effects.Count > 0)
        {
            return string.Join(" ", effects);
        }
        return item.itemType == ItemType.Material ? "Nguyên liệu chế tạo" : "Vật phẩm sinh tồn";
    }

    /// <summary>Sử dụng vật phẩm đang chọn trong bảng chi tiết.</summary>
    public void UseSelectedItem()
    {
        if (currentSelectedItem == null) return;

        bool used = false;
        if (ItemUsageManager.Instance != null)
        {
            used = ItemUsageManager.Instance.UseItem(currentSelectedItem);
        }

        if (used)
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.RemoveItem(currentSelectedItem, 1);
            }
            CloseItemDetailPopup();
            UpdateAllDungeonHUD();
        }
    }

    /// <summary>Vứt bỏ vật phẩm đang chọn khỏi túi đồ.</summary>
    public void DiscardSelectedItem()
    {
        if (currentSelectedItem == null) return;

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RemoveItem(currentSelectedItem, 1);
        }

        if (FloatingTextManager.Instance != null && PlayerMovement.Instance != null)
        {
            FloatingTextManager.Instance.SpawnText(PlayerMovement.Instance.transform.position, $"Đã vứt {currentSelectedItem.itemName}", Color.gray);
        }

        CloseItemDetailPopup();
        UpdateAllDungeonHUD();
    }

    /// <summary>Đóng bảng chi tiết vật phẩm.</summary>
    public void CloseItemDetailPopup()
    {
        if (panelItemDetail != null)
            panelItemDetail.SetActive(false);
        currentSelectedItem = null;
    }
}
