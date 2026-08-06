using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using Unity.Services.Authentication;
using Newtonsoft.Json;

/// <summary>
/// Gère les interactions avec les leaderboards UGS.
/// 
/// - 2 leaderboards : Easy et Hard (IDs configurables dans l'Inspector)
/// - Submit : envoie le score + métadonnées (index avion, nom avion)
/// - Fetch  : récupère le top N avec pagination (GetScoresAsync)
/// 
/// Attend que AuthManager.OnAuthenticated soit déclenché avant toute opération.
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager instance;

    [Header("Leaderboard IDs (à copier depuis le Dashboard UGS)")]
    [Tooltip("ID du leaderboard Easy dans le Dashboard UGS")]
    public string leaderboardIdEasy = "too_close_leaderboard_easy";
    [Tooltip("ID du leaderboard Hard dans le Dashboard UGS")]
    public string leaderboardIdHard = "too_close_leaderboard_hard";

    [Header("Pagination")]
    [Tooltip("Nombre d'entrées par page")]
    public int pageSize = 10;

    // ──────────────────────────────────────────────────────────
    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Dès que l'auth est prête, on tente de migrer les highscores locaux non encore soumis
        AuthManager.OnAuthenticated += OnAuthReadyForMigration;
    }

    void OnDestroy()
    {
        AuthManager.OnAuthenticated -= OnAuthReadyForMigration;
    }

    void OnAuthReadyForMigration()
    {
        // On ne se désabonne pas pour continuer à recevoir les refreshes (ex: après linking Play Games)
        _ = TryMigrateLocalHighScoresAsync();
    }

    // ──────────────────────────────────────────────────────────
    // ─── MIGRATION : Highscores locaux → UGS ─────────────────
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Vérifie si le joueur a un highscore local (PlayerPrefs) non encore soumis au leaderboard UGS.
    /// Si oui, le soumet. Un flag PlayerPrefs ("UGS_Migrated_Easy" / "UGS_Migrated_Hard")
    /// évite de resoumettre à chaque lancement.
    /// 
    /// Logique :
    ///   1. Lit le highscore local (PlayerPrefs "highscore" ou "highscoreHard")
    ///   2. Récupère le score actuel du joueur sur UGS
    ///   3. Soumet seulement si le score local est supérieur ou si aucun score UGS n'existe
    /// </summary>
    async Task TryMigrateLocalHighScoresAsync()
    {
        if (Unity.Services.Core.UnityServices.State != Unity.Services.Core.ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn) return;

        await MigrateModeAsync(false); // Easy
        await MigrateModeAsync(true);  // Hard
    }

    async Task MigrateModeAsync(bool isHardMode)
    {
        string migrationKey = isHardMode ? "UGS_Migrated_Hard" : "UGS_Migrated_Easy";
        string localKey     = isHardMode ? "highscoreHard"     : "highscore";
        string leaderboardId = isHardMode ? leaderboardIdHard  : leaderboardIdEasy;

        int localScore = PlayerPrefs.GetInt(localKey, 0);

        // Pas de score local → rien à migrer
        if (localScore <= 0)
        {
            PlayerPrefs.SetInt(migrationKey, 1);
            PlayerPrefs.Save();
            return;
        }

        // Migration déjà effectuée → on vérifie quand même si le score UGS est inférieur au local
        // (cas rare : migration ancienne mais score local amélioré hors-ligne)
        bool alreadyMigrated = PlayerPrefs.GetInt(migrationKey, 0) == 1;

        try
        {
            // Fetch du score UGS actuel du joueur
            int ugsScore = 0;
            try
            {
                var options = new GetPlayerScoreOptions { IncludeMetadata = false };
                LeaderboardEntry existing = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId, options);
                ugsScore = (int)existing.Score;
            }
            catch
            {
                // Aucun score sur UGS → ugsScore reste 0
                ugsScore = 0;
            }

            // Soumettre si le score local est meilleur que le score UGS (ou si aucun score UGS)
            if (localScore > ugsScore)
            {
                // Récupérer les métadonnées de l'avion actuel
                int planeIndex = PlayerPrefs.GetInt("SelectedPlaneIndex", 0);
                string planeName = "?";
                if (ChoosingPlaneScript.instance != null)
                {
                    PlaneData data = ChoosingPlaneScript.instance.GetCurrentPlaneData();
                    if (data != null) planeName = data.planeName;
                }

                var metadata = new Dictionary<string, object>
                {
                    { "planeIndex", planeIndex },
                    { "planeName", planeName },
                    { "migrated", true } // marqueur pour distinguer les scores migrés dans le dashboard
                };

                var scoreOptions = new AddPlayerScoreOptions { Metadata = metadata };
                LeaderboardEntry result = await LeaderboardsService.Instance.AddPlayerScoreAsync(
                    leaderboardId, localScore, scoreOptions);

                Debug.Log($"[Leaderboard] Migration {(isHardMode ? "Hard" : "Easy")}: " +
                          $"local={localScore} > ugs={ugsScore} → submitted | Rank: {result.Rank + 1}");
            }
            else
            {
                Debug.Log($"[Leaderboard] Migration {(isHardMode ? "Hard" : "Easy")}: " +
                          $"local={localScore} <= ugs={ugsScore} → rien à soumettre.");
            }

            PlayerPrefs.SetInt(migrationKey, 1);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Leaderboard] Migration {(isHardMode ? "Hard" : "Easy")} failed: {e.Message}");
        }
    }

    // ──────────────────────────────────────────────────────────
    // ─── SUBMIT SCORE ─────────────────────────────────────────
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Soumet le score au leaderboard correspondant à la difficulté.
    /// Stocke l'index et le nom de l'avion comme métadonnées (JSON).
    /// Appelé depuis Inventory.SaveData()
    /// </summary>
    public void SubmitScore(int score, bool isHardMode)
    {
        if (Unity.Services.Core.UnityServices.State != Unity.Services.Core.ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("[Leaderboard] Unity Services not initialized or not signed in — cannot submit score.");
            return;
        }

        // Récupérer l'avion actuellement utilisé
        int planeIndex = PlayerPrefs.GetInt("SelectedPlaneIndex", 0);
        string planeName = "Unknown";
        if (ChoosingPlaneScript.instance != null)
        {
            PlaneData data = ChoosingPlaneScript.instance.GetCurrentPlaneData();
            if (data != null) planeName = data.planeName;
        }

        // Construire les métadonnées (objet dictionnaire)
        var metadata = new Dictionary<string, object>
        {
            { "planeIndex", planeIndex },
            { "planeName", planeName }
        };

        string leaderboardId = isHardMode ? leaderboardIdHard : leaderboardIdEasy;
        _ = SubmitScoreAsync(leaderboardId, score, metadata);
    }

    async Task SubmitScoreAsync(string leaderboardId, int score, object metadata)
    {
        try
        {
            var options = new AddPlayerScoreOptions { Metadata = metadata };
            LeaderboardEntry result = await LeaderboardsService.Instance.AddPlayerScoreAsync(
                leaderboardId, score, options);

            Debug.Log($"[Leaderboard] Score submitted: {result.Score} | Rank: {result.Rank + 1} | LB: {leaderboardId}");
        }
        catch (Exception e)
        {
            Debug.LogError("[Leaderboard] Submit failed: " + e.Message);
        }
    }

    // ──────────────────────────────────────────────────────────
    // ─── FETCH SCORES (avec pagination) ───────────────────────
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Récupère une page du leaderboard et retourne les entrées via callback.
    /// Le numéro de page est 0-based (page 0 = entries 1 à pageSize).
    /// </summary>
    public void FetchScores(bool isHardMode, int page, Action<LeaderboardScoresPage, int> onComplete)
    {
        if (Unity.Services.Core.UnityServices.State != Unity.Services.Core.ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("[Leaderboard] Unity Services not initialized or not signed in — cannot fetch scores.");
            onComplete?.Invoke(null, page);
            return;
        }

        string leaderboardId = isHardMode ? leaderboardIdHard : leaderboardIdEasy;
        _ = FetchScoresAsync(leaderboardId, page, onComplete);
    }

    async Task FetchScoresAsync(string leaderboardId, int page, Action<LeaderboardScoresPage, int> onComplete)
    {
        try
        {
            var options = new GetScoresOptions
            {
                Offset       = page * pageSize,
                Limit        = pageSize,
                IncludeMetadata = true
            };

            LeaderboardScoresPage result = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, options);
            Debug.Log($"[Leaderboard] Fetched {result.Results.Count} entries (page {page}).");
            onComplete?.Invoke(result, page);
        }
        catch (Exception e)
        {
            Debug.LogError("[Leaderboard] Fetch failed: " + e.Message);
            onComplete?.Invoke(null, page);
        }
    }

    // ──────────────────────────────────────────────────────────
    // ─── FETCH SCORES AMIS ────────────────────────────────────
    // ──────────────────────────────────────────────────────────

    public void FetchFriendsScores(bool isHardMode, List<string> friendIds, Action<List<LeaderboardEntry>> onComplete)
    {
        if (Unity.Services.Core.UnityServices.State != Unity.Services.Core.ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn)
        {
            onComplete?.Invoke(null);
            return;
        }

        if (friendIds == null || friendIds.Count == 0)
        {
            onComplete?.Invoke(null);
            return;
        }

        string leaderboardId = isHardMode ? leaderboardIdHard : leaderboardIdEasy;
        _ = FetchFriendsScoresInternalAsync(leaderboardId, friendIds, onComplete);
    }

    async Task FetchFriendsScoresInternalAsync(string leaderboardId, List<string> friendIds, Action<List<LeaderboardEntry>> onComplete)
    {
        try
        {
            var options = new GetScoresByPlayerIdsOptions { IncludeMetadata = true };
            LeaderboardScoresWithNotFoundPlayerIds result = await LeaderboardsService.Instance.GetScoresByPlayerIdsAsync(leaderboardId, friendIds, options);
            
            var resultsList = result?.Results;
            
            // On trie manuellement par score décroissant au cas où UGS ne le fait pas sur ce endpoint précis
            if (resultsList != null)
            {
                resultsList.Sort((a, b) => b.Score.CompareTo(a.Score));
            }
            
            Debug.Log($"[Leaderboard] Fetched {resultsList?.Count ?? 0} friend entries.");
            onComplete?.Invoke(resultsList);
        }
        catch (Exception e)
        {
            Debug.LogError("[Leaderboard] FetchFriendsScores failed: " + e.Message);
            onComplete?.Invoke(null);
        }
    }

    // ──────────────────────────────────────────────────────────
    // ─── FETCH PLAYER SCORE (rang du joueur actuel) ───────────
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Récupère l'entrée du joueur actuel dans le leaderboard (son rang et score).
    /// </summary>
    public void FetchPlayerScore(bool isHardMode, Action<LeaderboardEntry> onComplete)
    {
        if (Unity.Services.Core.UnityServices.State != Unity.Services.Core.ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn)
        {
            onComplete?.Invoke(null);
            return;
        }

        string leaderboardId = isHardMode ? leaderboardIdHard : leaderboardIdEasy;
        _ = FetchPlayerScoreAsync(leaderboardId, onComplete);
    }

    async Task FetchPlayerScoreAsync(string leaderboardId, Action<LeaderboardEntry> onComplete)
    {
        try
        {
            var options = new GetPlayerScoreOptions { IncludeMetadata = true };
            LeaderboardEntry entry = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId, options);
            onComplete?.Invoke(entry);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Leaderboard] FetchPlayerScore failed (maybe no score yet): " + e.Message);
            onComplete?.Invoke(null);
        }
    }

    // ──────────────────────────────────────────────────────────
    // ─── HELPER : Deserialize métadonnées avion ───────────────
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Parse les métadonnées JSON d'une entrée pour obtenir planeIndex et planeName.
    /// Retourne (0, "Unknown") si le JSON est absent ou invalide.
    /// </summary>
    public static (int planeIndex, string planeName) ParseMetadata(LeaderboardEntry entry)
    {
        if (entry?.Metadata == null) return (0, "?");
        try
        {
            var meta = JsonConvert.DeserializeObject<Dictionary<string, object>>(entry.Metadata);
            int idx = meta.ContainsKey("planeIndex") ? Convert.ToInt32(meta["planeIndex"]) : 0;
            string name = meta.ContainsKey("planeName") ? meta["planeName"].ToString() : "?";
            return (idx, name);
        }
        catch
        {
            return (0, "?");
        }
    }
}
