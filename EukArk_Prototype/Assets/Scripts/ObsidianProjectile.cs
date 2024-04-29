using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using UnityEngine;

public class ObsidianProjectile : MonoBehaviour
{
    float projectileSpeed = 10f;
    Rigidbody2D rb2d = null;
    public Vector2 fireDirection
    {
        get; set;
    } = Vector2.zero;
    public GameObject aimedGameobject;
    CombatComponents playerCombatComp = null;

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        StartCoroutine(DestroyItSelf());
    }

    void Update()
    {
        if(aimedGameobject == null)
        {
            rb2d.velocity = fireDirection * projectileSpeed;
            //Debug.Log(fireDirection.x + ", " + fireDirection.y);
        }
        else
        {
            //Debug.Log("Fire");
            Vector2 direction = aimedGameobject.transform.position - transform.position;
            if(direction.magnitude > 0.1f)
            {
                direction.Normalize();
                fireDirection = direction;
            }
            //Debug.Log(fireDirection.x + ", " + fireDirection.y);
            rb2d.velocity = fireDirection * projectileSpeed;
        }
    }

    public void SetPlayerCombatComp(CombatComponents _playerCombatComp)
    {
        playerCombatComp = _playerCombatComp;
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
            CombatComponents enemyCombatComp = _other.gameObject.GetComponentInChildren<CombatComponents>();
            enemyCombatComp.Hit(2f);
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
