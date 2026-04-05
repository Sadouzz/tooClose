using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MissileSpawner : MonoBehaviour
{
    [Header("Configuration")]
    public Transform[] spawnPos;
    public GameObject[] missiles; // 0: Normal, 1: Rapide
    public static MissileSpawner instance;

    [Header("Difficulté")]
    public float initialSpawnDelay = 5.0f;
    public float minimumSpawnDelay = 1.0f;
    public float difficultyScaling = 0.05f;

    public int scoreMilestone = 100;
    public int milestoneIncreaser = 100, destroyedMissiles;

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

    [Header("Réglages par Difficulté")]
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

    void LateUpdate() // On utilise LateUpdate pour que les indicateurs suivent après le mouvement
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

        // Si le jeu n'a pas encore commencé, on applique le délai initial
        if (!gameStarted) currentSpawnDelay = initialSpawnDelay;
    }

    void Update()
    {
        if (Inventory.instance == null || !Inventory.instance.inPlay) return;

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
        if (timer >= currentSpawnDelay && currentMissiles < 15)
        {
            SpawnMissileBatch();
            timer = 0;
        }
    }

    void HandleProgression()
    {
        if (Inventory.instance.score >= scoreMilestone)
        {
            scoreMilestone += milestoneIncreaser;
            // Utilise la limite selon la difficulté choisie
            if (missilesRequired < currentMaxBatch) missilesRequired++;

            currentSpawnDelay = Mathf.Max(minimumSpawnDelay, currentSpawnDelay - difficultyScaling);
        }
    }

    IEnumerator DelaySpawn()
    {
        yield return new WaitForSeconds(2f);
        SpawnMissileBatch();
    }

    void SpawnMissileBatch()
    {
        for (int i = 0; i < missilesRequired; i++)
        {
            // --- LOGIQUE ANTI-REPETITION ---
            int randomIndex = Random.Range(0, spawnPos.Length);

            // Si on a plus d'un point de spawn, on boucle tant qu'on tombe sur le même que le précédent
            if (spawnPos.Length > 1)
            {
                while (randomIndex == lastSpawnIndex)
                {
                    randomIndex = Random.Range(0, spawnPos.Length);
                }
            }

            lastSpawnIndex = randomIndex; // On enregistre le nouveau point utilisé
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

        // CRÉATION DE L'INDICATEUR
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

    public void ResetData()
    {
        currentMissiles = 0;
        destroyedMissiles = 0;
        missilesRequired = 1;
    }
    public void DestroyAllMissiles()
    {
        GameObject[] missilesInGame = GameObject.FindGameObjectsWithTag("Missile");
        foreach (GameObject m in missilesInGame) Destroy(m);
        currentMissiles = 0;
    }
}