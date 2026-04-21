using UnityEngine;

public class PropellerSpin : MonoBehaviour
{
    [Header("Réglages")]
    public float spinSpeed = 1000f; // Vitesse (plus c'est haut, plus ça tourne vite)
    public bool isSpinning = true;

    void Update()
    {
        if (isSpinning)
        {
            // En 2D, on tourne autour de l'axe Z. 
            // Si c'est de la 3D, ça pourrait être (spinSpeed, 0, 0) selon l'orientation.
            transform.Rotate(0, spinSpeed * Time.deltaTime, 0);
        }
    }
}