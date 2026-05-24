using System;
using UnityEngine;

public class CustomObject : MonoBehaviour
{
    private Animator myAnimator;
    [SerializeField] private Transform[] customTransforms;

    public Transform[] CustomTransforms => customTransforms;

    private void Awake()
    {
        myAnimator = GetComponent<Animator>();
    }

    public void SetAnimationTrigger(string triggerName)
    {
        myAnimator.SetTrigger(triggerName);
    }
}
