using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConferenceAnimator : MonoBehaviour
{
    Animator animator;
    public int state;

    // Start is called before the first frame update
    void Awake()
    {
        animator = GetComponent<Animator>();
        animator.SetInteger("State", state);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
