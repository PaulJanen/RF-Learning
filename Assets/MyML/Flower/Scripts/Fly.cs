
using System;
using UnityEngine;

public class Fly : MonoBehaviour
{
    public Action touchedGround;
    public Rigidbody rb;

    public Vector3 prvVelocity;
    public Vector3 prvAngularVelocity;
    public bool isAlreadyFroozen = false;

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

    public void SaveVelocity()
    {
        prvVelocity = rb.velocity;
        prvAngularVelocity = rb.angularVelocity;
    }

    public void LoadSavedVelocity()
    {
        rb.velocity = prvVelocity;
        rb.angularVelocity = prvAngularVelocity;
    }

}
