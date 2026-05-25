using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MissileSpawner : MonoBehaviour
{
    [Header("Configuration")]
    public Transform[] spawnPos;
    public GameObject[] missiles; // 0: Normal, 1: Rapide
    public static MissileSpawner instance;

    [Header("Difficulte")]
    public float initialSpawnDelay = 5.0f;
    public float minimumSpawnDelay = 1.0f;
    public float difficultyScaling = 0.05f;

    public int scoreMilestone = 100;
    public int milestoneIncreaser = 100, destroyedMissiles, destroyedEnemies;

    [Header("Stats en cours")]
    public int currentMissiles;
    public int missilesRequired = 1;
    private float timer;
    private float currentSpawnDelay;
    private bool gameStarted;

    // --- NOUVELLE VARIABLE ---
    private int lastSpawnIndex = -1;

    [Header("UI Indicators")]
    public GameObject indicatorPrefab;
    public Transform canvasTransform; // Glisse ton Canvas ici
    private List<OffScreenIndicator> activeIndicators = new List<OffScreenIndicator>();

    [Header("Reglages par Difficulte")]
    // Valeurs pour le mode Easy
    public float easyInitialDelay = 5.0f;
    public int easyMaxMissilesBatch = 3;
    public float easyFastMissileMultiplier = 2000f; // Diviseur (plus gros = moins de chance)

    // Valeurs pour le mode Hard
    public float hardInitialDelay = 3.0f;
    public int hardMaxMissilesBatch = 5;
    public float hardFastMissileMultiplier = 1000f; // Diviseur (plus petit = plus de chance)

    private float currentFastMissileMultiplier;
    private int currentMaxBatch;

    [Header("Wave Management")]
    public int currentWave = 1;
    public int missilesToSpawnThisWave;
    public int missilesSpawnedThisWave;
    public bool isTransitioningWave = false;

    [Header("Wave Settings")]
    [Tooltip("Frequence recurrente des vagues de combat (ex: toutes les 5 vagues). Mettre a 0 pour desactiver la recurrence.")]
    public int combatWaveFrequency = 5;

    [Tooltip("Liste specifique de vagues qui doivent etre en mode combat (ex: 2, 3...)")]
    public List<int> specificCombatWaves = new List<int>();

    [Header("Shooting Wave Settings")]
    public int baseEnemiesToSpawn = 5;
    public int enemiesToSpawnThisWave;
    public int enemiesDestroyedThisWave;
    public int activeEnemies;
    public GameObject enemyPrefab;

    bool justStarted = true;

    private GameObject proceduralEnemyTemplate;
    public TextMeshProUGUI waveBannerText;
    private Transform playerTransform;

    void LateUpdate() // On utilise LateUpdate pour que les indicateurs suivent apres le mouvement
    {
        for (int i = activeIndicators.Count - 1; i >= 0; i--)
        {
            if (activeIndicators[i] == null)
                activeIndicators.RemoveAt(i);
            else
                activeIndicators[i].UpdateIndicator();
        }
    }

    private void Awake()
    {
        if (instance == null) instance = this;
        currentSpawnDelay = initialSpawnDelay;
    }

    private void Start()
    {
        UpdateDifficulty();

        if (enemyPrefab == null)
        {
            proceduralEnemyTemplate = CreateProceduralEnemyPrefab();
        }

        CreateDynamicWaveUI();
    }

    public void UpdateDifficulty()
    {
        string diff = PlayerPrefs.GetString("Difficulty", "Easy");

        if (diff == "Easy")
        {
            initialSpawnDelay = easyInitialDelay;
            currentMaxBatch = easyMaxMissilesBatch;
            currentFastMissileMultiplier = easyFastMissileMultiplier;
        }
        else
        {
            initialSpawnDelay = hardInitialDelay;
            currentMaxBatch = hardMaxMissilesBatch;
            currentFastMissileMultiplier = hardFastMissileMultiplier;
        }

        // Si le jeu n'a pas encore commence, on applique le delai initial
        if (!gameStarted) currentSpawnDelay = initialSpawnDelay;
    }

    void Update()
    {
        if (Inventory.instance == null || !Inventory.instance.inPlay) return;

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (!gameStarted)
        {
            StartCoroutine(DelaySpawn());
            gameStarted = true;
        }

        // Only progress and handle timers in Dodging mode and when not transitioning
        if (!isTransitioningWave && !IsCombatWave(currentWave))
        {
            HandleProgression();
            HandleTimer();
        }
    }

    public bool IsCombatWave(int wave)
    {
        if (specificCombatWaves != null && specificCombatWaves.Contains(wave))
        {
            return true;
        }

        if (combatWaveFrequency > 0 && wave % combatWaveFrequency == 0)
        {
            return true;
        }

        return false;
    }

    void HandleTimer()
    {
        timer += Time.deltaTime;
        
        // Spawn missiles if delay met and we haven't reached the wave's spawn quota
        if (timer >= currentSpawnDelay && missilesSpawnedThisWave < missilesToSpawnThisWave)
        {
            int toSpawn = Mathf.Min(missilesRequired, missilesToSpawnThisWave - missilesSpawnedThisWave);
            SpawnMissileBatch(toSpawn);
            timer = 0;
        }

        // Check if all missiles spawned have been destroyed/escaped and we finished spawning
        if (missilesSpawnedThisWave >= missilesToSpawnThisWave && currentMissiles <= 0 && !isTransitioningWave && !justStarted)
        {
            Debug.Log("Transitioning to next wave");
            StartCoroutine(TransitionToNextWave());
        }
    }

    void HandleProgression()
    {
        if (Inventory.instance.score >= scoreMilestone)
        {
            scoreMilestone += milestoneIncreaser;
            // Utilise la difficulte choisie
            if (missilesRequired < currentMaxBatch) missilesRequired++;

            currentSpawnDelay = Mathf.Max(minimumSpawnDelay, currentSpawnDelay - difficultyScaling);
        }
    }

    IEnumerator DelaySpawn()
    {
        yield return new WaitForSeconds(1.5f);
        
        currentWave = 1;
        isTransitioningWave = false;
        
        if (IsCombatWave(currentWave))
        {
            // Activer le mode shooting
            if (PlayerMovement.instance != null)
            {
                PlayerMovement.instance.currentPhase = PlayerMovement.GamePhase.Shooting;
                PlayerMovement.instance.transform.rotation = Quaternion.identity;
                PlayerMovement.instance.InitializeCameraProxyY();
            }

            yield return StartCoroutine(ShowWaveBanner("VAGUE " + currentWave + "\nMODE COMBAT !", Color.red));

            int divisionFactor = combatWaveFrequency > 0 ? combatWaveFrequency : 5;
            enemiesToSpawnThisWave = baseEnemiesToSpawn + (currentWave / divisionFactor) * 2;
            enemiesDestroyedThisWave = 0;
            activeEnemies = 0;

            // Spawn enemies in a beautiful grid occupying the whole top screen
            StartCoroutine(SpawnEnemyGridRoutine());
        }
        else
        {
            missilesToSpawnThisWave = 3 + currentWave * 2; // Wave 1: 5 missiles
            missilesSpawnedThisWave = 0;

            yield return StartCoroutine(ShowWaveBanner("VAGUE " + currentWave, new Color(1f, 0.9f, 0f)));
            
            int toSpawn = Mathf.Min(missilesRequired, missilesToSpawnThisWave);
            SpawnMissileBatch(toSpawn);
        }

        justStarted = false;
    }

    void SpawnMissileBatch(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // --- LOGIQUE ANTI-REPETITION ---
            int randomIndex = Random.Range(0, spawnPos.Length);

            // Si on a plus d'un point de spawn, on boucle tant qu'on tombe sur le meme que le precedent
            if (spawnPos.Length > 1)
            {
                while (randomIndex == lastSpawnIndex)
                {
                    randomIndex = Random.Range(0, spawnPos.Length);
                }
            }

            lastSpawnIndex = randomIndex; // On enregistre le nouveau point utilise
            SpawnSingleMissile(randomIndex);
        }
    }

    // Overload for backward compatibility
    void SpawnMissileBatch()
    {
        int toSpawn = Mathf.Min(missilesRequired, missilesToSpawnThisWave - missilesSpawnedThisWave);
        SpawnMissileBatch(toSpawn);
    }

    void SpawnSingleMissile(int posIndex)
    {
        Vector3 spawnPosVector = new Vector3(spawnPos[posIndex].position.x, spawnPos[posIndex].position.y, 0);

        int missileType = 0;
        // En Hard (1000f), la chance monte 2x plus vite qu'en Easy (2000f)
        float fastMissileChance = Mathf.Clamp(Inventory.instance.score / currentFastMissileMultiplier, 0f, 0.7f);

        if (Random.value < fastMissileChance && missiles.Length > 1)
        {
            missileType = 1; // Missile Rapide
        }

        // ON NE GARDE QU'UN SEUL INSTANTIATE ICI
        GameObject missile = Instantiate(missiles[missileType], spawnPosVector, Quaternion.identity);
        ParticleSystem ps = missile.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            ps.Clear();
            ps.Play();
        }

        // CREATION DE L'INDICATEUR
        if (indicatorPrefab != null && canvasTransform != null)
        {
            GameObject indObj = Instantiate(indicatorPrefab, canvasTransform);
            OffScreenIndicator indScript = indObj.GetComponent<OffScreenIndicator>();
            indScript.target = missile.transform;

            if (missileType == 1) { indObj.GetComponent<Image>().color = Color.red; indObj.transform.GetChild(0).GetComponent<Image>().color = Color.white; }
            else { indObj.transform.GetChild(0).GetComponent<Image>().color = Color.red; }

            activeIndicators.Add(indScript);
        }

        currentMissiles++;
        missilesSpawnedThisWave++;
    }

    public void ResetData()
    {
        currentMissiles = 0;
        destroyedMissiles = 0;
        destroyedEnemies = 0;
        missilesRequired = 1;
        
        currentWave = 1;
        isTransitioningWave = false;
        gameStarted = false;
        justStarted = true;
        missilesSpawnedThisWave = 0;
        missilesToSpawnThisWave = 0;
        timer = 0f;

        // Reset player to Dodging mode
        if (PlayerMovement.instance != null)
        {
            PlayerMovement.instance.currentPhase = PlayerMovement.GamePhase.Dodging;
            PlayerMovement.instance.RefreshMovementMode();
            PlayerMovement.instance.transform.rotation = Quaternion.identity;
        }

        // Clear any active enemies
        GameObject[] activeEnemiesInGame = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject e in activeEnemiesInGame) Destroy(e);
        
        // Clear any active bullets
        GameObject[] activeBullets = GameObject.FindGameObjectsWithTag("Bullet");
        foreach (GameObject b in activeBullets) Destroy(b);
    }

    public void DestroyAllMissiles()
    {
        GameObject[] missilesInGame = GetAllMissiles();
        foreach (GameObject m in missilesInGame) Destroy(m);
        currentMissiles = 0;
    }

    public GameObject[] GetAllMissiles()
    {
        return GameObject.FindGameObjectsWithTag("Missile");
    }

    // --- GESTION DES VAGUES ---

    IEnumerator TransitionToNextWave()
    {
        isTransitioningWave = true;
        
        // Attendre que l'ecran soit propre
        yield return new WaitForSeconds(1.2f);
        
        // Nettoyer tous les missiles (comme les missiles tirés par les ennemis)
        DestroyAllMissiles();

        currentWave++;

        // Mode combat de l'espace si la vague est de type combat
        if (IsCombatWave(currentWave))
        {
            yield return StartCoroutine(ShowWaveBanner("VAGUE " + currentWave + "\nMODE COMBAT !", Color.red));

            // Activer le mode shooting
            if (PlayerMovement.instance != null)
            {
                PlayerMovement.instance.currentPhase = PlayerMovement.GamePhase.Shooting;
                // LOCK rotation straight up when entering combat!
                PlayerMovement.instance.transform.rotation = Quaternion.identity;
                
                // Initialize the camera proxy Y coordinate immediately so it is guaranteed to be correct!
                PlayerMovement.instance.InitializeCameraProxyY();
            }

            int divisionFactor = combatWaveFrequency > 0 ? combatWaveFrequency : 5;
            enemiesToSpawnThisWave = baseEnemiesToSpawn + (currentWave / divisionFactor) * 2;
            enemiesDestroyedThisWave = 0;
            activeEnemies = 0;

            // Spawn enemies in a beautiful grid occupying the whole top screen
            StartCoroutine(SpawnEnemyGridRoutine());
        }
        else
        {
            // Mode dodge standard
            if (PlayerMovement.instance != null)
            {
                PlayerMovement.instance.currentPhase = PlayerMovement.GamePhase.Dodging;
                // REMOVED rotation reset here so the player's plane doesn't rotate unexpectedly during dodging wave changes!
            }

            yield return StartCoroutine(ShowWaveBanner("VAGUE " + currentWave, new Color(1f, 0.9f, 0f)));

            missilesToSpawnThisWave = 3 + currentWave * 2;
            missilesSpawnedThisWave = 0;
            timer = 0f;

            // Spawn premier lot
            int toSpawn = Mathf.Min(missilesRequired, missilesToSpawnThisWave);
            SpawnMissileBatch(toSpawn);
        }

        isTransitioningWave = false;
    }

    IEnumerator SpawnEnemyGridRoutine()
    {
        if (playerTransform == null) yield break;

        // Calculate the camera's base target Y coordinate using the pre-initialized value from PlayerMovement.cs
        // This ensures the grid center is perfectly stable and unaffected by the player's momentary Y joystick offsets!
        float baseCameraProxyY = PlayerMovement.instance != null ? PlayerMovement.instance.cameraProxyY : (playerTransform.position.y + 3.0f);

        int count = enemiesToSpawnThisWave;
        int cols = 3;
        if (count >= 8) cols = 4; // Use 4 columns for denser formations

        for (int i = 0; i < count; i++)
        {
            int c = i % cols;
            int r = i / cols;

            // Distribute column X positions nicely between -1.4f and 1.4f to ensure they stay well inside the narrow mobile screen
            float posX = -1.4f + c * (2.8f / Mathf.Max(1, cols - 1));
            
            // Distribute row Y positions perfectly spanning the ENTIRE top-half of the viewport (from 0.8f up to 4.0f relative to camera center)
            float posY = 3f + r * 1.6f;

            SpawnEnemyAtPosition(posX, baseCameraProxyY + posY);

            // Stagger spawn delay
            yield return new WaitForSeconds(0.15f);
        }
    }

    void SpawnEnemyAtPosition(float relX, float absY)
    {
        if (playerTransform == null) return;

        // Vector3 uses playerTransform.position.x + relX for horizontal position, and the absolute Y coordinate!
        Vector3 spawnPosVector = new Vector3(PlayerMovement.instance.cameraTargetProxy.position.x + relX, absY, 0);

        GameObject prefab = enemyPrefab != null ? enemyPrefab : proceduralEnemyTemplate;
        if (prefab != null)
        {
            // Rotated 180 degrees on the Z axis so the enemy ship faces downwards towards the player!
            Quaternion enemyRotation = Quaternion.Euler(0, 0, 180f);
            GameObject enemyObj = Instantiate(prefab, spawnPosVector, enemyRotation);
            enemyObj.SetActive(true);
            activeEnemies++;
        }
    }

    // Keep this method signature for compatibility
    void SpawnEnemy()
    {
        if (playerTransform == null) return;
        float baseCameraProxyY = playerTransform.position.y + 3.0f;
        SpawnEnemyAtPosition(Random.Range(-2.2f, 2.2f), baseCameraProxyY + 2.0f);
    }

    public void OnEnemyDestroyed()
    {
        activeEnemies--;
        enemiesDestroyedThisWave++;

        // Si tous les ennemis de la vague sont detruits, on passe a la vague suivante
        if (enemiesDestroyedThisWave >= enemiesToSpawnThisWave && activeEnemies <= 0 && isTransitioningWave == false)
        {
            StartCoroutine(TransitionToNextWave());
        }
    }

    // --- UI DYNAMIQUE DES VAGUES ---

    void CreateDynamicWaveUI()
    {
        Transform targetCanvas = canvasTransform;
        
        if (targetCanvas == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null) targetCanvas = canvas.transform;
        }

        if (targetCanvas == null) return;

        waveBannerText.gameObject.SetActive(true);



        waveBannerText.fontSize = 50;
        waveBannerText.fontStyle = FontStyles.Bold;
        waveBannerText.color = new Color(1f, 0.9f, 0f, 0f); // transparent au depart

        // Positionnement au milieu-haut de l'ecran
        

        waveBannerText.gameObject.SetActive(false);
    }

    IEnumerator ShowWaveBanner(string message, Color color)
    {
        if (waveBannerText == null) CreateDynamicWaveUI();

        if (waveBannerText != null)
        {
            waveBannerText.gameObject.SetActive(true);
            waveBannerText.text = message;
            waveBannerText.color = new Color(color.r, color.g, color.b, 0f);

            float duration = 0.5f;
            float elapsed = 0f;
            Vector3 targetScale = Vector3.one * 1.2f;
            waveBannerText.transform.localScale = Vector3.one * 0.5f;

            // Fade In & Scale Up
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                waveBannerText.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0f, 1f, t));
                waveBannerText.transform.localScale = Vector3.Lerp(Vector3.one * 0.5f, targetScale, t);
                yield return null;
            }

            waveBannerText.color = color;
            waveBannerText.transform.localScale = targetScale;

            yield return new WaitForSeconds(1.6f);

            // Fade Out & Scale Down
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                waveBannerText.color = new Color(color.r, color.g, color.b, Mathf.Lerp(1f, 0f, t));
                waveBannerText.transform.localScale = Vector3.Lerp(targetScale, Vector3.one * 0.8f, t);
                yield return null;
            }

            waveBannerText.gameObject.SetActive(false);
        }
    }

    // Generateur procedural d'ennemi en cas d'absence de prefab dans l'editeur
    private GameObject CreateProceduralEnemyPrefab()
    {
        GameObject enemy = new GameObject("ProceduralSpaceEnemy");
        enemy.tag = "Enemy";

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        
        // Texture de 32x32 representant un vaisseau ennemi sleek en triangle
        Texture2D tex = new Texture2D(32, 32);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                // Forme triangulaire pointant vers le bas
                bool inTriangle = (x >= y / 2) && (31 - x >= y / 2);
                if (inTriangle)
                {
                    float distToCenter = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16)) / 16f;
                    // Couleur rouge metallique avec coeur orange brillant
                    Color c = Color.Lerp(new Color(1f, 0.3f, 0f), new Color(0.3f, 0.05f, 0.05f), distToCenter);
                    tex.SetPixel(x, y, c);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        sr.sortingOrder = 4;

        enemy.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

        // Collider 2D
        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.2f, 1.2f);
        col.isTrigger = true;

        // Rigidbody 2D Kinematic
        Rigidbody2D rb2d = enemy.AddComponent<Rigidbody2D>();
        rb2d.bodyType = RigidbodyType2D.Kinematic;

        // Script de comportement
        enemy.AddComponent<EnemyScript>();

        enemy.SetActive(false);
        DontDestroyOnLoad(enemy);

        return enemy;
    }
}