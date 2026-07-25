using UnityEngine;

public class PrivacyPolicyManager : MonoBehaviour
{
    public static PrivacyPolicyManager instance;

    [Header("UI Elements")]
    [Tooltip("Le grand panneau qui contient tout le texte de la politique de confidentialité.")]
    public GameObject privacyPanel;
    
    [Tooltip("Le gros bouton jaune J'ACCEPTE.")]
    public GameObject acceptButton;
    
    [Tooltip("Le bouton de fermeture (une croix X ou un texte FERMER).")]
    public GameObject closeButton;

    private const string PRIVACY_PREF_KEY = "PrivacyAccepted";

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        // Au lancement, on vérifie si la politique a été acceptée (0 = non, 1 = oui)
        if (PlayerPrefs.GetInt(PRIVACY_PREF_KEY, 0) == 0)
        {
            // Non acceptée, on affiche le panel avec uniquement le bouton Accepter
            ShowPanel(true);
        }
        else
        {
            // Déjà acceptée, on s'assure que le panel est bien caché au démarrage
            if (privacyPanel != null) privacyPanel.SetActive(false);
        }
    }

    // Méthode à relier au gros bouton "J'ACCEPTE ET JE CONTINUE"
    public void AcceptPrivacy()
    {
        PlayerPrefs.SetInt(PRIVACY_PREF_KEY, 1);
        PlayerPrefs.Save();
        
        if (privacyPanel != null) privacyPanel.SetActive(false);
    }

    // Méthode à relier au bouton de fermeture (la croix)
    public void ClosePrivacyPolicy()
    {
        if (privacyPanel != null) privacyPanel.SetActive(false);
    }

    // Méthode à relier à votre texte cliquable "CONFIDENTIALITÉ ET CONDITIONS..." dans les options
    public void OuvrirDepuisMenu()
    {
        // Quand on l'ouvre depuis le menu, ça veut dire qu'on l'a déjà acceptée avant.
        // Donc on affiche uniquement le bouton Fermer (et pas le bouton Accepter).
        ShowPanel(false);
    }

    private void ShowPanel(bool isFirstTime)
    {
        if (privacyPanel != null) privacyPanel.SetActive(true);
        
        if (acceptButton != null) acceptButton.SetActive(isFirstTime);
        if (closeButton != null) closeButton.SetActive(!isFirstTime);
    }
    private const string PRIVACY_URL = "https://ousmansadjo.com/projects/too-close/privacy/";

    // Ouvre la politique de confidentialité dans le navigateur du téléphone
    public void OpenPrivacyURL()
    {
        Application.OpenURL(PRIVACY_URL);
    }
}
