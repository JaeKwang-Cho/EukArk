using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MonsterAnimation : MonoBehaviour
{
    MonsterMovement parentMonsterMovement = null;
    Rigidbody2D parentRigidBody2D = null;
    CombatComponents parentCombatComponents = null;

    Animator animator;
    public MonsterState currMonsterState = MonsterState.Idle;
    MonsterState prevMonsterState = MonsterState.Idle;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public void UpdateAnimation()
    {
        if (parentMonsterMovement)
        {
            prevMonsterState = currMonsterState;
            currMonsterState = parentMonsterMovement.GetMonsterState();
        }

        switch (currMonsterState)
        {
            case MonsterState.Idle:
                {
                    if (Mathf.Abs(parentRigidBody2D.velocity.x) > Mathf.Epsilon)
                    {
                        animator.SetBool("IsRunning", true);
                    }
                    else
                    {
                        animator.SetBool("IsRunning", false);
                    }
                }
                break;
            case MonsterState.Chase:
                {
                    if (parentCombatComponents.IsAttackAfterDelay())
                    {
                        animator.SetBool("IsRunning", false);
                        return;
                    }
                    animator.SetBool("IsRunning", true);
                }
                break;
            case MonsterState.TryToAttack:
                {
                    if(prevMonsterState != currMonsterState)
                    {
                        animator.SetTrigger("TriggerTryToAttack");
                    }
                }
                break;
            case MonsterState.Melee:
                {
                    if (prevMonsterState != currMonsterState)
                    {
                        animator.SetTrigger("TriggerMelee");
                    }   
                }
                break;
            case MonsterState.Hit:
                {
                    //animator.ResetTrigger("TriggerMelee");
                    //animator.ResetTrigger("TriggerTryToAttack");
                    if (prevMonsterState != currMonsterState)
                    {
                        animator.SetTrigger("TriggerHit");
                    }
                }
                break;
            case MonsterState.Die:
                {
                    animator.SetTrigger("TriggerDie");
                }
                break;
            default:
                { }
                break;
        }
    }

    public void SetParent(MonsterMovement _parentMonsterMovement)
    {
        parentMonsterMovement = _parentMonsterMovement;
        parentRigidBody2D = parentMonsterMovement.GetRigidbody2D();
        parentCombatComponents = parentMonsterMovement.GetCombatComponents();
    }
}
