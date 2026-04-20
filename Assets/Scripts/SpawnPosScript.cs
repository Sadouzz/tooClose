using UnityEngine;

public class SpawnPosScript : MonoBehaviour
{
    [Header("Positions (Relatives au Grand-Parent)")]
    [Tooltip("Saisis ici les coordonnées X,Y,Z par rapport au grand-parent")]
    public Vector3 zoomPos;
    private Vector3 initPos;

    private Transform grandParent;

    void Start()
    {
        // 1. On va chercher le parent du parent
        if (transform.parent != null && transform.parent.parent != null)
        {
            grandParent = transform.parent.parent;

            // 2. On sauvegarde la position initiale LOCALE par rapport au grand-parent
            initPos = grandParent.InverseTransformPoint(transform.position);
        }
        else
        {
            Debug.LogWarning("Attention : L'objet n'a pas de grand-parent !");
            initPos = transform.position; // Fallback de sécurité
        }
    }

    void FixedUpdate()
    {
        // S'il n'y a pas de grand-parent (erreur de hiérarchie), on utilise le système classique
        if (grandParent == null)
        {
            HandleBasicMovement();
            return;
        }

        // 3. On applique la position en fonction du PowerUp
        if (PlayerPowerUpManager.instance != null && PlayerPowerUpManager.instance.isZoomActive)
        {
            // TransformPoint convertit la position "zoomPos" (relative au grand-parent) en position réelle sur l'écran
            transform.position = grandParent.TransformPoint(zoomPos);
        }
        else
        {
            // Retour à la position de base (qui suivra toujours le grand-parent)
            transform.position = grandParent.TransformPoint(initPos);
        }
    }

    // Fonction de sécurité si ton objet n'est pas placé correctement dans la hiérarchie
    private void HandleBasicMovement()
    {
        if (PlayerPowerUpManager.instance != null && PlayerPowerUpManager.instance.isZoomActive)
            transform.position = zoomPos;
        else
            transform.position = initPos;
    }
}