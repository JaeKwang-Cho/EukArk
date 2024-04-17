using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatComponents : MonoBehaviour
{
    [Header("Numeric Attributes")]
    [SerializeField] float healthPoints = 10f;
    [SerializeField] float damagePoints = 4f;
    [SerializeField] float attackableTime = 0.1f;

    [Header("AI Attributes")]
    [SerializeField] bool isAI = true;
    [SerializeField] float attackReactTime = 1f;
    [SerializeField] float attackCooldown = 3f;
    [SerializeField] float attackAfterDelay = 1f;

    public bool isAlive = true;
    public bool isAttacking = false;
    public bool isInAttackRange = false;
    public bool isAttackCooldown = false;
    public bool isAttackAfterDelay = false;
    public bool isAttackSuccess = false;
    public bool isImmune
    {
        get; set;
    } = false;

    BoxCollider2D MeleeAttackBox;
    GameObject parentGameObject;
    public List<CombatComponents> combatCompListInAttackRange;

    void Start()
    {
        MeleeAttackBox = GetComponent<BoxCollider2D>();
        parentGameObject = gameObject.transform.parent.gameObject;
        combatCompListInAttackRange = new List<CombatComponents>();
    }

    public void Hit(float _damage)
    {
        if (isImmune)
        {
            return;
        }
        healthPoints -= _damage;
        Debug.Log("Is Ai : " + isAI + " Hit...");
        if (healthPoints <= 0)
        {
            isAlive = false;
        }

        if (!isAI)
        {
            PlayerMovement playerMovement = parentGameObject.GetComponent<PlayerMovement>();
            if (isAlive)
            {
                playerMovement.SetHitState();
            }
            else
            {
                playerMovement.SetDieState();
            }
        }
        else
        {
            MonsterMovement monsterMovement = parentGameObject.GetComponent<MonsterMovement>();

            StopAllCoroutines();
            isAttacking = false;
            isAttackAfterDelay = false;
            isAttackSuccess = false;

            isAttackCooldown = true;
            StartCoroutine(AIAttackCooldown());

            if (isAlive)
            {
                monsterMovement.SetHitState();
                
            }
            else
            {
                monsterMovement.SetDieState();
            }
        }
    }

    public void DealDamage(CombatComponents _other)
    {
        _other.Hit(damagePoints);
    }

    public void Attack()
    {
        foreach (CombatComponents combatComponents in combatCompListInAttackRange)
        {
            DealDamage(combatComponents);
        }
    }

    private void OnTriggerEnter2D(Collider2D _other)
    {
        if (!isAI)
        {
            // if object is Player, Add to List;
            if (_other.CompareTag("Monster"))
            {
                combatCompListInAttackRange.Add(_other.gameObject.GetComponentInChildren<CombatComponents>());
            }
        }
        else
        {
            // if object is AI, try to attack.
            if (_other.CompareTag("Player") && combatCompListInAttackRange.Count == 0)
            {
                isInAttackRange = true;
                combatCompListInAttackRange.Add(_other.gameObject.GetComponentInChildren<CombatComponents>());
            }
        }
    }

    private void OnTriggerStay2D(Collider2D _other)
    {
        if (isAttacking && _other.CompareTag("Player"))
        {
            isAttackSuccess = true;
        }
    }

    private void OnTriggerExit2D(Collider2D _other)
    {
        if (!isAI)
        {
            // if object is Player, Remove from List;
            if (_other.CompareTag("Monster"))
            {
                combatCompListInAttackRange.Remove(_other.gameObject.GetComponentInChildren<CombatComponents>());
            }
        }
        else
        {
            // if object is AI, try to chase.
            if (_other.CompareTag("Player"))
            {
                isInAttackRange = false;
                combatCompListInAttackRange.Clear();
            }
        }
    }

    public bool TryAttackProgress()
    {
        if (isAttackCooldown)
        {
            return false;
        }
        CombatComponents playerCombatComp = combatCompListInAttackRange[0];
        if (playerCombatComp.isImmune)
        {
            return false;
        }

        return true;
    }

    public void AttackProgress()
    {
        isAttackCooldown = true;
        isAttackAfterDelay = true;
        StartCoroutine(AIAttackReact());
    }

    IEnumerator AIAttackReact()
    {
        yield return new WaitForSecondsRealtime(attackReactTime);
        isAttacking = true;
        Attack();
        StartCoroutine(CheckAttacking());
    }

    IEnumerator CheckAttacking()
    {
        yield return new WaitForSecondsRealtime(attackableTime);
        isAttacking = false;
        StartCoroutine(AIAttckAfterDelay());
    }

    IEnumerator AIAttckAfterDelay()
    {
        yield return new WaitForSecondsRealtime(attackAfterDelay);
        isAttackAfterDelay = false;
        StartCoroutine(AIAttackCooldown());
    }

    IEnumerator AIAttackCooldown()
    {
        yield return new WaitForSecondsRealtime(attackCooldown);
        isAttackCooldown = false;
    }

    public bool IsAlive()
    {
        return isAlive;
    }

    public bool IsAttacking()
    {
        return isAttacking;
    }

    public bool IsInAttackRange()
    {
        return isInAttackRange;
    }

    public bool IsAttackSuccess()
    {
        return isAttackSuccess;
    }

    public bool IsAttackCooldown()
    {
        return isAttackCooldown;
    }

    public bool IsAttackAfterDelay()
    {
        return isAttackAfterDelay;
    }

}
