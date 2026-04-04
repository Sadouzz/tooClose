using UnityEngine;
using TMPro;

public class HighScoreScript : MonoBehaviour
{
    [Header("UI Text References")]
    public TextMeshProUGUI normalScoreText;
    public TextMeshProUGUI hardScoreText;

    public static HighScoreScript instance;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        DisplayHighScores();
    }

    public void DisplayHighScores()
    {
        int normalScore = PlayerPrefs.GetInt("highscore", 0);
        int hardScore = PlayerPrefs.GetInt("highscoreHard", 0);

        if (normalScoreText != null) normalScoreText.text = normalScore.ToString();
        if (hardScoreText != null) hardScoreText.text = hardScore.ToString();
    }

    // --- LA LOGIQUE DE SAUVEGARDE INTÉGRÉE ---
    public void SaveNewScore(int score, bool isHardMode)
    {
        // 1. Choisir la bonne clé selon le mode
        string key = isHardMode ? "highscoreHard" : "highscore";

        // 2. Récupérer l'ancien record
        int oldHighscore = PlayerPrefs.GetInt(key, 0);

        if (score > oldHighscore)
        {
            // Nouveau record !
            PlayerPrefs.SetInt(key, score);

            // 3. Activation du panel spécial (via ton UIManager)
            if (UIManager.instance != null && UIManager.instance.highscorePanel != null)
            {
                UIManager.instance.highscorePanel.SetActive(true);
            }

            // 4. Sauvegarde physique immédiate
            PlayerPrefs.Save();

            // Rafraîchir l'affichage des textes
            DisplayHighScores();
        }
    }
}