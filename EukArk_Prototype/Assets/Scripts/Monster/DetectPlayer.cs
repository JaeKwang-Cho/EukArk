using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectPlayer : MonoBehaviour
{
    CircleCollider2D detectCircle;
    bool IsDetected;
    GameObject detectedPlayer;

    void Start()
    {
        detectCircle = GetComponent<CircleCollider2D>();
        IsDetected = false;
        detectedPlayer = null;
    }

    void OnTriggerEnter2D(Collider2D _other)
    {
        if (!IsDetected && _other.CompareTag("Player"))
        {
            detectedPlayer = _other.gameObject;
            IsDetected = true;
            //Debug.Log("player detect");
        }
    }

    public GameObject GetDetectedPlayer()
    {
        return detectedPlayer;
    }

    public bool IsPlayerDetect()
    {
        return IsDetected;
    }
}
