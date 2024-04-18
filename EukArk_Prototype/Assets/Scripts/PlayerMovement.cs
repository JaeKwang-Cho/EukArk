using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
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

    // Ground
    [Header("Ground Status")]
    // Crouch
    public bool isCrouching = false;
    // Run
    public bool isMoving = false;

    // Dash
    [Header("Dash Status")]
    public bool isDashing = false;
    public bool isDashCooldown = false;
    public bool isSwiftMelee = false;
    public bool isSwiftMeleeCooldown = false;
    Coroutine dashCoroutine = null;
    Coroutine dashCooldownCoroutine = null;

    // InAir
    [Header("InAir Status")]
    public bool isInAir = false;
    public bool isStartToJump = false;
    public bool isFalling = false;
    public bool isRising = false;
    public bool isLanding = false;

    // Ladder
    [Header("Ladder Status")]
    public bool isOnLadder = false;
    public bool isClimbing = false;
    public bool isLadderHanging = false;

    // Hang
    [Header("Hang Status")]
    public bool isCliffHanging = false;
    public bool isClimbingCliff = false;
    public bool isDroppingCliff = false;
    Coroutine climbingCoroutine = null;

    [Header("Combat Status")]
    // General
    public bool isHit = false;
    public bool isDie = false;
    // Ground Melee
    public bool isGroundMelee = false;
    public string currMeleeAnimName;

    public float comboCooldownTime = 0.5f;
    public static int numOfClicks = 0;
    public float maxComboDelayTime = 0.75f;
    public bool isMeleeCooldown = false;
    public bool isMeleeButtonPressed = false;
    public bool isUpperMeleeCooldown = false;
    Coroutine ComboWaiter = null;
    Coroutine ComboCooldownWaiter = null;

    // InAir Melee (X)

    // Stomp
    public bool isStomping = false;
    public bool bStartStomp = false;
    public bool bMiddleStomp = false;
    public bool bEndStomp = false;
    public float stompSetTime = 0.5f;
    public float stompSpeed = 10f;
    public float stompAfterDelay = 0.5f;
    public bool isStompCooldown = false;
    public float stompCooldown = 5f;

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
        if (isGroundMelee)
        {
            WaitUntilMeleeAnimEnds();
        }
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
        if (Mathf.Abs(moveInput.x) > Mathf.Epsilon && !isGroundMelee)
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
    // ======= Stomp Animation Area ======
    // ===================================

    void OnStomp()
    {
        if (isDie || isHit)
        {
            return;
        }

        if (isStompCooldown)
        {
            return;
        }

        if (!isInAir)
        {
            return;
        }

        //Debug.Log("Start Stomp");
        rb2d.gravityScale = 0f;
        rb2d.velocity = Vector2.zero;

        animator.SetBool("IsStomping", true);

        isStompCooldown = true;
        isStomping = true;
        bStartStomp = true;

        StartCoroutine(StartToNail());
    }

    IEnumerator StartToNail()
    {
        yield return new WaitForSecondsRealtime(stompSetTime);
        bStartStomp = false;
        bMiddleStomp = true;
        //Debug.Log("Start Nail");
        rb2d.gravityScale = currGravityScale;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        LayerMask excludeMonster = LayerMask.GetMask("Monster");

        bodyCollider2d.excludeLayers = excludeMonster;
        feetCollider2d.excludeLayers = excludeMonster;

        Vector2 stompVelocity = new Vector2(mousePos.x - transform.position.x, -stompSpeed);
        rb2d.velocity = stompVelocity;

        StartCoroutine(StompAttack());
    }

    IEnumerator StompAttack()
    {
        yield return new WaitUntil(checkStompToGround);
        bMiddleStomp = false;
        bEndStomp = true;
        rb2d.velocity = Vector2.zero;

        //Debug.Log("Start Attack");
        combatComponents.SplashAttack();
        StartCoroutine(EndStomp());
    }

    bool checkStompToGround()
    {
        return !isInAir;
    }

    IEnumerator EndStomp()
    {
        yield return new WaitForSecondsRealtime(stompAfterDelay);
        bEndStomp = false;
        animator.SetBool("IsStomping", false);
        isStomping = false;

        LayerMask excludeNothing = 0;

        bodyCollider2d.excludeLayers = excludeNothing;
        feetCollider2d.excludeLayers = excludeNothing;

        StartCoroutine(StompCooldown());
    }

    IEnumerator StompCooldown()
    {
        yield return new WaitForSecondsRealtime(stompCooldown);
        isStompCooldown = false;
    }

    // ===================================
    // ======= Dash Animation Area =======
    // ===================================

    void OnDash(InputValue _value)
    {
        if (isDie || isHit || isStomping)
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
        if (isDie || isHit || isStomping)
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
        if (isDie || isHit || isStomping)
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
        if (isDie || isHit || isStomping)
        {
            return;
        }

        if (isInAir)
        {
            if (Input.GetKey(KeyCode.S))
            {
                OnStomp();
            }
            else
            {
                animator.SetTrigger("TriggerAirMelee");
                combatComponents.Attack();
            }
            return;
        }

        if (isCrouching)
        {
            isGroundMelee = true;
            animator.SetTrigger("TriggerLowMelee");
            combatComponents.Attack();
            currMeleeAnimName = "LowMelee";

            rb2d.velocity = new Vector2(0f, rb2d.velocity.y);
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

        if (!isUpperMeleeCooldown && Input.GetKey(KeyCode.W)) 
        {
            isGroundMelee = true;
            animator.SetTrigger("TriggerUpperMelee");
            combatComponents.Attack();
            currMeleeAnimName = "UpperMelee";

            rb2d.velocity = new Vector2(0f, rb2d.velocity.y);
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
        rb2d.velocity = new Vector2(0f, rb2d.velocity.y);

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
            isGroundMelee = true;
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
        isGroundMelee = false;
    }

    IEnumerator WaitComboCooldown()
    {
        yield return new WaitForSecondsRealtime(comboCooldownTime);

        //Debug.Log("ready to combo");
        numOfClicks = 0;
        isMeleeCooldown = false;
        ComboCooldownWaiter = null;
    }

    void WaitUntilMeleeAnimEnds()
    {
        if(animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.95f && animator.GetCurrentAnimatorStateInfo(0).IsName(currMeleeAnimName))
        {
            isGroundMelee = false;
            currMeleeAnimName = "";
        }
    }

    // ===================================
    // ======= Run Animation Area ========
    // ===================================

    void BasicMove()
    {
        if (isLadderHanging || isCliffHanging || isClimbingCliff || isStomping || isGroundMelee)
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
        if (isStomping)
        {
            return;
        }
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
        if (isInAir || isLadderHanging || isStomping)
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
        if (isDie || isHit || isStomping || isGroundMelee) 
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
        if (isCliffHanging || isStomping)
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
        if (isOnLadder || isStomping)
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
