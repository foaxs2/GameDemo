using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using TMPro;

public class StartSceneManager : MonoBehaviour
{
    [Header("Main Menu Panels")]
    public GameObject panelMainMenu;
    public GameObject panelSaveSlots;
    public GameObject panelSettings;

    [Header("Main Menu Buttons")]
    public Button btnStart;
    public Button btnSettings;
    public Button btnExit;
    public Button btnBackFromSlots; // Nút quay về trong panel Save Slots

    [Header("Game Manager Prefab Bootstrap")]
    public GameObject gameManagerPrefab; // Prefab GameManager để khởi tạo nếu thiếu khi chạy Start scene đầu tiên

    [Header("Save Slot UI Cards")]
    public SaveSlotCardUI[] slotCards; // Gán 3 card tương ứng Slot 1, 2, 3

    [Header("Delete Confirmation Panel")]
    public GameObject panelDeleteConfirm;
    public TextMeshProUGUI txtDeleteConfirmPrompt;
    public Button btnConfirmDelete;
    public Button btnCancelDelete;

    [Header("Settings Panel Elements")]
    public Slider sliderMasterVol;
    public Slider sliderBGMVol;
    public Slider sliderSFXVol;
    public Toggle toggleFullscreen;
    public TMP_Dropdown dropdownResolution;
    public TMP_Dropdown dropdownLanguage;
    public Button btnCloseSettings;

    private int slotToDelete = -1;

    private void Awake()
    {
        // Khởi tạo GameManager prefab nếu chưa tồn tại
        if (SaveSystem.Instance == null && gameManagerPrefab != null)
        {
            Instantiate(gameManagerPrefab);
            Debug.Log("[START] Khởi tạo GameManager prefab thành công!");
        }
    }

    private void Start()
    {
        // ─── Main Menu Buttons Setup ───
        if (btnStart != null) btnStart.onClick.AddListener(OpenSaveSlots);
        if (btnSettings != null) btnSettings.onClick.AddListener(OpenSettings);
        if (btnExit != null) btnExit.onClick.AddListener(ExitGame);
        if (btnBackFromSlots != null) btnBackFromSlots.onClick.AddListener(CloseSaveSlots);

        // ─── Delete Confirmation Setup ───
        if (btnConfirmDelete != null) btnConfirmDelete.onClick.AddListener(ConfirmDeleteSlot);
        if (btnCancelDelete != null) btnCancelDelete.onClick.AddListener(CloseDeleteConfirmation);

        // ─── Settings Setup ───
        if (sliderMasterVol != null) sliderMasterVol.onValueChanged.AddListener(SetMasterVolume);
        if (sliderBGMVol != null) sliderBGMVol.onValueChanged.AddListener(SetBGMVolume);
        if (sliderSFXVol != null) sliderSFXVol.onValueChanged.AddListener(SetSFXVolume);
        if (toggleFullscreen != null) toggleFullscreen.onValueChanged.AddListener(SetFullscreen);
        if (dropdownResolution != null) dropdownResolution.onValueChanged.AddListener(SetResolution);
        if (dropdownLanguage != null) dropdownLanguage.onValueChanged.AddListener(SetLanguage);
        if (btnCloseSettings != null) btnCloseSettings.onClick.AddListener(CloseSettings);

        // Khởi động UI chính
        ShowMainMenu();
        LoadSettingsFromPrefs();
    }

    // ─── NAVIGATION FUNCTIONS ───

    public void ShowMainMenu()
    {
        if (panelMainMenu != null) panelMainMenu.SetActive(true);
        if (panelSaveSlots != null) panelSaveSlots.SetActive(false);
        if (panelSettings != null) panelSettings.SetActive(false);
        if (panelDeleteConfirm != null) panelDeleteConfirm.SetActive(false);
    }

    public void OpenSaveSlots()
    {
        if (panelMainMenu != null) panelMainMenu.SetActive(false);
        if (panelSaveSlots != null) panelSaveSlots.SetActive(true);
        RefreshSaveSlots();
    }

    public void CloseSaveSlots()
    {
        ShowMainMenu();
    }

    public void OpenSettings()
    {
        if (panelMainMenu != null) panelMainMenu.SetActive(false);
        if (panelSettings != null) panelSettings.SetActive(true);
    }

    public void CloseSettings()
    {
        ShowMainMenu();
    }

    public void ExitGame()
    {
        Debug.Log("[START] Thoát game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─── SAVE SLOTS LOGIC ───

    public void RefreshSaveSlots()
    {
        if (slotCards == null) return;

        for (int i = 0; i < slotCards.Length; i++)
        {
            int slotIndex = i + 1;
            SaveSlotCardUI card = slotCards[i];
            if (card == null) continue;

            bool hasSave = SaveSystem.HasSave(slotIndex);
            card.SetupCard(slotIndex, hasSave, this);
        }
    }

    public void SelectSlot(int slotIndex)
    {
        SaveSystem.CurrentSlot = slotIndex;

        if (SaveSystem.HasSave(slotIndex))
        {
            // Có save -> Load tiến trình và vào thẳng Town
            Debug.Log($"[START] Chọn Slot {slotIndex}. Đang load save...");
            bool loaded = SaveSystem.Instance.Load();
            if (loaded)
            {
                SceneManager.LoadScene("Town");
            }
            else
            {
                Debug.LogError($"[START] Lỗi khi load slot {slotIndex}!");
            }
        }
        else
        {
            // Trống -> Tạo mới dữ liệu và vào thẳng Town
            Debug.Log($"[START] Slot {slotIndex} trống. Đang tạo nhân vật mới...");
            
            // Xóa dữ liệu tĩnh cũ của Player trước khi khởi tạo mới
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.killedMonsters.Clear();
                PlayerManager.Instance.RestoreSanityCollapse();
            }

            SaveSystem.Instance.InitializeNewGame();
            SaveSystem.Instance.Save(); // Lưu phát đầu tiên để tạo file save
            SceneManager.LoadScene("Town");
        }
    }

    public void RequestDeleteSlot(int slotIndex)
    {
        slotToDelete = slotIndex;
        if (panelDeleteConfirm != null)
        {
            if (txtDeleteConfirmPrompt != null)
            {
                txtDeleteConfirmPrompt.text = $"Bạn có chắc chắn muốn xóa vĩnh viễn dữ liệu tại <color=red>Slot {slotIndex}</color>? Hành động này không thể hoàn tác.";
            }
            panelDeleteConfirm.SetActive(true);
        }
        else
        {
            // Nếu không có panel confirm thì xóa trực tiếp (Fallback)
            ConfirmDeleteSlot();
        }
    }

    private void ConfirmDeleteSlot()
    {
        if (slotToDelete != -1)
        {
            SaveSystem.DeleteSave(slotToDelete);
            slotToDelete = -1;
            CloseDeleteConfirmation();
            RefreshSaveSlots();
        }
    }

    private void CloseDeleteConfirmation()
    {
        if (panelDeleteConfirm != null) panelDeleteConfirm.SetActive(false);
        slotToDelete = -1;
    }

    // ─── SETTINGS LOGIC (PLAYERPREFS) ───

    private void LoadSettingsFromPrefs()
    {
        // 1. Âm thanh
        float masterVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float bgmVol = PlayerPrefs.GetFloat("BGMVolume", 0.75f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

        if (sliderMasterVol != null) sliderMasterVol.value = masterVol;
        if (sliderBGMVol != null) sliderBGMVol.value = bgmVol;
        if (sliderSFXVol != null) sliderSFXVol.value = sfxVol;

        AudioListener.volume = masterVol; // Áp dụng master volume cho hệ thống âm thanh Unity

        // 2. Fullscreen
        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        if (toggleFullscreen != null) toggleFullscreen.isOn = isFullscreen;
        Screen.fullScreen = isFullscreen;

        // 3. Resolution
        int resIndex = PlayerPrefs.GetInt("ResolutionIndex", 0);
        if (dropdownResolution != null) dropdownResolution.value = resIndex;

        // 4. Language
        int langIndex = PlayerPrefs.GetInt("LanguageIndex", 0);
        if (dropdownLanguage != null) dropdownLanguage.value = langIndex;
    }

    public void SetMasterVolume(float value)
    {
        PlayerPrefs.SetFloat("MasterVolume", value);
        AudioListener.volume = value;
        Debug.Log($"[SETTINGS] Master Volume: {Mathf.RoundToInt(value * 100)}%");
    }

    public void SetBGMVolume(float value)
    {
        PlayerPrefs.SetFloat("BGMVolume", value);
        // Có thể áp dụng trực tiếp cho AudioSource nhạc nền nếu có SoundManager riêng
        Debug.Log($"[SETTINGS] BGM Volume: {Mathf.RoundToInt(value * 100)}%");
    }

    public void SetSFXVolume(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        Debug.Log($"[SETTINGS] SFX Volume: {Mathf.RoundToInt(value * 100)}%");
    }

    public void SetFullscreen(bool isFullscreen)
    {
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        Screen.fullScreen = isFullscreen;
        Debug.Log($"[SETTINGS] Toàn màn hình: {isFullscreen}");
    }

    public void SetResolution(int index)
    {
        PlayerPrefs.SetInt("ResolutionIndex", index);
        if (dropdownResolution == null) return;

        string optText = dropdownResolution.options[index].text;
        string[] split = optText.Split('x');
        if (split.Length == 2)
        {
            int w = int.Parse(split[0].Trim());
            int h = int.Parse(split[1].Trim());
            Screen.SetResolution(w, h, Screen.fullScreen);
            Debug.Log($"[SETTINGS] Đổi độ phân giải: {w}x{h}");
        }
    }

    public void SetLanguage(int index)
    {
        PlayerPrefs.SetInt("LanguageIndex", index);
        string lang = index == 0 ? "Tiếng Việt" : "English";
        Debug.Log($"[SETTINGS] Ngôn ngữ được chọn: {lang}");
    }
}
