using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TownSettingsUI : MonoBehaviour
{
    [Header("Settings Panel Elements")]
    public Slider sliderMasterVol;
    public Slider sliderBGMVol;
    public Slider sliderSFXVol;
    public Toggle toggleFullscreen;
    public TMP_Dropdown dropdownResolution;
    public TMP_Dropdown dropdownLanguage;
    public Button btnCloseSettings;
    public Button btnSaveAndExit;

    private void Start()
    {
        // ─── Settings Listeners Setup ───
        if (sliderMasterVol != null) sliderMasterVol.onValueChanged.AddListener(SetMasterVolume);
        if (sliderBGMVol != null) sliderBGMVol.onValueChanged.AddListener(SetBGMVolume);
        if (sliderSFXVol != null) sliderSFXVol.onValueChanged.AddListener(SetSFXVolume);
        if (toggleFullscreen != null) toggleFullscreen.onValueChanged.AddListener(SetFullscreen);
        if (dropdownResolution != null) dropdownResolution.onValueChanged.AddListener(SetResolution);
        if (dropdownLanguage != null) dropdownLanguage.onValueChanged.AddListener(SetLanguage);
        if (btnCloseSettings != null) btnCloseSettings.onClick.AddListener(CloseSettings);
        if (btnSaveAndExit != null) btnSaveAndExit.onClick.AddListener(SaveAndExit);

        // Load values from PlayerPrefs on start
        LoadSettingsFromPrefs();
    }

    public void OpenSettings()
    {
        gameObject.SetActive(true);
        LoadSettingsFromPrefs(); // Load lại để đồng bộ trong trường hợp thay đổi bên ngoài
    }

    public void CloseSettings()
    {
        gameObject.SetActive(false);
    }

    public void SaveAndExit()
    {
        Debug.Log("[TOWN SETTINGS] Đang thực hiện Lưu và Thoát...");
        
        // 1. Lưu tiến trình hiện tại bằng SaveSystem
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.Save();
            Debug.Log("[TOWN SETTINGS] Đã lưu game thành công vào slot hiện tại.");
        }
        else
        {
            Debug.LogError("[TOWN SETTINGS] Không tìm thấy SaveSystem instance để lưu game!");
        }

        // 2. Quay lại scene Start (màn hình Menu chính)
        SceneManager.LoadScene("Start");
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
        Debug.Log($"[SETTINGS-TOWN] Master Volume: {Mathf.RoundToInt(value * 100)}%");
    }

    public void SetBGMVolume(float value)
    {
        PlayerPrefs.SetFloat("BGMVolume", value);
        Debug.Log($"[SETTINGS-TOWN] BGM Volume: {Mathf.RoundToInt(value * 100)}%");
    }

    public void SetSFXVolume(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        Debug.Log($"[SETTINGS-TOWN] SFX Volume: {Mathf.RoundToInt(value * 100)}%");
    }

    public void SetFullscreen(bool isFullscreen)
    {
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        Screen.fullScreen = isFullscreen;
        Debug.Log($"[SETTINGS-TOWN] Toàn màn hình: {isFullscreen}");
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
            Debug.Log($"[SETTINGS-TOWN] Đổi độ phân giải: {w}x{h}");
        }
    }

    public void SetLanguage(int index)
    {
        PlayerPrefs.SetInt("LanguageIndex", index);
        string lang = index == 0 ? "Tiếng Việt" : "English";
        Debug.Log($"[SETTINGS-TOWN] Ngôn ngữ được chọn: {lang}");
    }
}
