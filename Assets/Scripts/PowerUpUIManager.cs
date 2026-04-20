using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PowerUpUIManager : MonoBehaviour
{
    public static PowerUpUIManager instance;

    [Header("Flying Elements")]
    public TextMeshProUGUI nameText;
    public Image flyingIcon;

    [Header("HUD Target (In Game UI)")]
    public Image activePowerUpSlot;

    [Header("Settings - Slot Animation (No Animator)")]
    public float pulseDuration = 0.3f;
    public float pulseScaleAmount = 1.3f;

    [Header("Logic Storage")]
    private string currentStoredPowerUp = ""; // Garde en mémoire le powerup ramassé

    public AudioSource audio;

    private void Awake() => instance = this;

    private void Start()
    {
        audio = GameObject.FindGameObjectWithTag("EventSoundPickPowerUp").GetComponent<AudioSource>();
        activePowerUpSlot.enabled = false;
        activePowerUpSlot.sprite = null;
    }

    // --- FONCTIONS DE COMMUNICATION ---

    // Cette fonction permet au Player de savoir ce qu'il a en stock
    public string GetStoredPowerUpName()
    {
        return currentStoredPowerUp;
    }

    // Cette fonction vide le slot une fois le powerup utilisé
    public void ClearStoredPowerUp()
    {
        currentStoredPowerUp = "";
        activePowerUpSlot.enabled = false;
        activePowerUpSlot.sprite = null;
        activePowerUpSlot.rectTransform.localScale = Vector3.one;
    }

    // --- ANIMATION ---

    public void ShowPowerUpFeedback(string name, Sprite icon, Vector3 worldPos, Color color)
    {
        audio.Play();
        StopAllCoroutines();
        activePowerUpSlot.rectTransform.localScale = Vector3.one;
        StartCoroutine(AnimatePowerUp(name, icon, worldPos, color));
    }

    IEnumerator AnimatePowerUp(string name, Sprite icon, Vector3 worldPos, Color color)
    {
        // 1. Initialisation
        nameText.text = name;
        nameText.color = color;
        flyingIcon.sprite = icon;

        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        flyingIcon.rectTransform.position = screenPos;
        nameText.rectTransform.position = screenPos + new Vector2(0, 70f);

        nameText.gameObject.SetActive(true);
        flyingIcon.gameObject.SetActive(true);
        flyingIcon.transform.localScale = Vector3.zero;

        // 2. Pop
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 5f;
            float scale = Mathf.Sin(t * Mathf.PI * 0.8f) * 1.2f;
            flyingIcon.transform.localScale = new Vector3(scale, scale, 1);
            yield return null;
        }
        flyingIcon.transform.localScale = Vector3.one;

        yield return new WaitForSeconds(0.6f);
        nameText.gameObject.SetActive(false);

        // 3. Vol vers HUD
        t = 0;
        Vector2 startPos = flyingIcon.rectTransform.position;
        Vector2 endPos = activePowerUpSlot.rectTransform.position;

        while (t < 1)
        {
            t += Time.deltaTime * 2f;
            float easedT = t * t;
            flyingIcon.rectTransform.position = Vector2.Lerp(startPos, endPos, easedT);
            flyingIcon.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.7f, 0.7f, 1), easedT);
            yield return null;
        }

        // 4. Finalisation
        flyingIcon.gameObject.SetActive(false);

        // --- STOCKAGE DE LA LOGIQUE ---
        // On normalise le nom pour le PlayerPowerUpManager
        if (name.ToUpper().Contains("BLAZE") || name.ToUpper().Contains("INFERNO"))
            currentStoredPowerUp = "Blaze";
        else if (name.ToUpper().Contains("SHIELD") || name.ToUpper().Contains("BOUCLIER"))
            currentStoredPowerUp = "Shield";
        else if (name.ToUpper().Contains("ZOOM"))
            currentStoredPowerUp = "Zoom";
        else if (name.ToUpper().Contains("SLOWMO"))
            currentStoredPowerUp = "SlowMo";
        else
            currentStoredPowerUp = name;

        activePowerUpSlot.sprite = icon;
        activePowerUpSlot.enabled = true;

        // Pulse final
        t = 0;
        RectTransform slotTransform = activePowerUpSlot.rectTransform;
        while (t < 1)
        {
            t += Time.deltaTime / pulseDuration;
            float sinCurve = Mathf.Sin(t * Mathf.PI);
            float scaleValue = Mathf.Lerp(1f, pulseScaleAmount, sinCurve);
            slotTransform.localScale = new Vector3(scaleValue, scaleValue, 1);
            yield return null;
        }
        slotTransform.localScale = Vector3.one;
    }
}