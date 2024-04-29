using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParryingComp : MonoBehaviour
{
    [Header("Effect Attributes")]
    [SerializeField] ParticleSystem parryingEffect;
    Color parryingColor = Color.black;

    public List<CombatComponents> combatCompListInAttackRange = new List<CombatComponents>();
    CircleCollider2D parryingCircle;

    void Start()
    {
        parryingCircle = GetComponent<CircleCollider2D>();
    }

    public void SetParryingColor(Color _parryingColor)
    {
        parryingColor = _parryingColor;
    }

    public void DoParrying()
    {
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(LayerMask.GetMask("Monster"));
        contactFilter.IsFilteringTrigger(parryingCircle);

        List<Collider2D> allCollisionMonsters = new List<Collider2D>();
        parryingCircle.OverlapCollider(contactFilter, allCollisionMonsters);

        HashSet<MonsterMovement> checkDups = new HashSet<MonsterMovement>();

        foreach(Collider2D monCol in allCollisionMonsters)
        {//IsParryingAble()
            MonsterMovement moveComp = monCol.gameObject.GetComponent<MonsterMovement>();
            if (moveComp != null && checkDups.Add(moveComp) && moveComp.IsParryingAble())
            {
                moveComp.SetStunStateByParrying();
                Debug.Log("Stun by Parrying");
                PlayParryingEffect(monCol.gameObject.transform.position);
            }
        }
    }

    void PlayParryingEffect(Vector3 _position)
    {
        if (parryingEffect != null)
        {
            ParticleSystem particle = Instantiate(parryingEffect, _position, Quaternion.identity);
            Destroy(particle.gameObject, particle.main.duration + particle.main.startLifetime.constantMax);
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
