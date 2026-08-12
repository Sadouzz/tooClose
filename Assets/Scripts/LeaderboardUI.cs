using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Leaderboards.Models;
using Unity.Services.Authentication;
using UnityEngine.Localization.Settings;

/// <summary>
/// UI de leaderboard avec pagination.
/// 
/// Hiérarchie attendue dans la scène :
///   LeaderboardPanel
///     ├── Header
///     │     ├── TabEasyButton
///     │     └── TabHardButton
///     ├── PlayerRankPanel
///     │     ├── PlayerRankText     (ex: "Votre rang : #12")
///     │     └── PlayerScoreText    (ex: "Votre meilleur : 4250")
///     ├── ScrollView
///     │     └── Content            ← assigner à rowsContainer
///     ├── PrevPageButton
///     ├── NextPageButton
///     └── PageText                 (ex: "Page 1 / 5")
/// 
/// Assigner :
///   - rowContainer     : le "Content" du ScrollView
///   - rowPrefab        : prefab d'une ligne (LeaderboardRowPrefab)
///   - planeSprites[]   : array de sprites dans le même ordre que ChoosingPlaneScript
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────
    // ─── Inspector ────────────────────────────────────────────
    // ──────────────────────────────────────────────────────────
    [Header("Panel Root")]
    public GameObject leaderboardPanel;

    [Header("Tabs - Difficulté")]
    public Button tabEasyButton;
    public Button tabHardButton;
    
    [Header("Tabs - Filtre")]
    public Button tabGlobalButton;
    public Button tabFriendsButton;

    [Header("Colors")]
    public Color  tabActiveColor   = new Color(0.2f, 0.6f, 1f);
    public Color  tabInactiveColor = new Color(0.3f, 0.3f, 0.3f);

    [Header("Player's own rank (affiché en haut)")]
    public TextMeshProUGUI playerRankText;
    public TextMeshProUGUI playerScoreText;
    public Image            playerPlaneIcon;

    [Header("Liste des scores")]
    public Transform rowsContainer;    // Content du ScrollView
    public GameObject rowPrefab;       // Prefab d'une ligne

    [Header("Pagination")]
    public Button             prevPageButton;
    public Button             nextPageButton;
    public TextMeshProUGUI    pageText;

    [Header("Avions — même ordre que dans ChoosingPlaneScript")]
    [Tooltip("Sprites des avions dans le même ordre (index 0, 1, 2…)")]
    public Sprite[] planeSprites;

    [Header("Feedback")]
    public TextMeshProUGUI loadingText;

    [Header("Localization")]
    public string stringTableName = "UITexts";

    private string GetTranslation(string key, string fallback)
    {
        string tr = LocalizationSettings.StringDatabase.GetLocalizedString(stringTableName, key);
        if (string.IsNullOrEmpty(tr) || tr.Contains("No translation")) return fallback;
        return tr;
    }

    // ──────────────────────────────────────────────────────────
    // ─── Privé ────────────────────────────────────────────────
    // ──────────────────────────────────────────────────────────
    private LeaderboardEntry cachedPlayerEntry = null;
    private LeaderboardScoresPage latestPage = null;
    private bool   isHardMode      = false;
    private bool   isFriendsFilter = false;
    private int    currentPage = 0;
    private int    totalPages  = 1;
    private bool   isFetching  = false;

    private List<GameObject> spawnedRows = new List<GameObject>();

    // ──────────────────────────────────────────────────────────
    void Start()
    {
        // Tabs Difficulté
        if (tabEasyButton != null) tabEasyButton.onClick.AddListener(() => SwitchTab(false));
        if (tabHardButton != null) tabHardButton.onClick.AddListener(()  => SwitchTab(true));

        // Tabs Filtre (Global / Amis)
        if (tabGlobalButton != null) tabGlobalButton.onClick.AddListener(() => SwitchFilterTab(false));
        if (tabFriendsButton != null) tabFriendsButton.onClick.AddListener(() => SwitchFilterTab(true));

        // Pagination
        if (prevPageButton != null) prevPageButton.onClick.AddListener(PrevPage);
        if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);

        // Par défaut, panel fermé
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
    }

    // ──────────────────────────────────────────────────────────
    // ─── Ouvrir / Fermer ──────────────────────────────────────
    // ──────────────────────────────────────────────────────────

    public void OpenLeaderboard()
    {
        if (leaderboardPanel != null) leaderboardPanel.SetActive(true);

        // Attendre que l'auth soit prête avant de fetch
        if (AuthManager.instance != null && AuthManager.instance.IsAuthenticated)
        {
            RefreshAll();
        }
        else
        {
            AuthManager.OnAuthenticated += OnAuthReady;
        }
    }

    public void CloseLeaderboard()
    {
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
    }

    void OnAuthReady()
    {
        AuthManager.OnAuthenticated -= OnAuthReady;
        RefreshAll();
    }

    // ──────────────────────────────────────────────────────────
    // ─── Tabs ─────────────────────────────────────────────────
    // ──────────────────────────────────────────────────────────
    void SwitchTab(bool hard)
    {
        isHardMode  = hard;
        currentPage = 0;

        // Couleurs des onglets
        var easyImg = tabEasyButton?.GetComponent<Image>();
        var hardImg = tabHardButton?.GetComponent<Image>();
        if (easyImg != null) easyImg.color = hard ? tabInactiveColor : tabActiveColor;
        if (hardImg != null) hardImg.color  = hard ? tabActiveColor   : tabInactiveColor;

        RefreshAll();
    }

    void SwitchFilterTab(bool friends)
    {
        isFriendsFilter = friends;
        currentPage = 0;

        // Couleurs des onglets
        var globalImg = tabGlobalButton?.GetComponent<Image>();
        var friendsImg = tabFriendsButton?.GetComponent<Image>();
        if (globalImg != null) globalImg.color = friends ? tabInactiveColor : tabActiveColor;
        if (friendsImg != null) friendsImg.color = friends ? tabActiveColor : tabInactiveColor;

        RefreshAll();
    }

    // ──────────────────────────────────────────────────────────
    // ─── Pagination ───────────────────────────────────────────
    // ──────────────────────────────────────────────────────────
    void PrevPage()
    {
        if (currentPage > 0 && !isFriendsFilter) // Pas de pagination en amis (tout d'un coup)
        {
            currentPage--;
            FetchPage();
        }
    }

    void NextPage()
    {
        if (currentPage < totalPages - 1 && !isFriendsFilter)
        {
            currentPage++;
            FetchPage();
        }
    }

    // ──────────────────────────────────────────────────────────
    // ─── Fetch ────────────────────────────────────────────────
    // ──────────────────────────────────────────────────────────

    void RefreshAll()
    {
        cachedPlayerEntry = null;
        latestPage = null;
        isFetching = true;
        SetLoading(true);

        // Fetch player score first, then fetch page of scores
        LeaderboardManager.instance?.FetchPlayerScore(isHardMode, (entry) =>
        {
            cachedPlayerEntry = entry;
            OnPlayerScoreFetched(entry);

            FetchPageInternal();
        });
    }

    void FetchPage()
    {
        if (isFetching) return;
        isFetching = true;
        SetLoading(true);
        latestPage = null;

        FetchPageInternal();
    }

    private List<LeaderboardEntry> currentResultsList = null;

    void FetchPageInternal()
    {
        currentResultsList = null;
        if (isFriendsFilter)
        {
            var friendIds = FriendsManager.instance?.GetFriendPlayerIds();
            if (friendIds != null && AuthManager.instance != null)
            {
                if (!friendIds.Contains(AuthManager.instance.PlayerId))
                {
                    friendIds.Add(AuthManager.instance.PlayerId); // s'inclure soi-même
                }
            }

            LeaderboardManager.instance?.FetchFriendsScores(isHardMode, friendIds, (results) =>
            {
                currentResultsList = results;
                isFetching = false;
                SetLoading(false);
                
                // Forcer la désactivation de la pagination en mode Amis
                totalPages = 1; 
                currentPage = 0;
                
                CheckAndRender();
            });
        }
        else
        {
            LeaderboardManager.instance?.FetchScores(isHardMode, currentPage, (page, pageIndex) =>
            {
                latestPage = page;
                currentResultsList = page?.Results;
                isFetching = false;
                SetLoading(false);
                CheckAndRender();
            });
        }
    }

    // ──────────────────────────────────────────────────────────
    // ─── Callbacks ────────────────────────────────────────────
    // ──────────────────────────────────────────────────────────

    void OnPlayerScoreFetched(LeaderboardEntry entry)
    {
        if (entry == null)
        {
            if (playerRankText  != null) playerRankText.text  = GetTranslation("NotRanked", "Pas encore classé");
            if (playerScoreText != null) playerScoreText.text = "";
            if (playerPlaneIcon != null) playerPlaneIcon.gameObject.SetActive(false);
            return;
        }

        if (playerRankText  != null) playerRankText.text  = GetTranslation("YourRank", "Votre rang :") + $" #{entry.Rank + 1}";
        if (playerScoreText != null) playerScoreText.text = GetTranslation("BestScore", "Best :") + $" {(int)entry.Score}";

        // Afficher le sprite de l'avion
        var (planeIdx, _) = LeaderboardManager.ParseMetadata(entry);
        if (playerPlaneIcon != null)
        {
            playerPlaneIcon.gameObject.SetActive(true);
            if (planeSprites != null && planeIdx < planeSprites.Length && planeSprites[planeIdx] != null)
                playerPlaneIcon.sprite = planeSprites[planeIdx];
        }
    }

    void CheckAndRender()
    {
        ClearRows();

        int pageSize = LeaderboardManager.instance != null ? LeaderboardManager.instance.pageSize : 10;

        // Calculer le total réel si on a la réponse UGS
        if (!isFriendsFilter && latestPage != null)
        {
            totalPages = Mathf.Max(1, Mathf.CeilToInt((float)latestPage.Total / pageSize));
        }

        // Si on est sur une page vide après la première page, on a dépassé la fin.
        // (Sécurité au cas où)
        if (!isFriendsFilter && (currentResultsList == null || currentResultsList.Count == 0) && currentPage > 0)
        {
            // On peut s'arrêter à totalPages - 1
            currentPage = Mathf.Max(0, totalPages - 1);
            FetchPageInternal();
            return;
        }

        if (currentResultsList == null || currentResultsList.Count == 0)
        {
            if (loadingText != null)
            {
                loadingText.gameObject.SetActive(true);
                loadingText.text = GetTranslation("NoScore", "Aucun score pour l'instant.");
            }

            totalPages = 1;
            if (pageText != null) pageText.gameObject.SetActive(false);
            if (prevPageButton != null) prevPageButton.interactable = false;
            if (nextPageButton != null) nextPageButton.interactable = false;
            return;
        }

        // Mettre à jour le label de page (format 1/x)
        if (pageText != null)
        {
            pageText.gameObject.SetActive(true);
            pageText.text = $"{currentPage + 1}/{totalPages}";
        }

        // Boutons pagination
        if (prevPageButton != null) prevPageButton.interactable = currentPage > 0 && !isFriendsFilter;
        if (nextPageButton != null) nextPageButton.interactable = (!isFriendsFilter && currentPage < totalPages - 1);

        // Générer les lignes
        bool playerFoundInList = false;
        string currentPlayerId = AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : null;

        foreach (var entry in currentResultsList)
        {
            SpawnRow(entry);
            if (currentPlayerId != null && entry.PlayerId == currentPlayerId)
            {
                playerFoundInList = true;
            }
        }

        // Si le joueur a un score et n'est pas déjà visible sur cette page
        if (cachedPlayerEntry != null && !playerFoundInList)
        {
            SpawnSeparatorRow();
            SpawnRow(cachedPlayerEntry);
        }
    }

    void SpawnSeparatorRow()
    {
        if (rowPrefab == null || rowsContainer == null) return;

        GameObject row = Instantiate(rowPrefab, rowsContainer);
        spawnedRows.Add(row);

        // Rendre translucide le fond de la séparation
        var bg = row.GetComponent<Image>();
        if (bg != null) bg.color = new Color(0, 0, 0, 0.15f);

        // Vider les autres textes et mettre "..."
        var rankTxt = row.transform.Find("RankText")?.GetComponent<TextMeshProUGUI>();
        if (rankTxt != null) rankTxt.text = "";

        var nameTxt = row.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        if (nameTxt != null) nameTxt.text = ". . .";

        var scoreTxt = row.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
        if (scoreTxt != null) scoreTxt.text = "";

        var planeImg = row.transform.Find("PlaneIcon")?.GetComponent<Image>();
        if (planeImg != null) planeImg.gameObject.SetActive(false);

        var planeNameTxt = row.transform.Find("PlaneNameText")?.GetComponent<TextMeshProUGUI>();
        if (planeNameTxt != null) planeNameTxt.text = "";
    }

    // ──────────────────────────────────────────────────────────
    // ─── Rows ─────────────────────────────────────────────────
    // ──────────────────────────────────────────────────────────

    void ClearRows()
    {
        // Supprimer toutes les lignes générées
        foreach (var row in spawnedRows)
            if (row != null) Destroy(row);
        spawnedRows.Clear();

        // Supprimer aussi les placeholders créés dans l'éditeur (ex: fausses données de design)
        if (rowsContainer != null)
        {
            foreach (Transform child in rowsContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    void SpawnRow(LeaderboardEntry entry)
    {
        if (rowPrefab == null || rowsContainer == null) return;

        GameObject row = Instantiate(rowPrefab, rowsContainer);
        spawnedRows.Add(row);

        // Rang
        var rankTxt = row.transform.Find("RankText")?.GetComponent<TextMeshProUGUI>();
        if (rankTxt != null) rankTxt.text = $"#{entry.Rank + 1}";

        // Pseudo (PlayerName retourné par UGS, sinon PlayerID tronqué)
        var nameTxt = row.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        if (nameTxt != null)
        {
            string displayName = string.IsNullOrEmpty(entry.PlayerName)
                ? "Player" // on ne garde que Player sans l'ID
                : entry.PlayerName;

            // Retirer le tag # rajouté par UGS (ex: Pseudo#1234 -> Pseudo)
            int hashIndex = displayName.IndexOf('#');
            if (hashIndex > 0)
            {
                displayName = displayName.Substring(0, hashIndex);
            }

            nameTxt.text = displayName;
        }

        // Score
        var scoreTxt = row.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
        if (scoreTxt != null) scoreTxt.text = ((int)entry.Score).ToString("N0");

        // Avion (icône + nom)
        var (planeIdx, planeName) = LeaderboardManager.ParseMetadata(entry);

        var planeImg = row.transform.Find("PlaneIcon")?.GetComponent<Image>();
        if (planeImg != null)
        {
            if (planeSprites != null && planeIdx < planeSprites.Length && planeSprites[planeIdx] != null)
            {
                planeImg.sprite = planeSprites[planeIdx];
                planeImg.gameObject.SetActive(true);
            }
            else
            {
                planeImg.gameObject.SetActive(false);
            }
        }

        var planeNameTxt = row.transform.Find("PlaneNameText")?.GetComponent<TextMeshProUGUI>();
        if (planeNameTxt != null) planeNameTxt.text = planeName;

        // Mettre en évidence la ligne du joueur actuel
        if (AuthenticationService.Instance.IsSignedIn &&
            entry.PlayerId == AuthenticationService.Instance.PlayerId)
        {
            var bg = row.GetComponent<Image>();
            if (bg != null) bg.color = new Color(0.2f, 0.5f, 1f, 0.3f);
        }
    }

    // ──────────────────────────────────────────────────────────
    void SetLoading(bool loading)
    {
        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(loading);
            loadingText.text = GetTranslation("Loading", "Chargement...");
        }

        // On désactive le texte de pagination seulement si on vient d'ouvrir ou rafraîchir tout (latestPage == null)
        if (loading && pageText != null && latestPage == null)
        {
            pageText.gameObject.SetActive(false);
        }
    }
}
