using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCFOV : MonoBehaviour
{
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField] private float fieldOfViewAngle = 60f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform head;
    [SerializeField] private float headTurnSpeed = 5f;
    [SerializeField] private float bodyRotationSpeed = 2f;
    [SerializeField] private float bodyRotationThreshold = 10f;
    [SerializeField] private float soundDetectionRadius = 15f;
    [SerializeField] private float minimumSoundThreshold = 0.05f;
    [SerializeField] private float distanceToPlayer;
    [SerializeField] private Transform _player;
    [SerializeField] private bool _isPlayerDetected = false;
    private Animator _animator;
    private Vector3 _lastKnownPlayerPosition;
    private Vector3 _directionToPlayer;
    private MicInput _playerMic; // Reference to the MicInput script
    private bool _isTouchedLeft;
    private bool _isTouchedRight;

    private float _lookAtWeight;

    private Quaternion _defaultHeadRotation;

    void Start()
    {
        // Get the Animator component on the NPC
        _animator = GetComponent<Animator>();
        // Store the initial head rotation
        _defaultHeadRotation = head.rotation;
    }
    
    void FixedUpdate()
    {
        _isTouchedRight = _animator.GetCurrentAnimatorStateInfo(0).IsName("Turning Right");
        _isTouchedLeft = _animator.GetCurrentAnimatorStateInfo(0).IsName("Turning Left");
        DetectPlayer();
        if (_isPlayerDetected)
        {
            RotateBodyTowardsPlayer();
        }
        
    }

    void DetectPlayer()
    {
        // Get all colliders within the detection radius (since there's only one player, we expect 0 or 1 result)
        Collider[] hits = Physics.OverlapSphere(head.position, detectionRadius, playerLayer);
        
        // Check if player was detected
        if (hits.Length > 0)
        {
            _playerMic = hits[0].gameObject.GetComponent<MicInput>();
            Transform playerHead = hits[0].transform.GetChild(0).GetChild(0);

            // Check if MicInput exists on the player
            if (_playerMic == null)
            {
                Debug.LogWarning("MicInput component not found on player!");
                return;
            }
            
            if (playerHead == null)
            {
                Debug.LogWarning("Head transform not found!");
                return;
            }
            
            distanceToPlayer = (head.position - _playerMic.transform.position).magnitude;
            float soundThreshold = Mathf.Lerp(soundDetectionRadius / soundDetectionRadius, minimumSoundThreshold, 1 - (distanceToPlayer / soundDetectionRadius));
            print(soundThreshold);

            if (_playerMic.soundVolume > soundThreshold)
            {
                _lastKnownPlayerPosition = _player.position;
                print("Heard!");
            }

            _player = playerHead;
            print(_player);

            // Check if within field of view
            _directionToPlayer = (_player.position - head.position).normalized;
            float angle = Vector3.Angle(transform.forward, _directionToPlayer);
            
            //print("Angle to Player: " + angle);

            if (angle < fieldOfViewAngle / 2f)
            {
                print("Seen?");
                // Raycast to ensure no obstacles are blocking the view

                Vector3 rayOrigin = head.position;
                Debug.DrawRay(rayOrigin, _directionToPlayer * detectionRadius, Color.green);

                Physics.Raycast(rayOrigin, _directionToPlayer, out RaycastHit what, detectionRadius);
                //print(what.collider.gameObject.name);
                
                if (Physics.Raycast(rayOrigin, _directionToPlayer, out RaycastHit rayHit, detectionRadius))
                {
                    print("Raycast Hit: " + rayHit.collider.gameObject.name);
                    if (rayHit.collider.gameObject.layer == LayerMask.NameToLayer("Player"))
                    {
                        _isPlayerDetected = true; // Make the head look at the player
                        print("Seen!");
                        _lastKnownPlayerPosition = _player.position;
                        return; // Exit early since we've detected the player
                    }
                }
                else
                {
                    print("Raycast hit nothing!");
                }
            }
        }
        _isPlayerDetected = false;
    }

    void DistanceToPlayer()
    {
        distanceToPlayer = (_player.position - head.position).magnitude;
    }

    void RotateBodyTowardsPlayer()
    {
        if (_player == null) return;

        float angleToPlayer = Vector3.Angle(transform.forward, _directionToPlayer);

        if (angleToPlayer > fieldOfViewAngle / 2f - bodyRotationThreshold)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * bodyRotationSpeed);
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (_isPlayerDetected && _player != null)
        {
            // Set the weight for IK
            _lookAtWeight = Mathf.Lerp(_lookAtWeight, 1f, Time.deltaTime * headTurnSpeed);
            _animator.SetLookAtWeight(_lookAtWeight);

            // Set the look at position to the player's position
            _animator.SetLookAtPosition(_lastKnownPlayerPosition);
            
            Debug.Log("Looking at: " + _player.name);
        }
        else
        {
            _lookAtWeight = Mathf.Lerp(_lookAtWeight, 0f, Time.deltaTime * headTurnSpeed);
            // Set IK LookAt to the last known position as weight fades out
            _animator.SetLookAtWeight(_lookAtWeight);
            _animator.SetLookAtPosition(_lastKnownPlayerPosition);
        }
    }

    void OnDrawGizmos()
    {
        // Visualize detection radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(head.position, detectionRadius);

        // Visualize field of view
        Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfViewAngle / 2f, 0) * head.forward * detectionRadius;
        Vector3 rightBoundary = Quaternion.Euler(0, fieldOfViewAngle / 2f, 0) * head.forward * detectionRadius;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(head.position, head.position + leftBoundary);
        Gizmos.DrawLine(head.position, head.position + rightBoundary);
    }
}
