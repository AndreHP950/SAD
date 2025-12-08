using UnityEngine;
using UnityEngine.InputSystem;

public class FeliciaAnimationController : MonoBehaviour
{
    PlayerMovementThirdPerson movement;
    Animator animator;
    PlayerBackpack backpack;

    bool isGrounded = true;
    bool isIdling = true;

    float timerAir = 0f;
    float groundedTimer = 0f;
    float stopTimer = 0f;


    private void Start()
    {
        movement = GetComponent<PlayerMovementThirdPerson>();
        animator = GetComponentInChildren<Animator>();
        backpack = GetComponent<PlayerBackpack>();
    }

    private void Update()
    {
        float speed = movement.UpdateVelocity();

        if (movement.playerInputManager.GetVertical() != 0 || movement.playerInputManager.GetHorizontal() != 0)
        {
            stopTimer = 0f;

            animator.SetFloat("Movement", speed);
        }
        else
        {
            animator.SetFloat("Movement", 0);

            stopTimer += Time.deltaTime;

            if (stopTimer > 3f)
            {
                if (backpack.isRightSide) animator.SetTrigger("IdleRight");
                else animator.SetTrigger("IdleLeft");
                stopTimer = 0f;
            }
        }


        if (!isGrounded)
        {
            if (movement.IsGroundedPrecise())
            {
                animator.SetTrigger("JumpEnded");
                isGrounded = true;
            }
        }
        else
        {
            if (movement.isJumping)
            {
                animator.SetTrigger("JumpStart");
                isGrounded = false;
            }
            else if (isGrounded && !movement.characterController.isGrounded)
            {
                timerAir += Time.deltaTime;

                if (timerAir >= 0.2f)
                {
                    animator.SetTrigger("OnAir");
                    isGrounded = false;
                }
            }
            else timerAir = 0f;
        } 
    }
}
