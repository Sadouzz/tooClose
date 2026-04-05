using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpawnObjects : MonoBehaviour
{
    [Header("Prefabs & Probabilities")]
    public GameObject starPrefab;         // Ton objet index 0 (Etoile)
    public GameObject[] powerUpPrefabs;    // Le reste des objets

    [Range(0, 100)]
    public float starSpawnChance = 80f;    // 80% de chances d'avoir une étoile

    [Header("References")]
    public Transform[] spawnPos;
    public float maxDistance = 50f;
    public float checkInterval = 0.5f;

    private float timer;
    private float nextCheckTime;
    private GameObject player;

    [Header("Difficulty Settings")]
    public float easyStarChance = 85f;    // Beaucoup d'étoiles en facile
    public float hardStarChance = 50f;    // Moins d'étoiles en difficile (plus de powerups ou juste plus vide)
    public float easySpawnInterval = 1.2f;
    public float hardSpawnInterval = 0.8f;

    public float currentSpawnInterval;

    public static SpawnObjects instance;

    private void Awake()
    {
        if (instance != null) return;
        instance = this;
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        // Premier rafraîchissement au démarrage
        RefreshSpawningPositions();
        UpdateDifficulty();
    }

    void Update()
    {
        if (Inventory.instance != null && !Inventory.instance.inPlay) return;

        // Gestion du Timer de Spawn
        timer += Time.deltaTime;
        if (timer >= currentSpawnInterval) // Un peu plus lent pour ne pas flood l'écran
        {
            SpawnRandomObject();
            timer = 0;
        }

        // Rafraîchir les positions valides régulièrement
        if (Time.time > nextCheckTime)
        {
            RefreshSpawningPositions();
            nextCheckTime = Time.time + checkInterval;
        }
    }

    public void UpdateDifficulty()
    {
        string diff = PlayerPrefs.GetString("Difficulty", "Easy");

        if (diff == "Easy")
        {
            starSpawnChance = easyStarChance;
            currentSpawnInterval = easySpawnInterval;
        }
        else
        {
            starSpawnChance = hardStarChance;
            currentSpawnInterval = hardSpawnInterval;
        }
    }

    public void RefreshSpawningPositions()
    {
        GameObject[] spawnPosGameObjects = GameObject.FindGameObjectsWithTag("SpawnPos");
        List<Transform> validSpawns = new List<Transform>();

        foreach (var sp in spawnPosGameObjects)
        {
            float dist = Vector3.Distance(player.transform.position, sp.transform.position);

            // On ne garde que les spawns à bonne distance et HORS ÉCRAN pour ne pas voir l'objet apparaître
            if (dist < maxDistance && !IsInFieldOfView(sp.transform.position))
            {
                validSpawns.Add(sp.transform);
            }
        }
        spawnPos = validSpawns.ToArray();
    }

    private bool IsInFieldOfView(Vector3 worldPosition)
    {
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(worldPosition);
        return screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
    }

    void SpawnRandomObject()
    {
        // Sécurité de base
        if (spawnPos.Length == 0 || (starPrefab == null && powerUpPrefabs.Length == 0))
            return;

        // --- 1. SÉLECTION DE L'OBJET (PROBABILITÉ) ---
        GameObject prefabToSpawn;
        float roll = Random.Range(0f, 100f);

        if (roll <= starSpawnChance)
        {
            prefabToSpawn = starPrefab;
        }
        else
        {
            if (powerUpPrefabs.Length > 0)
                prefabToSpawn = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Length)];
            else
                prefabToSpawn = starPrefab; // Fallback sur l'étoile si pas de powerups définis
        }

        // --- 2. LOGIQUE DE POSITIONNEMENT (VÉRIFICATION DE PLACE) ---
        float checkRadius = 0.8f;
        int maxTries = 5; // On limite à 5 essais pour ne pas geler le jeu si tout est plein
        int tries = 0;

        while (tries < maxTries)
        {
            int r = Random.Range(0, spawnPos.Length);
            Vector2 spawnPosition = spawnPos[r].position;

            // On utilise OverlapCircleAll pour scanner la zone en 2D
            Collider2D[] colliders = Physics2D.OverlapCircleAll(spawnPosition, checkRadius);

            bool areaClear = true;

            foreach (var col in colliders)
            {
                // Liste des tags qui bloquent le spawn
                if (col.CompareTag("PowerUp") || col.CompareTag("Star") ||
                    col.CompareTag("Missile"))
                {
                    areaClear = false;
                    break;
                }
            }

            if (areaClear)
            {
                // Si la zone est libre, on fait apparaître l'objet
                Instantiate(prefabToSpawn, new Vector3(spawnPosition.x, spawnPosition.y, 0f), Quaternion.identity);
                return; // On sort de la fonction car le spawn a réussi
            }

            tries++;
        }

        // Debug.Log("Espace encombré, spawn annulé pour ce tick.");
    }
    public void DestroyAllObjects()
    {
        // On nettoie tout ce qui a un tag Star ou PowerUp
        string[] tags = { "Star", "PowerUp" };
        foreach (string t in tags)
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(t);
            foreach (GameObject obj in objects) Destroy(obj);
        }
    }
}