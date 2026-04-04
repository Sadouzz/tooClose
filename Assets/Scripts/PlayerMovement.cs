
using UnityEngine.InputSystem;
using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public Joystick joystick;
    public float speed, rotationSpeed;
    public Rigidbody2D rb;

    [Header("Movement Settings")]
    public bool isLateralMode; // false = Joystick, true = Lateral

    [Header("Juice Settings")]
    public float maxTiltAngle = 20f; // L'angle max de l'inclinaison
    public float tiltSpeed = 10f;    // La vitesse à laquelle il s'incline

    public SpriteRenderer sr;
    public BoxCollider2D bc;
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

    // Appelle cette fonction si tu changes de mode en plein jeu (via le menu settings)
    public void RefreshMovementMode()
    {
        // 0 = Joystick, 1 = Lateral (doit correspondre à ton SettingsScript)
        isLateralMode = PlayerPrefs.GetInt("MovementMode", 0) == 1;

        // Optionnel : Cacher/Afficher le joystick visuellement selon le mode
        if (joystick != null)
            joystick.gameObject.SetActive(!isLateralMode);
    }

    void Update()
    {
        if(!Inventory.instance.dead) transform.position += transform.up * speed * Time.deltaTime;
        if (!move) return;

        if (isLateralMode)
        {
            HandleLateralMovement();
        }
        else
        {
            HandleJoystickMovement();
        }

        ApplyVisualTilt();

        
        // Gestion de l'invincibilité
        bc.enabled = !isInvincible;
    }

    void ApplyVisualTilt()
    {
        float tiltTarget = 0f;

        if (isLateralMode)
        {
            // En mode latéral, on check la pression de l'écran
            if (Pointer.current != null && Pointer.current.press.isPressed)
            {
                float xPos = Pointer.current.position.ReadValue().x;
                tiltTarget = (xPos < Screen.width / 2f) ? maxTiltAngle : -maxTiltAngle;
            }
        }
        else
        {
            // En mode Joystick, on utilise l'axe horizontal du joystick
            tiltTarget = -joystick.Horizontal * maxTiltAngle;
        }

        // On applique une rotation locale au SpriteRenderer (sur l'axe Y pour un effet de 3D/Tilt)
        // Si ton jeu est en pure 2D, tu peux aussi jouer sur le scale.x ou une légère rotation Y
        Quaternion targetRotation = Quaternion.Euler(0, tiltTarget, 0);
        sr.transform.localRotation = Quaternion.Lerp(sr.transform.localRotation, targetRotation, tiltSpeed * Time.deltaTime);
    }

    // --- LOGIQUE JOYSTICK (Ton code original) ---
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
        // On décale l'angle cible vers la gauche
        targetLateralAngle += rotationSpeed * Time.deltaTime * 50f;
    }

    void MoveRight()
    {
        // On décale l'angle cible vers la droite
        targetLateralAngle -= rotationSpeed * Time.deltaTime * 50f;
    }

    // Dans HandleLateralMovement, on applique le lissage à la fin
    void HandleLateralMovement()
    {
        

        if (Pointer.current != null && Pointer.current.press.isPressed)
        {
            float xPos = Pointer.current.position.ReadValue().x;
            if (xPos < Screen.width / 2f) MoveLeft();
            else MoveRight();
        }

        // On applique le lissage EXACTEMENT comme dans ton code Joystick
        float smoothAngle = Mathf.LerpAngle(
            transform.eulerAngles.z,
            targetLateralAngle,
            rotationSpeed * Time.deltaTime // Utilise la même puissance de rotation
        );

        transform.rotation = Quaternion.Euler(0f, 0f, smoothAngle);
    }


    // --- RESTE DE TES FONCTIONS ---
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