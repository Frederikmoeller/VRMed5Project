using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationBehavior : MonoBehaviour
{
    private Animator _animatorController;
    private NPCFOV _NPCFOV;

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
        _NPCFOV = other.GetComponentInParent<NPCFOV>();
        _NPCFOV._playerInteracted = true;
        _animatorController = other.GetComponentInParent<Animator>();
        _animatorController.SetBool("IsTouched", true);
    }

    private void OnTriggerExit(Collider other)
    {
        _animatorController.SetBool("IsTouched", false);
    }
}
