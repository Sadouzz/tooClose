using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Mirror;

namespace Connection
{
    public class MultiplayerManager : MonoBehaviour
    {
        public static MultiplayerManager instance;

        [Header("Settings")]
        public int maxPlayers = 2;
        public string lobbyName = "SurvivalRaceLobby";

        private Lobby currentLobby;
        private float heartbeatTimer;
        private float lobbyPollTimer;
        private bool isMatchmaking = false;

        private void Awake()
        {
            if (instance == null) instance = this;
        }

        public async void StartMatchmaking()
        {
            if (isMatchmaking) return;
            isMatchmaking = true;
            Debug.Log("Démarrage du matchmaking...");

            try
            {
                currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
                Debug.Log("Lobby trouvé ! En attente du Host pour démarrer Relay...");
            }
            catch (LobbyServiceException)
            {
                Debug.Log("Aucun lobby disponible. Création d'un nouveau lobby en tant que Host.");
                await CreateLobbyAndRelay();
            }
        }

        private async Task CreateLobbyAndRelay()
        {
            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                Debug.Log("Relay Allocation créée. Join Code : " + joinCode);

                CreateLobbyOptions options = new CreateLobbyOptions
                {
                    IsPrivate = false,
                    Data = new Dictionary<string, DataObject>
                    {
                        { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                    }
                };

                currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
                Debug.Log("Lobby créé avec succès ! ID: " + currentLobby.Id);

                NetworkManager.singleton.StartHost();
            }
            catch (RelayServiceException e)
            {
                Debug.LogError("Erreur Relay : " + e.Message);
                isMatchmaking = false;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError("Erreur Lobby : " + e.Message);
                isMatchmaking = false;
            }
        }

        private void Update()
        {
            HandleLobbyHeartbeat();
            HandleLobbyPolling();
        }

        private async void HandleLobbyHeartbeat()
        {
            if (currentLobby != null && currentLobby.HostId == AuthenticationService.Instance.PlayerId)
            {
                heartbeatTimer -= Time.deltaTime;
                if (heartbeatTimer < 0f)
                {
                    heartbeatTimer = 15f;
                    await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
                }
            }
        }

        private async void HandleLobbyPolling()
        {
            if (currentLobby != null && isMatchmaking)
            {
                lobbyPollTimer -= Time.deltaTime;
                if (lobbyPollTimer < 0f)
                {
                    lobbyPollTimer = 1.1f;
                    currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);

                    if (currentLobby.HostId != AuthenticationService.Instance.PlayerId)
                    {
                        if (currentLobby.Data != null && currentLobby.Data.ContainsKey("RelayJoinCode"))
                        {
                            string joinCode = currentLobby.Data["RelayJoinCode"].Value;
                            if (!string.IsNullOrEmpty(joinCode))
                            {
                                Debug.Log("Join Code trouvé ! Connexion au Relay...");
                                JoinRelayServer(joinCode);
                                isMatchmaking = false;
                            }
                        }
                    }
                }
            }
        }

        private async void JoinRelayServer(string joinCode)
        {
            try
            {
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                NetworkManager.singleton.StartClient();
            }
            catch (RelayServiceException e)
            {
                Debug.LogError("Erreur lors de la jonction au Relay : " + e.Message);
            }
        }
    }
}
