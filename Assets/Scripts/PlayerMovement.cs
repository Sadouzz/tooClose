
using UnityEngine.InputSystem;
using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public enum GamePhase { Dodging, Shooting }

    [Header("Game Phase")]
    public GamePhase currentPhase = GamePhase.Dodging;

    public Joystick joystick;
    public float speed, rotationSpeed;
    public Rigidbody2D rb;

    [Header("Movement Settings")]
    public bool isLateralMode; // false = Joystick, true = Lateral

    [Header("Juice Settings")]
    public float maxTiltAngle = 20f;
    public float tiltSpeed = 10f;

    [Header("Shooter Mode Settings (Acecraft)")]
    public float shooterStrafeSpeed = 15f; // Vitesse de déplacement gauche/droite
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.15f;
    private float nextFireTime = 0f;

    public SpriteRenderer sr;
    public BoxCollider2D bc;
    public int life;
    public GameObject smoke;
    public bool move;
    bool isInvincible;
    private float targetLateralAngle;

    public static PlayerMovement instance;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        targetLateralAngle = transform.eulerAngles.z;
        RefreshMovementMode();
    }

    public void RefreshMovementMode()
    {
        isLateralMode = PlayerPrefs.GetInt("MovementMode", 0) == 1;

        if (joystick != null)
            joystick.gameObject.SetActive(!isLateralMode);
    }

    void Update()
    {
        // Avancée constante vers le "haut" local du vaisseau
        if (!Inventory.instance.dead)
        {
            transform.position += transform.up * speed * Time.deltaTime;
        }

        if (!move) return;

        // --- MACHINE À ÉTATS ---
        if (currentPhase == GamePhase.Dodging)
        {
            HandleJoystickMovement();
        }
        else if (currentPhase == GamePhase.Shooting)
        {
            HandleShooterMovement();
            HandleShooting();
        }

        // Le Juice (inclinaison) fonctionne dans les deux modes !
        ApplyVisualTilt();

        bc.enabled = !isInvincible;
    }

    void ApplyVisualTilt()
    {
        float tiltTarget = 0f;

        if (isLateralMode)
        {
            if (Pointer.current != null && Pointer.current.press.isPressed)
            {
                float xPos = Pointer.current.position.ReadValue().x;
                tiltTarget = (xPos < Screen.width / 2f) ? maxTiltAngle : -maxTiltAngle;
            }
        }
        else
        {
            tiltTarget = -joystick.Horizontal * maxTiltAngle;
        }

        Quaternion targetRotation = Quaternion.Euler(0, tiltTarget, 0);
        sr.transform.localRotation = Quaternion.Lerp(sr.transform.localRotation, targetRotation, tiltSpeed * Time.deltaTime);
    }

    // ==========================================
    // PHASE 1 : MODE DODGE (Ton code original)
    // ==========================================
    void HandleJoystickMovement()
    {
        Vector2 direction = new Vector2(joystick.Horizontal, joystick.Vertical);

        if (direction.magnitude > 0.3f)
        {
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            float smoothAngle = Mathf.LerpAngle(
                transform.eulerAngles.z,
                targetAngle,
                rotationSpeed * Time.deltaTime
            );
            transform.rotation = Quaternion.Euler(0f, 0f, smoothAngle);
        }
    }

    void MoveLeft()
    {
        targetLateralAngle += rotationSpeed * Time.deltaTime * 50f;
    }

    void MoveRight()
    {
        targetLateralAngle -= rotationSpeed * Time.deltaTime * 50f;
    }

    void HandleLateralMovement()
    {
        if (Pointer.current != null && Pointer.current.press.isPressed)
        {
            float xPos = Pointer.current.position.ReadValue().x;
            if (xPos < Screen.width / 2f) MoveLeft();
            else MoveRight();
        }

        float smoothAngle = Mathf.LerpAngle(
            transform.eulerAngles.z,
            targetLateralAngle,
            rotationSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.Euler(0f, 0f, smoothAngle);
    }

    // ==========================================
    // PHASE 2 : MODE SHOOTER (Façon Acecraft)
    // ==========================================
    void HandleShooterMovement()
    {
        float moveDir = 0f;

        // Récupération de l'input selon le mode
        if (isLateralMode)
        {
            if (Pointer.current != null && Pointer.current.press.isPressed)
            {
                float xPos = Pointer.current.position.ReadValue().x;
                moveDir = (xPos < Screen.width / 2f) ? -1f : 1f;
            }
        }
        else
        {
            moveDir = joystick.Horizontal; // En mode tir, on ignore le joystick.Vertical
        }

        // Déplacement strict sur l'axe X (gauche/droite)
        transform.Translate(Vector3.right * moveDir * shooterStrafeSpeed * Time.deltaTime, Space.World);

        // Verrouillage de la rotation pour s'assurer que le vaisseau pointe toujours tout droit vers le haut
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, 0), rotationSpeed * Time.deltaTime);
    }

    void HandleShooting()
    {
        if (Time.time >= nextFireTime)
        {
            if (bulletPrefab != null && firePoint != null)
            {
                // Note : Pour l'optimisation mobile, pense à passer sur de l'Object Pooling plus tard
                Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            }
            nextFireTime = Time.time + fireRate;
        }
    }

    // Appelle cette fonction depuis ton gestionnaire de score quand le palier est atteint
    public void TriggerShooterMode(float durationInSeconds)
    {
        if (currentPhase == GamePhase.Dodging)
        {
            StartCoroutine(ShooterModeRoutine(durationInSeconds));
        }
    }

    private IEnumerator ShooterModeRoutine(float duration)
    {
        // Passage en mode tir
        currentPhase = GamePhase.Shooting;

        // Attendre la durée du mode
        yield return new WaitForSeconds(duration);

        // Retour au mode esquive
        currentPhase = GamePhase.Dodging;

        // Réinitialisation de l'angle cible pour éviter un à-coup violent lors de la transition
        targetLateralAngle = transform.eulerAngles.z;
    }

    // --- UTILITAIRES ---
    void ToggleSprite() => sr.enabled = !sr.enabled;

    public IEnumerator InvincibleTiming()
    {
        isInvincible = true;
        for (int i = 0; i < 8; i++)
        {
            ToggleSprite();
            yield return new WaitForSecondsRealtime(0.2f);
        }
        sr.enabled = true;
        isInvincible = false;
    }
}