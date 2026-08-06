using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Friends.Exceptions;

/// <summary>
/// Gère les interactions avec le module Friends d'Unity Gaming Services.
/// Initialisé automatiquement après l'authentification.
/// </summary>
public class FriendsManager : MonoBehaviour
{
    public static FriendsManager instance;

    // Événements pour mettre à jour l'UI
    public static event Action OnFriendsUpdated;
    public static event Action<string> OnFriendRequestReceived;

    public bool IsInitialized { get; private set; } = false;

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        AuthManager.OnAuthenticated += InitializeFriendsService;
    }

    void OnDestroy()
    {
        AuthManager.OnAuthenticated -= InitializeFriendsService;
    }

    private async void InitializeFriendsService()
    {
        if (IsInitialized) return;

        try
        {
            // Initialiser le SDK Friends
            await FriendsService.Instance.InitializeAsync();
            IsInitialized = true;
            Debug.Log("[Friends] Service Initialized.");

            // S'abonner aux événements
            FriendsService.Instance.RelationshipAdded += (relationshipEvent) => 
            {
                var relationship = relationshipEvent.Relationship;
                if (relationship.Type == RelationshipType.FriendRequest)
                {
                    Debug.Log($"[Friends] Demande reçue de: {relationship.Member.Profile?.Name ?? relationship.Member.Id}");
                    OnFriendRequestReceived?.Invoke(relationship.Member.Id);
                }
                OnFriendsUpdated?.Invoke();
            };

            FriendsService.Instance.RelationshipDeleted += (relationshipEvent) => 
            {
                OnFriendsUpdated?.Invoke();
            };

            // Notifier l'UI que c'est prêt
            OnFriendsUpdated?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Friends] Initialization failed: {ex.Message}");
        }
    }

    /// <summary>Envoie une demande d'ami à un Player ID cible.</summary>
    public async Task<bool> SendFriendRequestAsync(string targetPlayerId)
    {
        if (!IsInitialized || string.IsNullOrEmpty(targetPlayerId)) return false;

        // Anti-doublon : Ne pas s'ajouter soi-même
        if (targetPlayerId == Unity.Services.Authentication.AuthenticationService.Instance.PlayerId) 
        {
            Debug.Log("[Friends] Vous ne pouvez pas vous ajouter vous-même.");
            return false;
        }

        // Anti-doublon : Vérifier s'il n'est pas déjà ami, ou si une requête n'est pas déjà en cours
        foreach (var rel in FriendsService.Instance.Relationships)
            if (rel.Member.Id == targetPlayerId) return false;
            
        foreach (var rel in FriendsService.Instance.OutgoingFriendRequests)
            if (rel.Member.Id == targetPlayerId) return false;

        foreach (var rel in FriendsService.Instance.IncomingFriendRequests)
            if (rel.Member.Id == targetPlayerId) return false; // Acceptez plutôt sa requête

        try
        {
            await FriendsService.Instance.AddFriendAsync(targetPlayerId);
            Debug.Log($"[Friends] Demande d'ami envoyée à {targetPlayerId}.");
            return true;
        }
        catch (FriendsServiceException ex)
        {
            Debug.LogError($"[Friends] Erreur ajout ami : {ex.Message}");
            return false;
        }
    }

    /// <summary>Accepte une demande d'ami entrante.</summary>
    public async Task<bool> AcceptFriendRequestAsync(string requesterId)
    {
        if (!IsInitialized) return false;

        try
        {
            // Accepter = Envoyer une demande à celui qui nous a envoyé une demande
            await FriendsService.Instance.AddFriendAsync(requesterId);
            Debug.Log($"[Friends] Demande acceptée pour {requesterId}.");
            return true;
        }
        catch (FriendsServiceException ex)
        {
            Debug.LogError($"[Friends] Erreur acceptation ami : {ex.Message}");
            return false;
        }
    }

    /// <summary>Supprime un ami ou annule une demande.</summary>
    public async Task<bool> RemoveFriendAsync(string targetPlayerId)
    {
        if (!IsInitialized) return false;

        try
        {
            await FriendsService.Instance.DeleteFriendAsync(targetPlayerId);
            Debug.Log($"[Friends] Ami supprimé : {targetPlayerId}.");
            return true;
        }
        catch (FriendsServiceException ex)
        {
            Debug.LogError($"[Friends] Erreur suppression ami : {ex.Message}");
            return false;
        }
    }

    /// <summary>Récupère toutes les demandes d'amis entrantes.</summary>
    public List<Relationship> GetIncomingRequests()
    {
        List<Relationship> requests = new List<Relationship>();
        if (!IsInitialized) return requests;

        foreach (var relationship in FriendsService.Instance.IncomingFriendRequests)
        {
            requests.Add(relationship);
        }
        return requests;
    }

    /// <summary>Récupère la liste finale des IDs des amis.</summary>
    public List<string> GetFriendPlayerIds()
    {
        List<string> friendIds = new List<string>();
        if (!IsInitialized) return friendIds;

        foreach (var relationship in FriendsService.Instance.Relationships)
        {
            if (relationship.Type == RelationshipType.Friend)
            {
                friendIds.Add(relationship.Member.Id);
            }
        }
        return friendIds;
    }

    /// <summary>Récupère la liste des objets Relationship pour les amis.</summary>
    public List<Relationship> GetFriends()
    {
        List<Relationship> friends = new List<Relationship>();
        if (!IsInitialized) return friends;

        foreach (var relationship in FriendsService.Instance.Relationships)
        {
            if (relationship.Type == RelationshipType.Friend)
            {
                friends.Add(relationship);
            }
        }
        return friends;
    }
}
