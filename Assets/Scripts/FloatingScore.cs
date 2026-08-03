using UnityEngine;
using TMPro;

public class FloatingScore : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float flySpeed = 5f;
    private float fadeSpeed = 0.8f;
    private bool isFlying = false;

    public void Init(string text, Color color)
    {
        textMesh = gameObject.AddComponent<TextMeshPro>();
        textMesh.text = text;
        textMesh.color = color;
        textMesh.fontSize = 5f;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.sortingOrder = 50;

        isFlying = true;
        Destroy(gameObject, 2f);
    }

    void Update()
    {
        if (!isFlying) return;

        if (Inventory.instance != null && Inventory.instance.scoreText != null)
        {
            Canvas canvas = Inventory.instance.scoreText.GetComponentInParent<Canvas>();
            Vector3 targetWorldPos;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Vector3 screenPos = Inventory.instance.scoreText.transform.position;
                targetWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));
            }
            else
            {
                targetWorldPos = Inventory.instance.scoreText.transform.position;
            }
            
            targetWorldPos.z = 0;

            transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, flySpeed * Time.deltaTime);

            flySpeed += 20f * Time.deltaTime;

            Color c = textMesh.color;
            c.a -= fadeSpeed * Time.deltaTime;
            textMesh.color = c;

            if (c.a <= 0f)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            transform.position += Vector3.up * flySpeed * Time.deltaTime;
            flySpeed += 10f * Time.deltaTime;
            Color c = textMesh.color;
            c.a -= fadeSpeed * Time.deltaTime;
            textMesh.color = c;
            if (c.a <= 0f) Destroy(gameObject);
        }
    }

    public static void Create(Vector3 position, string text, Color color)
    {
        GameObject go = new GameObject("FloatingScore");
        go.transform.position = position;
        FloatingScore fs = go.AddComponent<FloatingScore>();
        fs.Init(text, color);
    }
}
