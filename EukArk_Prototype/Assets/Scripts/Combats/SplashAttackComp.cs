using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplashAttackComp : MonoBehaviour
{
    float damagePoints = 4f;
    public bool isAttacking = false;
    public bool isAI = false;

    public List<CombatComponents> combatCompListInAttackRange = new List<CombatComponents>();
    CircleCollider2D splashAttackCircle;

    void Start()
    {
        splashAttackCircle = GetComponent<CircleCollider2D>();
    }

    public void SetSplashAttributes(bool _isAI, float _damagePoints)
    {
        damagePoints = _damagePoints;
        isAI = _isAI;
    }

    public void AttackSplash()
    {
        ContactFilter2D contactFilter = new ContactFilter2D();
        if (!isAI)
        {
            contactFilter.SetLayerMask(LayerMask.GetMask("Monster"));
        }
        else
        {
            contactFilter.SetLayerMask(LayerMask.GetMask("Player"));
        }
        contactFilter.IsFilteringTrigger(splashAttackCircle);

        List<Collider2D> allCollisionMonsters = new List<Collider2D>();
        splashAttackCircle.OverlapCollider(contactFilter, allCollisionMonsters);

        foreach(Collider2D monCol in allCollisionMonsters)
        {
            CombatComponents combatComp = monCol.gameObject.GetComponentInChildren<CombatComponents>();
            if(combatComp != null)
            {
                combatComp.Hit(damagePoints);
                Debug.Log("Splash Attack");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D _other)
    {
        
        //if (isAI && _other.CompareTag("Player"))
        //{
        //    CombatComponents combatComponentes = _other.gameObject.GetComponentInChildren<CombatComponents>();
        //    combatComponentes.Hit(damagePoints);
        //}
        //else if(!isAI && _other.CompareTag("Monster"))
        //{
        //    CombatComponents combatComponentes = _other.gameObject.GetComponentInChildren<CombatComponents>();
        //    combatComponentes.Hit(damagePoints);
        //}
    }
}
