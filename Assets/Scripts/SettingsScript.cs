using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using TMPro;

public class SettingsScript : MonoBehaviour
{
    public static SettingsScript instance;

    [Header("UI References")]
    public Image buttonImage; 
    public Sprite joystickIcon;
    public Sprite lateralIcon;

    [Header("UI Text References")]
    public TextMeshProUGUI soundText;
    public TextMeshProUGUI musicText;
    public TextMeshProUGUI vibrationText;

    private string movementModeKey = "MovementMode";

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        UpdateSettingsUI();
        UpdateAudioVibrationUI();
        ApplyMusicSetting();
        ApplySoundSetting();
    }

    // --- Mouvement ---
    public void ToggleMovementMode()
    {
        int currentMode = PlayerPrefs.GetInt(movementModeKey, 0);
        PlayerPrefs.SetInt(movementModeKey, currentMode == 0 ? 1 : 0);
        PlayerPrefs.Save();
        UpdateSettingsUI();
    }

    private void UpdateSettingsUI()
    {
        int currentMode = PlayerPrefs.GetInt(movementModeKey, 0);
        if (buttonImage != null)
        {
            buttonImage.sprite = currentMode == 0 ? joystickIcon : lateralIcon;
        }
        
        if (PlayerMovement.instance != null)
        {
            PlayerMovement.instance.RefreshMovementMode();
        }
    }

    // --- Sons (SFX) ---
    public void ToggleSound()
    {
        bool isOn = PlayerPrefs.GetInt("Sound", 1) == 1;
        PlayerPrefs.SetInt("Sound", isOn ? 0 : 1);
        PlayerPrefs.Save();
        
        UpdateAudioVibrationUI();
        ApplySoundSetting();
    }

    private void ApplySoundSetting()
    {
        bool soundOn = PlayerPrefs.GetInt("Sound", 1) == 1;
        // On modifie le volume global de la caméra pour tous les sons (explosions, tirs, etc.)
        // La musique ne sera pas affectée car on lui a dit d'ignorer ce volume global !
        AudioListener.volume = soundOn ? 1f : 0f;
    }

    // --- Musique ---
    public void ToggleMusic()
    {
        bool isOn = PlayerPrefs.GetInt("Music", 1) == 1;
        PlayerPrefs.SetInt("Music", isOn ? 0 : 1);
        PlayerPrefs.Save();
        
        UpdateAudioVibrationUI();
        ApplyMusicSetting();
    }

    private void ApplyMusicSetting()
    {
        bool musicOn = PlayerPrefs.GetInt("Music", 1) == 1;
        if (UIManager.instance != null && UIManager.instance.musicSource != null)
        {
            UIManager.instance.musicSource.mute = !musicOn;
            // IMPORTANT : On dit à la musique d'ignorer la baisse de volume global (AudioListener.volume)
            // Ainsi, si les "Sons" sont OFF (AudioListener.volume = 0), la musique continuera de jouer !
            UIManager.instance.musicSource.ignoreListenerVolume = true;
        }
    }

    // --- Vibration ---
    public void ToggleVibration()
    {
        bool isOn = PlayerPrefs.GetInt("Vibration", 1) == 1;
        PlayerPrefs.SetInt("Vibration", isOn ? 0 : 1);
        PlayerPrefs.Save();
        
        UpdateAudioVibrationUI();

        // Petite vibration de test quand on l'active dans les paramètres
        if (PlayerPrefs.GetInt("Vibration", 1) == 1)
        {
            Handheld.Vibrate();
        }
    }

    // Méthode publique statique à appeler depuis les autres scripts (ex: quand le joueur se fait toucher)
    public static void PlayVibration()
    {
        if (PlayerPrefs.GetInt("Vibration", 1) == 1)
        {
            Handheld.Vibrate();
        }
    }

    // --- Évaluez-nous ---
    public void RateUs()
    {
        // Remplace par le vrai nom de ton package Play Store (ex: com.TonStudio.TonJeu)
        Application.OpenURL("market://details?id=com.Sadouzz.tooClose");
    }

    // --- UI Texts Update ---
    private void UpdateAudioVibrationUI()
    {
        if (soundText != null)
            soundText.text = PlayerPrefs.GetInt("Sound", 1) == 1 ? "Sons : ON" : "Sons : OFF";
            
        if (musicText != null)
            musicText.text = PlayerPrefs.GetInt("Music", 1) == 1 ? "Musique : ON" : "Musique : OFF";
            
        if (vibrationText != null)
            vibrationText.text = PlayerPrefs.GetInt("Vibration", 1) == 1 ? "Vibration : ON" : "Vibration : OFF";
    }

    public void DeleteAllPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        SceneManager.LoadScene(0);
    }
}