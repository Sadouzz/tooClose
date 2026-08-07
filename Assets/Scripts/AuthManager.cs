using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Analytics;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

//#if UNITY_IOS || UNITY_TVOS
//using UnityEngine.SocialPlatforms.GameCenter;
//#endif

/// <summary>
/// Gère l'authentification UGS au lancement de l'application.
/// 
/// Flux :
///   1. Initialise UGS (UnityServices.InitializeAsync)
///   2. Sign-in anonyme si pas encore connecté
///   3. Tente de lier le compte plateforme (Play Games / Game Center)
///      via LinkWithGooglePlayGamesAsync / LinkWithAppleGameCenterAsync
///   4. Déclenche OnAuthenticated pour notifier les autres systèmes
/// 
/// Remarque : la liaison échoue silencieusement si le compte est déjà lié
/// ou si le joueur refuse — on reste en anonyme dans ce cas.
/// </summary>
public class AuthManager : MonoBehaviour
{
    public static AuthManager instance;

    // Évènement déclenché une fois l'auth terminée (succès ou fallback anonyme)
    public static event Action OnAuthenticated;

    public bool IsAuthenticated => UnityServices.State == ServicesInitializationState.Initialized
                                   && AuthenticationService.Instance.IsSignedIn;

    public string PlayerName { get; private set; } = "Player";
    public string PlayerId  { get; private set; } = "";

    public bool IsAccountLinked()
    {
        if (!IsAuthenticated) return false;
        var info = AuthenticationService.Instance.PlayerInfo;
        if (info == null || info.Identities == null) return false;
        
        foreach (var id in info.Identities)
        {
            if (id.TypeId == "google.play" || id.TypeId == "googleplaygames" || id.TypeId == "apple.gamecenter" || id.TypeId == "apple-game-center")
                return true;
        }
        return false;
    }

    // ──────────────────────────────────────────────────────────
    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        await InitializeAndSignIn();
    }

    // ──────────────────────────────────────────────────────────
    /// <summary>Point d'entrée principal — initialise et connecte.</summary>
    public async Task InitializeAndSignIn()
    {
        try
        {
            // 1. Initialiser Unity Services
            await UnityServices.InitializeAsync();
            Debug.Log("[Auth] UGS Initialized.");

            try
            {
                AnalyticsService.Instance.StartDataCollection();
                Debug.Log("[Auth] Analytics data collection started.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Auth] Failed to start analytics: " + ex.Message);
            }

            // 2. S'inscrire aux évènements de session
            AuthenticationService.Instance.SignedIn  += OnSignedIn;
            AuthenticationService.Instance.SignedOut += OnSignedOut;

            // 3. Connexion anonyme (récupère le session token existant si dispo)
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("[Auth] Signed in anonymously. PlayerID: " + AuthenticationService.Instance.PlayerId);
            }

            try
            {
                // Récupère les infos du joueur pour avoir le PlayerName actuel
                await AuthenticationService.Instance.GetPlayerInfoAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Auth] Failed to fetch player profile info: " + ex.Message);
            }

            // 4. Mettre à jour les infos locales (on ne tente plus le Link automatique)
            PlayerId   = AuthenticationService.Instance.PlayerId;
            PlayerName = !string.IsNullOrEmpty(AuthenticationService.Instance.PlayerName) 
                ? AuthenticationService.Instance.PlayerName 
                : "Player";

            OnAuthenticated?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError("[Auth] Authentication failed: " + e.Message);
            // On déclenche quand même l'évènement pour ne pas bloquer le jeu
            OnAuthenticated?.Invoke();
        }
    }

    /// <summary>
    /// Met à jour le pseudo (Player Name) du joueur sur UGS.
    /// </summary>
    public async Task UpdatePlayerName(string newName)
    {
        if (!IsAuthenticated)
        {
            Debug.LogWarning("[Auth] Cannot update name, player not authenticated.");
            return;
        }

        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
            PlayerName = newName;
            Debug.Log("[Auth] Player name updated successfully to: " + newName);
            OnAuthenticated?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError("[Auth] Failed to update player name: " + e.Message);
        }
    }

    // ──────────────────────────────────────────────────────────
    // ─── Liaisons manuelles (Appelées par des boutons UI) ─────
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Appelé par un bouton unique dans l'UI. Détecte la plateforme automatiquement.
    /// </summary>
    public void LinkPlatformAccount()
    {
#if UNITY_ANDROID
        LinkGooglePlayGames();
//#elif UNITY_IOS || UNITY_TVOS
//        LinkGameCenter();
#else
        Debug.Log("[Auth] Platform not supported for automatic linking.");
#endif
    }

    /// <summary>
    /// Appelé manuellement par un bouton pour lier le compte Google Play Games (Android).
    /// </summary>
    public async void LinkGooglePlayGames()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsAuthenticated)
        {
            Debug.LogWarning("[Auth] Not authenticated yet.");
            return;
        }

        Debug.Log("[Auth] Starting manual Play Games link...");
        await TryLinkGooglePlayGames();

        // Mettre à jour les infos locales
        PlayerId   = AuthenticationService.Instance.PlayerId;
        PlayerName = AuthenticationService.Instance.PlayerName ?? "Player";

        OnAuthenticated?.Invoke();
#else
        Debug.LogWarning("[Auth] Google Play Games linking is only available on Android device.");
        await Task.CompletedTask;
#endif
    }

    /// <summary>
    /// Appelé manuellement par un bouton pour lier le compte Game Center (iOS).
    /// </summary>
    public async void LinkGameCenter()
    {
//#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
//        if (!IsAuthenticated)
//        {
//            Debug.LogWarning("[Auth] Not authenticated yet.");
//            return;
//        }
//
//        Debug.Log("[Auth] Starting manual Game Center link...");
//        await TryLinkGameCenter();
//
//        // Mettre à jour les infos locales
//        PlayerId   = AuthenticationService.Instance.PlayerId;
//        PlayerName = AuthenticationService.Instance.PlayerName ?? "Player";
//
//        OnAuthenticated?.Invoke();
//#else
//        Debug.LogWarning("[Auth] Game Center linking is only available on iOS device.");
//        await Task.CompletedTask;
//#endif
    }

    // ──────────────────────────────────────────────────────────
    // ─── ANDROID — Google Play Games ─────────────────────────
    // ──────────────────────────────────────────────────────────
#if UNITY_ANDROID
    async Task TryLinkGooglePlayGames()
    {
        try
        {
            // Vérifier si déjà lié à un compte Play Games
            if (AuthenticationService.Instance.SessionTokenExists)
            {
                Debug.Log("[Auth] Session token exists, skipping Play Games link attempt.");
                return;
            }

            // Obtenir le auth code depuis Play Games SDK
            string authCode = await GetPlayGamesAuthCodeAsync();
            if (string.IsNullOrEmpty(authCode))
            {
                Debug.LogWarning("[Auth] Play Games: Could not get auth code.");
                return;
            }

            // Lier le compte Play Games à l'identité UGS anonyme
            await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(authCode);
            Debug.Log("[Auth] Linked to Google Play Games successfully.");
        }
        catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
        {
            // Le compte était déjà lié — on signe directement avec Play Games
            Debug.Log("[Auth] Account already linked. Signing in with Play Games.");
            try
            {
                string authCode = await GetPlayGamesAuthCodeAsync();
                if (!string.IsNullOrEmpty(authCode))
                    await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);
            }
            catch (Exception e2)
            {
                Debug.LogWarning("[Auth] SignIn with Play Games failed: " + e2.Message);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Auth] Play Games link failed (staying anonymous): " + ex.Message);
        }
    }

    /// <summary>Lance le sign-in Play Games et retourne le server auth code.</summary>
    Task<string> GetPlayGamesAuthCodeAsync()
    {
        var tcs = new TaskCompletionSource<string>();

        PlayGamesPlatform.Activate();
        Social.localUser.Authenticate(success =>
        {
            if (!success)
            {
                tcs.SetResult(null);
                return;
            }

            PlayGamesPlatform.Instance.RequestServerSideAccess(
                /* forceRefreshToken= */ false,
                code => tcs.SetResult(code)
            );
        });

        return tcs.Task;
    }
#endif

    // ──────────────────────────────────────────────────────────
    // ─── iOS — Game Center ───────────────────────────────────
    // ──────────────────────────────────────────────────────────
//#if UNITY_IOS || UNITY_TVOS
/*
    async Task TryLinkGameCenter()
    {
        try
        {
            if (AuthenticationService.Instance.SessionTokenExists)
            {
                Debug.Log("[Auth] Session token exists, skipping Game Center link attempt.");
                return;
            }

            var gcData = await GetGameCenterAuthDataAsync();
            if (gcData == null)
            {
                Debug.LogWarning("[Auth] Game Center: Could not retrieve authentication parameters.");
                return;
            }

            await AuthenticationService.Instance.LinkWithAppleGameCenterAsync(
                gcData.Value.userLoginId,
                gcData.Value.publicKeyUrl,
                gcData.Value.signature,
                gcData.Value.salt,
                gcData.Value.timestamp
            );
            Debug.Log("[Auth] Linked to Apple Game Center successfully.");
        }
        catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
        {
            Debug.Log("[Auth] Game Center already linked. Signing in with Game Center.");
            try
            {
                var gcData = await GetGameCenterAuthDataAsync();
                if (gcData != null)
                {
                    await AuthenticationService.Instance.SignInWithAppleGameCenterAsync(
                        gcData.Value.userLoginId,
                        gcData.Value.publicKeyUrl,
                        gcData.Value.signature,
                        gcData.Value.salt,
                        gcData.Value.timestamp
                    );
                }
            }
            catch (Exception e2)
            {
                Debug.LogWarning("[Auth] SignIn with Game Center failed: " + e2.Message);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Auth] Game Center link failed (staying anonymous): " + ex.Message);
        }
    }

    struct GameCenterAuthData
    {
        public string userLoginId;
        public string publicKeyUrl;
        public string signature;
        public string salt;
        public ulong timestamp;
    }

    Task<GameCenterAuthData?> GetGameCenterAuthDataAsync()
    {
        var tcs = new TaskCompletionSource<GameCenterAuthData?>();

        Social.localUser.Authenticate(success =>
        {
            if (!success)
            {
                Debug.LogWarning("[Auth] Social.localUser authentication failed.");
                tcs.SetResult(null);
                return;
            }

            // Retrieve identity verification signature parameters directly
            GameCenterPlatform.ShowDefaultAchievementCompletionBanner(true);
            
            // Unity 2021/2022+ signature retrieval pattern
            UnityEngine.SocialPlatforms.GameCenter.GameCenterPlatform.GetLocalUserSignature((signature, salt, timestamp, publicKeyUrl, error) =>
            {
                if (!string.IsNullOrEmpty(error) || signature == null)
                {
                    Debug.LogWarning($"[Auth] Failed to retrieve Game Center signature: {error}");
                    tcs.SetResult(null);
                    return;
                }

                var data = new GameCenterAuthData
                {
                    userLoginId = Social.localUser.id,
                    publicKeyUrl = publicKeyUrl,
                    signature = Convert.ToBase64String(signature),
                    salt = Convert.ToBase64String(salt),
                    timestamp = timestamp
                };

                tcs.SetResult(data);
            });
        });

        return tcs.Task;
    }
*/
//#endif

    // ──────────────────────────────────────────────────────────
    // ─── Évènements AuthenticationService ────────────────────
    // ──────────────────────────────────────────────────────────
    void OnSignedIn()
    {
        Debug.Log("[Auth] Signed in — PlayerID: " + AuthenticationService.Instance.PlayerId);
    }

    void OnSignedOut()
    {
        Debug.Log("[Auth] Signed out.");
    }

    void OnDestroy()
    {
        if (AuthenticationService.Instance != null)
        {
            AuthenticationService.Instance.SignedIn  -= OnSignedIn;
            AuthenticationService.Instance.SignedOut -= OnSignedOut;
        }
    }
}