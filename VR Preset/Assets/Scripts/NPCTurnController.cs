using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCTurnController : MonoBehaviour
{
    private Animator animator;
    private bool isTurningRight = false;
    private bool isTurningLeft = false;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Check which turn animation is playing
        isTurningRight = animator.GetCurrentAnimatorStateInfo(0).IsName("Turning Right");
        isTurningLeft = animator.GetCurrentAnimatorStateInfo(0).IsName("Turning Left");

        // Check if the turn animation is done
        if (isTurningRight || isTurningLeft)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.normalizedTime >= 1.0f && !animator.IsInTransition(0)) // Turn animation complete
            {
                animator.SetBool("IsTurning", false);  // Stop turning after the animation finishes
            }
        }
    }
}
