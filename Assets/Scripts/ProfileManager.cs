using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

/// <summary>
/// Gère l'affichage du profil du joueur et la modification de son pseudo.
/// 
/// Affiche :
///   - Pseudo (modifiable)
///   - Nombre d'avions débloqués
///   - Meilleur score Easy
///   - Meilleur score Hard
///   - Nombre total de missiles détruits
///   - Nombre total d'ennemis détruits
///   - Solde total d'étoiles
/// </summary>
public class ProfileManager : MonoBehaviour
{
    public static ProfileManager instance;

    [Header("Panel Root")]
    public GameObject profilePanel;

    [Header("Modification du pseudo")]
    public TMP_InputField nicknameInput;
    public Button         saveNicknameButton;

    [Header("Liaison du compte")]
    public Button         linkAccountButton;
    public TextMeshProUGUI linkAccountText;

    [Header("UI Textes - Statistiques")]
    public TextMeshProUGUI planesUnlockedText;
    public TextMeshProUGUI easyHighScoreText;
    public TextMeshProUGUI hardHighScoreText;
    public TextMeshProUGUI totalMissilesText;
    public TextMeshProUGUI totalEnemiesText;
    public TextMeshProUGUI totalStarsText;

    [Header("Affichages du Pseudo (Partout dans l'UI)")]
    public TextMeshProUGUI[] playerNameDisplays;

    [Header("Affichage de l'ID")]
    public TextMeshProUGUI playerIdText;

    // ──────────────────────────────────────────────────────────
    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
    }

    void Start()
    {
        if (nicknameInput != null)
        {
            nicknameInput.onEndEdit.AddListener(SaveNickname);
        }

        // Par défaut, le panel est fermé
        if (profilePanel != null) profilePanel.SetActive(false);

        // Afficher immédiatement les pseudos mis en cache
        RefreshPlayerNameDisplays();

        // Mettre à jour les pseudos dès que l'auth (ou un changement de pseudo) réussit
        AuthManager.OnAuthenticated += RefreshPlayerNameDisplays;
    }

    void OnDestroy()
    {
        AuthManager.OnAuthenticated -= RefreshPlayerNameDisplays;
    }

    // ──────────────────────────────────────────────────────────
    // ─── Ouvrir / Fermer ──────────────────────────────────────
    // ──────────────────────────────────────────────────────────

    public void OpenProfile()
    {
        if (profilePanel != null) profilePanel.SetActive(true);
        RefreshProfileData();
    }

    public void CloseProfile()
    {
        if (profilePanel != null) profilePanel.SetActive(false);
    }

    // ──────────────────────────────────────────────────────────
    // ─── Données & UI ─────────────────────────────────────────
    // ──────────────────────────────────────────────────────────

    public void RefreshPlayerNameDisplays()
    {
        if (AuthManager.instance == null) return;

        string fullName = AuthManager.instance.PlayerName;
        string cleanName = fullName;

        // UGS rajoute un # et des chiffres (ex: Pseudo#1234). On coupe le # pour l'affichage propre
        if (!string.IsNullOrEmpty(fullName))
        {
            int hashIndex = fullName.IndexOf('#');
            if (hashIndex > 0)
            {
                cleanName = fullName.Substring(0, hashIndex);
            }
        }

        // Met à jour l'input du profil avec le nom propre
        if (nicknameInput != null)
        {
            nicknameInput.text = cleanName;
        }

        // Met à jour tous les textes (Menu principal, page stat, etc.) avec le nom propre
        if (playerNameDisplays != null)
        {
            foreach (var display in playerNameDisplays)
            {
                if (display != null)
                {
                    display.text = cleanName;
                }
            }
        }

        // Met à jour l'affichage de l'ID du joueur
        if (playerIdText != null)
        {
            playerIdText.text = AuthManager.instance.PlayerId;
        }
    }

    private string GetTranslation(string key, string fallback)
    {
        try
        {
            string tr = LocalizationSettings.StringDatabase.GetLocalizedString("UITexts", key);
            if (string.IsNullOrEmpty(tr) || tr.Contains("No translation")) return fallback;
            return tr;
        }
        catch
        {
            return fallback;
        }
    }

    public void RefreshProfileData()
    {
        // 1. Mettre à jour tous les affichages du pseudo et l'ID
        RefreshPlayerNameDisplays();

        // 2. Calculer le nombre d'avions débloqués
        int totalPlanes = 8;
        if (ChoosingPlaneScript.instance != null)
        {
            totalPlanes = ChoosingPlaneScript.instance.transform.childCount;
        }
        else
        {
            // Tente de trouver dynamiquement dans la scène s'il n'est pas instancié en Singleton
            ChoosingPlaneScript cps = FindFirstObjectByType<ChoosingPlaneScript>();
            if (cps != null) totalPlanes = cps.transform.childCount;
        }

        int unlockedCount = 0;
        for (int i = 0; i < totalPlanes; i++)
        {
            // L'index 0 (avion de base) est toujours débloqué par défaut
            if (i == 0 || PlayerPrefs.GetInt("Unlocked_" + i, 0) == 1)
            {
                unlockedCount++;
            }
        }

        if (planesUnlockedText != null)
        {
            planesUnlockedText.text = $"{unlockedCount} / {totalPlanes}";
        }

        // 3. Charger les records
        int normalHighScore = PlayerPrefs.GetInt("highscore", 0);
        int hardHighScore = PlayerPrefs.GetInt("highscoreHard", 0);

        if (easyHighScoreText != null) easyHighScoreText.text = normalHighScore.ToString("N0");
        if (hardHighScoreText != null) hardHighScoreText.text = hardHighScore.ToString("N0");

        // 4. Charger les statistiques globales
        int totalMissiles = PlayerPrefs.GetInt("totalDestroyedMissiles", 0);
        int totalEnemies  = PlayerPrefs.GetInt("totalDestroyedEnemies",  0);
        int totalStars    = PlayerPrefs.GetInt("stars", 0);

        if (totalMissilesText != null) totalMissilesText.text = totalMissiles.ToString("N0");
        if (totalEnemiesText  != null) totalEnemiesText.text  = totalEnemies.ToString("N0");
        if (totalStarsText    != null) totalStarsText.text    = totalStars.ToString("N0");

        // 5. Désactiver le bouton de lien si déjà lié
        if (linkAccountButton != null)
        {
            bool isLinked = AuthManager.instance != null && AuthManager.instance.IsAccountLinked();
            linkAccountButton.interactable = !isLinked;
            
            if (linkAccountText != null)
            {
                string linkedText = GetTranslation("SAUVEGARDE_DONE", "SAUVEGARDÉ");
                string notLinkedText = GetTranslation("SAUV. LA PROGRESSION", "SAUV. LA PROGRESSION");
                linkAccountText.text = isLinked ? linkedText : notLinkedText;
            }
        }
    }

    // ──────────────────────────────────────────────────────────
    // ─── Actions ──────────────────────────────────────────────
    // ──────────────────────────────────────────────────────────

    async void SaveNickname(string newName)
    {
        if (string.IsNullOrEmpty(newName)) return;

        if (nicknameInput != null) nicknameInput.interactable = false;

        if (AuthManager.instance != null)
        {
            await AuthManager.instance.UpdatePlayerName(newName);
        }

        if (nicknameInput != null) nicknameInput.interactable = true;

        // Rafraîchir l'affichage local du pseudo
        RefreshProfileData();
    }

    public void CopyPlayerId()
    {
        if (AuthManager.instance != null && !string.IsNullOrEmpty(AuthManager.instance.PlayerId))
        {
            GUIUtility.systemCopyBuffer = AuthManager.instance.PlayerId;
            Debug.Log("[Profile] Player ID copied to clipboard: " + AuthManager.instance.PlayerId);
        }
    }
}
