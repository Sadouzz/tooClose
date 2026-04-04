using UnityEngine;

public class PowerUpItem : MonoBehaviour
{
    public string powerUpName = "Vitesse Boost !";
    public Sprite powerUpIcon; // L'icône qui va voler vers le haut
    public Color feedbackColor = Color.yellow;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // On envoie les infos au manager d'UI
            PowerUpUIManager.instance.ShowPowerUpFeedback(powerUpName, powerUpIcon, transform.position, feedbackColor);

            // On détruit l'objet physique
            Destroy(gameObject);
        }
    }
}