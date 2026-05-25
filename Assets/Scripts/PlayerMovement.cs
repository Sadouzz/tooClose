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
    public float shooterStrafeSpeed = 15f; // Vitesse de deplacement gauche/droite/haut/bas
    public float maxShooterStrafeX = 3.2f; // Limite max de deplacement gauche/droite
    public float minShooterStrafeY = -4.5f; // Limite max de recul (bas de l'ecran)
    public float maxShooterStrafeY = 0.0f; // Limite max d'avancee (centre de l'ecran)
    public float transitionBlendSpeed = 3.5f; // Vitesse de transition de la camera entre phases
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

    public  Transform cameraTargetProxy;
    
    [HideInInspector]
    public float cameraProxyY;

    [HideInInspector]
    public float combatBaseX;
    
    private GamePhase lastPhase = GamePhase.Dodging;
    private float cameraTransitionTimer = 0f;
    private bool isTransitioning = false;
    private Vector3 proxyVelocity = Vector3.zero; // Pour SmoothDamp
    public float transitionSmoothTime = 0.35f;    // Duree du lissage SmoothDamp (plus petit = plus rapide)

    public static PlayerMovement instance;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        targetLateralAngle = transform.eulerAngles.z;
        RefreshMovementMode();

        // Procedural bullet prefab fallback to avoid empty reference issues
        if (bulletPrefab == null)
        {
            bulletPrefab = CreateProceduralBulletPrefab();
        }

        // Procedural fire point fallback
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.parent = this.transform;
            fp.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            firePoint = fp.transform;
        }

        FollowProxy();
    }

    public void FollowProxy(){
        // Initialize the camera follow proxy
        GameObject proxyObj = new GameObject("CameraFollowProxy");
        cameraTargetProxy = proxyObj.transform;
        cameraTargetProxy.position = transform.position;

        if (UIManager.instance != null && UIManager.instance.vcam != null)
        {
            UIManager.instance.vcam.Follow = cameraTargetProxy;
            UIManager.instance.vcam.LookAt = cameraTargetProxy;
        }
    }

    public void RefreshMovementMode()
    {
        isLateralMode = PlayerPrefs.GetInt("MovementMode", 0) == 1;

        if (joystick != null)
            joystick.gameObject.SetActive(!isLateralMode);
    }

    public void InitializeCameraProxyY()
    {
        cameraProxyY = transform.position.y + 3.0f;
        combatBaseX = transform.position.x; // Record current X coordinate when entering combat phase!
    }

    public void ResetCameraProxy()
    {
        currentPhase = GamePhase.Dodging;
        lastPhase = GamePhase.Dodging;
        isTransitioning = false;
        proxyVelocity = Vector3.zero;
        if (cameraTargetProxy != null)
        {
            cameraTargetProxy.position = transform.position;
        }
        cameraProxyY = transform.position.y;
        combatBaseX = transform.position.x;
    }

    void Update()
    {
        // Detect phase change
        if (currentPhase == GamePhase.Shooting && lastPhase == GamePhase.Dodging)
        {
            InitializeCameraProxyY();
            lastPhase = GamePhase.Shooting;
            isTransitioning = true;
            proxyVelocity = Vector3.zero; // Reset SmoothDamp velocity
            cameraTransitionTimer = 0f;   // Not used for smooth, kept for compat
        }
        else if (currentPhase == GamePhase.Dodging && lastPhase == GamePhase.Shooting)
        {
            lastPhase = GamePhase.Dodging;
            isTransitioning = true;
            proxyVelocity = Vector3.zero;
            // Reinitialisation de l'angle pour eviter un a-coup violent
            targetLateralAngle = transform.eulerAngles.z;
        }

        // Update the camera proxy target's position
        if (cameraTargetProxy != null)
        {
            Vector3 targetProxyPos;

            if (currentPhase == GamePhase.Shooting)
            {
                // En mode combat : le proxy monte a la vitesse du joueur, centre sur combatBaseX
                cameraProxyY += speed * Time.deltaTime;
                targetProxyPos = new Vector3(combatBaseX, cameraProxyY, transform.position.z);
            }
            else
            {
                // En mode esquive : le proxy COLLE directement au joueur
                // Cinemachine se charge du lissage visuel via ses propres reglages Damping
                targetProxyPos = transform.position;
            }

            if (isTransitioning)
            {
                // SmoothDamp uniquement pendant la transition de phase (pas pendant le gameplay normal)
                cameraTargetProxy.position = Vector3.SmoothDamp(
                    cameraTargetProxy.position,
                    targetProxyPos,
                    ref proxyVelocity,
                    transitionSmoothTime
                );
                // Fin de transition quand on est suffisamment proche
                if (Vector3.Distance(cameraTargetProxy.position, targetProxyPos) < 4f)
                {
                    isTransitioning = false;
                    proxyVelocity = Vector3.zero;
                    cameraTargetProxy.position = targetProxyPos; // Snap final propre
                }
            }
            else
            {
                // Hors transition : le proxy est EXACTEMENT sur la cible chaque frame
                // C'est Cinemachine qui rend le mouvement fluide via son Damping
                cameraTargetProxy.position = targetProxyPos;
            }

            // Garder la Cinemachine pointee sur le proxy
            if (UIManager.instance != null && UIManager.instance.vcam != null && UIManager.instance.vcam.Follow != cameraTargetProxy)
            {
                UIManager.instance.vcam.Follow = cameraTargetProxy;
                UIManager.instance.vcam.LookAt = cameraTargetProxy;
            }
        }

        // Avance constante vers le "haut" local du vaisseau (pour defilement)
        if (!Inventory.instance.dead)
        {
            transform.position += transform.up * speed * Time.deltaTime;
        }

        if (!move) return;

        // --- MACHINE A ETATS ---
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
    // PHASE 2 : MODE SHOOTER (Facon Acecraft)
    // ==========================================
    void HandleShooterMovement()
    {
        float moveDirX = 0f;
        float moveDirY = 0f;

        // Recuperation de l'input selon le mode (Joystick / Lateral)
        if (isLateralMode)
        {
            if (Pointer.current != null && Pointer.current.press.isPressed)
            {
                Vector2 pressPos = Pointer.current.position.ReadValue();
                // Deplacement horizontal
                moveDirX = (pressPos.x < Screen.width / 2f) ? -1f : 1f;
                // Deplacement vertical
                moveDirY = (pressPos.y < Screen.height / 2f) ? -1f : 1f;
            }
        }
        else
        {
            moveDirX = joystick.Horizontal;
            moveDirY = joystick.Vertical; // Autoriser le deplacement vertical en mode tir
        }

        // Deplacement libre sur les axes X et Y en coordonnées globales
        transform.Translate(new Vector3(moveDirX * shooterStrafeSpeed, moveDirY * shooterStrafeSpeed, 0f) * Time.deltaTime, Space.World);

        // Clamp horizontal (X) relative to the recorded combat base X coordinate!
        float clampedX = Mathf.Clamp(transform.position.x, combatBaseX - maxShooterStrafeX, combatBaseX + maxShooterStrafeX);

        // Clamp vertical (Y) entre le bas et le centre de la camera
        float clampedY = Mathf.Clamp(transform.position.y, cameraProxyY + minShooterStrafeY, cameraProxyY + maxShooterStrafeY);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);

        // Verrouillage de la rotation pour s'assurer que le vaisseau pointe toujours tout droit vers le haut
        // Vitesse elevee pour que l'alignement soit quasi-instantane mais sans saut brusque
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, 0), 12f * Time.deltaTime);
    }

    void HandleShooting()
    {
        if (Time.time >= nextFireTime)
        {
            if (bulletPrefab != null && firePoint != null)
            {
                GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
                bullet.SetActive(true); // S'assurer qu'il est actif
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

        // Attendre la duree du mode
        yield return new WaitForSeconds(duration);

        // Retour au mode esquive
        currentPhase = GamePhase.Dodging;

        // Reinitialisation de l'angle cible pour eviter un a-coup violent lors de la transition
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

    // Generateur procedural de laser bullet
    private GameObject CreateProceduralBulletPrefab()
    {
        GameObject bullet = new GameObject("ProceduralLaser");
        bullet.tag = "Bullet";
        
        SpriteRenderer srComp = bullet.AddComponent<SpriteRenderer>();
        
        // 8x32 glowing cyan laser texture
        Texture2D tex = new Texture2D(8, 32);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                float distToCenter = Mathf.Abs(x - 3.5f) / 3.5f;
                float alpha = 1f - distToCenter;
                tex.SetPixel(x, y, new Color(0f, 0.85f, 1f, alpha));
            }
        }
        tex.Apply();
        srComp.sprite = Sprite.Create(tex, new Rect(0, 0, 8, 32), new Vector2(0.5f, 0.5f));
        srComp.sortingOrder = 5;

        // Add sleek trail renderer
        TrailRenderer tr = bullet.AddComponent<TrailRenderer>();
        tr.time = 0.08f;
        tr.startWidth = 0.12f;
        tr.endWidth = 0.0f;
        tr.material = new Material(Shader.Find("Sprites/Default"));
        tr.startColor = new Color(0f, 0.85f, 1f, 0.6f);
        tr.endColor = new Color(0f, 0.4f, 1f, 0f);

        // Dynamic collider
        BoxCollider2D col = bullet.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.15f, 0.6f);
        col.isTrigger = true;

        // Kinematic Rigidbody 2D
        Rigidbody2D rb2d = bullet.AddComponent<Rigidbody2D>();
        rb2d.bodyType = RigidbodyType2D.Kinematic;

        // Add behavior script
        bullet.AddComponent<BulletScript>();

        // Disable original template
        bullet.SetActive(false);
        DontDestroyOnLoad(bullet);

        return bullet;
    }
}