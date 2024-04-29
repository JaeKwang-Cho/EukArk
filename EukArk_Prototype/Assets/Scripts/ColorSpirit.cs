using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;

public class ColorSpirit : MonoBehaviour
{
    [SerializeField] GameObject playerObjectToFollow;
    [SerializeField] AnimationCurve followCurve;
    [SerializeField] AnimationCurve waveCurve;
    [SerializeField] float speedMultifier = 3f;
    [SerializeField] float waveAmplitude = 1f;
    [SerializeField] float waveLength = 4f;
    [SerializeField] Color spiritColor;
    [SerializeField] float distanceToFollowMaxSpeed = 5f;

    public Vector2 followOffset { get; set; } = Vector2.zero;
    Rigidbody2D rb2d;
    float waveTime = 0f;

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        FollowPlayer();
        ApplyWaveSpeed();
    }

    void FollowPlayer()
    {
        Vector2 direction = playerObjectToFollow.transform.position - transform.position;
        float distance = Mathf.Clamp(direction.magnitude, 0f, distanceToFollowMaxSpeed);
        if(Mathf.Abs(direction.x) < 0.5f)
        {
            rb2d.velocity = Vector2.zero;
            return;
        }
        float remappedDist = distance / distanceToFollowMaxSpeed;
        float followSpeed = followCurve.Evaluate(remappedDist) * speedMultifier;

        direction.Normalize();
        rb2d.velocity = direction * followSpeed;
        //Debug.Log(followSpeed);
        //Debug.Log(remappedDist);
    }

    void ApplyWaveSpeed()
    {
        waveTime += Time.deltaTime;
        if(waveTime >= waveLength)
        {
            waveTime = 0f;
        }
        float waveDirection = waveCurve.Evaluate(waveTime / waveLength) * waveAmplitude;
        rb2d.velocity = new Vector2(rb2d.velocity.x, rb2d.velocity.y + waveDirection);
    }
}
