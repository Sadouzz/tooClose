using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DieManagerUI : MonoBehaviour
{
    public TextMeshProUGUI scoreText, pickedStarsText, chronoText, totalStarsText, destroyedMissilesText;
    public TextMeshProUGUI scoreStarsText, pickedStarsStarsText, chronoStarsText, destroyedMissilesStarsText;
    
    [Header("X2 Reward")]
    public GameObject reviveButton;
    public GameObject x2Button;

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
        chronoText.text = time;
        scoreText.text = score.ToString();
        destroyedMissilesText.text = destroyedMissiles.ToString();
        pickedStarsText.text = stars.ToString();

        int timeStars = totalSeconds / 10;
        int scoreStars = score / 10;

        chronoStarsText.text = "+" + timeStars;
        scoreStarsText.text = "+" + scoreStars;
        destroyedMissilesStarsText.text = "+" + destroyedMissiles;
        pickedStarsStarsText.text = "+" + stars;

        int totalEarned = timeStars + scoreStars + destroyedMissiles + stars;
        totalStarsText.text = "+" + totalEarned;

        if (reviveButton != null)
        {
            reviveButton.SetActive(true); // Toujours visible
            Button btn = reviveButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = !(Inventory.instance != null && Inventory.instance.hasRevived);
            }
        }

        if (x2Button != null)
        {
            x2Button.SetActive(true);
            Button btn = x2Button.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = true;
            }
        }
    }

    public void OnClickX2Reward()
    {
        if (AdMob.instance != null)
        {
            AdMob.instance.ShowRewardedAd("DoubleRewards");
        }
    }

    public void UpdateDoubledRewards(int additionalStars)
    {
        int currentDisplayed = 0;
        if (int.TryParse(totalStarsText.text.Replace("+", ""), out currentDisplayed))
        {
            totalStarsText.text = "+" + (currentDisplayed + additionalStars);
        }

        if (x2Button != null)
        {
            Button btn = x2Button.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = false;
            }
        }
    }

    void Start() {}
    void Update() {}
}
