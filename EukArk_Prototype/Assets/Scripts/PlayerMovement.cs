using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float runSpeed = 5f;
    [SerializeField] float jumpSpeed = 5f;

    Vector2 moveInput;
    Rigidbody2D rb2d;
    Animator animator;
    CapsuleCollider2D bodyCollider2d;
    BoxCollider2D feetCollider2d;

    private Vector2 speedToApply = new Vector2();
    private Vector2 jumpToApply = new Vector2();
    private float currGravityScale;

    private bool IsCrouching = false;
    private bool IsInAir = false;
    private Coroutine jumpAnimCoroutine;

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        bodyCollider2d = GetComponent<CapsuleCollider2D>();
        feetCollider2d = GetComponent<BoxCollider2D>();
        currGravityScale = rb2d.gravityScale;
    }

    void Update()
    {
        UpdateJumpAnimation();
        Run();
        FlipSprite();
        OnCrouch();
    }

    void OnMove(InputValue _value)
    {
        moveInput = _value.Get<Vector2>();
    }

    void Run()
    {
        if (IsCrouching)
        {
            return;
        }
        float playerVelocity = moveInput.x * runSpeed;
        speedToApply.Set(playerVelocity, rb2d.velocity.y);
        rb2d.velocity = speedToApply;
    }

    void OnCrouch()
    {
        if (!feetCollider2d.IsTouchingLayers(LayerMask.GetMask("Ground")))
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            animator.SetBool("IsCrouching", true);
            IsCrouching = true;
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            animator.SetBool("IsCrouching", false);
            IsCrouching = false;
        }
    }

    void OnJump(InputValue _inputValue)
    {
        //if (!bIsAlive){return;}
        
        if (!feetCollider2d.IsTouchingLayers(LayerMask.GetMask("Ground")))
        {
            return;
        }
        if (_inputValue.isPressed)
        {
            animator.SetBool("IsRunning", false);
            animator.SetTrigger("TriggerJump");
            jumpToApply.Set(0f, jumpSpeed);
            rb2d.velocity += jumpToApply;
            if(jumpAnimCoroutine != null)
            {
                StopCoroutine(jumpAnimCoroutine);
            }
            jumpAnimCoroutine = StartCoroutine(StartUpdateJumpAnimation());
        }
    }

    void UpdateJumpAnimation()
    {
        if(!IsInAir)
        {
            return;
        }
        if (feetCollider2d.IsTouchingLayers(LayerMask.GetMask("Ground")))
        {
            animator.ResetTrigger("TriggerJump");
            animator.SetTrigger("TriggerLanding");
            animator.SetBool("IsJumping", false);
            IsInAir = false;
            return;
        }
        
        float yVelocity = rb2d.velocity.y;
        if(yVelocity > Mathf.Epsilon)
        {
            animator.SetBool("IsJumping", true);
        }
        else if(yVelocity < Mathf.Epsilon)
        {
            animator.SetBool("IsJumping", false);
        }
    }

    IEnumerator StartUpdateJumpAnimation()
    {
        yield return new WaitForSeconds(0.03f);
        IsInAir = true;
        //Debug.Log("StartUpdateJumpAnimation");
    }
 
    void OnFire(InputValue _inputValue)
    {
        //if (!bIsAlive){return;}
        if (!feetCollider2d.IsTouchingLayers(LayerMask.GetMask("Ground")))
        {
            return;
        }
        if (_inputValue.isPressed)
        {
            animator.SetTrigger("TriggerAttack");
        }
    }

    void FlipSprite()
    {
        if (Mathf.Abs(moveInput.x) > Mathf.Epsilon)
        {
            transform.localScale = new Vector2(Mathf.Sign(moveInput.x), 1f);
            if (feetCollider2d.IsTouchingLayers(LayerMask.GetMask("Ground")))
            {
                animator.SetBool("IsRunning", true);
            }
            else
            {
                animator.SetBool("IsRunning", false);
            }
        }
        else
        {
            animator.SetBool("IsRunning", false);
        }
    }
    /*
    void ClimbLadder()
    {
        if (!bodyCollider2d.IsTouchingLayers(LayerMask.GetMask("Ladder")))
        {
            rb2d.gravityScale = currGravityScale;
            animator.SetBool("bIsClimbing", false);
            return;
        }
        rb2d.gravityScale = 0f;
        if (Mathf.Abs(moveInput.y) > Mathf.Epsilon)
        {
            animator.SetBool("bIsClimbing", true);
        }
        else
        {
            animator.SetBool("bIsClimbing", false);
        }

        float climbVelocity = moveInput.y * climbingSpeed;
        //Vector2 climbToApply = new Vector2(rb2d.velocity.x, climbVelocity);
        climbToApply.Set(rb2d.velocity.x, climbVelocity);
        rb2d.velocity = climbToApply;
    }
    */
}
