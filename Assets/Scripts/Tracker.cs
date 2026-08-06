using UnityEngine;
using System.Collections;

/// <summary>
/// Tracker — chasseur de type "bruit de fond stressant".
/// Il suit le joueur lentement avec un vol nerveux (micro-corrections),
/// génère une pression ambiante (son + tremblement caméra proportionnels à la proximité),
/// et se désengage après un certain temps.
/// Sa vitesse de rapprochement augmente avec le temps de run.
/// </summary>
public class Tracker : MonoBehaviour
{
    [Header("Mouvement")]
    [Tooltip("Vitesse de base de rapprochement vers le joueur.")]
    public float baseApproachSpeed = 1.2f; // Réduit (était 1.8f)

    [Tooltip("Facteur multiplicateur appliqué à la vitesse en fonction du temps de run (par seconde écoulée).")]
    public float speedScalingPerSecond = 0.001f; // Réduit (était 0.003f)

    [Tooltip("Vitesse max après scaling.")]
    public float maxApproachSpeed = 2.5f; // Réduit (était 4.5f)

    [Tooltip("Amplitude des micro-corrections nerveuses (bruit de Perlin).")]
    public float jitterAmplitude = 0.3f; // Réduit (était 0.6f)

    [Tooltip("Fréquence du jitter (plus haut = plus nerveux).")]
    public float jitterFrequency = 4.0f;

    [Header("Pression Ambiante")]
    [Tooltip("Distance en dessous de laquelle le Tracker commence à perturber le joueur.")]
    public float pressureStartDistance = 8.0f;

    [Tooltip("Amplitude max du tremblement caméra quand le Tracker est au plus proche.")]
    public float maxCameraShakeAmplitude = 0.35f;

    [Tooltip("Volume max du son de proximité.")]
    public float maxProximityVolume = 0.45f;

    [Header("Durée de Vie")]
    [Tooltip("Durée en secondes avant que le Tracker ne se désengage.")]
    public float lifetime = 12.0f;

    [Tooltip("Durée du fade-out de désengagement.")]
    public float disengageDuration = 1.5f;

    [Header("Audio")]
    public AudioClip proximityLoopClip;

    // --- Privés ---
    private Transform player;
    private AudioSource proximityAudio;
    private SpriteRenderer spriteRenderer;
    private float spawnTime;
    private float perlinSeedX;
    private float perlinSeedY;
    private bool isDisengaging = false;

    // Référence à l'explosion pour la mort par collision missile
    private GameObject explosionPrefab;

    void Start()
    {
        spawnTime = Time.time;
        perlinSeedX = Random.Range(0f, 100f);
        perlinSeedY = Random.Range(100f, 200f);

        // Trouver le joueur
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        // SpriteRenderer — si aucun n'existe, on en crée un procédural
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateTrackerSprite();
            spriteRenderer.sortingOrder = 4;
        }

        // Collider
        if (GetComponent<Collider2D>() == null)
        {
            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;
            col.isTrigger = true;
        }

        // Rigidbody
        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Audio de proximité (boucle mécanique)
        proximityAudio = gameObject.AddComponent<AudioSource>();
        proximityAudio.loop = true;
        proximityAudio.playOnAwake = false;
        proximityAudio.volume = 0f;
        proximityAudio.spatialBlend = 0f; // 2D
        if (proximityLoopClip != null)
        {
            proximityAudio.clip = proximityLoopClip;
            proximityAudio.Play();
        }

        // Récupérer le prefab d'explosion depuis le MissileSpawner
        if (MissileSpawner.instance != null && MissileSpawner.instance.missiles.Length > 0)
        {
            MissileScript ms = MissileSpawner.instance.missiles[0].GetComponent<MissileScript>();
            if (ms != null) explosionPrefab = ms.explosionPrefab;
        }

        // Tag pour le nettoyage
        gameObject.tag = "Tracker";

        // Désengagement automatique après la durée de vie
        StartCoroutine(LifetimeRoutine());
    }

    void Update()
    {
        if (player == null || isDisengaging) return;

        // --- Calcul de la vitesse actuelle (scaling avec le temps de run) ---
        float runTime = Inventory.instance != null ? Inventory.instance.totalSeconds : 0f;
        float currentSpeed = Mathf.Min(baseApproachSpeed + runTime * speedScalingPerSecond, maxApproachSpeed);

        // --- SlowMo : ralentir le chasseur si le power-up est actif ---
        float slowFactor = 1f;
        if (PlayerPowerUpManager.instance != null && PlayerPowerUpManager.instance.isSlowMoActive)
        {
            slowFactor = PlayerPowerUpManager.instance.slowMoFactor;
            currentSpeed *= slowFactor;
        }

        // --- Mouvement vers le joueur avec jitter nerveux ---
        Vector3 dirToPlayer = (player.position - transform.position).normalized;

        // Bruit de Perlin pour le jitter latéral
        float time = Time.time * jitterFrequency * slowFactor;
        float jitterX = (Mathf.PerlinNoise(time + perlinSeedX, 0f) - 0.5f) * 2f * jitterAmplitude * slowFactor;
        float jitterY = (Mathf.PerlinNoise(0f, time + perlinSeedY) - 0.5f) * 2f * jitterAmplitude * slowFactor;
        Vector3 jitterOffset = new Vector3(jitterX, jitterY, 0f);

        transform.position += (dirToPlayer * currentSpeed + jitterOffset) * Time.deltaTime;

        // --- Rotation pour pointer vers le joueur ---
        float angle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, angle), 8f * Time.deltaTime);

        // --- Pression Ambiante (proportionnelle à la proximité) ---
        float distance = Vector3.Distance(transform.position, player.position);
        float proximityFactor = 0f;

        if (distance < pressureStartDistance)
        {
            // 0 quand à pressureStartDistance, 1 quand collé au joueur
            proximityFactor = 1f - (distance / pressureStartDistance);
            proximityFactor = Mathf.Clamp01(proximityFactor);
        }

        // Audio : volume et pitch augmentent avec la proximité
        if (proximityAudio != null)
        {
            proximityAudio.volume = Mathf.Lerp(proximityAudio.volume, proximityFactor * maxProximityVolume, 5f * Time.deltaTime);
            proximityAudio.pitch = Mathf.Lerp(0.8f, 1.4f, proximityFactor);
        }

        // Camera shake léger et continu proportionnel à la proximité
        if (proximityFactor > 0.3f && CameraShake.instance != null)
        {
            float shakeAmp = proximityFactor * maxCameraShakeAmplitude;
            CameraShake.instance.Shake(0.1f, shakeAmp);
        }
    }

    IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        StartCoroutine(Disengage());
    }

    IEnumerator Disengage()
    {
        isDisengaging = true;

        // Le Tracker s'éloigne vers le haut-droit et fade-out
        Vector3 escapeDir = (Vector3.up + Vector3.right).normalized;
        float elapsed = 0f;
        Color startColor = spriteRenderer.color;
        float escapeSpeed = 8f;

        while (elapsed < disengageDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / disengageDuration;

            transform.position += escapeDir * escapeSpeed * Time.deltaTime;

            // Fade-out visuel
            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            spriteRenderer.color = c;

            // Fade-out audio
            if (proximityAudio != null)
                proximityAudio.volume = Mathf.Lerp(proximityAudio.volume, 0f, t);

            yield return null;
        }

        // Notifier le MissileSpawner
        if (MissileSpawner.instance != null)
            MissileSpawner.instance.OnHunterDestroyed();

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        // Collision avec le joueur = dégâts
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
        // Collision avec un missile = les deux explosent (alignement stratgique)
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

            // Feedback pour l'alignement missile/chasseur
            if (CameraShake.instance != null)
                CameraShake.instance.Shake(0.15f, 1.2f);

            Explode();
        }
        // Collision avec le Blaze
        else if (col.CompareTag("Blaze"))
        {
            Explode();
        }
    }

    void Explode()
    {
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        if (CameraShake.instance != null)
            CameraShake.instance.Shake(0.15f, 0.8f);
        // Score bonus pour avoir éliminé un chasseur
        if (Inventory.instance != null)
        {
            int points = 300 * Inventory.instance.scoreMultiplier;
            Inventory.instance.score += points;
            Inventory.instance.scoreText.text = Inventory.instance.score.ToString();
            Inventory.instance.TriggerCrashBonus(transform.position, points);
        }

        if (MissileSpawner.instance != null)
            MissileSpawner.instance.OnHunterDestroyed();

        Destroy(gameObject);
    }

    public void Destabilize(Transform newTarget)
    {
        player = newTarget;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.cyan;
        }
    }

    // --- Sprite procédural : silhouette fine, nerveux ---
    Sprite CreateTrackerSprite()
    {
        int w = 24, h = 32;
        Texture2D tex = new Texture2D(w, h);
        float cx = w / 2f, cy = h / 2f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Forme losange fin (silhouette nerveuse)
                float normX = Mathf.Abs(x - cx) / cx;
                float normY = Mathf.Abs(y - cy) / cy;

                bool inShape = (normX + normY * 0.6f) < 0.55f;

                if (inShape)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) / (w * 0.5f);
                    // Cyan électrique → bleu sombre
                    Color c = Color.Lerp(new Color(0f, 0.95f, 1f), new Color(0.05f, 0.15f, 0.4f), dist);
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
