using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    [Header("Numeric Attributes")]
    [Header("Numeric Attributes - Speed")]
    [SerializeField] float runSpeed = 5f;
    //[SerializeField] float crouchSpeed = 1f;
    [SerializeField] float jumpSpeed = 7f;
    [SerializeField] float maxAirSpeed;
    [SerializeField] float airSpeed = 3f;

    [Header("Numeric Attributes - Combat")]
    [SerializeField] float dashDodgeTime = 0.2f;
    [SerializeField] float dashDodgeLength = 2f;

    [Header("Numeric Attributes - Cooldown")]
    [SerializeField] float dashCooldown = 2f;
    //[SerializeField] float swiftMeleeCooldown = 5f;

    [Header("Numeric Attributes - Melee")]
    [SerializeField] AnimationCurve meleeSpeedScale;
    [SerializeField] float meleeSpeed = 5f;
    private float meleeDirection = 1f;

    Vector2 moveInput;
    Rigidbody2D rb2d;
    Animator animator;
    CapsuleCollider2D bodyCollider2d;
    BoxCollider2D feetCollider2d;
    CombatComponents combatComponents;
    CliffHanger cliffHanger;
    public CrossHairComponents crossHair
    {
        get; set;
    } = null;

    private Vector2 speedToApply = new Vector2();
    private Vector2 jumpToApply = new Vector2();
    private Vector2 climbToApply = new Vector2();
    private float currGravityScale;
    private float dashSpeed = 5f;
    public float spriteDirection = 1f;
    private float hasSpeedBeforeJump = 0f;
    
    // Ground
    [Header("Ground Status")]
    // SpecialKey
    public bool isSpecialKeyDown = false;
    // Run
    public bool isMoving = false;

    // Dash
    [Header("Dash Status")]
    public bool isDashing = false;
    public bool isDashCooldown = false;
    //public bool isSwiftMelee = false;
    //public bool isSwiftMeleeCooldown = false;

    // InAir
    [Header("InAir Status")]
    public bool isInAir = false;
    public bool isStartToJump = false;
    public bool isFalling = false;
    public bool isRising = false;
    public bool isLanding = false;

    public bool isAirMelee = false;
    public bool isAirMeleeCooldown = false;
    private float airMeleeAnimLength = 0f;


    // Hang
    [Header("Hang Status")]
    public bool isCliffProgressing = false;
    public bool isInitialPos = false;
    public bool isCliffHang = false;
    public bool isCliffY = false;
    public bool isCliffX = false;
    public bool isCliffFall = false;
    Coroutine cliffCoroutine = null;
    GameObject handForHang = null;
    Vector3 handInitialOffset = Vector3.zero;
    public float CliffYAnimLength = 0f;
    public float CliffXAnimLength = 0f;
    Vector3 beforeHangPos = Vector3.zero;

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
    public bool isMeleeButtonReleased = false;
    //public bool isUpperMeleeCooldown = false;
    Coroutine ComboWaiter = null;
    Coroutine ComboCooldownWaiter = null;

    [Header("Fire Attributes")]
    public bool isFire = false;
    public float groundObAnimLength = 0f;
    public float airObAnimLength = 0f;

    // InAir Melee (X)

    // Stomp
    /*
    public bool isStomping = false;
    public bool bStartStomp = false;
    public bool bMiddleStomp = false;
    public bool bEndStomp = false;
    public float stompSetTime = 0.5f;
    public float stompSpeed = 10f;
    public float stompAfterDelay = 0.5f;
    public bool isStompCooldown = false;
    public float stompCooldown = 5f;
    */

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        bodyCollider2d = GetComponent<CapsuleCollider2D>();
        feetCollider2d = GetComponent<BoxCollider2D>();

        animator = GetComponentInChildren<Animator>();
        combatComponents = GetComponentInChildren<CombatComponents>();
        cliffHanger = GetComponentInChildren<CliffHanger>();
        cliffHanger.SetFeetCollider2d(feetCollider2d);

        maxAirSpeed = runSpeed;
        currGravityScale = rb2d.gravityScale;
        FindHandObject();
        GetCliffAnimLength();
    }

    void FindHandObject()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject temp = transform.GetChild(i).gameObject;
            if (temp.CompareTag("HandForHang"))
            {
                handForHang = temp;
                break;
            }
        }
    }

    void GetCliffAnimLength()
    {
        AnimationClip[] animationClips = animator.runtimeAnimatorController.animationClips;
        foreach(AnimationClip animationClip in animationClips)
        {
            if(animationClip.name == "CliffUp_Player_Test")
            {
                CliffYAnimLength = animationClip.length;
            }
            else if(animationClip.name == "CliffSide_Player_Test")
            {
                CliffXAnimLength = animationClip.length;
            }
            else if (animationClip.name == "AirMelee_Player_Test")
            {
                airMeleeAnimLength = animationClip.length;
            }
            else if (animationClip.name == "ob_Default")
            {
                groundObAnimLength = animationClip.length;
            }
            else if (animationClip.name == "ob_Jump")
            {
                airObAnimLength = animationClip.length;
            }
        }

        //Debug.Log(airMeleeAnimLength);
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
        UpdateMeleeBufferInput();
        if (isGroundMelee)
        {
            WaitUntilMeleeAnimEnds();
        }
        UpdateJumpAnim();
        UpdateRunAnim();
        FlipSprite();
    }

    void UpdateMovementState()
    {
        // In the Air
        if (!feetCollider2d.IsTouchingLayers(LayerMask.GetMask("Ground")))
        {
            isInAir = true;
            float yVelocity = rb2d.velocity.y;
            if (cliffHanger.IsOnTopCorner() && !isCliffFall && !isCliffY && !isCliffX)
            {
                isCliffProgressing = true;
                isCliffHang = true;
            }
            else if(!cliffHanger.IsOnTopCorner() && isCliffProgressing)
            {
                isCliffHang = false;
            }

            if (!isCliffProgressing)
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
                hasSpeedBeforeJump = 0f;
                isInAir = false;
            }
            if (isCliffProgressing)
            {
                isCliffProgressing = false;
                isCliffX = false;
                isCliffFall = false;
                isInitialPos = false;
                cliffCoroutine = null;
                animator.SetBool("IsCliffFall", false);
            }
            isFalling = false;
            isRising = false;
        }

        ActionOnTopCorner();
        CheckSpecialKey();
        FlipSprite();
        BasicMove();
    }

    void FlipSprite()
    {
        if(isGroundMelee || isCliffProgressing || isFire)
        {
            return;
        }
        /*
        spriteDirection = Mathf.Sign(crossHair.transform.position.x - transform.position.x);
        bool flipThreshold = Mathf.Abs(crossHair.transform.position.x - transform.position.x) > 0.5f;
        if (flipThreshold)
        {
            transform.localScale = new Vector2(spriteDirection, 1f);
        }
        */
        if (Mathf.Abs(moveInput.x) > Mathf.Epsilon)
        {
            spriteDirection = Mathf.Sign(rb2d.velocity.x);
            transform.localScale = new Vector2(spriteDirection, 1f);
            isMoving = true;
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
    // ======= Dash Animation Area =======
    // ===================================

    void OnCyanParrying(InputValue _inputValue)
    {
        Debug.Log("OnCyanParrying");
        combatComponents.Parrying();
        animator.SetTrigger("TriggerParrying");
    }

    void OnYellowParrying(InputValue _inputValue)
    {
        if (isCliffProgressing)
        {
            return;
        }
        Debug.Log("OnYellowParrying");
        combatComponents.Parrying();
        animator.SetTrigger("TriggerParrying");
    }

    void OnMagentaParrying(InputValue _inputValue)
    {
        Debug.Log("OnMagentaParrying");
        combatComponents.Parrying();
        animator.SetTrigger("TriggerParrying");
    }

    // ===================================
    // ======= Stomp Animation Area ======
    // ===================================
    /*
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

        float xGap = Mathf.Abs(mousePos.x - transform.position.x);
        float yGap = Mathf.Abs(mousePos.y - transform.position.y);

        float xDirection = Mathf.Sign(mousePos.x - transform.position.x);
        Vector2 stompVelocity;
        if (xGap > yGap)
        {
            stompVelocity = new Vector2(xDirection * stompSpeed, -stompSpeed);
        }
        else 
        {
            stompVelocity = new Vector2(xGap / yGap * xDirection * stompSpeed , -stompSpeed);
        }
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
    */
    // ===================================
    // ======= Dash Animation Area =======
    // ===================================

    void OnDash(InputValue _value)
    {
        if (isDie || isHit /*|| isStomping*/)
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
        StartCoroutine(DashEnd());
        StartCoroutine(DashCooldown());
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
    }

    IEnumerator DashEnd()
    {
        yield return new WaitForSecondsRealtime(dashDodgeTime);
        LayerMask excludeNothing = 0;

        bodyCollider2d.excludeLayers = excludeNothing;
        feetCollider2d.excludeLayers = excludeNothing;

        combatComponents.isImmune = false;

        isDashing = false;
    }

    // ===================================
    // ======== Swift Attack Area ========
    // ===================================

    /*
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

        combatComponents.MeleeAttack();

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
    */

    // ===================================
    // ======= Attack Animation Area =====
    // ===================================

    void OnFire(InputValue _inputValue)
    {
        if (isDie || isHit || isFire /*|| isStomping*/ )
        {
            return;
        }

        if (isSpecialKeyDown)
        {
            if (isInAir)
            {
                animator.SetBool("isObAir", true);
                StartCoroutine(WaitObAirAnimEnds());
            }
            else
            {
                animator.SetBool("isObGround", true);
                StartCoroutine(WaitObGroundAnimEnds());
            }
            combatComponents.MissileAttack();

            isFire = true;
        }
        else
        {
            animator.SetTrigger("TriggerMissile");
        }

        spriteDirection = Mathf.Sign(crossHair.transform.position.x - transform.position.x);
        transform.localScale = new Vector2(spriteDirection, 1f);
    }

    IEnumerator WaitObGroundAnimEnds()
    {
        yield return new WaitForSecondsRealtime(groundObAnimLength);
        isFire = false;
        animator.SetBool("isObGround", false);
    }

    IEnumerator WaitObAirAnimEnds()
    {
        yield return new WaitForSecondsRealtime(airObAnimLength);
        isFire = false;
        animator.SetBool("isObAir", false);
    }



    void OnMelee(InputValue _inputValue)
    {
        if (isDie || isHit/* || isStomping*/)
        {
            return;
        }

        if (isMeleeCooldown)
        {
            //Debug.Log("Cooldown");
            return;
        }

        if (isDashing)
        {
            //OnSwiftAttack();
            return;
        }

        if (isSpecialKeyDown)
        {
            // 여기에 일반 원거리 공격
            return;
        }

        combatComponents.ResetMeleeAttack();

        spriteDirection = Mathf.Sign(crossHair.transform.position.x - transform.position.x);
        transform.localScale = new Vector2(spriteDirection, 1f);

        float xGapValue = crossHair.transform.position.x - transform.position.x;
        float yGapValue = crossHair.transform.position.y - transform.position.y;

        SetDirectionOfSlash(xGapValue, yGapValue);

        if (isInAir)
        {
            //if (Input.GetKey(KeyCode.S))
            {
                //OnStomp();
            }
            //else
            if(isAirMeleeCooldown){
                return;
            }
            isAirMeleeCooldown = true;
            isAirMelee = true;
            StartCoroutine(WaitTilAirMeleeAnim());
            return;
        }

        if (isMoving)
        {
            isMoving = false;
        }
        
        // Ground Melee
        numOfClicks++;
        //Debug.Log("start" + numOfClicks);
        numOfClicks = Mathf.Clamp(numOfClicks, 0, 3);
        if (numOfClicks <= 2)
        {
            ComboReset();
        }
        if (numOfClicks == 0)
        {
            return;
        }
        isMeleeButtonReleased = true;

        float distanceToCursor = Vector2.Distance(crossHair.transform.position, transform.position);

        meleeDirection = meleeSpeed * xGapValue / distanceToCursor;

        if (combatComponents.slashSpriteForPlayer != null)
        {
            combatComponents.slashSpriteForPlayer.color = Color.white;
        }
        //Debug.Log("end " + numOfClicks);
    }

    void SetDirectionOfSlash(float _xGapValue, float _yGapValue)
    {
        Vector2 cursorDirection = new Vector2(_xGapValue, _yGapValue);
        cursorDirection.Normalize();
        combatComponents.SetSlashTransform(cursorDirection.x / 2f, cursorDirection.y / 2f);
    }

    IEnumerator WaitTilAirMeleeAnim()
    {
        yield return new WaitForSecondsRealtime(airMeleeAnimLength);
        isAirMeleeCooldown = false;
        isAirMelee = false;
        animator.SetBool("IsAirMelee", false);
        if (combatComponents.slashSpriteForPlayer != null)
        {
            combatComponents.slashSpriteForPlayer.color = Color.clear;
        }
    }

    void UpdateMeleeBufferInput()
    {
        bool bAttackMotion = false;
        if (isAirMelee)
        {
            animator.SetBool("IsAirMelee", true);
            combatComponents.MeleeAttack();

            if (combatComponents.slashSpriteForPlayer != null)
            {
                combatComponents.slashSpriteForPlayer.color = Color.white;
            }

            return;
        }

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

        if(bAttackMotion && isMeleeButtonReleased)
        {
            combatComponents.MeleeAttack();
        }

        if(animator.GetCurrentAnimatorStateInfo(0).IsName("A_Combo") 
            || animator.GetCurrentAnimatorStateInfo(0).IsName("B_Combo")
            || animator.GetCurrentAnimatorStateInfo(0).IsName("C_Combo"))
        {
            MeleeGoForward();
            if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.9f && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            {
                isMeleeButtonReleased = false;

                if (combatComponents.slashSpriteForPlayer != null)
                {
                    combatComponents.slashSpriteForPlayer.color = Color.clear;
                }
            }
            
        }
    }

    void MeleeGoForward()
    {
        float normalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        float curSpeedScale = meleeSpeedScale.Evaluate(normalizedTime);

        float forwardSpeed = meleeDirection * curSpeedScale;
        speedToApply.Set(forwardSpeed, rb2d.velocity.y);
        rb2d.velocity = speedToApply;
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
        if (isCliffProgressing/* || isStomping */|| isGroundMelee )
        {
            return;
        }
        float playerVelocity = moveInput.x * runSpeed;
        //if (isSpecialKeyDown)
        {
            //playerVelocity = moveInput.x * crouchSpeed;
        }
        if (isDashing)
        {
            playerVelocity = moveInput.x * dashSpeed;
        }
        else if (isInAir)
        {
            if (Mathf.Abs(hasSpeedBeforeJump) > Mathf.Epsilon)
            {
                hasSpeedBeforeJump += airSpeed * moveInput.x * Time.deltaTime;
                playerVelocity = hasSpeedBeforeJump;
                playerVelocity = Mathf.Clamp(playerVelocity, -maxAirSpeed, maxAirSpeed);
            }
            else
            {
                hasSpeedBeforeJump += airSpeed * moveInput.x * Time.deltaTime;
                playerVelocity = hasSpeedBeforeJump;
                playerVelocity = Mathf.Clamp(playerVelocity, -airSpeed, airSpeed);
            }
            //Debug.Log(playerVelocity);
        }
        speedToApply.Set(playerVelocity, rb2d.velocity.y);
        rb2d.velocity = speedToApply;
    }

    void UpdateRunAnim()
    {
        if (isInAir)
        {
            animator.SetBool("IsRunning", false);
            return;
        }
        animator.SetBool("IsInAir", false);
        if (isDashing)
        {
            animator.SetBool("IsDashing", true);
            /*
            if (isSwiftMelee)
            {
                animator.SetBool("IsSwiftMelee", true);
            }
            */
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

    void CheckSpecialKey()
    {
        //if (isStomping)
        {
            //return;
        }
        if (moveInput.y < 0f)
        {
            isSpecialKeyDown = true;
        }
        else
        {
            isSpecialKeyDown = false;
        }
    }

    // ===================================
    // ======== Jump Animtion Area =======
    // ===================================


    void OnJump(InputValue _inputValue)
    {
        if (isDie || isHit /*|| isStomping*/ || isGroundMelee) 
        {
            return;
        }

        if (isInAir && !isCliffProgressing)
        {
            return;
        }
        if (isSpecialKeyDown)
        {
            return;
        }
        if (_inputValue.isPressed)
        {
            hasSpeedBeforeJump = airSpeed * moveInput.x;

            rb2d.gravityScale = currGravityScale;
            isStartToJump = true;
            jumpToApply.Set(0f, jumpSpeed);
            rb2d.velocity += jumpToApply;
        }
    }

    void UpdateJumpAnim()
    {
        if(isCliffProgressing)
        {
            if (!isCliffFall)
            {
                animator.SetBool("IsFalling", false);
                return;
            }
        }
        if (isInAir)
        {
            animator.SetBool("IsInAir", true);
        }

        if (isFalling)
        {
            animator.SetBool("IsFalling", true);
        }
        else if (isLanding)
        {
            animator.SetBool("IsFalling", false);
            animator.SetBool("IsInAir", false);
            isLanding = false;
        }
        else if (isStartToJump)
        {
            animator.SetBool("IsRunning", false);

            animator.SetTrigger("TriggerJump");

            isStartToJump = false;
        }
        else if (isRising)
        {
            animator.SetBool("IsFalling", false);
            isRising = false;
        }
    }

    // ===================================
    // ======== Hang Animation Area ======
    // ===================================

    void ActionOnTopCorner()
    {
        if (!isCliffProgressing)
        {
            return;
        }
        if (isCliffFall)
        {
            return;
        }
        if (cliffHanger.handAttach == null && !isCliffX)
        {
            return;
        }

        if (!isInitialPos)
        {
            isInitialPos = true;

            Transform attachTransform = cliffHanger.handAttach.transform;
            handInitialOffset = handForHang.transform.InverseTransformPoint(transform.position);
            handInitialOffset = new Vector3(handInitialOffset.x * spriteDirection, handInitialOffset.y, 0f);

            beforeHangPos = transform.position;
            transform.position = attachTransform.position + handInitialOffset;

            bodyCollider2d.enabled = false;
            feetCollider2d.enabled = false;
            //Debug.Log("InitialPos");

            animator.SetBool("IsCliffHang", true);
            //animator.SetTrigger("TriggerCliffHang");
        }

        if (!isCliffY && !isCliffX && !isCliffFall)
        {
            rb2d.gravityScale = 0f;
            rb2d.velocity = Vector2.zero;

            if (moveInput.y == -1f)
            {
                isCliffFall = true;
                isCliffY = false;
                animator.SetBool("IsCliffHang", false);
                animator.SetBool("IsCliffFall", true);
            }
            else if (moveInput.y == 1f)
            {
                isCliffY = true;
                animator.SetBool("IsCliffY", true);
                animator.SetBool("IsCliffHang", false);
                //animator.SetTrigger("TriggerCliffY");
            }
        }

        if (isCliffFall)
        {
            animator.SetBool("IsCliffHang", false);
            animator.SetBool("IsCliffFall", true);
            //animator.SetTrigger("TriggerCliffFall");

            transform.position = new Vector3(beforeHangPos.x, transform.position.y, 0f);

            rb2d.gravityScale = currGravityScale;

            isCliffHang = false;
            isInitialPos = false;
            bodyCollider2d.enabled = true;
            feetCollider2d.enabled = true;

            //Debug.Log("Dropping Cliff");
            return;
        }

        if (isCliffY)
        {
            CliffYDirection();
        }else if (isCliffX)
        {
            CliffXDirection();
        }
    }

    void CliffYDirection()
    {
        float cliffHeight = bodyCollider2d.size.y / 2 - handInitialOffset.y;

        //CliffXAnimLength
        float cliffSpeed = cliffHeight / CliffYAnimLength;
        climbToApply.Set(0f, cliffSpeed);
        rb2d.velocity = climbToApply;
        if (cliffCoroutine == null)
        {
            cliffCoroutine = StartCoroutine(AfterClimbingYDirection());
            //Debug.Log("CliffYDirection");
        }
    }

    IEnumerator AfterClimbingYDirection()
    {
        yield return new WaitUntil(IsEscapeTopCorner);
        //yield return new WaitForSecondsRealtime(CliffYAnimLength);
        cliffCoroutine = null;
        isCliffY = false;
        isCliffX = true;
        // start to x direction
    }

    bool IsEscapeTopCorner()
    {
        return cliffHanger.IsEscapeTopCorner();
    }

    void CliffXDirection()
    {
        float cliffWidth = bodyCollider2d.size.x - handInitialOffset.x * spriteDirection;
        float cliffSpeed = cliffWidth / CliffXAnimLength;
        climbToApply.Set(cliffSpeed * spriteDirection, 0f);
        rb2d.velocity = climbToApply;
        if (cliffCoroutine == null)
        {
            animator.SetBool("IsCliffY", false);
            animator.SetBool("IsCliffX", true);
            cliffCoroutine = StartCoroutine(AfterClimbingXDirection());
        }
    }

    IEnumerator AfterClimbingXDirection()
    {
        yield return new WaitForSecondsRealtime(CliffXAnimLength);
        cliffCoroutine = null;
        rb2d.gravityScale = currGravityScale;
        bodyCollider2d.enabled = true;
        feetCollider2d.enabled = true;

        animator.SetBool("IsCliffX", false);
        animator.SetBool("IsCliffFall", true);
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
