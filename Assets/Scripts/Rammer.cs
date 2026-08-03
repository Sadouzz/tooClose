using UnityEngine;
using System.Collections;

/// <summary>
/// Rammer — chasseur de type "charge à second souffle".
/// Il s'aligne avec le joueur, montre un tell visuel (clignotement), 
/// charge en ligne droite, et s'il rate, tente une seconde charge plus courte
/// et plus rapide avant de se désengager.
/// La fenêtre de tell se raccourcit avec le temps de run.
/// </summary>
public class Rammer : MonoBehaviour
{
    public enum RammerState
    {
        Approaching,    // Se positionne pour la charge
        Aligning,       // S'aligne avec le joueur
        Telegraphing,   // Tell visuel avant la première charge
        Charging,       // Première charge
        Recovering,     // Freine après un raté
        Telegraphing2,  // Tell avant la seconde charge (plus court)
        Charging2,      // Seconde charge (plus rapide)
        Disengaging     // Quitte l'écran
    }

    [Header("État")]
    public RammerState currentState = RammerState.Approaching;

    [Header("Approche")]
    [Tooltip("Vitesse de déplacement pendant la phase d'approche.")]
    public float approachSpeed = 3.5f;

    [Tooltip("Distance d'alignement cible avant de commencer le tell.")]
    public float alignDistance = 6.0f;

    [Header("Tell (Télégraphie)")]
    [Tooltip("Durée de base du tell (en secondes). Se réduit avec le temps de run.")]
    public float baseTellDuration = 1.2f;

    [Tooltip("Durée minimum du tell (ne descend jamais en dessous).")]
    public float minTellDuration = 0.4f;

    [Tooltip("Réduction du tell par seconde de run écoulée.")]
    public float tellReductionPerSecond = 0.005f;

    [Tooltip("Nombre de clignotements pendant le tell.")]
    public int tellFlashCount = 4;

    [Header("Charge")]
    [Tooltip("Vitesse de la première charge.")]
    public float chargeSpeed = 18f;

    [Tooltip("Durée max de la première charge avant abandon.")]
    public float chargeDuration = 1.2f;

    [Tooltip("Vitesse de la seconde charge (plus rapide).")]
    public float secondChargeSpeed = 24f;

    [Tooltip("Durée max de la seconde charge.")]
    public float secondChargeDuration = 0.7f;

    [Header("Récupération")]
    [Tooltip("Durée de la phase de freinage après un raté.")]
    public float recoveryDuration = 0.6f;

    [Header("Désengagement")]
    [Tooltip("Vitesse de fuite après le second souffle.")]
    public float disengageSpeed = 12f;

    [Tooltip("Durée du désengagement.")]
    public float disengageFadeDuration = 1.5f;

    [Header("Audio")]
    public AudioClip chargeBuildupSound;
    public AudioClip chargeReleaseSound;

    // --- Privés ---
    private Transform player;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Vector3 chargeDirection;
    private Color originalColor;
    private float stateTimer;
    private bool hasSecondChance = true;

    // Référence à l'explosion
    private GameObject explosionPrefab;

    void Start()
    {
        // Trouver le joueur
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        // SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateRammerSprite();
            spriteRenderer.sortingOrder = 4;
        }
        originalColor = spriteRenderer.color;

        // Collider
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.0f);
            col.isTrigger = true;
        }

        // Rigidbody
        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 0.3f;
        audioSource.spatialBlend = 0f;

        // Explosion prefab
        if (MissileSpawner.instance != null && MissileSpawner.instance.missiles.Length > 0)
        {
            MissileScript ms = MissileSpawner.instance.missiles[0].GetComponent<MissileScript>();
            if (ms != null) explosionPrefab = ms.explosionPrefab;
        }

        // Tag
        gameObject.tag = "Rammer";


        currentState = RammerState.Approaching;
    }

    void Update()
    {
        if (player == null) return;

        switch (currentState)
        {
            case RammerState.Approaching:
                UpdateApproach();
                break;
            case RammerState.Aligning:
                UpdateAlign();
                break;
            case RammerState.Telegraphing:
                // Géré par coroutine
                break;
            case RammerState.Charging:
                UpdateCharge(chargeSpeed, chargeDuration);
                break;
            case RammerState.Recovering:
                // Géré par coroutine
                break;
            case RammerState.Telegraphing2:
                // Géré par coroutine
                break;
            case RammerState.Charging2:
                UpdateCharge(secondChargeSpeed, secondChargeDuration);
                break;
            case RammerState.Disengaging:
                // Géré par coroutine
                break;
        }
    }

    // --- PHASE : APPROCHE ---
    // --- Utilitaire : facteur de ralentissement SlowMo ---
    private float GetSlowFactor()
    {
        if (PlayerPowerUpManager.instance != null && PlayerPowerUpManager.instance.isSlowMoActive)
            return PlayerPowerUpManager.instance.slowMoFactor;
        return 1f;
    }

    void UpdateApproach()
    {
        float slow = GetSlowFactor();

        // Se déplace vers une position à alignDistance du joueur (en amont)
        Vector3 targetPos = player.position + Vector3.up * alignDistance;
        Vector3 dirToTarget = (targetPos - transform.position).normalized;

        transform.position += dirToTarget * approachSpeed * slow * Time.deltaTime;

        // Rotation vers le joueur
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float angle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, angle), 6f * Time.deltaTime);

        // Quand assez proche de la position d'alignement, passer à l'alignement fin
        if (Vector3.Distance(transform.position, targetPos) < 1.5f)
        {
            currentState = RammerState.Aligning;
            stateTimer = 0f;
        }
    }

    // --- PHASE : ALIGNEMENT ---
    void UpdateAlign()
    {
        float slow = GetSlowFactor();

        // S'aligne horizontalement avec le joueur (même X) en restant au-dessus
        float targetX = player.position.x;
        float currentX = Mathf.Lerp(transform.position.x, targetX, 5f * slow * Time.deltaTime);
        transform.position = new Vector3(currentX, transform.position.y, transform.position.z);

        // Rotation vers le joueur
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float angle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, angle), 10f * Time.deltaTime);

        stateTimer += Time.deltaTime;

        // Après un court délai d'alignement, passer au tell
        if (stateTimer > 0.5f)
        {
            StartCoroutine(TelegraphRoutine(false));
        }
    }

    // --- PHASE : TELL (Clignotement) ---
    IEnumerator TelegraphRoutine(bool isSecondCharge)
    {
        currentState = isSecondCharge ? RammerState.Telegraphing2 : RammerState.Telegraphing;

        // Calcul de la durée du tell ajustée au temps de run
        float runTime = Inventory.instance != null ? Inventory.instance.totalSeconds : 0f;
        float tellDuration = isSecondCharge
            ? Mathf.Max(minTellDuration, (baseTellDuration * 0.5f) - runTime * tellReductionPerSecond)
            : Mathf.Max(minTellDuration, baseTellDuration - runTime * tellReductionPerSecond);

        // Son de build-up
        if (chargeBuildupSound != null && audioSource != null)
        {
            audioSource.pitch = isSecondCharge ? 1.3f : 1.0f;
            audioSource.PlayOneShot(chargeBuildupSound);
        }

        // Clignotement rouge/blanc comme tell
        float flashInterval = tellDuration / (tellFlashCount * 2f);
        Color warningColor = new Color(1f, 0.2f, 0.1f); // Rouge vif

        for (int i = 0; i < tellFlashCount; i++)
        {
            spriteRenderer.color = warningColor;
            yield return new WaitForSeconds(flashInterval);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashInterval);
        }

        // Verrouiller la direction de charge AU MOMENT du lancement
        chargeDirection = (player.position - transform.position).normalized;

        // Son de lancement
        if (chargeReleaseSound != null && audioSource != null)
        {
            audioSource.pitch = isSecondCharge ? 1.2f : 1.0f;
            audioSource.PlayOneShot(chargeReleaseSound);
        }

        // Passer à la charge
        currentState = isSecondCharge ? RammerState.Charging2 : RammerState.Charging;
        stateTimer = 0f;
    }

    // --- PHASE : CHARGE ---
    void UpdateCharge(float speed, float maxDuration)
    {
        float slow = GetSlowFactor();

        transform.position += chargeDirection * speed * slow * Time.deltaTime;

        // Rotation verrouillée dans la direction de charge
        float angle = Mathf.Atan2(chargeDirection.y, chargeDirection.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        stateTimer += Time.deltaTime;

        // Si la charge dure trop longtemps sans toucher, on passe à la suite
        if (stateTimer > maxDuration)
        {
            if (currentState == RammerState.Charging && hasSecondChance)
            {
                // Premier raté → récupération puis second souffle
                hasSecondChance = false;
                StartCoroutine(RecoveryRoutine());
            }
            else
            {
                // Second raté ou plus de chance → désengagement
                StartCoroutine(DisengageRoutine());
            }
        }
    }

    // --- PHASE : RÉCUPÉRATION (freinage après raté) ---
    IEnumerator RecoveryRoutine()
    {
        currentState = RammerState.Recovering;

        // Freinage progressif
        float elapsed = 0f;
        Vector3 startVelocity = chargeDirection * chargeSpeed;

        while (elapsed < recoveryDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / recoveryDuration;

            // Décélération
            Vector3 currentVelocity = Vector3.Lerp(startVelocity, Vector3.zero, t);
            transform.position += currentVelocity * Time.deltaTime;

            yield return null;
        }

        // Petit temps de pause avant de se réaligner
        yield return new WaitForSeconds(0.2f);

        // Lancer le second tell (plus court et plus agressif)
        StartCoroutine(TelegraphRoutine(true));
    }

    // --- PHASE : DÉSENGAGEMENT ---
    IEnumerator DisengageRoutine()
    {
        currentState = RammerState.Disengaging;

        Vector3 escapeDir = (Vector3.up + Vector3.left * 0.5f).normalized;
        float elapsed = 0f;
        Color startColor = spriteRenderer.color;

        while (elapsed < disengageFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / disengageFadeDuration;

            transform.position += escapeDir * disengageSpeed * Time.deltaTime;

            // Fade-out
            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            spriteRenderer.color = c;

            yield return null;
        }

        if (MissileSpawner.instance != null)
            MissileSpawner.instance.OnHunterDestroyed();

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        // Collision avec le joueur pendant une charge = dégâts
        if (col.CompareTag("Player"))
        {
            PlayerPowerUpManager powerUpManager = col.GetComponentInParent<PlayerPowerUpManager>();
            if (powerUpManager != null && (powerUpManager.isShieldActive || powerUpManager.isBlazeActive))
            {
                Explode();
                return;
            }

            if (PlayerMovement.instance != null)
            {
                if (PlayerMovement.instance.life > 1)
                {
                    PlayerMovement.instance.life--;
                    if (PlayerMovement.instance.life == 1)
                        PlayerMovement.instance.smoke.SetActive(true);
                }
                else
                {
                    Inventory.instance.DieProcess();
                }
            }
            Explode();
        }
        // Collision avec un missile = moment signature ! Les deux explosent.
        else if (col.CompareTag("Missile"))
        {
            // Scurit : viter que a n'arrive hors-cran juste au moment du spawn
            if (Camera.main != null)
            {
                Vector3 vp = Camera.main.WorldToViewportPoint(transform.position);
                if (vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) return;
            }
            else if (spriteRenderer != null && !spriteRenderer.isVisible) return;

            MissileScript ms = col.GetComponent<MissileScript>();
            if (ms != null) ms.HandleDestruction(true);

            // Feedback fort pour l'alignement missile/chasseur
            if (CameraShake.instance != null)
                CameraShake.instance.Shake(0.25f, 1.5f);

            // Petit slow-mo pour marquer l'exploit
            //StartCoroutine(BriefSlowMotion());

            Explode();
        }
        // Collision avec le Blaze
        else if (col.CompareTag("Blaze"))
        {
            Explode();
        }
    }

    IEnumerator BriefSlowMotion()
    {
        Time.timeScale = 0.3f;
        yield return new WaitForSecondsRealtime(0.15f);
        Time.timeScale = 1f;
    }

    void Explode()
    {
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        if (CameraShake.instance != null)
            CameraShake.instance.Shake(0.2f, 1.2f);

        // Score bonus plus élevé (ennemi plus dangereux)
        if (Inventory.instance != null)
        {
            Inventory.instance.score += 500 * Inventory.instance.scoreMultiplier;
            Inventory.instance.scoreText.text = Inventory.instance.score.ToString();
        }

        if (MissileSpawner.instance != null)
            MissileSpawner.instance.OnHunterDestroyed();

        Destroy(gameObject);
    }

    // --- Sprite procédural : silhouette massive, anguleuse ---
    Sprite CreateRammerSprite()
    {
        int w = 28, h = 36;
        Texture2D tex = new Texture2D(w, h);
        float cx = w / 2f, cy = h / 2f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Forme pentagonale massive (silhouette anguleuse)
                float normX = Mathf.Abs(x - cx) / cx;
                float normY = (float)y / h;

                // Forme : triangle large en haut, carré en bas
                float widthAtY = normY < 0.5f ? Mathf.Lerp(0.3f, 0.9f, normY * 2f) : 0.9f;
                bool inShape = normX < widthAtY && normY > 0.05f && normY < 0.95f;

                if (inShape)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) / (w * 0.5f);
                    // Rouge sombre → orange vif au centre
                    Color c = Color.Lerp(new Color(1f, 0.5f, 0.1f), new Color(0.4f, 0.05f, 0.05f), dist);
                    tex.SetPixel(x, y, c);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }
}
