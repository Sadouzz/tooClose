using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileScript : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 5f;
    public float rotatingSpeed = 200f;
    public float duration = 5f;

    [Header("References")]
    public Rigidbody2D rb;
    public AudioSource missileSound;
    public GameObject explosionPrefab;
    public SpriteRenderer spriteRenderer;
    public TrailRenderer trail;
    public AudioSource audio;

    [Header("PowerUp States")]
    public bool isShieldActive = false; 
    public bool isBlazeActive = false;   
    public float lastNearMissTime = -999f;

    private Transform target;
    private bool isExpiring = false;

    void Start()
    {
        audio = GameObject.FindGameObjectWithTag("EventSoundMissileExplosion").GetComponent<AudioSource>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        // On cherche le joueur
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;

        // Lance le compte à rebours avant auto-destruction
        StartCoroutine(LifetimeCountdown());
    }

    void FixedUpdate()
    {
        if (target == null || isExpiring) return;

        // 1. On définit les vitesses de base pour cette frame
        float currentSpeed = speed;
        float currentRotSpeed = rotatingSpeed;

        // 2. On vérifie si le SlowMo est actif via le Manager
        if (PlayerPowerUpManager.instance != null && PlayerPowerUpManager.instance.isSlowMoActive)
        {
            // On multiplie par le facteur (ex: 0.5f divise la vitesse par 2)
            currentSpeed *= PlayerPowerUpManager.instance.slowMoFactor;
            currentRotSpeed *= PlayerPowerUpManager.instance.slowMoFactor;
        }

        // Calcul de la direction vers le joueur
        Vector2 direction = (Vector2)target.position - rb.position;
        direction.Normalize();

        // Rotation fluide vers la cible (avec la vitesse potentiellement ralentie)
        float rotateAmount = Vector3.Cross(direction, transform.up).z;
        rb.angularVelocity = -rotateAmount * currentRotSpeed;

        // Avancement constant (avec la vitesse potentiellement ralentie)
        rb.linearVelocity = transform.up * currentSpeed;
    }

    IEnumerator LifetimeCountdown()
    {
        yield return new WaitForSeconds(duration);
        StartCoroutine(FadeOutAndDestroy());
    }

    IEnumerator FadeOutAndDestroy()
    {
        isExpiring = true; // Empêche le missile de continuer à traquer le joueur

        float fadeDuration = 0.75f;
        float elapsed = 0f;

        Vector3 startScale = transform.localScale;
        Color startColor = spriteRenderer.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            // Réduction de la taille
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            // Fondu transparent
            Color newColor = startColor;
            newColor.a = Mathf.Lerp(startColor.a, 0f, t);
            spriteRenderer.color = newColor;

            yield return null;
        }

        HandleDestruction(false); // Détruire sans explosion
    }

    public void OnMissileExplode()
    {
        // 1. Récupérer le Particle System enfant
        ParticleSystem trail = GetComponentInChildren<ParticleSystem>();

        if (trail != null)
        {
            // 2. Le détacher du missile (il devient un objet racine dans la hiérarchie)
            trail.transform.parent = null;

            // 3. Arrêter l'émission de nouvelles particules
            var emission = trail.emission;
            emission.enabled = false;

            // 4. Détruire l'objet de la traînée une fois que les dernières particules sont mortes
            Destroy(trail.gameObject, trail.main.startLifetime.constantMax);
        }

        // 5. Détruire le missile normalement
        Destroy(this.gameObject);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (isExpiring) return;

        // 1. COLLISION AVEC LE JOUEUR (On vérifie les PowerUps d'abord)
        if (col.CompareTag("Player"))
        {
            // On récupère le manager sur le joueur
            PlayerPowerUpManager powerUpManager = col.GetComponentInParent<PlayerPowerUpManager>();

            if (powerUpManager != null)
            {
                // Si le Blaze est actif OU le Bouclier est actif
                if (powerUpManager.isShieldActive || powerUpManager.isBlazeActive)
                {
                    // Le missile explose mais le joueur survit !
                    HandleDestruction(true);
                    return; // On arrête la fonction ici
                }
            }

            // Si on arrive ici, c'est qu'aucun PowerUp n'était actif
            if (PlayerMovement.instance.life > 1)
            {
                PlayerMovement.instance.life--;
                if (PlayerMovement.instance.life == 1)
                {
                    PlayerMovement.instance.smoke.SetActive(true);
                }
            }
            else
                Inventory.instance.DieProcess();
            HandleDestruction(true);
        }

        // 2. COLLISION AVEC UN AUTRE MISSILE
        else if (col.CompareTag("Missile"))
        {
            HandleDestruction(true);
        }

        // 3. COLLISION AVEC LE BLAZE
        else if (col.CompareTag("Blaze"))
        {
            HandleDestruction(true);
        }

        // 4. COLLISION AVEC LES BULLETS (LASERS) DU JOUEUR
        else if (col.CompareTag("Bullet"))
        {
            Destroy(col.gameObject); // Détruire la balle laser
            HandleDestruction(true); // Détruire le missile avec une explosion !
        }
    }

    // Centralisation de la destruction pour éviter la répétition de code
    public void HandleDestruction(bool spawnExplosion)
    {
        if (spawnExplosion && explosionPrefab != null)
        {
            audio.Play();
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        if (CameraShake.instance != null)
        {
            CameraShake.instance.Shake(0.12f, 0.6f);
        }

        OnMissileExplode();

        // Mise à jour des scores via le Spawner
        if (MissileSpawner.instance != null)
        {
            MissileSpawner.instance.currentMissiles--;
            MissileSpawner.instance.destroyedMissiles++;

            if (Inventory.instance != null)
            {
                Inventory.instance.RefreshDestroyedMissiles(MissileSpawner.instance.destroyedMissiles);
                if (spawnExplosion)
                {
                    int points = 50 * Inventory.instance.scoreMultiplier;
                    Inventory.instance.score += points;
                    Inventory.instance.scoreText.text = Inventory.instance.score.ToString();
                    Inventory.instance.TriggerCrashBonus(transform.position, points);
                }
            }
        }

        Destroy(gameObject);
    }

    public void MuteVolume()
    {
        if (missileSound != null) missileSound.volume = 0f;
    }

    public void Destabilize(Transform newTarget)
    {
        target = newTarget;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.cyan;
        }
    }
}