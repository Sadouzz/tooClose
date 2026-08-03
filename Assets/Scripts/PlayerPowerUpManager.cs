using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization.Settings;

public class PlayerPowerUpManager : MonoBehaviour
{
    public static PlayerPowerUpManager instance;

    [Header("Localization")]
    public string stringTableName = "UITexts";

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

    private string activeSliderPowerUp = ""; // Garde en m�moire quel pouvoir le Slider doit afficher

    [Header("Visual Effects (Child Objects)")]
    public GameObject shieldEffectObject;
    public GameObject blazeEffectObject;
    public GameObject slowMoEffectObject;

    [Header("Audio Settings")]
    [Tooltip("Son joué à l'activation de n'importe quel PowerUp")]
    public AudioClip powerUpActivationSound;
    public float powerUpActivationVolume = 0.8f;

    [Header("Wave Visual Settings")]
    [Tooltip("L'image de l'onde de choc (utilisée pour EMP et Ralenti)")]
    public Sprite empWaveSprite;

    [Header("Blaze Settings")]
    public float blazeRotationSpeed = 360f;

    [Header("Slow Motion Settings")]
    public float slowMoFactor = 0.5f;

    [Header("Cinemachine Zoom Settings")]
    public CinemachineCamera virtualCamera;
    public float zoomOutLensSize = 15f;
    public float normalLensSize = 10f;

    public int usedPowersCount;

    private Vector3 shieldOriginalScale = Vector3.zero;
    private Vector3 blazeOriginalScale = Vector3.zero;
    private AudioSource audioSource;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // Son 2D
    }

    private void Start()
    {
        if (shieldEffectObject) shieldOriginalScale = shieldEffectObject.transform.localScale;
        if (blazeEffectObject) blazeOriginalScale = blazeEffectObject.transform.localScale;
        
        Reset();

        if (virtualCamera != null)
        {
            normalLensSize = virtualCamera.Lens.OrthographicSize;
        }
        else
        {
            Debug.LogWarning("PlayerPowerUpManager : Aucune Virtual Camera n'est assign�e !");
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
        usedPowersCount = 0;

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

        // Met � jour la jauge UI en temps r�el
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

        // Jouer le son global d'activation de PowerUp
        if (powerUpActivationSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(powerUpActivationSound, powerUpActivationVolume);
        }

        switch (storedPowerUp)
        {
            case "Shield": ActivateShield(10f); break;
            case "Blaze": ActivateBlaze(10f); break;
            case "SlowMo": ActivateSlowMo(8f); break;
            case "Zoom": ActivateZoom(8f); break;
            case "EMP": ActivateEMP(); break;
        }
        usedPowersCount++;
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
            // Si le pouvoir suivi est termin�, on cache le Slider
            sliderParent.SetActive(false);
            activeSliderPowerUp = "";

            // --- NOUVEAU : On vide le texte ---
            if (powerUpNameText != null) powerUpNameText.text = "";
        }
    }

    private string GetTranslation(string key, string fallback)
    {
        string tr = LocalizationSettings.StringDatabase.GetLocalizedString(stringTableName, key);
        if (string.IsNullOrEmpty(tr) || tr.Contains("No translation")) return fallback;
        return tr;
    }

    private void SetupSlider(string powerUpName, float newTotalTime)
    {
        activeSliderPowerUp = powerUpName;

        if (powerUpSlider != null) powerUpSlider.maxValue = newTotalTime;

        // --- NOUVEAU : On met à jour le texte affiché avec la localisation ---
        if (powerUpNameText != null) 
        {
            string locKey = "";
            string fallback = powerUpName;
            
            switch (powerUpName)
            {
                case "Shield": locKey = "BOUCLIER"; fallback = "Bouclier"; break;
                case "Blaze": locKey = "FLAMMES"; fallback = "Flammes"; break;
                case "SlowMo": locKey = "SLOWMO"; fallback = "Ralenti"; break;
                case "Zoom": locKey = "ZOOM"; fallback = "Zoom"; break;
                case "EMP": locKey = "EMP"; fallback = "EMP"; break;
            }
            
            if (!string.IsNullOrEmpty(locKey))
            {
                powerUpNameText.text = GetTranslation(locKey, fallback);
            }
            else
            {
                powerUpNameText.text = powerUpName;
            }
        }
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
        if (shieldEffectObject)
        {
            shieldEffectObject.SetActive(true);
            shieldEffectObject.transform.localScale = Vector3.zero;
        }

        float scaleDuration = 0.5f;
        float elapsed = 0f;

        while (shieldTimer > 0)
        {
            shieldTimer -= Time.deltaTime;

            if (shieldEffectObject && elapsed < scaleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / scaleDuration);
                // Ease out cubic
                float easeT = 1f - Mathf.Pow(1f - t, 3f);
                shieldEffectObject.transform.localScale = Vector3.Lerp(Vector3.zero, shieldOriginalScale, easeT);
            }

            yield return null;
        }

        if (shieldEffectObject)
        {
            shieldEffectObject.SetActive(false);
            shieldEffectObject.transform.localScale = shieldOriginalScale;
        }
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
        if (blazeEffectObject)
        {
            blazeEffectObject.SetActive(true);
            blazeEffectObject.transform.localScale = Vector3.zero;
            blazeEffectObject.transform.localRotation = Quaternion.identity;
        }

        float scaleDuration = 0.5f;
        float elapsed = 0f;

        while (blazeTimer > 0)
        {
            blazeTimer -= Time.deltaTime;

            if (blazeEffectObject && elapsed < scaleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / scaleDuration);
                // Ease out cubic
                float easeT = 1f - Mathf.Pow(1f - t, 3f);
                blazeEffectObject.transform.localScale = Vector3.Lerp(Vector3.zero, blazeOriginalScale, easeT);
            }

            yield return null;
        }

        if (blazeEffectObject)
        {
            blazeEffectObject.SetActive(false);
            blazeEffectObject.transform.localScale = blazeOriginalScale;
        }
        isBlazeActive = false;
    }

    // --- LOGIQUE SLOW MOTION ---
    public void ActivateSlowMo(float duration)
    {
        slowMoTimer += duration;
        SetupSlider("SlowMo", slowMoTimer);

        StartCoroutine(PowerUpWaveRoutine());

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

    // --- LOGIQUE EMP ---
    public void ActivateEMP()
    {
        // 1. Feedback visuel et sonore global
        if (CameraShake.instance != null)
        {
            CameraShake.instance.Shake(0.5f, 5f); // Un gros shake pour l'impact
        }
        // Vibration si activé
        SettingsScript.PlayVibration();

        // Affichage du nom
        if (powerUpNameText != null)
        {
            powerUpNameText.text = GetTranslation("EMP", "EMP");
            StartCoroutine(ClearEMPTextRoutine());
        }

        // Effet visuel d'onde de choc (cercle qui s'agrandit)
        StartCoroutine(PowerUpWaveRoutine());

        // 2. Trouver toutes les cibles potentielles
        System.Collections.Generic.List<Transform> allTargets = new System.Collections.Generic.List<Transform>();

        GameObject[] missiles = GameObject.FindGameObjectsWithTag("Missile");
        GameObject[] rammers = GameObject.FindGameObjectsWithTag("Rammer");
        GameObject[] trackers = GameObject.FindGameObjectsWithTag("Tracker");
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (var m in missiles) allTargets.Add(m.transform);
        foreach (var r in rammers) allTargets.Add(r.transform);
        foreach (var t in trackers) allTargets.Add(t.transform);
        foreach (var e in enemies) allTargets.Add(e.transform);

        if (allTargets.Count <= 1) return; 

        // 3. Déstabiliser ceux qui traquent
        foreach (GameObject mObj in missiles)
        {
            MissileScript m = mObj.GetComponent<MissileScript>();
            if (m != null)
            {
                Transform newTarget = GetRandomTarget(allTargets, m.transform);
                if (newTarget != null) m.Destabilize(newTarget);
            }
        }

        foreach (GameObject rObj in rammers)
        {
            Rammer r = rObj.GetComponent<Rammer>();
            if (r != null)
            {
                Transform newTarget = GetRandomTarget(allTargets, r.transform);
                if (newTarget != null) r.Destabilize(newTarget);
            }
        }

        foreach (GameObject tObj in trackers)
        {
            Tracker t = tObj.GetComponent<Tracker>();
            if (t != null)
            {
                Transform newTarget = GetRandomTarget(allTargets, t.transform);
                if (newTarget != null) t.Destabilize(newTarget);
            }
        }
    }

    private IEnumerator PowerUpWaveRoutine()
    {
        GameObject waveObj = new GameObject("PowerUpWave");
        Transform playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform != null)
            waveObj.transform.position = playerTransform.position;
        else
            waveObj.transform.position = Vector3.zero;

        Color startColor = new Color(0f, 1f, 1f, 0.8f); // Cyan transparent
        Color endColor = new Color(0f, 1f, 1f, 0f);

        float duration = 1.5f; // Plus lent (était 0.8f)
        float maxRadius = 35f; // Assez grand pour sortir de l'écran (zoom max out)
        float elapsed = 0f;

        if (empWaveSprite != null)
        {
            // --- LOGIQUE AVEC IMAGE (SPRITE) ---
            SpriteRenderer sr = waveObj.AddComponent<SpriteRenderer>();
            sr.sprite = empWaveSprite;
            sr.color = startColor;
            waveObj.transform.localScale = Vector3.zero;

            // On s'assure qu'il passe au dessus du fond
            sr.sortingOrder = 50;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easeT = Mathf.Sqrt(t);
                
                float currentScale = Mathf.Lerp(0f, maxRadius, easeT);
                waveObj.transform.localScale = new Vector3(currentScale, currentScale, 1f);
                
                sr.color = Color.Lerp(startColor, endColor, t);
                yield return null;
            }
        }
        else
        {
            // --- LOGIQUE LINERENDERER (Par défaut) ---
            LineRenderer lr = waveObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            
            int segments = 100;
            lr.positionCount = segments + 1;
            lr.startWidth = 0.8f;
            lr.endWidth = 0.8f;
            
            Material mat = new Material(Shader.Find("Sprites/Default"));
            lr.material = mat;
            lr.sortingOrder = 50;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easeT = Mathf.Sqrt(t);
                float currentRadius = Mathf.Lerp(0f, maxRadius, easeT);
                
                Color currentColor = Color.Lerp(startColor, endColor, t);
                lr.startColor = currentColor;
                lr.endColor = currentColor;

                float angle = 0f;
                for (int i = 0; i < (segments + 1); i++)
                {
                    float x = Mathf.Sin(angle) * currentRadius;
                    float y = Mathf.Cos(angle) * currentRadius;
                    lr.SetPosition(i, new Vector3(x, y, 0f));
                    angle += (2f * Mathf.PI) / segments;
                }

                yield return null;
            }
        }

        Destroy(waveObj);
    }

    private IEnumerator ClearEMPTextRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        if (string.IsNullOrEmpty(activeSliderPowerUp) && powerUpNameText != null)
        {
            powerUpNameText.text = "";
        }
    }

    private Transform GetRandomTarget(System.Collections.Generic.List<Transform> targets, Transform self)
    {
        int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            int rnd = Random.Range(0, targets.Count);
            if (targets[rnd] != self && targets[rnd] != null)
            {
                return targets[rnd];
            }
        }
        return null;
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