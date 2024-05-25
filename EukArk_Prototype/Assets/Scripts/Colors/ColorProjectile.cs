using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorProjectile : MonoBehaviour
{
    [SerializeField] float projectileSpeed = 5f;
    [SerializeField] AnimationCurve speedCurve;
    [SerializeField] AnimationCurve trajectoryCurve;
    [SerializeField] float speedMultifier = 1f;
    [SerializeField] float amplitudeMultifier = 10f;

    Rigidbody2D rb2d = null;
    public Vector2 fireDirection
    {
        get; set;
    } = Vector2.zero;
    public GameObject aimedGameobject;
    CombatComponents playerCombatComp = null;
    Color attackColor;
    Vector2 trajectoryDirection;

    Vector3 firePostion;
    float distanceToSpeedConverge;
    bool bCalcDist;

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        StartCoroutine(DestroyItSelf());
        trajectoryDirection = new Vector2();
        amplitudeMultifier = amplitudeMultifier * (Random.Range(0, 2) * 2 - 1);
        bCalcDist = false;
    }

    void Update()
    {
        if (aimedGameobject == null)
        {
            rb2d.velocity = fireDirection * projectileSpeed;
            //Debug.Log(fireDirection.x + ", " + fireDirection.y);
        }
        else
        {
            if (!bCalcDist)
            {
                Vector3 temp = aimedGameobject.transform.position - firePostion;
                temp.z = 0;
                distanceToSpeedConverge = temp.magnitude;
                Debug.Log(distanceToSpeedConverge);
                bCalcDist = true;
            }
            //Debug.Log("Fire");
            Vector2 direction = aimedGameobject.transform.position - transform.position;
            float distance = Mathf.Clamp(direction.magnitude, 0f, distanceToSpeedConverge);
            if (direction.magnitude > 0.1f)
            {
                direction.Normalize();
                fireDirection = direction;
            }
            float remappedDist = distance / distanceToSpeedConverge;
            float speed = speedCurve.Evaluate(remappedDist) * speedMultifier;
            float trajectory = trajectoryCurve.Evaluate(remappedDist) * amplitudeMultifier;

            trajectoryDirection.Set(0f, trajectory);

            //Debug.Log(remappedDist);
            rb2d.velocity = fireDirection * projectileSpeed * speed + trajectoryDirection;
        }
    }

    public void SetPlayerCombatComp(CombatComponents _playerCombatComp, Color _attackColor, Vector3 _firePostion)
    {
        playerCombatComp = _playerCombatComp;
        GetComponent<SpriteRenderer>().color = _attackColor;
        attackColor = _attackColor;
        firePostion = _firePostion;
    }

    public void SetProjectileVelocity(float _x, float _y)
    {
        fireDirection.Set(_x, _y);
    }

    public void SetProjectileVelocity(Vector2 _vel)
    {
        fireDirection = _vel;
    }

    public void SetAimedGameObject(GameObject _gameObject)
    {
        aimedGameobject = _gameObject;
    }

    private void OnTriggerEnter2D(Collider2D _other)
    {
        if (_other.CompareTag("Monster"))
        {
            ColorComponent enemyColorComp = _other.gameObject.GetComponentInChildren<ColorComponent>();
            //enemyColorComp.TryToFillGauge(attackColor, 1);
            enemyColorComp.TryToFillGauge(attackColor, 1);
        }
        //Debug.Log("Enter");
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D _other)
    {
        //Debug.Log("Coll");
        Destroy(gameObject);
    }

    IEnumerator DestroyItSelf()
    {
        yield return new WaitForSecondsRealtime(5f);
        Destroy(gameObject);
    }
}
