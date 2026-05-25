using UnityEngine;

public class BulletScript : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 25f;
    
    private float spawnTime;

    void Start()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        // Bullets fly straight upwards in world space
        transform.Translate(Vector3.up * speed * Time.deltaTime, Space.World);

        // Auto destroy after 3 seconds to prevent memory leaks
        if (Time.time - spawnTime > 3f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        // Collision with space-shooter style enemies
        if (col.CompareTag("Enemy"))
        {
            EnemyScript enemy = col.GetComponent<EnemyScript>();
            if (enemy != null)
            {
                enemy.TakeDamage(1);
            }
            Destroy(gameObject);
        }
        // Collision with enemy-fired or dodging phase missiles
        else if (col.CompareTag("Missile"))
        {
            MissileScript missile = col.GetComponent<MissileScript>();
            if (missile != null)
            {
                missile.HandleDestruction(true); // Explode the missile!
            }
            Destroy(gameObject); // Destroy the laser bullet
        }
    }
}