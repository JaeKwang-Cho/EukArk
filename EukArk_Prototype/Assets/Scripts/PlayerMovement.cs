using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float runSpeed = 5f;
    //[SerializeField] float climbingSpeed = 1f;
    //[SerializeField] float jumpSpeed = 5f;
    //[SerializeField] Vector2 deathKick = new Vector2(10f, 10f);
    //[SerializeField] GameObject bulletObject;
    //[SerializeField] Transform gunTransform;

    Vector2 moveInput;
    Rigidbody2D rb2d;
    Animator animator;
    //CapsuleCollider2D bodyCollider2d;
    //BoxCollider2D feetCollider2d;

    private Vector2 speedToApply = new Vector2();
    //private Vector2 jumpToApply = new Vector2();
    //private Vector2 climbToApply = new Vector2();
    //private float currGravityScale;
    //private bool bIsAlive;

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        //bodyCollider2d = GetComponent<CapsuleCollider2D>();
        //feetCollider2d = GetComponent<BoxCollider2D>();
        //currGravityScale = rb2d.gravityScale;
        //bIsAlive = true;
    }

    void Update()
    {
        //if (!bIsAlive){return;}
        Run();
        FlipSprite();
        //ClimbLadder();
        //Die();
    }

    void OnMove(InputValue _value)
    {
        //if (!bIsAlive){return;}
        moveInput = _value.Get<Vector2>();
    }

    void Run()
    {
        float playerVelocity = moveInput.x * runSpeed;
        speedToApply.Set(playerVelocity, rb2d.velocity.y);
        rb2d.velocity = speedToApply;
    }

    /*
    void OnJump(InputValue _inputValue)
    {
        //if (!bIsAlive){return;}
        if (!feetCollider2d.IsTouchingLayers(LayerMask.GetMask("Ground")))
        {
            return;
        }
        if (_inputValue.isPressed)
        {
            jumpToApply.Set(0f, jumpSpeed);
            rb2d.velocity += jumpToApply;
        }
    }
    */

    /*
    void OnFire(InputValue _inputValue)
    {
        //if (!bIsAlive){return;}
        if (_inputValue.isPressed)
        {
            Instantiate(bulletObject, gunTransform.position, transform.rotation);
        }
    }
    */
    void FlipSprite()
    {
        if (Mathf.Abs(moveInput.x) > Mathf.Epsilon)
        {
            transform.localScale = new Vector2(Mathf.Sign(moveInput.x), 1f);
            animator.SetBool("IsRunning", true);
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

    void Die()
    {
        if (bodyCollider2d.IsTouchingLayers(LayerMask.GetMask("Enemy", "Hazards")))
        {
            bIsAlive = false;
            animator.SetTrigger("Dying");
            rb2d.velocity += deathKick;
            gameSession.ProcessPlayerDeath();
        }
    }
    */
}
