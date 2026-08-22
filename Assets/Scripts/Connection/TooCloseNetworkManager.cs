using UnityEngine;
using Mirror;

namespace Connection
{
    public class TooCloseNetworkManager : NetworkManager
    {
        public static new TooCloseNetworkManager singleton => NetworkManager.singleton as TooCloseNetworkManager;
        
        public static TooCloseNetworkManager instance => singleton;

        [Header("Puppet Master Prefabs")]
        public GameObject pilotPrefab;
        public GameObject puppetMissilePrefab;

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            // Dans le mode Puppet Master, on ne fait pas spawner automatiquement le prefab classique.
            // Le PuppetMasterManager s'en occupera une fois les 2 joueurs connectes via SpawnPlayersForRound().
        }

        [Server]
        public void SpawnPlayersForRound(int round)
        {
            int index = 0;
            // On parcourt les connexions actives (J1 = Host, J2 = Client)
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (!conn.isReady) continue;

                GameObject prefabToSpawn = null;
                Transform spawnPoint = null;

                if (round == 1)
                {
                    // Round 1 : J1 (index 0) = Pilote, J2 (index 1) = Missile
                    if (index == 0)
                    {
                        prefabToSpawn = pilotPrefab;
                        spawnPoint = PuppetMasterManager.instance.pilotSpawnPoint;
                    }
                    else
                    {
                        prefabToSpawn = puppetMissilePrefab;
                        spawnPoint = PuppetMasterManager.instance.GetRandomMissileSpawnPoint();
                    }
                }
                else if (round == 2)
                {
                    // Round 2 : J1 (index 0) = Missile, J2 (index 1) = Pilote
                    if (index == 0)
                    {
                        prefabToSpawn = puppetMissilePrefab;
                        spawnPoint = PuppetMasterManager.instance.GetRandomMissileSpawnPoint();
                    }
                    else
                    {
                        prefabToSpawn = pilotPrefab;
                        spawnPoint = PuppetMasterManager.instance.pilotSpawnPoint;
                    }
                }

                if (prefabToSpawn != null)
                {
                    Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
                    GameObject playerInstance = Instantiate(prefabToSpawn, pos, Quaternion.identity);
                    
                    // Attache le joueur a sa connexion reseau
                    NetworkServer.AddPlayerForConnection(conn, playerInstance);
                }

                index++;
            }
        }

        [Server]
        public void RespawnPuppetMissile(NetworkConnectionToClient conn)
        {
            if (PuppetMasterManager.instance.currentState == PuppetMasterManager.GameState.GameOver || 
                PuppetMasterManager.instance.currentState == PuppetMasterManager.GameState.Intermission) return;

            Transform spawnPoint = PuppetMasterManager.instance.GetRandomMissileSpawnPoint();
            Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            
            GameObject missileInstance = Instantiate(puppetMissilePrefab, pos, Quaternion.identity);
            
            // Si le joueur avait deja un objet (meme detruit), on le remplace
            NetworkServer.ReplacePlayerForConnection(conn, missileInstance);
        }
    }
}
