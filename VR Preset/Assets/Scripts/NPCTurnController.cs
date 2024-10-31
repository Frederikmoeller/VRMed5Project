using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class NPCTurnController : MonoBehaviour
{
    private Animator animator;
    private bool isTurningRight = false;
    private bool isTurningLeft = false;
    private NavMeshAgent _navMeshAgent;
    private Rigidbody rb;
    [SerializeField] private Vector3 NPCDestination;
    [SerializeField] private float searchRadius = 5f; // Radius to search for a valid NavMesh position
    private bool _isWaiting = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        _navMeshAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        //SetNewDestination();
    }

    void Update()
    {
        // Temporary (Delete later)
        NPCDestination = _navMeshAgent.destination;
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
        
        // Handle walking and reaching the destination
        /*if (_isWaiting) return;
        if (_navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance && !_navMeshAgent.pathPending)
        {
            if (!_isWaiting || _navMeshAgent.velocity.magnitude < 0.1)
            {
                animator.SetBool("Walking", false);
                StartCoroutine(WaitBeforeNextMove()); // Start wait coroutine before setting new destination
            }
        }
        else
        {
            animator.SetBool("Walking", false);
        }*/
    }

    // Coroutine to wait for a set amount of time before moving to the next destination
    IEnumerator WaitBeforeNextMove()
    {
        _isWaiting = true;
        yield return new WaitForSeconds(Random.Range(1f, 10f)); // Wait for the specified time
        //SetNewDestination();
        _isWaiting = false;
    }

    // Set a new random destination using NavMesh.SamplePosition to ensure it's valid
    void SetNewDestination()
    {
        Vector3 randomPoint = new Vector3(Random.Range(-49f, 49f), 0f, Random.Range(-49f, 49f)); // Random point within the level
        NavMeshHit hit; // Store the result of NavMesh.SamplePosition

        // Try to find a valid point on the NavMesh within the search radius
        if (NavMesh.SamplePosition(randomPoint, out hit, searchRadius, NavMesh.AllAreas))
        {
            _navMeshAgent.destination = hit.position; // Set the agent's destination to the valid NavMesh position
        }
        else
        {
            Debug.LogWarning("No valid NavMesh position found near " + randomPoint);
            SetNewDestination();
        }
    }
}
