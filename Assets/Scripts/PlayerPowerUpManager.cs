using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.Cinemachine; // Assure-toi d'avoir le package Cinemachine installé

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

    [Header("Visual Effects (Child Objects)")]
    public GameObject shieldEffectObject;
    public GameObject blazeEffectObject;
    public GameObject slowMoEffectObject;

    [Header("Blaze Settings")]
    public float blazeRotationSpeed = 360f;

    [Header("Slow Motion Settings")]
    public float slowMoFactor = 0.5f; // Multiplicateur de vitesse des missiles (ex: 0.5 = 50% plus lent)

    [Header("Cinemachine Zoom Settings")]
    public CinemachineCamera virtualCamera; // Glisse ta Virtual Camera ici depuis l'inspecteur
    public float zoomOutLensSize = 8f;
    private float normalLensSize = 5f;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Reset();

        // On sauvegarde la taille de base de la caméra au lancement
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
    }

    private void Update()
    {
        DetectDoubleTap();

        if (isBlazeActive && blazeEffectObject != null)
        {
            blazeEffectObject.transform.Rotate(0, 0, blazeRotationSpeed * Time.deltaTime, Space.Self);
        }
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
        // On demande à l'UI quel PowerUp est actuellement stocké
        string storedPowerUp = PowerUpUIManager.instance.GetStoredPowerUpName();

        if (string.IsNullOrEmpty(storedPowerUp)) return;

        switch (storedPowerUp)
        {
            case "Shield":
                ActivateShield(10f);
                break;
            case "Blaze":
                ActivateBlaze(10f);
                break;
            case "SlowMo":
                ActivateSlowMo(8f);
                break;
            case "Zoom":
                ActivateZoom(8f);
                break;
        }

        // Vide le slot UI après utilisation
        PowerUpUIManager.instance.ClearStoredPowerUp();
    }

    // --- LOGIQUE SHIELD ---
    public void ActivateShield(float duration)
    {
        if (isShieldActive) return;
        StartCoroutine(ShieldRoutine(duration));
    }

    private IEnumerator ShieldRoutine(float duration)
    {
        isShieldActive = true;
        if (shieldEffectObject) shieldEffectObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        if (shieldEffectObject) shieldEffectObject.SetActive(false);
        isShieldActive = false;
    }

    // --- LOGIQUE BLAZE ---
    public void ActivateBlaze(float duration)
    {
        if (isBlazeActive) return;
        StartCoroutine(BlazeRoutine(duration));
    }

    private IEnumerator BlazeRoutine(float duration)
    {
        isBlazeActive = true;
        if (blazeEffectObject) blazeEffectObject.SetActive(true);
        if (blazeEffectObject) blazeEffectObject.transform.localRotation = Quaternion.identity;

        yield return new WaitForSeconds(duration);

        if (blazeEffectObject) blazeEffectObject.SetActive(false);
        isBlazeActive = false;
    }

    // --- LOGIQUE SLOW MOTION ---
    public void ActivateSlowMo(float duration)
    {
        if (isSlowMoActive) return;
        StartCoroutine(SlowMoRoutine(duration));
    }

    private IEnumerator SlowMoRoutine(float duration)
    {
        isSlowMoActive = true;
        if (slowMoEffectObject) slowMoEffectObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        if (slowMoEffectObject) slowMoEffectObject.SetActive(false);
        isSlowMoActive = false;
    }

    // --- LOGIQUE ZOOM (LOUPE) ---
    public void ActivateZoom(float duration)
    {
        if (virtualCamera == null) return;

        StopCoroutine("ZoomRoutine"); // Évite les conflits si activé plusieurs fois de suite
        StartCoroutine(ZoomRoutine(duration));
    }

    private IEnumerator ZoomRoutine(float duration)
    {
        float transitionTime = 0.5f;
        float elapsed = 0;

        // Dézoom progressif (transition fluide)
        while (elapsed < transitionTime)
        {
            virtualCamera.Lens.OrthographicSize = Mathf.Lerp(normalLensSize, zoomOutLensSize, elapsed / transitionTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        virtualCamera.Lens.OrthographicSize = zoomOutLensSize; // S'assure d'atteindre la valeur exacte

        // Maintien du dézoom pendant la durée du bonus
        yield return new WaitForSeconds(duration);

        // Re-zoom progressif pour revenir à la normale
        elapsed = 0;
        while (elapsed < transitionTime)
        {
            virtualCamera.Lens.OrthographicSize = Mathf.Lerp(zoomOutLensSize, normalLensSize, elapsed / transitionTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        virtualCamera.Lens.OrthographicSize = normalLensSize; // Retour parfait à la valeur initiale
    }

    // --- GESTION DES COLLISIONS ---
    public void HandleImpact(Collider2D missileCollider, GameObject missileObject)
    {
        // 1. Priorité Blaze
        if (isBlazeActive && CheckSpecificColliderImpact(missileCollider, blazeEffectObject))
        {
            DestroyMissile(missileObject);
            return;
        }

        // 2. Bouclier
        if (isShieldActive)
        {
            DestroyMissile(missileObject);
            return;
        }

        // 3. Sinon, dégâts normaux
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
        // À adapter si tu as renommé Inventory
        if (Inventory.instance != null) Inventory.instance.DieProcess();
    }
}