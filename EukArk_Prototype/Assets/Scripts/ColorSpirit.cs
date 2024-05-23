using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;

public class ColorSpirit : MonoBehaviour
{
    [SerializeField] GameObject playerObjectToFollow;
    [SerializeField] GameObject crossHairToFollow;
    [SerializeField] AnimationCurve followCurve;
    [SerializeField] AnimationCurve waveCurve;
    [SerializeField] float speedMultifier = 3f;
    [SerializeField] float waveAmplitude = 1f;
    [SerializeField] float waveLength = 4.5f;
    [SerializeField] Color spiritColor;
    [SerializeField] float distanceToFollowMaxSpeed = 5f;
    [SerializeField] float distanceSelectedSpirit = 1f;
    [SerializeField] float selectToFollowMaxSpeed = 10f;
 
    public Vector2 followOffset { get; set; } = Vector2.zero;
    Rigidbody2D rb2d;
    float waveTime = 0f;
    bool bSelected = false;

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (bSelected)
        {
            FollowCrossHair();
            return;
        }
        FollowPlayer();
        ApplyWaveSpeed();
    }

    void FollowPlayer()
    {
        Vector3 direction = playerObjectToFollow.transform.position - transform.position;
        direction.z = 0f;
        float distance = Mathf.Clamp(direction.magnitude, 0f, distanceToFollowMaxSpeed);
        if(Mathf.Abs(direction.x) < 0.5f && Mathf.Abs(direction.y) < 0.5f)
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
        rb2d.velocity = new Vector3(rb2d.velocity.x, rb2d.velocity.y + waveDirection, 0f);
    }

    void FollowCrossHair()
    {
        Vector3 crossHairDirection = crossHairToFollow.transform.position - playerObjectToFollow.transform.position;
        //Debug.Log("crossHairToFollow position : " + crossHairToFollow.transform.position);
        crossHairDirection.z = 0f;
        float crossHairDist = Vector2.Distance(crossHairToFollow.transform.position, transform.position);
        float spiritDistanceToPlayer = Mathf.Clamp(crossHairDist, 0.1f, distanceSelectedSpirit);

        if(crossHairDist < 0.1f)
        {
            return;
        }

        crossHairDirection.Normalize();

        float xVal = crossHairDirection.x * spiritDistanceToPlayer;
        float yVal = crossHairDirection.y * spiritDistanceToPlayer;

        Vector3 localSpiritOffset = new Vector3(xVal, yVal, 0);
        //Debug.Log("localSpiritOffset : " + localSpiritOffset);
        Vector3 destinationPosition = localSpiritOffset + playerObjectToFollow.transform.position;
        //Debug.Log("destinationPosition : " + destinationPosition);
        destinationPosition.z = 0f;

        transform.position = Vector3.Lerp(destinationPosition, transform.position, 0.1f);
    }

    public bool SelectToFire()
    {
        bSelected = !bSelected;
        return bSelected;
    }

    public void UnseletToFire()
    {
        bSelected = false;
    }
}
