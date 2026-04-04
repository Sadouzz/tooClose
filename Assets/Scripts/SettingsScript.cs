using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Nécessaire pour manipuler le composant Image

public class SettingsScript : MonoBehaviour
{
    [Header("UI References")]
    public Image buttonImage; // Glisse l'image du bouton ici dans l'inspecteur
    public Sprite joystickIcon;
    public Sprite lateralIcon;

    private string movementModeKey = "MovementMode"; // La clé sauvegardée dans PlayerPrefs

    private void Start()
    {
        // Au démarrage, on initialise l'icône selon la valeur sauvegardée
        UpdateSettingsUI();
    }

    // Cette fonction sera appelée par le OnClick du bouton
    public void ToggleMovementMode()
    {
        // On récupère le mode actuel (0 pour joystick, 1 pour lateral par exemple)
        int currentMode = PlayerPrefs.GetInt(movementModeKey, 0);

        // On inverse la valeur
        if (currentMode == 0)
        {
            PlayerPrefs.SetInt(movementModeKey, 1); // Passe en Latéral
        }
        else
        {
            PlayerPrefs.SetInt(movementModeKey, 0); // Passe en Joystick
        }

        PlayerPrefs.Save();

        // On met à jour l'image du bouton
        UpdateSettingsUI();
    }

    private void UpdateSettingsUI()
    {
        int currentMode = PlayerPrefs.GetInt(movementModeKey, 0);

        if (currentMode == 0)
        {
            buttonImage.sprite = joystickIcon;
        }
        else
        {
            buttonImage.sprite = lateralIcon;
        }
        PlayerMovement.instance.RefreshMovementMode();
    }

    public void DeleteAllPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        SceneManager.LoadScene(0);
    }
}