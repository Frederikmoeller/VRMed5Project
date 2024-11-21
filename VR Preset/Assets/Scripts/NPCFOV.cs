using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCFOV : MonoBehaviour
{
    public bool _playerInteracted;
    [SerializeField] private float detectionRadius = 7.5f;
    [SerializeField] private float fieldOfViewAngle = 60f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private Transform head;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private float headTurnSpeed = 5f;
    [SerializeField] private float bodyRotationSpeed = 2f;
    [SerializeField] private float bodyRotationThreshold = 10f;
    [SerializeField] private float soundDetectionRadius = 15f;
    [SerializeField] private float minimumSoundThreshold = 0.05f;
    [SerializeField] private Transform _currentTarget;
    [SerializeField] private Transform _player;
    private bool _isPlayerDetected;
    [SerializeField] private Vector3 _lastKnownTargetPosition;
    private Animator _animator;
    private MicInput _playerMic;
    private float _lookAtWeight;
    [SerializeField] private float distanceToPlayer;
    private bool _isNewTarget;
    [SerializeField] private Vector3 _currentLookAtPosition;
    [SerializeField] private bool inFocusRange;
    [SerializeField] private float minimumDetectionMoveSpeed = 1f;
    [SerializeField] private float playerMoveSpeed;
    [SerializeField] private bool lookingAround;
    private float alignment;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _player = GameObject.FindGameObjectWithTag("Player").transform;
        _playerMic = _player.GetComponentInParent<MicInput>();
        _characterController = _player.GetComponent<CharacterController>();

        if (_playerMic == null)
        {
            Debug.LogWarning("MicInput component not found on player!");
        }

        inFocusRange = false;
    }
    
    void FixedUpdate()
    {
        DetectTarget();
        if (!lookingAround)
        {
            RotateBodyAnim();
        }

        playerMoveSpeed = _characterController.velocity.magnitude;
    }

    void DetectTarget()
    {
        // Reset detection if the current target is no longer valid
        if (!IsValidTarget(_currentTarget))
        {
            _currentTarget = null;
            _isPlayerDetected = false;
        }

        // Set up variables to track best potential target
        Transform bestTarget = null;
        float closestDistance = soundDetectionRadius;

        // Get all objects within the detection radius
        Collider[] hits = Physics.OverlapSphere(head.position, soundDetectionRadius, playerLayer);

        foreach (var hit in hits)
        {
            // Prioritize player if they've interacted with the NPC
            if (hit.CompareTag("Player"))
            {
                //playerMoveSpeed = _player.GetComponent<CharacterController>().velocity.magnitude;
                if (_playerInteracted)
                {
                    bestTarget = hit.transform.GetChild(0).GetChild(0);
                    _isPlayerDetected = true;
                }

                break; // Stop searching if the player is found and interacted
            }
            if (hit.CompareTag("PointOfInterest"))
            {
                // Find the closest "PointOfInterest" if the player is not prioritized
                float distance = Vector3.Distance(head.position, hit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    bestTarget = hit.transform;
                }
            }
        }

        if (distanceToPlayer <= detectionRadius)
        {
            inFocusRange = true;
        }
        
        VisionDetection();

        if (inFocusRange && _isPlayerDetected == false)
        {
            print(GetSpeedThreshold());
            if (playerMoveSpeed >= GetSpeedThreshold() && lookingAround == false)
            {
                lookingAround = true;
                _animator.SetBool("IsTurning", true);
                _animator.SetBool("TurnLeft", true);
            }
        }
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if (lookingAround)
        {                
            if (_playerInteracted)
            {
                lookingAround = false;
                _animator.SetBool("TurnLeft", false);
                _animator.SetBool("IsTurning", false);
            }
            if (stateInfo.normalizedTime >= 1.0f && !_animator.IsInTransition(0)) // Turn animation complete
            {
                if (_animator.GetBool("TurnLeft") && stateInfo.normalizedTime >= 1.0f)
                {
                    _animator.SetBool("TurnLeft", false);
                    _animator.SetBool("TurnRight", true);
                }
                else if (lookingAround && stateInfo.normalizedTime >= 1.0f && playerMoveSpeed >= GetSpeedThreshold())
                {
                    _animator.SetBool("TurnRight", false);
                    _animator.SetBool("TurnLeft", true);
                }
                else if (lookingAround && stateInfo.normalizedTime >= 1.0f && playerMoveSpeed <= GetSpeedThreshold())
                {
                    lookingAround = false;
                    _animator.SetBool("IsTurning", false);
                    _animator.SetBool("TurnRight", false);
                }
            }
        }


        // Sound detection to update player detection
        if (_playerMic.soundVolume > GetSoundThreshold())
        {
            if (distanceToPlayer <= soundDetectionRadius)
            {
                _playerInteracted = true;
            }
        }
        
        // Check if a new target is found and update accordingly
        if (bestTarget != _currentTarget)
        {
            _currentTarget = bestTarget;
            _isPlayerDetected = _currentTarget != null && _currentTarget.CompareTag("Player");
            _isNewTarget = true;  // Mark that we have a new target
            Vector3 directionToTarget = (_currentTarget.position - transform.position).normalized;
            RotateBodyTowardsTarget(directionToTarget);
        }

    }

    void VisionDetection()
    {
        Vector3 directionToTarget = (_player.position - head.position).normalized;
        // Project vectors onto the XZ plane (ignore Y-axis)
        float angleToTarget = Vector3.Angle(head.forward, directionToTarget);

        if (angleToTarget <= fieldOfViewAngle / 2f && distanceToPlayer < detectionRadius)
        {
            _playerInteracted = true;
        }
    }

    // Helper method to determine if the current target is valid
    // Helper method to determine if the current target is valid
    bool IsValidTarget(Transform target)
    {
        if (target == null) return false;
        float distance = Vector3.Distance(head.position, target.position);
        Vector3 directionToTarget = (target.position - head.position).normalized;
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

        // Check if target is within detection radius and field of view
        if (distance <= detectionRadius && angleToTarget <= fieldOfViewAngle / 2f)
        {
            // Raycast to detect obstacles between the NPC and the target
            if (Physics.Raycast(head.position, directionToTarget, out RaycastHit hit, distance, obstacleLayer))
            {
                // Obstacle detected in the path, target is not valid
                _playerInteracted = false;
                return false;
            }

            // No obstacles, target is visible
            return true;
        }

        if (_playerInteracted && inFocusRange && distance > detectionRadius)
        {
            _playerInteracted = false;
            inFocusRange = false;
            return false;
        }
        return false;
    }

    float GetSoundThreshold()
    {
    // Calculate sound threshold based on distance to player
    if (_player == null) return minimumSoundThreshold;
    distanceToPlayer = Vector3.Distance(head.position, _player.position);
    return Mathf.Lerp(0.5f, minimumSoundThreshold, 1 - distanceToPlayer / soundDetectionRadius);
    }
    
    float GetSpeedThreshold()
    {
        // Calculate sound threshold based on distance to player
        if (_player == null) return 1000f;
        distanceToPlayer = Vector3.Distance(head.position, _player.position);
        return Mathf.Lerp(3.5f, minimumDetectionMoveSpeed, 1 - distanceToPlayer / detectionRadius);
    }

    void RotateBodyTowardsTarget(Vector3 directionToTarget)
    {
        // Get the target rotation on the Y-axis
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(directionToTarget.x, 0, directionToTarget.z));
            
            // Smoothly rotate the body around the Y-axis to face the target directly
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * bodyRotationSpeed);
    }

    void RotateBodyAnim()
    {
        if (_currentTarget == null) return;
        
        Vector3 directionToTarget = (_currentTarget.position - transform.position).normalized;
        // Project vectors onto the XZ plane (ignore Y-axis)
        Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        Vector3 flatDirectionToTarget = new Vector3(directionToTarget.x, 0, directionToTarget.z).normalized;
        float angleToTarget = Vector3.SignedAngle(flatForward, flatDirectionToTarget, Vector3.up);

        if (alignment < .75f)
        {
            bodyRotationSpeed = 2f;
            RotateBodyTowardsTarget(directionToTarget);
        }
        else if (alignment > .75)
        {
            bodyRotationSpeed = .5f;
            RotateBodyTowardsTarget(directionToTarget);
        }


        if (angleToTarget > 0)
        {
            _animator.SetBool("TurnAroundLeft", true);
            _animator.SetBool("TurnAroundRight", false);
        }
        else if (angleToTarget < 0)
        {
            _animator.SetBool("TurnAroundLeft", false);
            _animator.SetBool("TurnAroundRight", true);
        }


        _animator.SetFloat("RotationLimit", alignment);
    }


    void OnAnimatorIK(int layerIndex)
    {
        if (_currentTarget != null)
        {
            Vector3 directionToTarget = (_currentTarget.position - transform.position).normalized;
            // Project vectors onto the XZ plane (ignore Y-axis)
            Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 flatDirectionToTarget = new Vector3(directionToTarget.x, 0, directionToTarget.z).normalized;
            alignment = Vector3.Dot(flatForward, flatDirectionToTarget);
            if (alignment >= 0f)
            {
                // Smoothly interpolate from the last target position to the new target position
                Vector3 targetPosition = _currentTarget.position;

                // Smoothly transition to the new target position
                _currentLookAtPosition = Vector3.Lerp(_currentLookAtPosition, targetPosition, Time.deltaTime * headTurnSpeed);
                
                // Set look-at weight and position
                _lookAtWeight = Mathf.Lerp(_lookAtWeight, 1f, Time.deltaTime * headTurnSpeed);
                if (!_animator.GetBool("IsTurning"))
                {
                    _lookAtWeight = Mathf.Lerp(_lookAtWeight, 1f, Time.deltaTime * headTurnSpeed);
                    _animator.SetLookAtWeight(_lookAtWeight);
                }
                else
                {
                    _lookAtWeight = 0;
                    _animator.SetLookAtWeight(_lookAtWeight);
                }
                _animator.SetLookAtPosition(_currentLookAtPosition);
            }
            else
            {
                _lookAtWeight = 0;
                _animator.SetLookAtWeight(_lookAtWeight);
            }
        }
        else
        {
            // Gradually reduce look-at weight when no target exists
            _lookAtWeight = Mathf.Lerp(_lookAtWeight, 0f, Time.deltaTime * headTurnSpeed);
            _animator.SetLookAtWeight(_lookAtWeight);
        }
    }

    void OnDrawGizmos()
    {
        // Visualize detection radius and field of view
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(head.position, detectionRadius);
        Gizmos.DrawWireSphere(head.position, soundDetectionRadius);

        Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfViewAngle / 2f, 0) * head.forward * detectionRadius;
        Vector3 rightBoundary = Quaternion.Euler(0, fieldOfViewAngle / 2f, 0) * head.forward * detectionRadius;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(head.position, head.position + leftBoundary);
        Gizmos.DrawLine(head.position, head.position + rightBoundary);
    }
}
