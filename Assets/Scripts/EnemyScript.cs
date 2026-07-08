using UnityEngine;
using System.Collections;

public class EnemyScript : MonoBehaviour
{
    [Header("Health & Combat")]
    public int maxHealth = 2;
    private int currentHealth;
    
    private Transform player;
    private Transform cameraProxy;
    private GameObject explosionPrefab;

    private float relativeX;
    private float verticalOffset;
    private float spawnDelayOffset;

    [Header("Enemy Shooting Settings")]
    public float enemyFireRate = 4.5f; // Faible cadence, moyenne de 4.5s
    public int minWaveToShootMissiles = 5; // A partir de quel niveau/vague ils tirent
    public float enemyMissileSpeed = 2.5f; // Vitesse des missiles ennemis (plus lent)
    public float enemyMissileRotationSpeed = 80f; // Vitesse de rotation des missiles ennemis
    private float nextEnemyFireTime = 0f;
    private float lastEnemyFireTime = 0f;

    [Header("Enemy Audio")]
    public AudioClip chargeSound;
    public AudioClip shootSound;
    private AudioSource audioSource;
    private bool hasPlayedChargeSound = false;

    private Transform healthBarFill;
    private GameObject healthBarObj;
    
    private LineRenderer shootCircle;
    private int circleSegments = 36;
    public float circleRadius = 1.0f;
    public Color circleColor = new Color(1f, 0.5f, 0f, 0.8f); // Orange-ish outline

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 0.2f;

        currentHealth = maxHealth;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        // Find the camera follow proxy
        GameObject proxyObj = GameObject.Find("CameraFollowProxy");
        if (proxyObj != null) cameraProxy = proxyObj.transform;

        // Save starting offsets relative to the true scrolling camera target coordinates (combatBaseX & cameraProxyY)
        // This guarantees absolute immunity to player movements and prevents any coordinate jumps!
        if (PlayerMovement.instance != null)
        {
            relativeX = transform.position.x - PlayerMovement.instance.combatBaseX;
            verticalOffset = transform.position.y - PlayerMovement.instance.cameraProxyY;
        }
        else if (cameraProxy != null)
        {
            relativeX = transform.position.x - cameraProxy.position.x;
            verticalOffset = transform.position.y - cameraProxy.position.y;
        }
        else
        {
            relativeX = transform.position.x;
            verticalOffset = 2.5f; // Default center-top offset fallback
        }

        spawnDelayOffset = Random.Range(0f, 100f);

        // Initialiser le prochain tir avec un petit decalage aléatoire pour eviter le tir synchrone
        lastEnemyFireTime = Time.time;
        nextEnemyFireTime = Time.time + Random.Range(1.5f, enemyFireRate);
        
        CreateShootCircle();

        // Automatically fetch the explosion prefab from a missile template if possible
        if (MissileSpawner.instance != null && MissileSpawner.instance.missiles.Length > 0)
        {
            MissileScript ms = MissileSpawner.instance.missiles[0].GetComponent<MissileScript>();
            if (ms != null) explosionPrefab = ms.explosionPrefab;
        }

        // ENTRY ANIMATION: After recording our intended target position, physically teleport the enemy 
        // high above the screen so the player can watch them smoothly fly down into formation!
        transform.position += new Vector3(0f, 6.0f, 0f);
    }

    void Update()
    {
        // Maintain relative screen position (Acecraft style)
        // Lock ourselves strictly relative to the scrolling camera target coordinates
        // which makes us stay perfectly static in screen viewport coordinates!
        float hoverOffset = Mathf.Sin(Time.time * 1.5f + spawnDelayOffset) * 0.6f;
        
        float targetX = relativeX + hoverOffset;
        float targetY = verticalOffset;

        if (PlayerMovement.instance != null)
        {
            // Target horizontal position is strictly locked to the combat base X!
            targetX += PlayerMovement.instance.combatBaseX;

            // Target vertical position is strictly locked to the scrolling camera baseline coordinate!
            targetY += PlayerMovement.instance.cameraProxyY;
        }
        else if (cameraProxy != null)
        {
            targetX += cameraProxy.position.x;
            targetY += cameraProxy.position.y;
        }
        else
        {
            // Fallback: move up at the player's scroll speed
            float scrollSpeed = PlayerMovement.instance != null ? PlayerMovement.instance.speed : 5f;
            transform.position += Vector3.up * scrollSpeed * Time.deltaTime;
            return;
        }

        // Smooth Lerp for high-quality, premium visual feel
        float smoothedX = Mathf.Lerp(transform.position.x, targetX, 5f * Time.deltaTime);
        // Reduced Y lerp speed to 3.5f so the entrance descent from the top of the screen is clearly visible and majestic!
        float smoothedY = Mathf.Lerp(transform.position.y, targetY, 3.5f * Time.deltaTime);

        transform.position = new Vector3(smoothedX, smoothedY, transform.position.z);

        // --- ENEMY MISSILE FIRING SYSTEM ---
        if (MissileSpawner.instance != null && MissileSpawner.instance.currentWave >= minWaveToShootMissiles)
        {
            float fill = 0f;
            if (nextEnemyFireTime > lastEnemyFireTime)
            {
                fill = (Time.time - lastEnemyFireTime) / (nextEnemyFireTime - lastEnemyFireTime);
            }
            UpdateShootCircle(fill);

            if (!hasPlayedChargeSound && fill >= 0.8f && chargeSound != null && audioSource != null)
            {
                hasPlayedChargeSound = true;
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(chargeSound);
            }

            if (Time.time >= nextEnemyFireTime)
            {
                // Set next fire time with a minor random range to prevent synchronized volleys
                lastEnemyFireTime = Time.time;
                nextEnemyFireTime = Time.time + enemyFireRate + Random.Range(-1.0f, 1.5f);
                hasPlayedChargeSound = false;
                ShootMissile();
            }
        }
        else
        {
            UpdateShootCircle(0f);
        }
    }

    void ShootMissile()
    {
        if (MissileSpawner.instance != null && MissileSpawner.instance.missiles != null && MissileSpawner.instance.missiles.Length > 0)
        {
            if (shootSound != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(0.85f, 1.15f);
                audioSource.PlayOneShot(shootSound);
            }

            GameObject missilePrefab = MissileSpawner.instance.missiles[0];
            Vector3 spawnPos = transform.position + Vector3.down * 0.8f;
            
            // Instancie le missile oriente vers le bas
            GameObject enemyMissile = Instantiate(missilePrefab, spawnPos, Quaternion.Euler(0, 0, 180f));
            enemyMissile.SetActive(true);

            // Active les effets visuels de la trainée de particules
            ParticleSystem ps = enemyMissile.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                ps.Clear();
                ps.Play();
            }

            // Applique les paramètres de vitesse configurables
            MissileScript ms = enemyMissile.GetComponent<MissileScript>();
            if (ms != null)
            {
                ms.speed = enemyMissileSpeed;
                ms.rotatingSpeed = enemyMissileRotationSpeed;
            }

            // Incremente le compteur de missiles du Spawner pour que la vague attende leur destruction
            MissileSpawner.instance.currentMissiles++;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Explode();
        }
        else
        {
            StartCoroutine(FlashRed());
        }
    }

    IEnumerator FlashRed()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            sr.color = Color.white;
        }
    }

    void Explode()
    {
        // Instantiate the explosion visual effect
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        
        // Play explosion sound effect
        var explosionSoundObj = GameObject.FindGameObjectWithTag("EventSoundMissileExplosion");
        if (explosionSoundObj != null)
        {
            AudioSource audio = explosionSoundObj.GetComponent<AudioSource>();
            if (audio != null) audio.Play();
        }

        // Camera Shake for combat impact
        if (CameraShake.instance != null)
        {
            CameraShake.instance.Shake(0.15f, 0.8f);
        }

        // Grant score points
        if (Inventory.instance != null)
        {
            Inventory.instance.score += 200 * Inventory.instance.scoreMultiplier;
            Inventory.instance.scoreText.text = Inventory.instance.score.ToString();
        }

        // Notify Spawner
        if (MissileSpawner.instance != null)
        {
            MissileSpawner.instance.destroyedEnemies++;
            MissileSpawner.instance.OnEnemyDestroyed();
        }

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            // Deal damage to the player if they don't have active defensive powerups
            if (PlayerMovement.instance != null)
            {
                PlayerPowerUpManager powerUpManager = col.GetComponentInParent<PlayerPowerUpManager>();
                if (powerUpManager != null && (powerUpManager.isShieldActive || powerUpManager.isBlazeActive))
                {
                    Explode(); // Explodes harmlessly on player's shield
                    return;
                }

                if (PlayerMovement.instance.life > 1)
                {
                    PlayerMovement.instance.life--;
                    if (PlayerMovement.instance.life == 1)
                    {
                        PlayerMovement.instance.smoke.SetActive(true);
                    }
                }
                else
                {
                    Inventory.instance.DieProcess();
                }
            }
            Explode();
        }
    }

    void CreateShootCircle()
    {
        GameObject circleObj = new GameObject("ShootCircle");
        circleObj.transform.SetParent(transform);
        circleObj.transform.localPosition = Vector3.zero;

        shootCircle = circleObj.AddComponent<LineRenderer>();
        shootCircle.useWorldSpace = false;
        shootCircle.startWidth = 0.06f;
        shootCircle.endWidth = 0.06f;
        
        // Use a basic sprite material or line material
        shootCircle.material = new Material(Shader.Find("Sprites/Default"));
        shootCircle.startColor = circleColor;
        shootCircle.endColor = circleColor;
        shootCircle.sortingOrder = 5;
        
        UpdateShootCircle(0f);
    }

    void UpdateShootCircle(float fillPercentage)
    {
        if (shootCircle == null) return;
        
        fillPercentage = Mathf.Clamp01(fillPercentage);
        
        if (fillPercentage <= 0.01f)
        {
            shootCircle.enabled = false;
            return;
        }

        shootCircle.enabled = true;
        
        int activeSegments = Mathf.RoundToInt(fillPercentage * circleSegments);
        shootCircle.positionCount = activeSegments + 1;
        
        float angle = 90f; // Start at top
        float angleStep = 360f / circleSegments;

        for (int i = 0; i <= activeSegments; i++)
        {
            float rad = angle * Mathf.Deg2Rad;
            float x = Mathf.Cos(rad) * circleRadius;
            float y = Mathf.Sin(rad) * circleRadius;
            shootCircle.SetPosition(i, new Vector3(x, y, 0));
            
            angle -= angleStep; // Go clockwise
        }
    }

    void CreateHealthBar()
    {
        healthBarObj = new GameObject("HealthBar");
        healthBarObj.transform.SetParent(transform);
        healthBarObj.transform.localPosition = new Vector3(0, 0.8f, 0); // Au dessus de la tete
        
        // Background (Rouge)
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(healthBarObj.transform);
        bg.transform.localPosition = Vector3.zero;
        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        // Background centré
        bgSr.sprite = CreatePixelSprite(new Color(0.8f, 0f, 0f, 0.8f), new Vector2(0.5f, 0.5f));
        bgSr.sortingOrder = 10;
        bg.transform.localScale = new Vector3(1.2f, 0.15f, 1f);

        // Fill (Vert)
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(healthBarObj.transform);
        // Placé à gauche de la barre (largeur totale de 1.2, donc bord gauche à -0.6)
        fill.transform.localPosition = new Vector3(-0.6f, 0f, 0f); 
        SpriteRenderer fillSr = fill.AddComponent<SpriteRenderer>();
        // Remplissage avec pivot à gauche (0, 0.5) pour qu'il se réduise de droite à gauche !
        fillSr.sprite = CreatePixelSprite(new Color(0f, 0.9f, 0f, 0.9f), new Vector2(0f, 0.5f));
        fillSr.sortingOrder = 11;
        
        healthBarFill = fill.transform;
        healthBarFill.localScale = new Vector3(1.2f, 0.15f, 1f);

        healthBarObj.SetActive(false);
    }

    Sprite CreatePixelSprite(Color color, Vector2 pivot)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        // L'argument 1f definit le pixelsPerUnit
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), pivot, 1f);
    }

    void UpdateHealthBar()
    {
        if (healthBarObj == null) CreateHealthBar();
        healthBarObj.SetActive(true);
        float pct = Mathf.Clamp01((float)currentHealth / maxHealth);
        healthBarFill.localScale = new Vector3(1.2f * pct, 0.15f, 1f);
    }
}