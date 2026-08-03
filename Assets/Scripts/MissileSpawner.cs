using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization.Settings;

public class MissileSpawner : MonoBehaviour
{
    [Header("Configuration")]
    public Transform[] spawnPos;
    public GameObject[] missiles; // 0: Normal, 1: Rapide
    public static MissileSpawner instance;

    [Header("Localization")]
    public string stringTableName = "UITexts";

    private string GetTranslation(string key, string fallback)
    {
        string tr = LocalizationSettings.StringDatabase.GetLocalizedString(stringTableName, key);
        if (string.IsNullOrEmpty(tr) || tr.Contains("No translation")) return fallback;
        return tr;
    }

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

    // --- ANTI-REPETITION ---
    private int lastSpawnIndex = -1;

    [Header("Audio Settings")]
    [Tooltip("Son joué quand un Tracker ou un Rammer apparaît")]
    public AudioClip hunterWarningSound;
    public float hunterWarningVolume = 0.8f;
    private AudioSource spawnerAudioSource;

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

    // ==========================================================
    // SYSTÈME DE DENSITÉ CONTINUE (remplace les vagues)
    // ==========================================================

    [Header("Courbe de Densité Continue")]
    [Tooltip("Le score à partir duquel le Tracker peut apparaître.")]
    public int trackerScoreThreshold = 300;

    [Tooltip("Le score à partir duquel le Rammer peut apparaître.")]
    public int rammerScoreThreshold = 800;

    [Tooltip("Le score à partir duquel Tracker et Rammer peuvent apparaître simultanément.")]
    public int bothHuntersScoreThreshold = 1500;

    [Tooltip("Probabilité de base qu'un chasseur spawn à chaque cycle de spawn de missiles (0 à 1).")]
    public float baseHunterSpawnChance = 0.08f;

    [Tooltip("Augmentation de la probabilité de chasseur par point de score.")]
    public float hunterChancePerScore = 0.00005f;

    [Tooltip("Probabilité max de spawn de chasseur par cycle.")]
    public float maxHunterSpawnChance = 0.35f;

    [Tooltip("Cooldown minimum en secondes entre deux chasseurs.")]
    public float hunterCooldown = 12f;

    [Tooltip("Réduction du cooldown par seconde de run écoulée.")]
    public float cooldownReductionPerSecond = 0.05f;

    [Tooltip("Cooldown minimum absolu.")]
    public float minHunterCooldown = 5f;

    private float lastHunterSpawnTime = -999f;
    private int activeHunters = 0;

    [Header("Hunter Prefabs")]
    [Tooltip("Prefab du Tracker. Laisser vide pour utiliser le template procédural.")]
    public GameObject trackerPrefab;

    [Tooltip("Prefab du Rammer. Laisser vide pour utiliser le template procédural.")]
    public GameObject rammerPrefab;

    // Templates procéduraux (fallback)
    private GameObject proceduralTrackerTemplate;
    private GameObject proceduralRammerTemplate;

    [Header("UI Bannière (optionnel)")]
    public TextMeshProUGUI waveBannerText;

    // --- Compatibilité : gardé pour les références externes ---
    [HideInInspector] public int currentWave = 0; // Plus utilisé activement, gardé pour les scripts qui le lisent

    bool justStarted = true;
    private Transform playerTransform;

    void LateUpdate()
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

        spawnerAudioSource = gameObject.AddComponent<AudioSource>();
        spawnerAudioSource.spatialBlend = 0f; // Son 2D global
    }

    private void Start()
    {
        UpdateDifficulty();
        CreateHunterTemplates();
    }

    public void UpdateDifficulty()
    {
        string diff = PlayerPrefs.GetString("Difficulty", "Easy");

        if (diff == "Easy")
        {
            initialSpawnDelay = easyInitialDelay;
            currentMaxBatch = easyMaxMissilesBatch;
            currentFastMissileMultiplier = easyFastMissileMultiplier;
            // Courbe modérée en Easy
            hunterCooldown = 15f;
            baseHunterSpawnChance = 0.12f;
            hunterChancePerScore = 0.0001f;
        }
        else
        {
            initialSpawnDelay = hardInitialDelay;
            currentMaxBatch = hardMaxMissilesBatch;
            currentFastMissileMultiplier = hardFastMissileMultiplier;
            // Courbe agressive en Hard
            hunterCooldown = 8f;
            baseHunterSpawnChance = 0.20f;
            hunterChancePerScore = 0.00015f;
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

        HandleProgression();
        HandleTimer();
    }

    void HandleTimer()
    {
        timer += Time.deltaTime;
        
        if (timer >= currentSpawnDelay)
        {
            int toSpawn = missilesRequired;
            SpawnMissileBatch(toSpawn);
            timer = 0;

            // --- SYSTÈME DE CHASSEURS : vérifier si on doit spawner un chasseur ---
            TrySpawnHunter();
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

        // Premier lot de missiles
        int toSpawn = missilesRequired;
        SpawnMissileBatch(toSpawn);

        justStarted = false;
    }

    // ==========================================================
    // SPAWN DE MISSILES (inchangé dans la logique)
    // ==========================================================

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

            lastSpawnIndex = randomIndex;
            SpawnSingleMissile(randomIndex);
        }
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
    }

    // ==========================================================
    // SYSTÈME DE CHASSEURS (DENSITÉ CONTINUE)
    // ==========================================================

    void TrySpawnHunter()
    {
        if (Inventory.instance == null) return;

        int score = Inventory.instance.score;
        float runTime = Inventory.instance.totalSeconds;

        // Pas encore le seuil pour les chasseurs
        if (score < trackerScoreThreshold) return;

        // Cooldown entre chasseurs (diminue avec le temps de run)
        float effectiveCooldown = Mathf.Max(minHunterCooldown, hunterCooldown - runTime * cooldownReductionPerSecond);
        if (Time.time - lastHunterSpawnTime < effectiveCooldown) return;

        // Limiter à 1 chasseur actif à la fois (sauf si seuil simultané atteint)
        int maxActiveHunters = score >= bothHuntersScoreThreshold ? 2 : 1;
        if (activeHunters >= maxActiveHunters) return;

        // Probabilité de spawn
        float spawnChance = Mathf.Min(baseHunterSpawnChance + score * hunterChancePerScore, maxHunterSpawnChance);

        if (Random.value < spawnChance)
        {
            // Choisir le type de chasseur
            bool canSpawnTracker = score >= trackerScoreThreshold;
            bool canSpawnRammer = score >= rammerScoreThreshold;

            if (canSpawnTracker && canSpawnRammer)
            {
                // Les deux sont disponibles : choix aléatoire pondéré
                // Le Rammer est plus rare (40% de chance)
                if (Random.value < 0.4f)
                    SpawnRammer();
                else
                    SpawnTracker();
            }
            else if (canSpawnTracker)
            {
                SpawnTracker();
            }
            // canSpawnRammer seul ne devrait pas arriver car son seuil est plus haut
        }
    }

    void SpawnTracker()
    {
        if (playerTransform == null) return;

        // Spawn en dehors de l'écran, sur un côté aléatoire
        Vector3 spawnOffset = GetHunterSpawnPosition();

        GameObject prefab = trackerPrefab != null ? trackerPrefab : proceduralTrackerTemplate;
        if (prefab == null) return;

        GameObject tracker = Instantiate(prefab, playerTransform.position + spawnOffset, Quaternion.identity);
        tracker.SetActive(true);

        activeHunters++;
        lastHunterSpawnTime = Time.time;

        // Indicateur hors-écran pour le Tracker
        CreateHunterIndicator(tracker.transform, new Color(0f, 0.9f, 1f)); // Cyan

        // Notification visuelle subtile (pas de bannière intrusive)
        StartCoroutine(ShowHunterWarning("TRACKER"));
    }

    void SpawnRammer()
    {
        if (playerTransform == null) return;

        // Spawn au-dessus du joueur, en avance
        Vector3 spawnOffset = Vector3.up * 12f + Vector3.right * Random.Range(-3f, 3f);

        GameObject prefab = rammerPrefab != null ? rammerPrefab : proceduralRammerTemplate;
        if (prefab == null) return;

        GameObject rammer = Instantiate(prefab, playerTransform.position + spawnOffset, Quaternion.identity);
        rammer.SetActive(true);

        activeHunters++;
        lastHunterSpawnTime = Time.time;

        // Indicateur hors-écran pour le Rammer
        CreateHunterIndicator(rammer.transform, new Color(1f, 0.3f, 0.1f)); // Orange-rouge

        // Notification visuelle
        StartCoroutine(ShowHunterWarning("RAMMER"));
    }

    Vector3 GetHunterSpawnPosition()
    {
        // Spawn en dehors du champ de vision, sur un côté aléatoire
        int side = Random.Range(0, 4);
        switch (side)
        {
            case 0: return new Vector3(0, 10f, 0);                      // Haut
            case 1: return new Vector3(0, -8f, 0);                      // Bas
            case 2: return new Vector3(-6f, Random.Range(2f, 8f), 0);   // Gauche
            case 3: return new Vector3(6f, Random.Range(2f, 8f), 0);    // Droite
            default: return Vector3.up * 10f;
        }
    }

    void CreateHunterIndicator(Transform hunterTransform, Color color)
    {
        if (indicatorPrefab == null || canvasTransform == null) return;

        GameObject indObj = Instantiate(indicatorPrefab, canvasTransform);
        OffScreenIndicator indScript = indObj.GetComponent<OffScreenIndicator>();
        indScript.target = hunterTransform;

        // Couleur distinctive pour le chasseur
        Image mainImg = indObj.GetComponent<Image>();
        if (mainImg != null) mainImg.color = color;

        if (indObj.transform.childCount > 0)
        {
            Image childImg = indObj.transform.GetChild(0).GetComponent<Image>();
            if (childImg != null) childImg.color = Color.white;
        }

        activeIndicators.Add(indScript);
    }

    /// <summary>
    /// Appelé par un chasseur quand il est détruit ou se désengage.
    /// </summary>
    public void OnHunterDestroyed()
    {
        activeHunters = Mathf.Max(0, activeHunters - 1);
    }

    /// <summary>
    /// Compatibilité : appelé par EnemyScript quand un ennemi est détruit.
    /// Plus de logique de vagues, mais le compteur reste utile pour les stats.
    /// </summary>
    public void OnEnemyDestroyed()
    {
        destroyedEnemies++;
    }

    // ==========================================================
    // AVERTISSEMENT VISUEL (petit texte rapide, pas une bannière de vague)
    // ==========================================================

    IEnumerator ShowHunterWarning(string hunterType)
    {
        // Jouer le son d'avertissement
        if (hunterWarningSound != null && spawnerAudioSource != null)
        {
            spawnerAudioSource.PlayOneShot(hunterWarningSound, hunterWarningVolume);
        }

        if (waveBannerText == null) yield break;

        string warningMsg = hunterType;
        Color warningColor = hunterType == "TRACKER" ? new Color(0f, 0.9f, 1f) : new Color(1f, 0.3f, 0.1f);

        waveBannerText.gameObject.SetActive(true);
        waveBannerText.text = warningMsg;
        waveBannerText.fontSize = 36;
        waveBannerText.fontStyle = FontStyles.Bold;
        waveBannerText.color = new Color(warningColor.r, warningColor.g, warningColor.b, 0f);
        waveBannerText.transform.localScale = Vector3.one;

        // Fade in rapide
        float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            waveBannerText.color = new Color(warningColor.r, warningColor.g, warningColor.b, Mathf.Lerp(0f, 1f, t));
            yield return null;
        }

        yield return new WaitForSeconds(0.8f);

        // Fade out
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            waveBannerText.color = new Color(warningColor.r, warningColor.g, warningColor.b, Mathf.Lerp(1f, 0f, t));
            yield return null;
        }

        waveBannerText.gameObject.SetActive(false);
    }

    // ==========================================================
    // RESET / NETTOYAGE
    // ==========================================================

    public void ResetData()
    {
        currentMissiles = 0;
        destroyedMissiles = 0;
        destroyedEnemies = 0;
        missilesRequired = 1;
        
        gameStarted = false;
        justStarted = true;
        timer = 0f;
        activeHunters = 0;
        lastHunterSpawnTime = -999f;

        // Reset player to Dodging mode
        if (PlayerMovement.instance != null)
        {
            PlayerMovement.instance.currentPhase = PlayerMovement.GamePhase.Dodging;
            PlayerMovement.instance.RefreshMovementMode();
            PlayerMovement.instance.transform.rotation = Quaternion.identity;
        }

        // Clear any active hunters
        DestroyAllHunters();

        // Clear any active enemies (compatibilité)
        GameObject[] activeEnemiesInGame = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject e in activeEnemiesInGame) Destroy(e);
        
        // Clear any active bullets
        GameObject[] activeBullets = GameObject.FindGameObjectsWithTag("Bullet");
        foreach (GameObject b in activeBullets) Destroy(b);
    }

    void DestroyAllHunters()
    {
        // Détruire tous les Trackers
        GameObject[] trackers = GameObject.FindGameObjectsWithTag("Tracker");
        foreach (GameObject t in trackers) Destroy(t);

        // Détruire tous les Rammers
        GameObject[] rammers = GameObject.FindGameObjectsWithTag("Rammer");
        foreach (GameObject r in rammers) Destroy(r);

        activeHunters = 0;
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

    // ==========================================================
    // TEMPLATES PROCÉDURAUX POUR LES CHASSEURS (FALLBACK)
    // ==========================================================

    void CreateHunterTemplates()
    {
        // Tracker template
        if (trackerPrefab == null)
        {
            proceduralTrackerTemplate = new GameObject("ProceduralTracker");
            proceduralTrackerTemplate.AddComponent<Tracker>();
            proceduralTrackerTemplate.SetActive(false);
            DontDestroyOnLoad(proceduralTrackerTemplate);
        }

        // Rammer template
        if (rammerPrefab == null)
        {
            proceduralRammerTemplate = new GameObject("ProceduralRammer");
            proceduralRammerTemplate.AddComponent<Rammer>();
            proceduralRammerTemplate.SetActive(false);
            DontDestroyOnLoad(proceduralRammerTemplate);
        }
    }
}
