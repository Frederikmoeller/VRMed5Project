using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCFOV : MonoBehaviour
{
    public bool _playerInteracted;
    [SerializeField] private float detectionRadius = 7.5f;
    [SerializeField] private float fieldOfViewAngle = 60f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform head;
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
    [SerializeField] private float currentSoundThreshold;
    [SerializeField] private bool inFocusRange;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _player = GameObject.FindGameObjectWithTag("Player").transform;
        _playerMic = _player.GetComponentInParent<MicInput>();

        if (_playerMic == null)
        {
            Debug.LogWarning("MicInput component not found on player!");
        }

        inFocusRange = false;
    }
    
    void FixedUpdate()
    {
        currentSoundThreshold = GetSoundThreshold();
        DetectTarget();
        RotateBodyTowardsTarget();
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
            if (hit.CompareTag("Player") && _playerInteracted)
            {
                bestTarget = hit.transform.GetChild(0).GetChild(0);
                _isPlayerDetected = true;
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
        }

    }

    // Helper method to determine if the current target is valid
    bool IsValidTarget(Transform target)
    {
    if (target == null) return false;
    float distance = Vector3.Distance(head.position, target.position);
    Vector3 directionToTarget = (target.position - head.position).normalized;
    float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

    // Check if target is within detection radius and field of view
    if (distance <= detectionRadius)
    {
        // Ensure there are no obstacles between NPC and target
        if (Physics.Raycast(head.position, directionToTarget, out RaycastHit hit, detectionRadius))
        {
            return hit.transform == target;
        }
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

    void RotateBodyTowardsTarget()
    {
        if (_currentTarget == null) return;

        // Calculate the direction and angle to the target
        Vector3 directionToTarget = (_currentTarget.position - transform.position).normalized;
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

        // Check if the angle to the target is close to or beyond the edge of the field of view
        if (Mathf.Abs(angleToTarget - fieldOfViewAngle / 2f) >= bodyRotationThreshold || angleToTarget > fieldOfViewAngle / 2f)
        {
            // Get the target rotation on the Y-axis
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(directionToTarget.x, 0, directionToTarget.z));

            // Smoothly rotate the body around the Y-axis to face the target directly
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * bodyRotationSpeed);
        }
    }


    void OnAnimatorIK(int layerIndex)
    {

        if (_currentTarget != null)
        {
            // Smoothly interpolate from the last target position to the new target position
            Vector3 targetPosition = _currentTarget.position;

            // If a new target is detected, immediately update last known position to avoid snapping
            if (_isNewTarget)
            {
                _lastKnownTargetPosition = targetPosition;
                _isNewTarget = false;
            }
                // Smoothly transition to the new target position
                _currentLookAtPosition = Vector3.Lerp(_currentLookAtPosition, _lastKnownTargetPosition, Time.deltaTime * headTurnSpeed);
            

            // Set look-at weight and position
            _lookAtWeight = Mathf.Lerp(_lookAtWeight, 1f, Time.deltaTime * headTurnSpeed);
            _animator.SetLookAtWeight(_lookAtWeight);
            _animator.SetLookAtPosition(_currentLookAtPosition);

            Debug.Log($"Smoothing towards target position at {targetPosition}, Weight: {_lookAtWeight}");
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
