using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    public Vector3 InitialPos;
    public Vector3 newPos;
    public Rigidbody rb;
    public float force = 10f;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        rb.velocity = transform.forward * force;
        //rb.AddForce(transform.forward*force, ForceMode.Impulse);
    }

    public void ChangePosToInitial()
    {
        GetComponent<ConfigurableJoint>().targetRotation = Quaternion.Euler(InitialPos);
    }

    public void ChangePosToNew()
    {
        GetComponent<ConfigurableJoint>().targetRotation = Quaternion.Euler(newPos);
    }
    
    public void DampenIt()
    {
        ConfigurableJoint joint = GetComponent<ConfigurableJoint>();
        JointDrive drive = joint.slerpDrive;
        drive.positionDamper = 20;
        joint.slerpDrive = drive;
    }
}
