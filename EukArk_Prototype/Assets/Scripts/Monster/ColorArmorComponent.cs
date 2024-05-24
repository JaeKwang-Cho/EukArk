using UnityEngine;

public class ColorArmorComponent : ColorComponent
{
    MonsterMovement parentMonsterMovement = null;
    Rigidbody2D parentRigidBody2D = null;
    CombatComponents parentCombatComponents = null;

    Animator animator;
    public MonsterState currMonsterState = MonsterState.Idle;
    MonsterState prevMonsterState = MonsterState.Idle;
    SpriteRenderer spriteRenderer;

    bool bArmorWork = true;


    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        currentColorGauge = 1;

        currentColor = new Color(baseColor.r, baseColor.g, baseColor.b, (float)currentColorGauge / totalColorGauge);
        spriteRenderer.color = currentColor;
    }

    public bool ArmorValid()
    {
        return bArmorWork;
    }

    public void DestroyArmor()
    {
        Destroy(gameObject);
    }

    public override void TryToFillGauge(Color _inputColor, int inputGauge)
    {
        if (baseColor == _inputColor)
        {
            currentColorGauge += inputGauge;
            currentColor = new Color(baseColor.r, baseColor.g, baseColor.b, (float)currentColorGauge / totalColorGauge);
            spriteRenderer.color = currentColor;
            if (totalColorGauge <= currentColorGauge)
            {
                bFilledColor = true;
                Debug.Log("Armor Filled");
                parentCombatComponents.isColorFilled = true;
            }
        }
    }

    public void UpdateAnimation()
    {
        /*
        if (parentMonsterMovement)
        {
            prevMonsterState = currMonsterState;
            currMonsterState = parentMonsterMovement.GetMonsterState();
        }

        switch (currMonsterState)
        {
            case MonsterState.Idle:
                {
                    animator.SetBool("IsStun", false);
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
                    animator.SetBool("IsStun", false);
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
                    animator.SetBool("IsStun", false);
                    if (prevMonsterState != currMonsterState)
                    {
                        animator.SetTrigger("TriggerTryToAttack");
                    }
                }
                break;
            case MonsterState.Melee:
                {
                    animator.SetBool("IsStun", false);
                    if (prevMonsterState != currMonsterState)
                    {
                        animator.SetTrigger("TriggerMelee");
                    }
                }
                break;
            case MonsterState.Hit:
                {
                    animator.SetBool("IsStun", false);
                    //animator.ResetTrigger("TriggerMelee");
                    //animator.ResetTrigger("TriggerTryToAttack");
                    if (prevMonsterState != currMonsterState)
                    {
                        animator.SetTrigger("TriggerHit");
                    }
                }
                break;
            case MonsterState.Stun:
                {
                    if (prevMonsterState != currMonsterState)
                    {
                        animator.SetBool("IsStun", true);
                    }
                }
                break;
            case MonsterState.Die:
                {
                    animator.SetBool("IsStun", false);
                    animator.SetTrigger("TriggerDie");
                }
                break;
            default:
                { }
                break;
        }
        */
    }

    public void SetParent(MonsterMovement _parentMonsterMovement)
    {
        parentMonsterMovement = _parentMonsterMovement;
        parentRigidBody2D = parentMonsterMovement.GetRigidbody2D();
        parentCombatComponents = parentMonsterMovement.GetCombatComponents();
    }
}
