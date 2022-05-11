using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorHandler : MonoBehaviour
{
    public Animator anim;
    int vertical;
    int horizontal;
    public bool canRotate;

    public void Initialize()
    {
        anim = GetComponent<Animator>();
        vertical = Animator.StringToHash("Vertical");
        horizontal = Animator.StringToHash("Horizontal");

    }

    public void UpdateAnimatorValues(float verticalMovement, float horizontalMovement)
    {
        #region Vertical
        PlayerObject mama = GetComponentInParent<PlayerObject>();
        float v = 0;

        if (verticalMovement > 0 && verticalMovement < 0.55f)
        {
            v = 0.5f;
        }
        else if (verticalMovement > 0.55f)
        {
            v = 1;
        }
        else if (verticalMovement < 0 && verticalMovement > -0.55f)
        {
            v = -0.5f;
        }
        else if (verticalMovement < -0.55f)
        {
            v = -1;
        }
        else
        {
            v = 0;
        }
        #endregion

        #region Horizontal
        float h = 0;

        if (horizontalMovement > 0 && horizontalMovement < 0.55f)
        {
            h = 0.5f;
        }
        else if (horizontalMovement > 0.55f)
        {
            h = 1;

        }
        else if (horizontalMovement < 0 && horizontalMovement > -0.55f)
        {
            h = -0.5f;

        }
        else if (horizontalMovement < -0.55f)
        {
            h = -1;
        } else
        {
            h = 0;
        }
        #endregion

        v = mama.isCrouch ? verticalMovement > 0 ? .2f : .1f : v;
        if (mama.isSprinting && v == .1f)
            PlayTargetAnimation("crouchToRun", false);
        if (v != 0 || h != 0 || mama.isCrouch)
        {
            anim.SetBool("PlayerMoving", true);
            anim.SetFloat(vertical, v, 0.1f, Time.deltaTime);
            anim.SetFloat(horizontal, h, 0.1f, Time.deltaTime);
        }
        if (v == 0 && h == 0)
        {
            anim.SetBool("PlayerMoving", false);
            switch (mama.playerState)
            {
                case PlayerObject.PlayerState.Healthy:
                    PlayTargetAnimation("baseIdle", false);
                    break;
                case PlayerObject.PlayerState.Injured:
                    PlayTargetAnimation("injuredIdle", false);
                    break;
                case PlayerObject.PlayerState.Crippled:
                    PlayTargetAnimation("crippledIdle", false);
                    break;
            }
        }
    }
    public void PlayTargetAnimation(string targetAnim, bool isInteracting)
    {
        anim.applyRootMotion = isInteracting;
        anim.SetBool("isInteracting", isInteracting);
        anim.CrossFade(targetAnim, 0);
    }
    public void CanRotate() => canRotate = true;

    public void StopRotation() => canRotate = false;
}