using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum MonsterState
{
    Idle,
    Chase,
    TryToAttack,
    Melee,
    Hit,
    Stun,
    Die,
}

public class MonsterMovement : MonoBehaviour
{
    [Header("Attributes")]
    [SerializeField] bool dumbCondition = false;

    [Header("Numeric Attributes")]
    [Header("Numeric Attributes - Speed")]
    [SerializeField] float idleSpeed = 1f;
    [SerializeField] float chaseSpeed = 1.5f;
    [SerializeField] float jumpSpeed = 5f;

    Rigidbody2D rb2d;

    CapsuleCollider2D bodyCollider2d;
    BoxCollider2D detectorCollider2d;

    CombatComponents combatComponents;

    public MonsterState monsterState;
    GameObject playerObject;
    CombatComponents playerCombatComps;
    DetectPlayer detector;
    MonsterAnimation monsterAnimation;
    ColorArmorComponent colorArmorComponent;
    ColorComponent colorComponent;

    private Vector2 speedToApply = new Vector2();
    private Vector2 jumpToApply = new Vector2();
    private float distanceToPlayer = 0f;
    bool isTryingToAttack = false;

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();

        bodyCollider2d = GetComponent<CapsuleCollider2D>();
        detectorCollider2d = GetComponent<BoxCollider2D>();

        combatComponents = GetComponentInChildren<CombatComponents>();
        detector = GetComponentInChildren<DetectPlayer>();

        monsterAnimation = GetComponentInChildren<MonsterAnimation>();
        monsterAnimation.SetParent(this);

        colorArmorComponent = GetComponentInChildren<ColorArmorComponent>();
        colorArmorComponent.SetParent(this);

        monsterState = MonsterState.Idle;
        playerObject = null;
    }

    void Update()
    {
        if (!dumbCondition)
        {
            StateSelector();
            StateActor();
            FlipSprite();
        }
        monsterAnimation.UpdateAnimation();
        colorArmorComponent.UpdateAnimation();
    }

    // Sense Platforms
    void OnTriggerExit2D(Collider2D _other)
    {
        // Idle Actions
        if (_other.CompareTag("Ground"))
        {
            if (monsterState == MonsterState.Idle)
            {
                idleSpeed = -idleSpeed;
                transform.localScale = new Vector2(Mathf.Sign(rb2d.velocity.x), 1f);
            }
            else if (monsterState == MonsterState.Chase)
            {
                if (bodyCollider2d.IsTouchingLayers(LayerMask.GetMask("Ground")))
                {
                    jumpToApply.Set(0f, jumpSpeed);
                    rb2d.velocity += jumpToApply;
                }
            }
        }
    }

    public bool HasArmor()
    {
        return colorArmorComponent.ArmorValid();
    }

    public void DestroyArmor()
    {
        colorArmorComponent.DestroyArmor();
    }


    void StateSelector()
    {
        if(monsterState == MonsterState.Die || monsterState == MonsterState.Hit || monsterState == MonsterState.Stun)
        {
            return;
        }

        if (playerObject == null)
        {
            if (detector.IsPlayerDetect())
            {
                playerObject = detector.GetDetectedPlayer();
                monsterState = MonsterState.Chase;
                playerCombatComps = playerObject.GetComponentInChildren<CombatComponents>();
            }
            else
            {
                monsterState = MonsterState.Idle;
            }
        }
        else
        {
            if (combatComponents.IsInAttackRange() && !combatComponents.IsAttackCooldown())
            {
                if (combatComponents.AITryAttackProgress())
                {
                    monsterState = MonsterState.TryToAttack;
                }
            }
            else if (combatComponents.IsAttacking())
            {
                monsterState = MonsterState.Melee;
            }
            else if(!combatComponents.IsInAttackRange())
            {
                monsterState = MonsterState.Chase;
            }
        }
    }

    void StateActor()
    {
        switch (monsterState)
        {
            case MonsterState.Die:
                {
                    // Die
                    speedToApply.Set(0, rb2d.velocity.y);
                    rb2d.velocity = speedToApply;
                    isTryingToAttack = false;
                    //Debug.Log("Die");
                }
                break;
            case MonsterState.Idle:
                {
                    speedToApply.Set(idleSpeed, rb2d.velocity.y);
                    rb2d.velocity = speedToApply;
                    isTryingToAttack = false;
                }
                break;
            case MonsterState.Chase:
                {
                    ChaseProgress();
                    // In right after attacking, stop a seconds.
                    if (combatComponents.IsAttackAfterDelay())
                    {
                        return;
                    }
                    if(Mathf.Abs(distanceToPlayer) > 0.5f)
                    {
                        speedToApply.Set(chaseSpeed, rb2d.velocity.y);
                    }
                    else
                    {
                        speedToApply.Set(0f, rb2d.velocity.y);
                    }
                    rb2d.velocity = speedToApply;
                    isTryingToAttack = false;
                }
                break;
            case MonsterState.TryToAttack:
                {
                    speedToApply.Set(0, rb2d.velocity.y);
                    rb2d.velocity = speedToApply;
                    if (!isTryingToAttack)
                    {
                        combatComponents.AIAttackProgress();
                        isTryingToAttack = true;
                    }
                }
                break;
            case MonsterState.Melee:
                {
                    speedToApply.Set(0, rb2d.velocity.y);
                    rb2d.velocity = speedToApply;
                    isTryingToAttack = false;
                }
                break;
            case MonsterState.Hit:
            case MonsterState.Stun:
                {
                    speedToApply.Set(0, rb2d.velocity.y);
                    rb2d.velocity = speedToApply;
                    isTryingToAttack = false;
                }
                break;
            default:
                { }
                break;
        }
    }

    void ChaseProgress()
    {
        if (monsterState == MonsterState.Die || monsterState == MonsterState.Hit || monsterState == MonsterState.Stun)
        {
            return;
        }

        distanceToPlayer = Mathf.Abs(playerObject.transform.position.x - gameObject.transform.position.x);
        float direction = Mathf.Sign(playerObject.transform.position.x - gameObject.transform.position.x);
        if (direction == 1f)
        {
            chaseSpeed = Mathf.Abs(chaseSpeed);
        }
        else
        {
            chaseSpeed = -Mathf.Abs(chaseSpeed);
        }
    }

    public bool IsParryingAble()
    {
        return monsterState == MonsterState.TryToAttack;
    }

    public void SetStunStateByParrying()
    {
        monsterState = MonsterState.Stun;
        combatComponents.AIStunedStopAllCoroutines();
    }

    public MonsterState GetMonsterState()
    {
        return monsterState;
    }

    public Rigidbody2D GetRigidbody2D()
    {
        return rb2d;
    }

    public CombatComponents GetCombatComponents()
    {
        return combatComponents;
    }

    // Flip Direction
    void FlipSprite()
    {
        switch (monsterState)
        {
            case MonsterState.Idle:
                {
                    transform.localScale = new Vector2(-Mathf.Sign(rb2d.velocity.x), 1f);
                }
                break;
            case MonsterState.Chase:
            case MonsterState.TryToAttack:
            case MonsterState.Melee:
                {
                    // Still facing player
                    transform.localScale = new Vector2(-Mathf.Sign(chaseSpeed), 1f);
                }
                break;
            case MonsterState.Hit:
            case MonsterState.Die:
                {
                }
                break;
        }
    }

    // Set Hit/Die State
    public void SetHitState()
    {
        monsterState = MonsterState.Hit;
        StartCoroutine(HitRecover());
    }

    IEnumerator HitRecover()
    {
        yield return new WaitForSecondsRealtime(0.4f);
        monsterState = MonsterState.Chase;
    }

    public void SetDieState()
    {
        monsterState = MonsterState.Die;
        StartCoroutine(DieProgress());
    }

    IEnumerator DieProgress()
    {
        yield return new WaitForSecondsRealtime(1f);
        Destroy(gameObject);
    }
}
