using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

namespace Connection
{
    public class PuppetMasterManager : NetworkBehaviour
    {
        public static PuppetMasterManager instance;

        public enum GameState { Waiting, Round1, Intermission, Round2, GameOver }

        [Header("State")]
        [SyncVar] public GameState currentState = GameState.Waiting;
        [SyncVar] public float roundTimer = 90f;
        [SyncVar] public int currentRound = 1;

        [Header("Scores (Nombre de fois où on a tué le pilote)")]
        [SyncVar] public int player1Score = 0; // J1 en tant que Missile
        [SyncVar] public int player2Score = 0; // J2 en tant que Missile

        [Header("Spawn Points")]
        public List<Transform> missileSpawnPoints;
        public Transform pilotSpawnPoint;

        public float roundDuration = 90f;

        private void Awake()
        {
            if (instance == null) instance = this;
        }

        public override void OnStartServer()
        {
            StartCoroutine(GameLoop());
        }

        [Server]
        private IEnumerator GameLoop()
        {
            // Attendre que les deux joueurs soient connectés
            while (NetworkServer.connections.Count < 2)
            {
                yield return null;
            }

            // --- ROUND 1 ---
            currentState = GameState.Round1;
            currentRound = 1;
            roundTimer = roundDuration;
            
            // J1 = Pilote, J2 = Missile
            RespawnAllPlayers();

            while (roundTimer > 0)
            {
                roundTimer -= Time.deltaTime;
                yield return null;
            }

            // --- INTERMISSION ---
            currentState = GameState.Intermission;
            roundTimer = 3f; // 3 secondes de pause
            
            // Nettoyage avant le swap
            DestroyAllPlayers();
            
            while (roundTimer > 0)
            {
                roundTimer -= Time.deltaTime;
                yield return null;
            }

            // --- ROUND 2 ---
            currentState = GameState.Round2;
            currentRound = 2;
            roundTimer = roundDuration;
            
            // SWAP: J1 = Missile, J2 = Pilote
            RespawnAllPlayers();

            while (roundTimer > 0)
            {
                roundTimer -= Time.deltaTime;
                yield return null;
            }

            // --- GAME OVER ---
            currentState = GameState.GameOver;
            DestroyAllPlayers();
        }

        [Server]
        public void RegisterKill(NetworkConnectionToClient killerConn)
        {
            // Dans ce mode, celui qui tue est toujours le Missile
            // Si c'est le Round 1, le J2 est le Missile
            if (currentRound == 1)
            {
                player2Score++;
            }
            // Si c'est le Round 2, le J1 est le Missile
            else if (currentRound == 2)
            {
                player1Score++;
            }
        }

        [Server]
        private void RespawnAllPlayers()
        {
            if (TooCloseNetworkManager.instance != null)
            {
                TooCloseNetworkManager.instance.SpawnPlayersForRound(currentRound);
            }
        }

        [Server]
        private void DestroyAllPlayers()
        {
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn.identity != null)
                {
                    NetworkServer.Destroy(conn.identity.gameObject);
                }
            }
        }

        public Transform GetRandomMissileSpawnPoint()
        {
            if (missileSpawnPoints == null || missileSpawnPoints.Count == 0) return null;
            return missileSpawnPoints[Random.Range(0, missileSpawnPoints.Count)];
        }
    }
}
