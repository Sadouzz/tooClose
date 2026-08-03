using UnityEngine;
using TMPro;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class NearMissManager : MonoBehaviour
{
    public static NearMissManager instance;

    [Header("UI Elements")]
    public TextMeshProUGUI nearMissText; // Le texte qui va pop et voler
    public RectTransform scoreTarget;     // L'endroit où le texte s'envole (ton Score HUD)

    [Header("Multiplier Target")]
    [Tooltip("La cible UI vers laquelle le texte near-miss vole (le texte du multiplier HUD).")]
    public RectTransform multiplierTarget; // Assigne le RectTransform du texte multiplier
    public TextMeshProUGUI multiplierText; // Le texte qui affiche le multiplier (ex: x2, x3...)

    [Header("Post-Processing Feedback")]
    public Volume globalVolume; // Glisse ton Global Volume ici
    private LensDistortion distortion;

    [Header("Settings")]
    public float slowMoIntensity = 0.6f;
    public float distortionStrength = -0.5f;

    [Header("Settings")]
    public float comboLeeway = 1.5f;      // Temps avant reset du combo
    public Color nearMissColor = Color.yellow;

    [Header("Logic")]
    private int currentCombo = 0;
    private float comboTimer;
    private Coroutine activeAnim;

    private void Awake() => instance = this;

    private void Start()
    {
        if (nearMissText != null) nearMissText.gameObject.SetActive(false);

        // Récupérer l'override LensDistortion depuis le Global Volume
        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out distortion);
            if (distortion == null)
            {
                Debug.LogWarning("[NearMissManager] LensDistortion non trouvée dans le Volume Profile !");
            }
        }
    }

    private void Update()
    {
        if (comboTimer > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0)
            {
                // Combo expiré → on reset le multiplier
                currentCombo = 0;
                if (Inventory.instance != null)
                {
                    Inventory.instance.scoreMultiplier = 1;
                    RefreshMultiplierUI();
                }
            }
        }
    }

    public void TriggerNearMiss(Vector3 missilePosition)
    {
        // 1. Calcul Logique
        currentCombo++;
        comboTimer = comboLeeway;
        int gain = 50 * currentCombo;

        // 2. Mise à jour Score
        Inventory.instance.score += gain;
        Inventory.instance.scoreText.text = Inventory.instance.score.ToString();

        // 3. Augmenter le multiplier de score
        Inventory.instance.scoreMultiplier = 1 + currentCombo;
        RefreshMultiplierUI();

        // 4. Lancer l'animation (On stoppe l'ancienne si elle tournait encore)
        if (activeAnim != null) StopCoroutine(activeAnim);
        activeAnim = StartCoroutine(AnimateNearMiss(currentCombo, gain, missilePosition));

        StopCoroutine("DoImpactEffects"); // Stop si un effet est déjà en cours
        StartCoroutine(DoImpactEffects());

        // 5. Feedback feeling
        Handheld.Vibrate();
    }

    private void RefreshMultiplierUI()
    {
        if (multiplierText == null) return;
        int mult = Inventory.instance != null ? Inventory.instance.scoreMultiplier : 1;
        
        // On s'assure qu'il est toujours visible
        multiplierText.gameObject.SetActive(true);
        multiplierText.text = "x" + mult;
    }

    public void ResetCombo()
    {
        currentCombo = 0;
        comboTimer = 0f;
        if (Inventory.instance != null) Inventory.instance.scoreMultiplier = 1;
        RefreshMultiplierUI();
    }
    IEnumerator AnimateNearMiss(int combo, int gain, Vector3 worldPos)
    {
        // --- INITIALISATION ---
        nearMissText.text = "TOO CLOSE! x" + combo + "\n+" + gain;
        nearMissText.color = nearMissColor;

        // Conversion position monde -> écran
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        nearMissText.rectTransform.position = screenPos;

        // SÉCURITÉ : On remet la rotation à zéro pour éviter que le texte soit penché
        nearMissText.rectTransform.rotation = Quaternion.identity;

        nearMissText.gameObject.SetActive(true);
        nearMissText.rectTransform.localScale = Vector3.zero;

        float t = 0;

        // --- PHASE 1 : LE POP ---
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * 10f;

            // CORRECTION DU SCALE : On utilise Mathf.Abs pour être SÛR que le scale n'est jamais négatif (ce qui renverse le texte)
            // Et on ajoute un clamp pour ne pas dépasser 1.2f
            float scaleValue = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 1.1f) * 1.2f);

            nearMissText.rectTransform.localScale = new Vector3(scaleValue, scaleValue, 1);
            yield return null;
        }

        // On s'assure qu'il finit à une taille normale (1,1,1) sans être renversé
        nearMissText.rectTransform.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(0.3f);

        // --- PHASE 2 : VOL VERS LA CIBLE (multiplier ou score) ---
        t = 0;
        Vector2 startPos = nearMissText.rectTransform.position;
        RectTransform flyTarget = multiplierTarget != null ? multiplierTarget : scoreTarget;

        while (t < 1)
        {
            t += Time.unscaledDeltaTime * 2.5f;
            float easedT = t * t;

            nearMissText.rectTransform.position = Vector2.Lerp(startPos, flyTarget.position, easedT);

            // On réduit la taille vers la cible sans jamais descendre en dessous de zéro
            float flyScale = Mathf.Lerp(1f, 0.3f, easedT);
            nearMissText.rectTransform.localScale = new Vector3(flyScale, flyScale, 1);

            yield return null;
        }

        nearMissText.gameObject.SetActive(false);

        // Pulse sur la cible d'arrivée (multiplier ou score)
        if (multiplierTarget != null)
            StartCoroutine(PulseTransform(multiplierTarget));
        else
            StartCoroutine(PulseScore());
    }
    IEnumerator PulseScore()
    {
        Transform sText = Inventory.instance.scoreText.transform;
        float t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * 5f;
            float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
            sText.localScale = new Vector3(s, s, 1);
            yield return null;
        }
        sText.localScale = Vector3.one;
    }

    IEnumerator PulseTransform(RectTransform target)
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * 5f;
            float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.3f;
            target.localScale = new Vector3(s, s, 1);
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    IEnumerator DoSlowMo()
    {
        Time.timeScale = slowMoIntensity;
        yield return new WaitForSecondsRealtime(0.08f);
        Time.timeScale = 1f;
    }

    IEnumerator DoImpactEffects()
    {
        // --- 1. ONDE DE CHOC (Distortion) ---
        if (distortion != null)
        {
            distortion.intensity.overrideState = true;
            distortion.intensity.value = distortionStrength;
            // On s'assure que l'override est bien actif
            distortion.active = true;
        }

        // Temps de maintien de l'impact (très court pour le "punch")
        // 0.05f ou 0.1f est idéal pour un feedback instantané
        yield return new WaitForSecondsRealtime(0.08f);

        // --- 2. RETOUR À LA NORMALE (Smooth transition) ---
        float t = 0;
        while (t < 1)
        {
            // On utilise unscaledDeltaTime au cas où tu aurais 
            // d'autres systèmes qui gèrent le temps ailleurs
            t += Time.unscaledDeltaTime * 5f;

            // On remet la distortion à 0 progressivement
            if (distortion != null)
            {
                distortion.intensity.overrideState = true;
                distortion.intensity.value = Mathf.Lerp(distortionStrength, 0f, t);
            }

            yield return null;
        }

        // Sécurité finale
        if (distortion != null)
        {
            distortion.intensity.overrideState = true;
            distortion.intensity.value = 0f;
        }
    }
}
