using UnityEngine;

public class PlaneData : MonoBehaviour
{
    [Header("Stats")]
    public float speed = 6f;
    public float rotationSpeed = 5f;
    public int price = 100, life = 1;
    public string planeName = "Chasseur Bleu";

    [Header("Juice Settings")]
    public float maxTiltAngle = 20f; // Inclinaison max spécifique à cet avion
    public float tiltSpeed = 10f;    // Vitesse d'inclinaison spécifique

    [Header("References")]
    public Rigidbody2D rb;
    public SpriteRenderer sr;
    public BoxCollider2D bc;
}