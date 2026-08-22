using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Connection
{
    public class MissileSpawnerOnline : NetworkBehaviour
    {
        [Header("Prefabs")]
        public GameObject missilePrefab;
        public GameObject fastMissilePrefab;

        [Header("Spawn Settings")]
        public float startSpawnDelay = 5.0f;
        public float minimumSpawnDelay = 1.0f;
        public float spawnDistance = 15f; // Distance de spawn autour du Host

        private float currentSpawnDelay;
        private float spawnTimer;
        private Transform hostPlayerTransform;

        public override void OnStartServer()
        {
            // Initialisation uniquement sur le serveur
            currentSpawnDelay = startSpawnDelay;
            spawnTimer = currentSpawnDelay;
        }

        [ServerCallback]
        void Update()
        {
            // Seulement le serveur (Host) spawne les missiles
            if (!isServer) return;

            // Retrouver le transform du joueur Host pour spawner autour de lui
            if (hostPlayerTransform == null)
            {
                // Dans Mirror, NetworkClient.localPlayer pointe vers le joueur local (Host)
                if (NetworkClient.localPlayer != null)
                {
                    hostPlayerTransform = NetworkClient.localPlayer.transform;
                }
                else return; // Pas encore prêt
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnMissilesBatch();
                
                // Diminuer le delay progressivement pour la difficulté
                currentSpawnDelay = Mathf.Max(minimumSpawnDelay, currentSpawnDelay - 0.1f);
                spawnTimer = currentSpawnDelay;
            }
        }

        [Server]
        private void SpawnMissilesBatch()
        {
            // Nombre de missiles dans un batch
            int batchSize = Random.Range(1, 4);

            for (int i = 0; i < batchSize; i++)
            {
                // Calculer une position aleatoire en arc de cercle au-dessus du joueur
                float randomAngle = Random.Range(-60f, 60f);
                Vector3 spawnOffset = Quaternion.Euler(0, 0, randomAngle) * Vector3.up * spawnDistance;
                Vector3 spawnPos = hostPlayerTransform.position + spawnOffset;

                // 20% de chance d'avoir un missile rapide
                GameObject prefabToSpawn = (Random.value < 0.2f && fastMissilePrefab != null) ? fastMissilePrefab : missilePrefab;

                if (prefabToSpawn != null)
                {
                    GameObject missile = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                    
                    // Aligner le missile pour qu'il pointe vers le bas initialement (ou vers le joueur)
                    Vector3 dir = (hostPlayerTransform.position - spawnPos).normalized;
                    missile.transform.up = dir;

                    // IMPORTANT : Spawn sur le reseau Mirror
                    NetworkServer.Spawn(missile);
                }
            }
        }
    }
}
