using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ─── Data Containers ─────────────────────────────────────────────
[Serializable]
public class SaveData
{
    public int saveVersion = 1;
    public PlayerSaveData player;
    public InventorySaveData inventory;
    public DungeonSaveData dungeon;
    public SkillSaveData skills;
    public GuildSaveData guild;
    public string activeTownEvent; // Sự kiện Town đang diễn ra
}

[Serializable]
public class PlayerSaveData
{
    public int level, currentExp, expToNextLevel, unspentStatPoints;
    public int currentHP, maxHP;
    public int str, dex, vit, agl;
    public int gold, sen, food;
    public List<string> killedMonsterNames = new List<string>();
    public List<int> killedMonsterCounts = new List<int>();
    
    // Thêm để hiển thị thông tin save slot
    public string playerName;
    public float playTime;
    public int characterIconIndex;
}

[Serializable]
public class InventorySlotSaveData
{
    public string itemID;
    public int quantity;
    public bool isEquipped;
    public string equippedSlot; // "Weapon","Armor","Accessory1","Accessory2",""
}

[Serializable]
public class InventorySaveData
{
    public List<InventorySlotSaveData> combatItems  = new List<InventorySlotSaveData>();
    public List<InventorySlotSaveData> storageItems = new List<InventorySlotSaveData>();
}

[Serializable]
public class DungeonSaveData
{
    public int currentFloor;
    public int currentMapSeed;
    public List<SerializableVector3Int> clearedFog      = new List<SerializableVector3Int>();
    public List<SerializableVector3Int> defeatedEnemies = new List<SerializableVector3Int>();
    public List<SerializableVector3Int> activatedEvents = new List<SerializableVector3Int>();
}

[Serializable]
public class SkillSaveData
{
    public List<string> skillIDs   = new List<string>();
    public List<int>    skillLevels = new List<int>();
}

public enum QuestType { Hunt, Gather }

[Serializable]
public class QuestSaveData
{
    public string questID;
    public bool isAccepted;
}

[Serializable]
public class GuildSaveData
{
    public int pendingSalaryCount;
    public List<QuestSaveData> activeBoardQuests = new List<QuestSaveData>();
    public List<string> completedQuests = new List<string>();
}

// Vector3Int không serialize được trực tiếp bằng JsonUtility
[Serializable]
public struct SerializableVector3Int
{
    public int x, y, z;
    public SerializableVector3Int(UnityEngine.Vector3Int v) { x = v.x; y = v.y; z = v.z; }
    public UnityEngine.Vector3Int ToVector3Int() => new UnityEngine.Vector3Int(x, y, z);
}

// ─── SaveSystem ───────────────────────────────────────────────────
public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    // Kho ItemData — kéo tất cả ItemData SO vào đây trong Inspector
    [Header("Item Registry (Kéo tất cả ItemData vào đây để Load)")]
    public ItemData[] allItems;

    [Header("Save Slot System")]
    public static int CurrentSlot = 1; // 1, 2, 3 mặc định

    public static string GetSavePath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, $"save_slot_{slot}.json");
    }

    private static string SavePath => GetSavePath(CurrentSlot);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (gameObject.GetComponent<Canvas>() != null || gameObject.GetComponent<Camera>() != null)
            {
                Destroy(this);
            }
            else
            {
                Destroy(gameObject);
            }
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Khởi chạy Coroutine tự động lưu
        StartCoroutine(AutoSaveRoutine());
    }

    private System.Collections.IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(60f);

            string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            // Chỉ tự động lưu nếu KHÔNG ở scene Dungeon, Combat hoặc Start
            if (activeScene != "Dungeon" && activeScene != "Combat" && activeScene != "Start")
            {
                Save();
                Debug.Log($"[AUTOSAVE] Đã tự động lưu game thành công vào slot {CurrentSlot}");
            }
        }
    }

    // ─── SAVE ────────────────────────────────────────────────────
    public void Save()
    {
        SaveData data = new SaveData();
        data.player    = BuildPlayerData();
        data.inventory = BuildInventoryData();
        data.dungeon   = BuildDungeonData();
        data.skills    = BuildSkillData();
        if (GuildManager.Instance != null) data.guild = GuildManager.Instance.BuildGuildData();

        if (TownEventManager.Instance != null)
            data.activeTownEvent = TownEventManager.Instance.CurrentEvent.ToString();
        else
            data.activeTownEvent = TownEvent.None.ToString();

        string json = JsonUtility.ToJson(data, true);
        string path = SavePath;
        File.WriteAllText(path, json);
        Debug.Log($"[SAVE] Đã lưu game vào slot {CurrentSlot}: {path}");
    }

    // ─── LOAD ────────────────────────────────────────────────────
    public bool Load()
    {
        ResetAllStates();
        string path = SavePath;
        if (!File.Exists(path))
        {
            Debug.Log($"[SAVE] Không tìm thấy file save tại slot {CurrentSlot}.");
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            if (data == null) { Debug.LogError("[SAVE] File save lỗi — không parse được."); return false; }

            ApplyPlayerData(data.player);
            ApplyInventoryData(data.inventory);
            ApplyDungeonData(data.dungeon);
            ApplySkillData(data.skills);
            if (GuildManager.Instance != null) GuildManager.Instance.ApplyGuildData(data.guild);

            if (TownEventManager.Instance != null && !string.IsNullOrEmpty(data.activeTownEvent))
            {
                if (Enum.TryParse(data.activeTownEvent, out TownEvent loadedEvent))
                {
                    TownEventManager.Instance.SetEvent(loadedEvent);
                }
                else
                {
                    TownEventManager.Instance.SetEvent(TownEvent.None);
                }
            }

            Debug.Log($"[SAVE] Đã load game thành công từ slot {CurrentSlot}.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SAVE] Lỗi khi load: {e.Message}");
            return false;
        }
    }

    public void InitializeNewGame()
    {
        Debug.Log("[SAVE] Khởi tạo dữ liệu game mới.");
        ResetAllStates();
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.gold = 50; // Vàng khởi đầu
            PlayerManager.Instance.level = 1;
            PlayerManager.Instance.currentExp = 0;
            PlayerManager.Instance.expToNextLevel = 50;
            PlayerManager.Instance.unspentStatPoints = 0;
            PlayerManager.Instance.str = 10;
            PlayerManager.Instance.dex = 10;
            PlayerManager.Instance.vit = 10;
            PlayerManager.Instance.agl = 10;
            PlayerManager.Instance.playTime = 0f;
            PlayerManager.Instance.playerName = "Slot " + CurrentSlot;
            PlayerManager.Instance.UpdateMaxHP();
            PlayerManager.Instance.currentHP = PlayerManager.Instance.maxHP;
        }

        if (TownEventManager.Instance != null)
        {
            TownEventManager.Instance.SetEvent(TownEvent.None); // Bắt đầu game mới không có sự kiện (Bình yên)
        }

        // Tìm vật phẩm hồi máu (HP) đầu tiên trong registry để làm quà khởi đầu
        ItemData startingPotion = null;
        if (allItems != null)
        {
            foreach (var item in allItems)
            {
                if (item != null && item.itemType == ItemType.Consumable && item.consumableType == ConsumableType.HP)
                {
                    startingPotion = item;
                    break;
                }
            }
        }

        if (startingPotion != null && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(startingPotion, 3);
            Debug.Log($"[SAVE] Đã tặng 3x {startingPotion.itemName} cho nhân vật mới.");
        }
    }

    private void ResetAllStates()
    {
        Debug.Log("[SAVE] Đang reset tất cả trạng thái manager để tránh tràn bộ nhớ/rò rỉ slot.");

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.killedMonsters.Clear();
            PlayerManager.Instance.RestoreSanityCollapse();
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.combatInventory.Clear();
            InventoryManager.Instance.storageInventory.Clear();
            for (int i = 0; i < InventoryManager.Instance.storageMaxSlots; i++)
            {
                InventoryManager.Instance.storageInventory.Add(new InventorySlot());
            }
            InventoryManager.Instance.equippedWeapon = null;
            InventoryManager.Instance.equippedArmor = null;
            InventoryManager.Instance.equippedAccessory1 = null;
            InventoryManager.Instance.equippedAccessory2 = null;
        }

        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.learnedSkills.Clear();
        }

        if (GuildManager.Instance != null)
        {
            GuildManager.Instance.ResetData();
            GuildManager.Instance.RefreshBoard();
        }

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.RefreshShop();
        }

        PlayerMovement.currentFloor = 1;
        PlayerMovement.currentKeys = 0;
        PlayerMovement.isReturningFromCombat = false;
        PlayerMovement.clearedFogTiles.Clear();
        PlayerMovement.defeatedEnemiesTiles.Clear();
        PlayerMovement.activatedEventTiles.Clear();
        PlayerMovement.savedDungeonPosition = Vector3Int.zero;
        PlayerMovement.floorGoldEarned = 0;
        PlayerMovement.floorExpEarned = 0;
    }

    public static bool HasSave() => File.Exists(SavePath);
    public static bool HasSave(int slot) => File.Exists(GetSavePath(slot));

    public static PlayerSaveData GetPlayerMetadata(int slot)
    {
        string path = GetSavePath(slot);
        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            return data?.player;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SAVE] Lỗi đọc metadata slot {slot}: {e.Message}");
            return null;
        }
    }

    [ContextMenu("Xóa File Save")]
    public void DeleteSaveManual()
    {
        DeleteSave();
    }

    public static void DeleteSave()
    {
        DeleteSave(CurrentSlot);
    }

    public static void DeleteSave(int slot)
    {
        string path = GetSavePath(slot);
        if (File.Exists(path)) 
        {
            File.Delete(path);
            Debug.Log($"[SAVE] Đã xóa file save của slot {slot} thành công.");
        }
    }

    // ─── BUILD DATA ──────────────────────────────────────────────
    private PlayerSaveData BuildPlayerData()
    {
        var p = PlayerManager.Instance;
        var data = new PlayerSaveData
        {
            level = p.level, currentExp = p.currentExp, expToNextLevel = p.expToNextLevel,
            unspentStatPoints = p.unspentStatPoints,
            currentHP = p.currentHP, maxHP = p.maxHP,
            str = p.str, dex = p.dex, vit = p.vit, agl = p.agl,
            gold = p.gold, sen = p.sen, food = p.food,
            playerName = p.playerName,
            playTime = p.playTime,
            characterIconIndex = p.characterIconIndex
        };
        
        foreach(var kvp in p.killedMonsters)
        {
            data.killedMonsterNames.Add(kvp.Key);
            data.killedMonsterCounts.Add(kvp.Value);
        }
        return data;
    }

    private InventorySaveData BuildInventoryData()
    {
        var inv = InventoryManager.Instance;
        var data = new InventorySaveData();

        foreach (var slot in inv.combatInventory)
        {
            if (slot.IsEmpty) continue;
            data.combatItems.Add(new InventorySlotSaveData
            {
                itemID = slot.item.name,
                quantity = slot.quantity,
                isEquipped = false,
                equippedSlot = GetEquippedSlot(slot.item, inv)
            });
        }
        foreach (var slot in inv.storageInventory)
        {
            if (slot.IsEmpty) continue;
            data.storageItems.Add(new InventorySlotSaveData
            {
                itemID = slot.item.name,
                quantity = slot.quantity,
                isEquipped = false,
                equippedSlot = GetEquippedSlot(slot.item, inv)
            });
        }
        return data;
    }

    private string GetEquippedSlot(ItemData item, InventoryManager inv)
    {
        if (inv.equippedWeapon     == item) return "Weapon";
        if (inv.equippedArmor      == item) return "Armor";
        if (inv.equippedAccessory1 == item) return "Accessory1";
        if (inv.equippedAccessory2 == item) return "Accessory2";
        return "";
    }

    private DungeonSaveData BuildDungeonData()
    {
        var data = new DungeonSaveData
        {
            currentFloor  = PlayerMovement.currentFloor,
            currentMapSeed = PlayerMovement.currentMapSeed
        };
        foreach (var v in PlayerMovement.clearedFogTiles)
            data.clearedFog.Add(new SerializableVector3Int(v));
        foreach (var v in PlayerMovement.defeatedEnemiesTiles)
            data.defeatedEnemies.Add(new SerializableVector3Int(v));
        foreach (var v in PlayerMovement.activatedEventTiles)
            data.activatedEvents.Add(new SerializableVector3Int(v));
        return data;
    }

    private SkillSaveData BuildSkillData()
    {
        var sm = SkillManager.Instance;
        var data = new SkillSaveData();
        foreach (var kv in sm.learnedSkills)
        {
            data.skillIDs.Add(kv.Key);
            data.skillLevels.Add(kv.Value);
        }
        return data;
    }

    // ─── APPLY DATA ──────────────────────────────────────────────
    private void ApplyPlayerData(PlayerSaveData d)
    {
        if (d == null || PlayerManager.Instance == null) return;
        var p = PlayerManager.Instance;
        p.level = d.level; p.currentExp = d.currentExp; p.expToNextLevel = d.expToNextLevel;
        p.unspentStatPoints = d.unspentStatPoints;
        p.str = d.str; p.dex = d.dex; p.vit = d.vit; p.agl = d.agl;
        p.gold = d.gold; p.sen = d.sen; p.food = d.food;
        p.playerName = string.IsNullOrEmpty(d.playerName) ? ("Slot " + CurrentSlot) : d.playerName;
        p.playTime = d.playTime;
        p.characterIconIndex = d.characterIconIndex;
        p.UpdateMaxHP();
        p.currentHP = Mathf.Min(d.currentHP, p.maxHP);
        
        p.killedMonsters.Clear();
        if (d.killedMonsterNames != null && d.killedMonsterCounts != null)
        {
            for(int i = 0; i < d.killedMonsterNames.Count && i < d.killedMonsterCounts.Count; i++)
            {
                p.killedMonsters[d.killedMonsterNames[i]] = d.killedMonsterCounts[i];
            }
        }
    }

    private void ApplyInventoryData(InventorySaveData d)
    {
        if (d == null || InventoryManager.Instance == null) return;
        var inv = InventoryManager.Instance;
        inv.combatInventory.Clear();
        inv.storageInventory.Clear();

        // Reinit storage slots
        for (int i = 0; i < inv.storageMaxSlots; i++)
            inv.storageInventory.Add(new InventorySlot());

        foreach (var sd in d.combatItems)
        {
            ItemData item = FindItem(sd.itemID);
            if (item == null) continue;
            inv.AddItem(item, sd.quantity);
            if (!string.IsNullOrEmpty(sd.equippedSlot))
                EquipFromSlot(item, sd.equippedSlot, inv);
        }
        foreach (var sd in d.storageItems)
        {
            ItemData item = FindItem(sd.itemID);
            if (item == null) continue;
            inv.AddItem(item, sd.quantity);
            if (!string.IsNullOrEmpty(sd.equippedSlot))
                EquipFromSlot(item, sd.equippedSlot, inv);
        }
        PlayerManager.Instance?.UpdateEquipmentStats();
    }

    private void EquipFromSlot(ItemData item, string slot, InventoryManager inv)
    {
        switch (slot)
        {
            case "Weapon":     inv.equippedWeapon     = item; break;
            case "Armor":      inv.equippedArmor      = item; break;
            case "Accessory1": inv.equippedAccessory1 = item; break;
            case "Accessory2": inv.equippedAccessory2 = item; break;
        }
    }

    private void ApplyDungeonData(DungeonSaveData d)
    {
        if (d == null) return;
        PlayerMovement.currentFloor   = d.currentFloor;
        PlayerMovement.currentMapSeed = d.currentMapSeed;
        PlayerMovement.clearedFogTiles.Clear();
        PlayerMovement.defeatedEnemiesTiles.Clear();
        PlayerMovement.activatedEventTiles.Clear();
        foreach (var v in d.clearedFog)      PlayerMovement.clearedFogTiles.Add(v.ToVector3Int());
        foreach (var v in d.defeatedEnemies) PlayerMovement.defeatedEnemiesTiles.Add(v.ToVector3Int());
        foreach (var v in d.activatedEvents) PlayerMovement.activatedEventTiles.Add(v.ToVector3Int());
    }

    private void ApplySkillData(SkillSaveData d)
    {
        if (d == null || SkillManager.Instance == null) return;
        var sm = SkillManager.Instance;
        sm.learnedSkills.Clear();
        for (int i = 0; i < d.skillIDs.Count && i < d.skillLevels.Count; i++)
            sm.learnedSkills[d.skillIDs[i]] = d.skillLevels[i];
    }

    private ItemData FindItem(string id)
    {
        if (allItems == null) return null;
        foreach (var item in allItems)
            if (item != null && item.name == id) return item;
        Debug.LogWarning($"[SAVE] Không tìm thấy ItemData với ID: {id}");
        return null;
    }
}
