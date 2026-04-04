using UnityEngine;
using TMPro;

public class DieManagerUI : MonoBehaviour
{
    public TextMeshProUGUI scoreText, pickedStarsText, chronoText, totalStarsText, destroyedMissilesText;
    public TextMeshProUGUI scoreStarsText, pickedStarsStarsText, chronoStarsText, destroyedMissilesStarsText;

    public static DieManagerUI instance;

    private void Awake()
    {
        if (instance != null)
        {
            return;
        }
        instance = this;
    }

    public void DisplayPanel(string time, int totalSeconds, int score, int destroyedMissiles, int stars)
    {
        // Textes de stats
        chronoText.text = time;
        scoreText.text = score.ToString();
        destroyedMissilesText.text = destroyedMissiles.ToString();
        pickedStarsText.text = stars.ToString();

        // Calcul des gains d'étoiles (Bonus)
        int timeStars = totalSeconds / 10;
        int scoreStars = score / 10;

        chronoStarsText.text = "+" + timeStars;
        scoreStarsText.text = "+" + scoreStars;
        destroyedMissilesStarsText.text = "+" + destroyedMissiles;
        pickedStarsStarsText.text = "+" + stars;

        int totalEarned = timeStars + scoreStars + destroyedMissiles + stars;
        totalStarsText.text = "+" + totalEarned;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
