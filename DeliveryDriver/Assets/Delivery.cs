using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Delivery : MonoBehaviour
{
    [SerializeField] Color32 yesPackageColor = Color.white;
    [SerializeField] Color32 noPackageColor = Color.white;

    [SerializeField] float destroyDelay = 0.5f;
    bool bHasPackage = false;

    SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnCollisionEnter2D(Collision2D _other)
    {
        Debug.Log("Collided by Something");
    }

    void OnTriggerEnter2D(Collider2D _other)
    {
        if (_other.CompareTag("Package") && !bHasPackage)
        {
            Debug.Log("Packaged Loaded");
            bHasPackage = true;
            Destroy(_other.gameObject, destroyDelay);
            spriteRenderer.color = yesPackageColor;
        }
        else if (_other.CompareTag("Customer") && bHasPackage)
        {
            Debug.Log("Customer Delivered");
            bHasPackage = false;
            Destroy(_other.gameObject, destroyDelay);
            spriteRenderer.color = noPackageColor;
        }        
    }
}

