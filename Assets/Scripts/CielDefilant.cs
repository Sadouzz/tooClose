using UnityEngine;

public class CielDefilant : MonoBehaviour
{
    // Vitesse de défilement (0.1f est une bonne base)
    public float vitesseDefilement = 0.1f;

    // Référence au renderer pour accéder au matériau
    private Renderer quadRenderer;

    void Start()
    {
        // Récupérer le composant Renderer du Quad
        quadRenderer = GetComponent<Renderer>();
    }

    void Update()
    {
        // Calculer le nouvel offset basé sur le temps
        // Time.time augmente en continu
        float offsetHorizontal = Time.time * vitesseDefilement;

        // Créer le vecteur d'offset (nous ne changeons que l'axe X)
        Vector2 nouvelOffset = new Vector2(offsetHorizontal, 0f);

        // Appliquer le décalage à la texture principale du matériau
        // Cela ne fonctionne que si la texture est en Wrap Mode : Repeat
        quadRenderer.material.mainTextureOffset = nouvelOffset;
    }
}