using UnityEngine;
using UnityEngine.SceneManagement;

public class TownUIController : MonoBehaviour
{
    public static TownUIController Instance { get; private set; }

    [Header("UI Panels")]
    public ShopUI shopUI;
    public GuildUI guildUI;
    public TrainingUI trainingUI;
    public TownSettingsUI settingsUI;

    [Header("HUD")]
    public GameObject topHUDBar;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        // Kiểm tra xem có bất kỳ bảng điều khiển nào đang mở hay không
        bool isAnyPanelOpen = (shopUI != null && shopUI.gameObject.activeSelf) ||
                              (guildUI != null && guildUI.guildPanel != null && guildUI.guildPanel.activeSelf) ||
                              (trainingUI != null && trainingUI.gameObject.activeSelf) ||
                              (settingsUI != null && settingsUI.gameObject.activeSelf);

        // Tự động ẩn HUD nếu có bảng đang mở, và hiện lại khi đóng hết
        if (topHUDBar != null)
        {
            topHUDBar.SetActive(!isAnyPanelOpen);
        }
    }

    public void OpenShop()
    {
        if (shopUI != null) shopUI.OpenShop();
    }

    public void OpenGuild()
    {
        if (guildUI != null) guildUI.OpenGuild();
    }

    public void OpenTraining()
    {
        if (trainingUI != null) trainingUI.OpenUI();
    }

    public void OpenSettings()
    {
        if (settingsUI != null) settingsUI.OpenSettings();
    }

    public void EnterDungeon()
    {
        Debug.Log($"Bắt đầu thám hiểm Dungeon! Tầng hiện tại: {PlayerMovement.currentFloor}");
        SceneManager.LoadScene("Dungeon");
    }
}
