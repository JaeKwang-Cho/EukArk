using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    [Header("Numeric Attributes")]
    [Header("Numeric Attributes - Speed")]
    [SerializeField] float runSpeed = 5f;
    [SerializeField] float crouchSpeed = 1f;
    [SerializeField] float jumpSpeed = 5f;
    [SerializeField] float climbingSpeed = 2f;

    [Header("Numeric Attributes - Combat")]
    [SerializeField] float dashDodgeTime = 0.2f;
    [SerializeField] float dashDodgeLength = 2f;

    [Header("Numeric Attributes - Cooldown")]
    [SerializeField] float dashCooldown = 2f;
    [SerializeField] float swiftMeleeCooldown = 5f;

    Vector2 moveInput;
    Rigidbody2D rb2d;
    Animator animator;
    CapsuleCollider2D bodyCollider2d;
    BoxCollider2D feetCollider2d;
    CombatComponents combatComponents;
    CliffHanger cliffHanger;

    private Vector2 speedToApply = new Vector2();
    private Vector2 jumpToApply = new Vector2();
    private Vector2 climbToApply = new Vector2();
    public float currGravityScale;
    private float dashSpeed = 5f;
    public float spriteDirection = 1f;

    [Header("Movement Status")]
    public bool isCrouching = false;
    public bool isStartToJump = false;
    public bool isInAir = false;
    public bool isOnLadder = false;
    public bool isMoving = false;
    public bool isClimbing = false;
    public bool isLadderHanging = false;
    public bool isFalling = false;
    public bool isRising = false;
    public bool isLanding = false;

    [Header("Dash Status")]
    public bool isDashing = false;
    public bool isDashCooldown = false;
    public bool isSwiftMelee = false;
    public bool isSwiftMeleeCooldown = false;
    Coroutine dashCoroutine = null;
    Coroutine dashCooldownCoroutine = null;

    [Header("Hang Status")]
    public bool isCliffHanging = false;
    public bool isClimbingCliff = false;
    public bool isDroppingCliff = false;
    Coroutine climbingCoroutine = null;

    [Header("Combat Status")]
    public bool isHit = false;
    public bool isDie = false;
    public float comboCooldownTime = 1f;
    public static int numOfClicks = 0;
    public float maxComboDelayTime = 0.75f;
    public bool isMeleeCooldown = false;
    public bool isMeleeButtonPressed = false;
    Coroutine ComboWaiter = null;
    Coroutine ComboCooldownWaiter = null;

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        bodyCollider2d = GetComponent<CapsuleCollider2D>();
        feetCollider2d = GetComponent<BoxCollider2D>();

        animator = GetComponentInChildren<Animator>();
        combatComponents = GetComponentInChildren<CombatComponents>();
        cliffHanger = GetComponentInChildren<CliffHanger>();
        cliffHanger.SetFeetCollider2d(feetCollider2d);

        currGravityScale = rb2d.gravityScale;
    }

    void Update()
    {
        if (isDie)
        {
            return;
        }

        if (isHit)
        {
            return;
        }

        if (isMeleeCooldown)
        {
            if (ComboCooldownWaiter == null)
            {
                ComboCooldownWaiter = StartCoroutine(WaitComboCooldown());
                //Debug.Log("cooldown");
            }
        }

        UpdateMovementState();
        UpdateComboBufferInput();
        UpdateClimbAnim();
        UpdateJumpAnim();
        UpdateRunAnim();
        UpdateCrouchAnim();
        FlipSprite();
    }

    void UpdateMovementState()
    {
        // In the Air
        if (!feetCollider2d.IsTouchingLayers(LayerMask.GetMask("Ground")))
        {
            isInAir = true;
            float yVelocity = rb2d.velocity.y;
            if (cliffHanger.IsOnTopCorner())
            {
                isCliffHanging = true;
            }
            else
            {
                isCliffHanging = false;
            }

            if (!isLadderHanging)
            {
                if (yVelocity < -Mathf.Epsilon)
                {
                    isFalling = true;
                    isRising = false;
                }
                else
                {
                    isFalling = false;
                    if (yVelocity > Mathf.Epsilon)
                    {
                        isRising = true;
                    }
                    else
                    {
                        isRising = false;
                    }
                }
            }
            else
            {
                isFalling = true;
                isRising = false;
            }
        }
        else
        {
            if (isInAir)
            {
                isLanding = true;
                isDroppingCliff = false;
            }
            isCliffHanging = false;
            isInAir = false;
            isFalling = false;
            isRising = false;
        }

        // On the Ladder
        if (bodyCollider2d.IsTouchingLayers(LayerMask.GetMask("Ladder")))
        {
            isOnLadder = true;
        }
        else
        {
            isOnLadder = false;
        }

        ActionOnTopCorner();
        ClimbLadder();
        Crouch();
        FlipSprite();
        BasicMove();
    }

    void FlipSprite()
    {
        if (Mathf.Abs(moveInput.x) > Mathf.Epsilon)
        {
            transform.localScale = new Vector2(Mathf.Sign(moveInput.x), 1f);
            isMoving = true;
            spriteDirection = transform.localScale.x;
        }
        else
        {
            isMoving = false;
        }
    }

    void OnMove(InputValue _value)
    {
        moveInput = _value.Get<Vector2>();
        //Debug.Log(moveInput);
    }

    // ===================================
    // ======= Dash Animation Area ========
    // ===================================

    void OnDash(InputValue _value)
    {
        if (isDie || isHit)
        {
            return;
        }

        if (isDashCooldown)
        {
            return;
        }

        isDashCooldown = true;
        isDashing = true;

        dashSpeed = dashDodgeLength / dashDodgeTime;
        SetPassThroughEnemy();
        dashCoroutine = StartCoroutine(DashEnd());
        dashCooldownCoroutine = StartCoroutine(DashCooldown());
    }

    void SetPassThroughEnemy()
    {
        LayerMask excludeMonster = LayerMask.GetMask("Monster");

        bodyCollider2d.excludeLayers = excludeMonster;
        feetCollider2d.excludeLayers = excludeMonster;

        combatComponents.isImmune = true;
    }

    IEnumerator DashCooldown()
    {
        yield return new WaitForSecondsRealtime(dashCooldown);
        isDashCooldown = false;
        dashCooldownCoroutine = null;
    }

    IEnumerator DashEnd()
    {
        yield return new WaitForSecondsRealtime(dashDodgeTime);
        LayerMask excludeNothing = 0;

        bodyCollider2d.excludeLayers = excludeNothing;
        feetCollider2d.excludeLayers = excludeNothing;

        combatComponents.isImmune = false;

        isDashing = false;
        dashCoroutine = null;
    }

    // ===================================
    // ======== Swift Attack Area ========
    // ===================================

    void OnSwiftAttack()
    {
        if (isDie || isHit)
        {
            return;
        }

        if (isSwiftMeleeCooldown)
        {
            return;
        }

        StopCoroutine(dashCoroutine);
        StopCoroutine(dashCooldownCoroutine);

        isSwiftMelee = true;
        isSwiftMeleeCooldown = true;

        combatComponents.Attack();

        animator.SetBool("IsSwiftMelee", false);
        StartCoroutine(SwiftMeleeCooldown());
        StartCoroutine(DashCooldown());
        StartCoroutine(SwiftMeleeEnd());
    }

    IEnumerator SwiftMeleeCooldown()
    {
        yield return new WaitForSecondsRealtime(swiftMeleeCooldown);
        isSwiftMeleeCooldown = false;
    }

    IEnumerator SwiftMeleeEnd()
    {
        yield return new WaitForSecondsRealtime(dashDodgeTime);
        LayerMask excludeNothing = 0;

        bodyCollider2d.excludeLayers = excludeNothing;
        feetCollider2d.excludeLayers = excludeNothing;

        combatComponents.isImmune = false;

        isDashing = false;
        isSwiftMelee = false;
        animator.SetBool("IsSwiftMelee", false);
    }

    // ===================================
    // ======= Attack Animation Area =====
    // ===================================

    void OnFire(InputValue _inputValue)
    {
        if (isDie || isHit)
        {
            return;
        }

        if (isInAir)
        {
            return;
        }
        animator.SetTrigger("TriggerMissile");
    }

    void OnMelee(InputValue _inputValue)
    {
        if (isDie || isHit)
        {
            return;
        }

        if (isInAir)
        {
            animator.SetTrigger("TriggerAirMelee");
            combatComponents.Attack();
            return;
        }

        if (isCrouching)
        {
            animator.SetTrigger("TriggerLowMelee");
            combatComponents.Attack();
            return;
        }

        if (isMeleeCooldown)
        {
            //Debug.Log("Cooldown");
            return;
        }

        if (isDashing)
        {
            OnSwiftAttack();
            return;
        }

        numOfClicks++;
        //Debug.Log("start" + numOfClicks);
        numOfClicks = Mathf.Clamp(numOfClicks, 0, 3);
        if (numOfClicks <= 2)
        {
            ComboReset();
        }
        isMeleeButtonPressed = true;
        //Debug.Log("end " + numOfClicks);
    }

    void UpdateComboBufferInput()
    {
        bool bAttackMotion = false;

        if (numOfClicks == 1)
        {
            animator.SetBool("A_Combo", true);
            //Debug.Log("A_Combo");
            bAttackMotion = true;
        }
        else if (numOfClicks >= 2 && animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.5f && animator.GetCurrentAnimatorStateInfo(0).IsName("A_Combo"))
        {
            animator.SetBool("A_Combo", false);
            animator.SetBool("B_Combo", true);
            //Debug.Log("B_Combo");
            bAttackMotion = true;
        }
        else if (numOfClicks >= 3 && animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.5f && animator.GetCurrentAnimatorStateInfo(0).IsName("B_Combo"))
        {
            animator.SetBool("B_Combo", false);
            animator.SetBool("C_Combo", true);
            //Debug.Log("C_Combo");

            numOfClicks = 0;
            bAttackMotion = true;
        }

        if(bAttackMotion && isMeleeButtonPressed)
        {
            combatComponents.Attack();
            isMeleeButtonPressed = false;
        }
    }

    void ComboReset()
    {
        if(ComboWaiter != null)
        {
            StopCoroutine(ComboWaiter);
            //Debug.Log("Reset waiter");
        }
        ComboWaiter = StartCoroutine(WaitNextMeleeInput());
    }

    IEnumerator WaitNextMeleeInput()
    {
        yield return new WaitForSecondsRealtime(maxComboDelayTime);

        animator.SetBool("A_Combo", false);
        animator.SetBool("B_Combo", false);
        animator.SetBool("C_Combo", false);
        //Debug.Log("late to combo");
        numOfClicks = 0;
        isMeleeCooldown = true;
    }

    IEnumerator WaitComboCooldown()
    {
        yield return new WaitForSecondsRealtime(comboCooldownTime);

        //Debug.Log("ready to combo");
        numOfClicks = 0;
        isMeleeCooldown = false;
        ComboCooldownWaiter = null;
    }

    // ===================================
    // ======= Run Animation Area ========
    // ===================================

    void BasicMove()
    {
        if (isLadderHanging || isCliffHanging || isClimbingCliff)
        {
            return;
        }
        float playerVelocity = moveInput.x * runSpeed;
        if (isCrouching)
        {
            playerVelocity = moveInput.x * crouchSpeed;
        }
        else if (isDashing)
        {
            playerVelocity = moveInput.x * dashSpeed;
        }
        speedToApply.Set(playerVelocity, rb2d.velocity.y);
        rb2d.velocity = speedToApply;
    }

    void UpdateRunAnim()
    {
        if (isInAir || isLadderHanging)
        {
            animator.SetBool("IsRunning", false);
            return;
        }
        if (isDashing)
        {
            animator.SetBool("IsDashing", true);
            if (isSwiftMelee)
            {
                animator.SetBool("IsSwiftMelee", true);
            }
        }
        else
        {
            animator.SetBool("IsDashing", false);
            if (isMoving)
            {
                animator.SetBool("IsRunning", true);
            }
            else
            {
                animator.SetBool("IsRunning", false);
            }
        }
    }

    // ===================================
    // ====== Crouch Animation Area ======
    // ===================================

    void Crouch()
    {
        if (moveInput.y < 0f)
        {
            isCrouching = true;
        }
        else
        {
            isCrouching = false;
        }
    }

    void UpdateCrouchAnim()
    {
        if (isInAir || isLadderHanging)
        {
            return;
        }
        if (isCrouching)
        {
            animator.SetBool("IsCrouching", true);
            if (isMoving)
            {
                animator.SetBool("IsCrouchWalking", true);
            }
            else
            {
                animator.SetBool("IsCrouchWalking", false);
            }
        }
        else if (!isCrouching)
        {
            animator.SetBool("IsCrouchWalking", false);
            animator.SetBool("IsCrouching", false);
        }
    }

    // ===================================
    // ======== Jump Animtion Area =======
    // ===================================


    void OnJump(InputValue _inputValue)
    {
        if (isDie || isHit) 
        {
            return;
        }

        if (isInAir && !isLadderHanging && !isCliffHanging)
        {
            return;
        }
        if (isCrouching)
        {
            return;
        }
        if (_inputValue.isPressed)
        {
            isInAir = true;
            isLadderHanging = false;
            isClimbing = false;

            rb2d.gravityScale = currGravityScale;
            isStartToJump = true;
            jumpToApply.Set(0f, jumpSpeed);
            rb2d.velocity += jumpToApply;
        }
    }

    void UpdateJumpAnim()
    {
        if (isFalling)
        {
            animator.ResetTrigger("TriggerRise");
            animator.SetTrigger("TriggerFalling");
        }
        else if (isLanding)
        {
            animator.ResetTrigger("TriggerFalling");
            animator.SetTrigger("TriggerLanding");
            isLanding = false;
        }
        else if (isLadderHanging)
        {
            animator.ResetTrigger("TriggerJump");
            animator.ResetTrigger("TriggerFalling");
            animator.ResetTrigger("TriggerLanding");
        }
        else if (isStartToJump)
        {
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsClimbing", false);
            animator.SetBool("IsHanging", false);

            animator.SetTrigger("TriggerJump");

            isStartToJump = false;
        }
        else if (isRising && !isStartToJump)
        {
            animator.SetTrigger("TriggerRise");
            isRising = false;
        }
    }

    // ===================================
    // ======== Climb Animtion Area ======
    // ===================================

    void ClimbLadder()
    {
        if (isCliffHanging)
        {
            return;
        }
        if (!isOnLadder)
        {
            rb2d.gravityScale = currGravityScale;
            isLadderHanging = false;
            isClimbing = false;
            return;
        }
        if (!isInAir)
        {
            isLadderHanging = false;
        }

        if (Mathf.Abs(moveInput.y) > Mathf.Epsilon)
        {
            isClimbing = true;
            isLadderHanging = true;
        }
        else
        {
            isClimbing = false;
            if (!isLadderHanging)
            {
                rb2d.gravityScale = currGravityScale;
            }
        }

        if (isLadderHanging)
        {
            rb2d.gravityScale = 0f;
            float climbVelocity = moveInput.y * climbingSpeed;
            climbToApply.Set(0f, climbVelocity);
            rb2d.velocity = climbToApply;
        }
    }

    void UpdateClimbAnim()
    {
        if (!isLadderHanging)
        {
            animator.SetBool("IsClimbing", false);
            animator.SetBool("IsHanging", false);
            return;
        }
        else
        {
            animator.SetBool("IsHanging", true);

            if (isClimbing)
            {
                animator.SetBool("IsClimbing", true);
                float animSpeed = Mathf.Sign(moveInput.y) * 0.5f;
                animator.SetFloat("fReverse", animSpeed);
            }
            else
            {
                animator.SetBool("IsClimbing", false);
            }
        }
    }

    // ===================================
    // ======== Hang Animation Area ======
    // ===================================

    void ActionOnTopCorner()
    {
        if (isOnLadder)
        {
            return;
        }
        if (!isCliffHanging && !isClimbingCliff)
        {
            rb2d.gravityScale = currGravityScale;
            return;
        }

        if (!isClimbingCliff && !isDroppingCliff)
        {
            rb2d.gravityScale = 0f;
            rb2d.velocity = Vector2.zero;
            if (moveInput.y == -1f)
            {
                isDroppingCliff = true;
                isClimbingCliff = false;
                //Debug.Log("Dropping Cliff");
            }
            else if (moveInput.y == 1f)
            {
                isClimbingCliff = true;
                isDroppingCliff = false;
                //Debug.Log("Climbing Cliff");
            }
            else
            {
                //Debug.Log("Hanging Cliff");
            }
        }

        if (isDroppingCliff)
        {
            rb2d.gravityScale = currGravityScale;
            return;
        }
        else if (!isCliffHanging && isClimbingCliff)
        {
            if(climbingCoroutine == null)
            {
                climbingCoroutine = StartCoroutine(EndOfClimbingCliff());

                //Debug.Log("Start Coroutine");
            }
            climbToApply.Set(climbingSpeed * spriteDirection, rb2d.velocity.y);
            rb2d.velocity = climbToApply;
            //Debug.Log("Cliff Stepping");
        }
        else if (isClimbingCliff)
        {
            climbToApply.Set(0f, climbingSpeed);
            rb2d.velocity = climbToApply;
            //Debug.Log("Cliff Upping"); 
        }
    }

    IEnumerator EndOfClimbingCliff()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        isClimbingCliff = false;
        isDroppingCliff = true;
        climbingCoroutine = null;
        rb2d.gravityScale = currGravityScale;
        //Debug.Log("Climbing Cliff Ends");
    }

    // ===========================================
    // ======== Hit / Death Animtion Area ========
    // ===========================================

    public void SetHitState()
    {
        isHit = true;
        animator.SetTrigger("TriggerHit");

        rb2d.gravityScale = currGravityScale;
        rb2d.velocity = Vector2.zero;

        StartCoroutine(HitRecover());
    }

    IEnumerator HitRecover()
    {
        yield return new WaitForSecondsRealtime(0.4f);
        isHit = false;
    }

    public void SetDieState()
    {
        isDie = true;
        animator.SetTrigger("TriggerDie");

        rb2d.gravityScale = currGravityScale;
        rb2d.velocity = Vector2.zero;

        StartCoroutine(DieProgress());
    }

    IEnumerator DieProgress()
    {
        yield return new WaitForSecondsRealtime(2f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
