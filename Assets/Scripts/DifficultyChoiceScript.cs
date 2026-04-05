using UnityEngine;
using UnityEngine.UI;

public class DifficultyChoiceScript : MonoBehaviour
{
    public Image imageEasy, imageHard;

    [Header("Settings de Transparence")]
    [Range(0f, 1f)] public float alphaActive = 1f;    // 100% opaque
    [Range(0f, 1f)] public float alphaInactive = 0.3f; // 30% transparent

    private void Start()
    {
        // On initialise au démarrage
        if (PlayerPrefs.GetString("Difficulty", "Easy") == "Easy")
        {
            ApplyAlpha(imageEasy, alphaActive);
            ApplyAlpha(imageHard, alphaInactive);
        }
        else
        {
            ApplyAlpha(imageEasy, alphaInactive);
            ApplyAlpha(imageHard, alphaActive);
        }
    }

    public void SetEasy()
    {
        PlayerPrefs.SetString("Difficulty", "Easy");
        ApplyAlpha(imageEasy, alphaActive);
        ApplyAlpha(imageHard, alphaInactive);

        if (SpawnObjects.instance != null) SpawnObjects.instance.UpdateDifficulty();
        if (MissileSpawner.instance != null) MissileSpawner.instance.UpdateDifficulty();
    }

    public void SetHard()
    {
        PlayerPrefs.SetString("Difficulty", "Hard");
        ApplyAlpha(imageEasy, alphaInactive);
        ApplyAlpha(imageHard, alphaActive);

        if (SpawnObjects.instance != null) SpawnObjects.instance.UpdateDifficulty();
        if (MissileSpawner.instance != null) MissileSpawner.instance.UpdateDifficulty();
    }

    // Fonction utilitaire pour changer l'alpha proprement
    private void ApplyAlpha(Image img, float alpha)
    {
        if (img == null) return;

        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}