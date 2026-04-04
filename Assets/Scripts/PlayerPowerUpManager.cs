using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerPowerUpManager : MonoBehaviour
{
    public static PlayerPowerUpManager instance;

    [Header("Input Settings")]
    [SerializeField] private float doubleTapTimeThreshold = 0.3f;
    private float lastTapTime = 0f;

    [Header("PowerUp States")]
    public bool isShieldActive = false;
    public bool isBlazeActive = false;

    [Header("Visual Effects (Child Objects)")]
    public GameObject shieldEffectObject;
    public GameObject blazeEffectObject;

    [Header("Blaze Settings")]
    public float blazeRotationSpeed = 360f;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Reset();
    }

    public void Reset()
    {
        if (shieldEffectObject) shieldEffectObject.SetActive(false);
        if (blazeEffectObject) blazeEffectObject.SetActive(false);
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

        if (storedPowerUp == "Shield")
        {
            ActivateShield(10f);
            PowerUpUIManager.instance.ClearStoredPowerUp(); // Vide le slot UI
        }
        else if (storedPowerUp == "Blaze")
        {
            ActivateBlaze(10f);
            PowerUpUIManager.instance.ClearStoredPowerUp(); // Vide le slot UI
        }
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

    // --- GESTION DES COLLISIONS ---
    public void HandleImpact(Collider2D missileCollider, GameObject missileObject)
    {
        // 1. Priorité Blaze (on vérifie si le missile touche spécifiquement l'objet qui tourne)
        if (isBlazeActive)
        {
            if (CheckSpecificColliderImpact(missileCollider, blazeEffectObject))
            {
                DestroyMissile(missileObject);
                return;
            }
        }

        // 2. Bouclier (si actif, n'importe quel impact sur le joueur détruit le missile)
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
        // On récupère le script du missile pour déclencher sa propre explosion
        MissileScript ms = missile.GetComponent<MissileScript>();
        if (ms != null) ms.HandleDestruction(true);
    }

    private void TakeDamage()
    {
        if (Inventory.instance != null) Inventory.instance.DieProcess();
    }
}