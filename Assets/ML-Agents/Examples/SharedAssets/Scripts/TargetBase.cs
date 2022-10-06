using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetBase : MonoBehaviour
{
    public Rigidbody rb;
    private Vector3 prvVelocity;
    private Vector3 prvAngularVelocity;
    private bool isAlreadyFroozen = false;

    public void FreezeRigidBody(bool freeze)
    {
        if (freeze)
        {
            if (isAlreadyFroozen == false)
            {
                isAlreadyFroozen = true;
                SaveVelocity();
                rb.constraints = RigidbodyConstraints.FreezePosition;
                rb.isKinematic = true;
            }
        }
        else
        {
            if (isAlreadyFroozen == true)
            {
                isAlreadyFroozen = false;
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.None;
                LoadSavedVelocity();
            }
        }
    }

    private void SaveVelocity()
    {
        prvVelocity = rb.velocity;
        prvAngularVelocity = rb.angularVelocity;
    }

    private void LoadSavedVelocity()
    {
        rb.velocity = prvVelocity;
        rb.angularVelocity = prvAngularVelocity;
    }
}
