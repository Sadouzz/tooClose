using UnityEngine;

public class PowerUpItem : MonoBehaviour
{
    public string powerUpName = "Vitesse Boost !";
    public Sprite powerUpIcon; // L'icone qui va voler vers le haut
    public Color feedbackColor = Color.yellow;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SpawnVFX();

            // On envoie les infos au manager d'UI
            PowerUpUIManager.instance.ShowPowerUpFeedback(powerUpName, powerUpIcon, transform.position, feedbackColor);

            // On detruit l'objet physique
            Destroy(gameObject);
        }
    }

    void SpawnVFX()
    {
        GameObject vfx = new GameObject("PowerUpVFX");
        vfx.transform.position = transform.position;
        
        ParticleSystem ps = vfx.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.playOnAwake = false;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        main.duration = 1f;
        main.startLifetime = 0.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        main.startColor = feedbackColor; // S'adapte a la couleur du prefab !
        main.loop = false;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 20) }); // Burst de particules

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var colOverLife = ps.colorOverLifetime;
        colOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(feedbackColor, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colOverLife.color = grad;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = 15;

        ps.Play();
        Destroy(vfx, 1.5f);
    }
}
