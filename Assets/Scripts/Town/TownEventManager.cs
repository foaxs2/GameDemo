using UnityEngine;

public enum TownEvent
{
    None,            // Bình yên
    ShopDiscount,    // Giảm 25% giá shop
    SkillDiscount,   // Giảm 25% học kỹ năng (trong guild và skill tree)
    ShopSellBonus,   // Giá bán vật phẩm tăng 50%
    FreeWine         // Miễn phí rượu ở Guild
}

public class TownEventManager : MonoBehaviour
{
    public static TownEventManager Instance { get; private set; }

    public TownEvent CurrentEvent { get; private set; } = TownEvent.None;

    private void Awake()
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

    public void SetEvent(TownEvent newEvent)
    {
        CurrentEvent = newEvent;
        Debug.Log($"[TOWN EVENT] Sự kiện hiện tại: {GetEventName(CurrentEvent)}");
    }

    public void RollNewEvent()
    {
        // 50% cơ hội xuất hiện sự kiện ngẫu nhiên, 50% cơ hội Bình yên (None)
        if (Random.Range(0f, 100f) < 50f)
        {
            // Chọn ngẫu nhiên 1 trong các sự kiện (loại trừ None)
            int eventCount = System.Enum.GetValues(typeof(TownEvent)).Length;
            CurrentEvent = (TownEvent)Random.Range(1, eventCount);
        }
        else
        {
            CurrentEvent = TownEvent.None;
        }

        Debug.Log($"[TOWN EVENT] Đã kích hoạt sự kiện mới: {GetEventName(CurrentEvent)}");
    }

    public string GetEventName(TownEvent townEvent)
    {
        switch (townEvent)
        {
            case TownEvent.ShopDiscount:
                return "Giảm 25% giá shop";
            case TownEvent.SkillDiscount:
                return "Giảm 25% học kĩ năng";
            case TownEvent.ShopSellBonus:
                return "Tăng 50% giá bán";
            case TownEvent.FreeWine:
                return "Miễn phí rượu";
            case TownEvent.None:
            default:
                return "Bình yên";
        }
    }
}
