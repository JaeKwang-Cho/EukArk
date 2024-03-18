using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Driver : MonoBehaviour
{
    [SerializeField] float steeringSpeed = 100f;
    [SerializeField] float movingSpeed = 30f;
    [SerializeField] float slowSpeed = 15f;
    [SerializeField] float boostSpeed = 40f;

    void Start()
    {

    }

    void Update()
    {
        float steerAmount = - Input.GetAxis("Horizontal") * steeringSpeed * Time.deltaTime;
        float moveAmount = Input.GetAxis("Vertical") * movingSpeed * Time.deltaTime;
        transform.Rotate(0, 0, steerAmount);
        transform.Translate(0, moveAmount, 0);
    }

    void OnCollisionEnter2D(Collision2D _other)
    {
        movingSpeed = slowSpeed;
    }

    void OnTriggerEnter2D(Collider2D _other)
    {
        if (_other.CompareTag("SpeedUp"))
        {
            movingSpeed = boostSpeed;
        }
    }
}
