using UnityEngine;
using UnityEngine.UI;

public class OffScreenIndicator : MonoBehaviour
{
    public Transform target;
    public float margin = 40f;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // SÉCURITÉ : Si on a oublié d'ajouter le CanvasGroup sur le Prefab, le script l'ajoute tout seul
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void UpdateIndicator()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // On vérifie que la caméra existe pour éviter d'autres erreurs
        if (Camera.main == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position);

        // Ta logique de détection...
        bool isOffScreen = screenPos.z < 0 || screenPos.x <= 0 || screenPos.x >= Screen.width || screenPos.y <= 0 || screenPos.y >= Screen.height;

        // Utilisation sécurisée
        canvasGroup.alpha = isOffScreen ? 1f : 0f;

        if (isOffScreen)
        {
            canvasGroup.alpha = 1;

            // Inversion si le missile est derrière pour éviter l'effet miroir
            if (screenPos.z < 0) screenPos *= -1;

            float x = Mathf.Clamp(screenPos.x, margin, Screen.width - margin);
            float y = Mathf.Clamp(screenPos.y, margin, Screen.height - margin);

            transform.position = new Vector3(x, y, 0);

            // Rotation vers le missile
            Vector3 dir = screenPos - transform.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90);
        }
        else
        {
            canvasGroup.alpha = 0; // Cache l'icône si le missile est visible
        }
    }
}