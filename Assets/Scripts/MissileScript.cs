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

    // Dans PlayerPowerUpManager.cs
    [Header("PowerUp States")]
    
    public bool isShieldActive = false; // Doit être PUBLIC
    public bool isBlazeActive = false;   // Doit être PUBLIC

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

        // Calcul de la direction vers le joueur
        Vector2 direction = (Vector2)target.position - rb.position;
        direction.Normalize();

        // Rotation fluide vers la cible
        float rotateAmount = Vector3.Cross(direction, transform.up).z;
        rb.angularVelocity = -rotateAmount * rotatingSpeed;

        // Avancement constant
        rb.linearVelocity = transform.up * speed;
    }

    IEnumerator LifetimeCountdown()
    {
        yield return new WaitForSeconds(duration);
        StartCoroutine(FadeOutAndDestroy());
    }

    IEnumerator FadeOutAndDestroy()
    {
        isExpiring = true; // Empêche le missile de continuer à traquer le joueur
        //rb.linearVelocity = Vector2.zero; // Stop le mouvement physique

        float fadeDuration = 0.75f;
        float elapsed = 0f;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        TrailRenderer trail = GetComponentInChildren<TrailRenderer>();

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

            /*Gradient originalGradient = trail.colorGradient;

            if (trail != null)
            {
                Gradient newGradient = new Gradient();

                GradientColorKey[] colorKeys = originalGradient.colorKeys;
                GradientAlphaKey[] alphaKeys = originalGradient.alphaKeys;

                for (int i = 0; i < alphaKeys.Length; i++)
                {
                    alphaKeys[i].alpha = Mathf.Lerp(originalGradient.alphaKeys[i].alpha, 0f, t);
                }

                newGradient.SetKeys(colorKeys, alphaKeys);
                trail.colorGradient = newGradient;
            }
            */
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

    /*void OnTriggerEnter2D(Collider2D col)
    {
        if (isExpiring) return;

        if (col.CompareTag("Player"))
        {
            Inventory.instance.DieProcess();
            HandleDestruction(true);
        }
        else if (col.CompareTag("Missile") /*|| col.CompareTag("Obstacle")) // Ajoute tes tags ici
        {
            HandleDestruction(true);
        }
    }*/
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
            Inventory.instance.DieProcess();
            HandleDestruction(true);
        }

        // 2. COLLISION AVEC UN AUTRE MISSILE
        else if (col.CompareTag("Missile"))
        {
            HandleDestruction(true);
        }

        // 3. COLLISION AVEC LE BLAZE (SI TU AS MIS UN TAG "Blaze" SUR L'ICONE QUI TOURNE)
        else if (col.CompareTag("Blaze"))
        {
            HandleDestruction(true);
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
                Inventory.instance.RefreshDestroyedMissiles(MissileSpawner.instance.destroyedMissiles);
        }

        Destroy(gameObject);
    }

    public void MuteVolume()
    {
        if (missileSound != null) missileSound.volume = 0f;
    }
}