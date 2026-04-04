using UnityEngine;

public class NearMissDetector : MonoBehaviour
{
    public float comboMultiplier = 1.0f;
    public float comboDecayTime = 2.0f; // Temps avant que le combo retombe
    private float lastMissTime;

    /*void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Missile"))
        {
            TriggerNearMiss(other.transform.position);
        }
    }*/

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Missile")) // Vérifie que c'est bien un missile
        {
            // On demande à l'Inventory de calculer les points
            // On passe la position du missile pour savoir où afficher le texte
            NearMissManager.instance.TriggerNearMiss(other.transform.position);
        }
    }
    /*
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Missile"))
        {
            // Calcul de la proximité (1.0 = très près, 0.1 = limite du cercle)
            float distance = Vector2.Distance(transform.position, other.transform.position);
            float radius = GetComponent<CircleCollider2D>().radius;
            float proximityFactor = 1f - (distance / radius);
            proximityFactor = Mathf.Clamp(proximityFactor, 0.2f, 1f);

            Inventory.instance.RegisterNearMiss(proximityFactor);

            // Optionnel : Petit ralenti rapide (Bullet Time)
            //StopAllCoroutines();
            //StartCoroutine(NearMissEffect());
        }
    }
    
    System.Collections.IEnumerator NearMissEffect()
    {
        Time.timeScale = 0.5f; // Ralenti à 50%
        yield return new WaitForSecondsRealtime(0.1f);
        Time.timeScale = 1f;
    }
    /*
    void TriggerNearMiss(Vector3 missilePos)
    {
        // 1. Calcul du multiplicateur
        if (Time.time - lastMissTime < comboDecayTime)
        {
            comboMultiplier += 0.5f; // On augmente le combo
        }
        else
        {
            comboMultiplier = 1.0f; // Réinitialisation
        }

        lastMissTime = Time.time;

        // 2. Lancer l'effet visuel et le texte
        //UIController.Instance.ShowNearMissText(comboMultiplier);
        //EffectsManager.Instance.DoSlowMotion(0.2f, 0.1f); // Durée, Intensité
    }*/
}
