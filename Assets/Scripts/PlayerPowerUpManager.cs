using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.UI;
using TMPro;

public class PlayerPowerUpManager : MonoBehaviour
{
    public static PlayerPowerUpManager instance;

    [Header("Input Settings")]
    [SerializeField] private float doubleTapTimeThreshold = 0.3f;
    private float lastTapTime = 0f;

    [Header("PowerUp States")]
    public bool isShieldActive = false;
    public bool isBlazeActive = false;
    public bool isSlowMoActive = false;
    public bool isZoomActive = false;

    [Header("Timers (Cumulables)")]
    private float shieldTimer = 0f;
    private float blazeTimer = 0f;
    private float slowMoTimer = 0f;
    private float zoomTimer = 0f;

    [Header("UI Settings")]
    public Slider powerUpSlider;
    public GameObject sliderParent;

    // --- NOUVEAU : Le texte pour afficher le nom ---
    public TextMeshProUGUI powerUpNameText; // Remplace "Text" par "TMPro.TextMeshProUGUI" si tu utilises TextMeshPro

    private string activeSliderPowerUp = ""; // Garde en mémoire quel pouvoir le Slider doit afficher

    [Header("Visual Effects (Child Objects)")]
    public GameObject shieldEffectObject;
    public GameObject blazeEffectObject;
    public GameObject slowMoEffectObject;

    [Header("Blaze Settings")]
    public float blazeRotationSpeed = 360f;

    [Header("Slow Motion Settings")]
    public float slowMoFactor = 0.5f;

    [Header("Cinemachine Zoom Settings")]
    public CinemachineCamera virtualCamera;
    public float zoomOutLensSize = 15f;
    public float normalLensSize = 10f;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Reset();

        if (virtualCamera != null)
        {
            normalLensSize = virtualCamera.Lens.OrthographicSize;
        }
        else
        {
            Debug.LogWarning("PlayerPowerUpManager : Aucune Virtual Camera n'est assignée !");
        }
    }

    public void Reset()
    {
        if (shieldEffectObject) shieldEffectObject.SetActive(false);
        if (blazeEffectObject) blazeEffectObject.SetActive(false);
        if (slowMoEffectObject) slowMoEffectObject.SetActive(false);

        isShieldActive = false;
        isBlazeActive = false;
        isSlowMoActive = false;
        isZoomActive = false;

        shieldTimer = 0f;
        blazeTimer = 0f;
        slowMoTimer = 0f;
        zoomTimer = 0f;
        activeSliderPowerUp = "";

        if (sliderParent != null) sliderParent.SetActive(false);

        // --- NOUVEAU : On vide le texte au reset ---
        if (powerUpNameText != null) powerUpNameText.text = "";
    }

    private void Update()
    {
        DetectDoubleTap();

        if (isBlazeActive && blazeEffectObject != null)
        {
            blazeEffectObject.transform.Rotate(0, 0, blazeRotationSpeed * Time.deltaTime, Space.Self);
        }

        // Met à jour la jauge UI en temps réel
        UpdateSliderUI();
    }

    private void DetectDoubleTap()
    {
        if (Pointer.current == null) return;

        if (Pointer.current.press.wasPressedThisFrame)
        {
            float timeSinceLastTap = Time.time - lastTapTime;

            if (timeSinceLastTap <= doubleTapTimeThreshold)
            {
                OnDoubleTapPerformed();
                lastTapTime = 0f;
            }
            else
            {
                lastTapTime = Time.time;
            }
        }
    }

    private void OnDoubleTapPerformed()
    {
        string storedPowerUp = PowerUpUIManager.instance.GetStoredPowerUpName();

        if (string.IsNullOrEmpty(storedPowerUp)) return;

        switch (storedPowerUp)
        {
            case "Shield": ActivateShield(10f); break;
            case "Blaze": ActivateBlaze(10f); break;
            case "SlowMo": ActivateSlowMo(8f); break;
            case "Zoom": ActivateZoom(8f); break;
        }

        PowerUpUIManager.instance.ClearStoredPowerUp();
    }

    // --- LOGIQUE DU SLIDER UI ---
    private void UpdateSliderUI()
    {
        if (powerUpSlider == null || sliderParent == null) return;

        float timeToShow = 0f;

        // On regarde le timer du pouvoir actuellement "suivi" par l'UI
        switch (activeSliderPowerUp)
        {
            case "Shield": timeToShow = shieldTimer; break;
            case "Blaze": timeToShow = blazeTimer; break;
            case "SlowMo": timeToShow = slowMoTimer; break;
            case "Zoom": timeToShow = zoomTimer; break;
        }

        if (timeToShow > 0)
        {
            sliderParent.SetActive(true);
            powerUpSlider.value = timeToShow;
        }
        else
        {
            // Si le pouvoir suivi est terminé, on cache le Slider
            sliderParent.SetActive(false);
            activeSliderPowerUp = "";

            // --- NOUVEAU : On vide le texte ---
            if (powerUpNameText != null) powerUpNameText.text = "";
        }
    }

    private void SetupSlider(string powerUpName, float newTotalTime)
    {
        activeSliderPowerUp = powerUpName;

        if (powerUpSlider != null) powerUpSlider.maxValue = newTotalTime;

        // --- NOUVEAU : On met à jour le texte affiché ---
        if (powerUpNameText != null) powerUpNameText.text = powerUpName;
    }

    // --- LOGIQUE SHIELD ---
    public void ActivateShield(float duration)
    {
        shieldTimer += duration;
        SetupSlider("Shield", shieldTimer);

        if (!isShieldActive) StartCoroutine(ShieldRoutine());
    }

    private IEnumerator ShieldRoutine()
    {
        isShieldActive = true;
        if (shieldEffectObject) shieldEffectObject.SetActive(true);

        while (shieldTimer > 0)
        {
            shieldTimer -= Time.deltaTime;
            yield return null;
        }

        if (shieldEffectObject) shieldEffectObject.SetActive(false);
        isShieldActive = false;
    }

    // --- LOGIQUE BLAZE ---
    public void ActivateBlaze(float duration)
    {
        blazeTimer += duration;
        SetupSlider("Blaze", blazeTimer);

        if (!isBlazeActive) StartCoroutine(BlazeRoutine());
    }

    private IEnumerator BlazeRoutine()
    {
        isBlazeActive = true;
        if (blazeEffectObject) blazeEffectObject.SetActive(true);
        if (blazeEffectObject) blazeEffectObject.transform.localRotation = Quaternion.identity;

        while (blazeTimer > 0)
        {
            blazeTimer -= Time.deltaTime;
            yield return null;
        }

        if (blazeEffectObject) blazeEffectObject.SetActive(false);
        isBlazeActive = false;
    }

    // --- LOGIQUE SLOW MOTION ---
    public void ActivateSlowMo(float duration)
    {
        slowMoTimer += duration;
        SetupSlider("SlowMo", slowMoTimer);

        if (!isSlowMoActive) StartCoroutine(SlowMoRoutine());
    }

    private IEnumerator SlowMoRoutine()
    {
        isSlowMoActive = true;
        if (slowMoEffectObject) slowMoEffectObject.SetActive(true);

        while (slowMoTimer > 0)
        {
            slowMoTimer -= Time.deltaTime;
            yield return null;
        }

        if (slowMoEffectObject) slowMoEffectObject.SetActive(false);
        isSlowMoActive = false;
    }

    // --- LOGIQUE ZOOM (LOUPE) ---
    public void ActivateZoom(float duration)
    {
        if (virtualCamera == null) return;

        zoomTimer += duration;
        SetupSlider("Zoom", zoomTimer);

        if (!isZoomActive)
        {
            StartCoroutine(ZoomRoutine());
        }
        else if (CameraShake.instance != null)
        {
            CameraShake.instance.Shake(0.15f, 1.5f);
        }
    }

    private IEnumerator ZoomRoutine()
    {
        isZoomActive = true;

        if (CameraShake.instance != null) CameraShake.instance.Shake(0.3f, 3f);

        while (zoomTimer > 0)
        {
            virtualCamera.Lens.OrthographicSize = Mathf.Lerp(virtualCamera.Lens.OrthographicSize, zoomOutLensSize, Time.deltaTime * 10f);
            zoomTimer -= Time.deltaTime;
            yield return null;
        }

        while (Mathf.Abs(virtualCamera.Lens.OrthographicSize - normalLensSize) > 0.1f)
        {
            virtualCamera.Lens.OrthographicSize = Mathf.Lerp(virtualCamera.Lens.OrthographicSize, normalLensSize, Time.deltaTime * 8f);
            yield return null;
        }

        virtualCamera.Lens.OrthographicSize = normalLensSize;
        isZoomActive = false;
    }

    // --- GESTION DES COLLISIONS ---
    public void HandleImpact(Collider2D missileCollider, GameObject missileObject)
    {
        if (isBlazeActive && CheckSpecificColliderImpact(missileCollider, blazeEffectObject))
        {
            DestroyMissile(missileObject);
            return;
        }

        if (isShieldActive)
        {
            DestroyMissile(missileObject);
            return;
        }

        TakeDamage();
    }

    private bool CheckSpecificColliderImpact(Collider2D missileCollider, GameObject container)
    {
        if (container == null) return false;
        Collider2D[] cols = container.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in cols)
        {
            if (col.enabled && col.bounds.Intersects(missileCollider.bounds)) return true;
        }
        return false;
    }

    private void DestroyMissile(GameObject missile)
    {
        MissileScript ms = missile.GetComponent<MissileScript>();
        if (ms != null) ms.HandleDestruction(true);
    }

    private void TakeDamage()
    {
        if (Inventory.instance != null) Inventory.instance.DieProcess();
    }
}