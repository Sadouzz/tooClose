using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpStar : MonoBehaviour
{
    public AudioSource picked;
    void Start()
    {
        //transform.position = new Vector3(transform.position.x, .5f, transform.position.z);
        picked = GameObject.FindGameObjectWithTag("EventSoundStar").GetComponent<AudioSource>();
    }

    void Update()
    {
        Quaternion newRotation = new Quaternion(transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w);
        newRotation *= Quaternion.Euler(0, 0, 90);
        transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, 2.5f * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            StarPicked();
        }
    }

    void StarPicked()
    {
        picked.Play();
        SpawnVFX();
        
        // Launch flying animation instead of destroying immediately
        StartCoroutine(FlyToTargetAndDestroy());
    }

    IEnumerator FlyToTargetAndDestroy()
    {
        // Disable collisions so it doesn't trigger again
        GetComponent<Collider2D>().enabled = false;
        
        // 1. Pop animation
        float t = 0;
        Vector3 initialScale = transform.localScale;
        while (t < 1)
        {
            t += Time.deltaTime * 5f;
            float scale = Mathf.Sin(t * Mathf.PI * 0.8f) * 1.5f;
            transform.localScale = initialScale * scale;
            yield return null;
        }
        transform.localScale = initialScale;

        yield return new WaitForSeconds(0.1f);

        // 2. Fly to UI target
        t = 0;
        Vector3 startPos = transform.position;
        
        while (t < 1)
        {
            t += Time.deltaTime * 2.5f;
            float easedT = t * t; // Acceleration for a snappy flight
            
            if (Inventory.instance != null && Inventory.instance.pickedStarsText != null)
            {
                Vector3 targetScreenPos = Inventory.instance.pickedStarsText.transform.position;
                targetScreenPos.z = Mathf.Abs(Camera.main.transform.position.z);
                Vector3 endPos = Camera.main.ScreenToWorldPoint(targetScreenPos);
                endPos.z = 0;

                transform.position = Vector3.Lerp(startPos, endPos, easedT);
            }
            
            transform.localScale = Vector3.Lerp(initialScale, initialScale * 0.4f, easedT);
            yield return null;
        }

        // 3. Add score and pulse UI
        if (Inventory.instance != null)
        {
            Inventory.instance.AddStars(1);
        }
        
        Destroy(gameObject);
    }

    IEnumerator PulseText(Transform textTransform)
    {
        float t = 0;
        Vector3 startScale = Vector3.one;
        while (t < 1)
        {
            t += Time.deltaTime * 6f;
            float scale = Mathf.Lerp(1f, 1.4f, Mathf.Sin(t * Mathf.PI));
            textTransform.localScale = startScale * scale;
            yield return null;
        }
        textTransform.localScale = startScale;
    }

    void SpawnVFX()
    {
        GameObject vfx = new GameObject("StarVFX");
        vfx.transform.position = transform.position;
        
        ParticleSystem ps = vfx.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        
        // FIX: Ensure system is stopped before modifying duration
        main.playOnAwake = false;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        Color starColor;
        ColorUtility.TryParseHtmlString("#F6D740", out starColor);

        main.duration = 1f;
        main.startLifetime = 0.4f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startColor = starColor;
        main.loop = false;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 12) }); // Burst de 12 particules

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var colOverLife = ps.colorOverLifetime;
        colOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(starColor, 0.0f), new GradientColorKey(new Color(1f, 0.6f, 0f), 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colOverLife.color = grad;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default")); // Rendu lumineux 2D
        renderer.sortingOrder = 15;

        ps.Play();
        Destroy(vfx, 1.5f); // Détruit le VFX après l'animation
    }
}
