using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationBehavior : MonoBehaviour
{
    private Animator _animatorController;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        print(other.gameObject.name);
        _animatorController = other.GetComponentInParent<Animator>();
        _animatorController.SetBool("IsTouched", true);
        if (other.gameObject.name.Contains("RShoulderCollider"))
        {
            _animatorController.SetBool("IsTurning", true);
            _animatorController.SetBool("TurnRight", true);
            _animatorController.SetBool("TurnLeft", false);
        }

        if (other.gameObject.name.Contains("LShoulderCollider"))
        {
            _animatorController.SetBool("IsTurning", true);
            _animatorController.SetBool("TurnLeft", true);
            _animatorController.SetBool("TurnRight", false);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        _animatorController.SetBool("IsTouched", false);
    }
}
