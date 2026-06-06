using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class QuestConfig
{
    public string questID;
    public QuestType type;
    public string targetName;
    public Sprite questIcon;
    public int requiredAmount;
    
    [Header("Rewards")]
    public int rewardGold;
    public int rewardExp;
    public ItemData rewardItem;
    public int rewardItemAmount = 1;
    public ItemData rewardEquipment;
}

public class GuildManager : MonoBehaviour
{
    public static GuildManager Instance { get; private set; }

    [Header("Cấu hình Quest Cố Định")]
    public List<QuestConfig> predefinedQuests;

    // Dữ liệu nội tại
    private int pendingSalaryCount = 1; // Khởi tạo bằng 1 để nhận lần đầu
    private List<QuestSaveData> activeBoardQuests = new List<QuestSaveData>();
    private List<string> completedQuests = new List<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            if (gameObject.GetComponent<Canvas>() != null || gameObject.GetComponent<Camera>() != null)
            {
                Destroy(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    public void ResetData()
    {
        pendingSalaryCount = 1;
        activeBoardQuests.Clear();
        completedQuests.Clear();
    }

    public GuildSaveData BuildGuildData()
    {
        return new GuildSaveData
        {
            pendingSalaryCount = this.pendingSalaryCount,
            activeBoardQuests = new List<QuestSaveData>(this.activeBoardQuests),
            completedQuests = new List<string>(this.completedQuests)
        };
    }

    public void ApplyGuildData(GuildSaveData data)
    {
        if (data == null) return;
        this.pendingSalaryCount = data.pendingSalaryCount;
        this.activeBoardQuests = new List<QuestSaveData>(data.activeBoardQuests);
        this.completedQuests = new List<string>(data.completedQuests);
    }

    public void AddSalaryClaim()
    {
        pendingSalaryCount++;
        Debug.Log($"[GUILD] Bạn có lương mới! Số lần nhận hiện tại: {pendingSalaryCount}");
    }

    public bool TryClaimSalary()
    {
        if (pendingSalaryCount > 0)
        {
            pendingSalaryCount--;
            PlayerManager.Instance.gold += 40;
            return true;
        }
        return false;
    }

    public bool HasSalary() => pendingSalaryCount > 0;

    public List<QuestSaveData> GetActiveQuests() => activeBoardQuests;

    public QuestConfig GetQuestConfig(string questID)
    {
        return predefinedQuests.Find(q => q.questID == questID);
    }

    public void AcceptQuest(string questID)
    {
        int acceptedCount = 0;
        foreach(var q in activeBoardQuests) if(q.isAccepted) acceptedCount++;

        if(acceptedCount >= 3)
        {
            Debug.LogWarning("[GUILD] Bạn đã nhận tối đa 3 nhiệm vụ!");
            return;
        }

        foreach(var q in activeBoardQuests)
        {
            if(q.questID == questID)
            {
                q.isAccepted = true;
                break;
            }
        }
    }

    public void CancelQuest(string questID)
    {
        foreach(var q in activeBoardQuests)
        {
            if(q.questID == questID)
            {
                q.isAccepted = false;
                break;
            }
        }
    }

    public bool CompleteQuest(string questID)
    {
        for (int i = 0; i < activeBoardQuests.Count; i++)
        {
            if (activeBoardQuests[i].questID == questID && activeBoardQuests[i].isAccepted)
            {
                QuestConfig config = GetQuestConfig(questID);
                if (config == null) return false;

                if (config.type == QuestType.Hunt)
                {
                    int currentKilled = PlayerManager.Instance.killedMonsters.ContainsKey(config.targetName) ? PlayerManager.Instance.killedMonsters[config.targetName] : 0;
                    if (currentKilled >= config.requiredAmount)
                    {
                        PlayerManager.Instance.killedMonsters[config.targetName] -= config.requiredAmount;
                        RewardPlayer(config);
                        completedQuests.Add(questID);
                        activeBoardQuests.RemoveAt(i);
                        return true;
                    }
                }
                else if (config.type == QuestType.Gather)
                {
                    ItemData item = SaveSystem.Instance.allItems != null ? System.Array.Find(SaveSystem.Instance.allItems, x => x != null && x.itemName == config.targetName) : null;
                    if (item != null && InventoryManager.Instance.HasItem(item, config.requiredAmount))
                    {
                        InventoryManager.Instance.RemoveItem(item, config.requiredAmount);
                        RewardPlayer(config);
                        completedQuests.Add(questID);
                        activeBoardQuests.RemoveAt(i);
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private void RewardPlayer(QuestConfig config)
    {
        if (config.rewardGold > 0) PlayerManager.Instance.gold += config.rewardGold;
        if (config.rewardExp > 0) PlayerManager.Instance.AddExp(config.rewardExp);
        if (config.rewardItem != null && config.rewardItemAmount > 0)
        {
            InventoryManager.Instance.AddItem(config.rewardItem, config.rewardItemAmount);
        }
        if (config.rewardEquipment != null)
        {
            InventoryManager.Instance.AddItem(config.rewardEquipment, 1);
        }
        Debug.Log($"[GUILD] Đã trả nhiệm vụ, nhận thưởng!");
    }

    public void RefreshBoard()
    {
        // 1. Giữ lại các nhiệm vụ đã nhận, xóa các nhiệm vụ chưa nhận khỏi bảng
        List<QuestSaveData> keptQuests = new List<QuestSaveData>();
        foreach(var q in activeBoardQuests)
        {
            if(q.isAccepted) keptQuests.Add(q);
        }
        activeBoardQuests = keptQuests;

        // 2. Tìm các nhiệm vụ hợp lệ để bốc (Chưa hoàn thành và chưa nằm trên bảng)
        List<QuestConfig> availableQuests = new List<QuestConfig>();
        foreach (var config in predefinedQuests)
        {
            if (!completedQuests.Contains(config.questID) && !activeBoardQuests.Exists(q => q.questID == config.questID))
            {
                availableQuests.Add(config);
            }
        }

        // 3. Xáo trộn ngẫu nhiên danh sách (Shuffle)
        for (int i = 0; i < availableQuests.Count; i++)
        {
            int randomIndex = Random.Range(i, availableQuests.Count);
            QuestConfig temp = availableQuests[i];
            availableQuests[i] = availableQuests[randomIndex];
            availableQuests[randomIndex] = temp;
        }

        // 4. Bốc ngẫu nhiên thêm vào cho đến khi đủ 6 bảng (hoặc hết nhiệm vụ)
        int index = 0;
        while (activeBoardQuests.Count < 6 && index < availableQuests.Count)
        {
            activeBoardQuests.Add(new QuestSaveData { questID = availableQuests[index].questID, isAccepted = false });
            index++;
        }
    }
}
