using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake instance;
    public CinemachineCamera vCam;
    private CinemachineBasicMultiChannelPerlin noise;

    private float defaultSize; // Pour stocker le zoom de base

    void Awake()
    {
        instance = this;
        if (vCam != null)
        {
            noise = vCam.GetComponent<CinemachineBasicMultiChannelPerlin>();
            // On sauvegarde la taille de la caméra au démarrage
            defaultSize = vCam.Lens.OrthographicSize;
        }
    }

    public void Shake(float duration, float amplitude)
    {
        if (noise != null) StartCoroutine(DoShake(duration, amplitude));
    }

    IEnumerator DoShake(float duration, float amplitude)
    {
        noise.AmplitudeGain = amplitude;
        yield return new WaitForSecondsRealtime(duration);
        noise.AmplitudeGain = 0f;
    }

    // --- ZOOM + DÉZOOM AUTOMATIQUE ---
    public void ImpactZoom(float targetSize, float zoomSpeed, float dezoomSpeed, float waitDuration)
    {
        if (vCam != null) StartCoroutine(AnimateFullZoom(targetSize, zoomSpeed, dezoomSpeed, waitDuration));
    }

    IEnumerator AnimateFullZoom(float targetSize, float zoomSpeed, float dezoomSpeed, float waitDuration)
    {
        // 1. ZOOM VERS LA CIBLE
        float t = 0;
        float currentSize = vCam.Lens.OrthographicSize;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * zoomSpeed;
            vCam.Lens.OrthographicSize = Mathf.Lerp(currentSize, targetSize, t);
            yield return null;
        }

        // 2. PAUSE (On attend un peu au zoom max)
        yield return new WaitForSecondsRealtime(waitDuration);

        // 3. DÉZOOM VERS LA TAILLE PAR DÉFAUT
        t = 0;
        currentSize = vCam.Lens.OrthographicSize;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * dezoomSpeed;
            vCam.Lens.OrthographicSize = Mathf.Lerp(currentSize, defaultSize, t);
            yield return null;
        }
    }
}