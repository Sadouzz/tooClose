using UnityEngine;
using System.Collections;

public class Explosion : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(DestroyMe());
    }

    IEnumerator DestroyMe()
    {
        yield return new WaitForSecondsRealtime(2);
        Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
