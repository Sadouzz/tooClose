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
        Inventory.instance.AddStars(1);
        picked.Play();
        Destroy(gameObject);
    }
}
