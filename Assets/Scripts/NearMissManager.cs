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
    public RectTransform multiplierTarget;
    public TextMeshProUGUI multiplierText;

    [Header("Post-Processing Feedback")]
    public Volume globalVolume;
    private LensDistortion distortion;

    [Header("Settings")]
    public float slowMoIntensity = 0.6f;
    public float distortionStrength = -0.5f;

    [Header("Near Miss Balance")]
    public float comboLeeway = 1.5f;      // Temps avant que la décroissance commence
    public int maxScoreMultiplier = 5;    // Plafond du multiplicateur global
    public float decayInterval = 0.5f;    // Décroissance progressive : -1 combo toutes les X secondes
    public Color nearMissColor = Color.yellow;

    [Header("Logic")]
    private int currentCombo = 0;
    private float comboTimer;
    private float decayTimer;
    private bool isDecaying = false;
    private Coroutine activeAnim;

    private void Awake() => instance = this;

    private void Start()
    {
        if (nearMissText != null) nearMissText.gameObject.SetActive(false);

        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out distortion);
            if (distortion == null)
                Debug.LogWarning("[NearMissManager] LensDistortion non trouvée dans le Volume Profile !");
        }
    }

    private void Update()
    {
        if (comboTimer > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0)
            {
                // Timer expiré → début de la décroissance progressive
                isDecaying = true;
                decayTimer = decayInterval;
            }
        }

        // Décroissance progressive : -1 niveau de combo toutes les decayInterval secondes
        if (isDecaying && currentCombo > 0)
        {
            decayTimer -= Time.deltaTime;
            if (decayTimer <= 0)
            {
                currentCombo = Mathf.Max(0, currentCombo - 1);
                if (Inventory.instance != null)
                {
                    Inventory.instance.scoreMultiplier = Mathf.Clamp(1 + currentCombo, 1, maxScoreMultiplier);
                    RefreshMultiplierUI();
                }
                decayTimer = decayInterval;

                if (currentCombo == 0)
                    isDecaying = false;
            }
        }
    }

    // proximity : 0.0 = bord externe de la zone de détection, 1.0 = quasi-contact avec le joueur
    public void TriggerNearMiss(Vector3 missilePosition, float proximity = 0.5f)
    {
        // 1. Calcul Logique
        currentCombo++;
        comboTimer = comboLeeway;
        isDecaying = false; // Un nouveau near miss stoppe la décroissance

        // 2. Score basé sur la proximité : entre x0.5 (bord) et x2.0 (très près)
        float proximityMultiplier = Mathf.Lerp(0.5f, 2.0f, proximity);
        int gain = Mathf.RoundToInt(50 * currentCombo * proximityMultiplier);

        // 3. Mise à jour Score
        if (Inventory.instance != null)
        {
            Inventory.instance.score += gain;
            Inventory.instance.scoreText.text = Inventory.instance.score.ToString();

            // 4. Augmenter le multiplicateur global (PLAFONNÉ à maxScoreMultiplier)
            Inventory.instance.scoreMultiplier = Mathf.Clamp(1 + currentCombo, 1, maxScoreMultiplier);
        }
        RefreshMultiplierUI();

        // 5. Choisir le label selon la proximité
        string label;
        if (proximity < 0.4f)       label = "Close!";
        else if (proximity < 0.75f) label = "TOO CLOSE!";
        else                         label = "INSANE!";

        // 6. Lancer l'animation
        if (activeAnim != null) StopCoroutine(activeAnim);
        activeAnim = StartCoroutine(AnimateNearMiss(label, currentCombo, gain, missilePosition));

        StopCoroutine("DoImpactEffects");
        StartCoroutine(DoImpactEffects());

        // 7. Feedback feeling
        Handheld.Vibrate();
    }

    private void RefreshMultiplierUI()
    {
        if (multiplierText == null) return;
        int mult = Inventory.instance != null ? Inventory.instance.scoreMultiplier : 1;
        multiplierText.gameObject.SetActive(true);
        multiplierText.text = "x" + mult;
    }

    public void ResetCombo()
    {
        currentCombo = 0;
        comboTimer = 0f;
        isDecaying = false;
        if (Inventory.instance != null) Inventory.instance.scoreMultiplier = 1;
        RefreshMultiplierUI();
    }

    IEnumerator AnimateNearMiss(string label, int combo, int gain, Vector3 worldPos)
    {
        // --- INITIALISATION ---
        nearMissText.text = label + " x" + combo + "\n+" + gain;
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
            float scaleValue = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 1.1f) * 1.2f);
            nearMissText.rectTransform.localScale = new Vector3(scaleValue, scaleValue, 1);
            yield return null;
        }

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

            float flyScale = Mathf.Lerp(1f, 0.3f, easedT);
            nearMissText.rectTransform.localScale = new Vector3(flyScale, flyScale, 1);

            yield return null;
        }

        nearMissText.gameObject.SetActive(false);

        // Pulse sur la cible d'arrivée
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
            distortion.active = true;
        }

        yield return new WaitForSecondsRealtime(0.08f);

        // --- 2. RETOUR À LA NORMALE (Smooth transition) ---
        float t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * 5f;
            if (distortion != null)
            {
                distortion.intensity.overrideState = true;
                distortion.intensity.value = Mathf.Lerp(distortionStrength, 0f, t);
            }
            yield return null;
        }

        if (distortion != null)
        {
            distortion.intensity.overrideState = true;
            distortion.intensity.value = 0f;
        }
    }
}
