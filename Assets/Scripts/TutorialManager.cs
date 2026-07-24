using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;

    [Header("UI Elements")]
    [Tooltip("Le grand panneau qui contient tout le tutoriel.")]
    public GameObject tutorialPanel; 
    
    [Tooltip("Glissez ici vos 3 (ou plus) pages (GameObjects) dans l'ordre.")]
    public GameObject[] pages; 
    
    [Header("Buttons")]
    [Tooltip("Le bouton avec la flèche de droite (Suivant).")]
    public GameObject nextButton;
    
    [Tooltip("Le bouton avec la flèche de gauche (Précédent).")]
    public GameObject previousButton;
    
    [Tooltip("Le bouton de la dernière page pour fermer le tutoriel (ex: icône Play rouge).")]
    public GameObject closePlayButton; 

    private int currentPageIndex = 0;
    private const string TUTORIAL_PREF_KEY = "TutorialDone";

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        // On s'assure que le panneau est bien caché au démarrage
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    // Ouvre le tutoriel (peut être appelé par un bouton du menu "Tutoriel")
    public void OpenTutorial()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        currentPageIndex = 0;
        UpdateUI();
        Time.timeScale = 0; // Pause le jeu
    }

    // Passe à la page suivante (à relier au OnClick de nextButton)
    public void NextPage()
    {
        if (currentPageIndex < pages.Length - 1)
        {
            currentPageIndex++;
            UpdateUI();
        }
    }

    // Revient à la page précédente (à relier au OnClick de previousButton)
    public void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdateUI();
        }
    }

    // Ferme le tutoriel (à relier au OnClick de closePlayButton)
    public void CloseTutorial()
    {
        // On sauvegarde le fait que le joueur a terminé le tuto
        PlayerPrefs.SetInt(TUTORIAL_PREF_KEY, 1);
        PlayerPrefs.Save();

        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        Time.timeScale = 1; // Reprend le jeu
    }

    // Met à jour l'affichage des bonnes pages et des bons boutons
    private void UpdateUI()
    {
        // 1. On affiche uniquement la page actuelle et on cache les autres
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
            {
                pages[i].SetActive(i == currentPageIndex);
            }
        }

        // 2. Le bouton "Précédent" n'est affiché que si on n'est pas sur la première page
        if (previousButton != null) 
            previousButton.SetActive(currentPageIndex > 0);
        
        bool isLastPage = (currentPageIndex == pages.Length - 1);
        
        // 3. Le bouton "Suivant" disparaît sur la dernière page
        if (nextButton != null) 
            nextButton.SetActive(!isLastPage);
            
        // 4. Le bouton "Fermer/Play" n'apparaît que sur la dernière page
        if (closePlayButton != null) 
            closePlayButton.SetActive(isLastPage);
    }
}
