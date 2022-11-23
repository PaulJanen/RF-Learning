using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimplePhysicsControler : MonoBehaviour
{

    public Rigidbody rb;
    public float speed = 10f;
    public float rotSpeed = 10f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);
        

        if (Input.GetKey(KeyCode.W))
            rb.AddForce(rb.transform.forward * speed, ForceMode.VelocityChange);
        if (Input.GetKey(KeyCode.S))
            rb.AddForce(rb.transform.forward * -speed, ForceMode.VelocityChange);
        if (Input.GetKey(KeyCode.A))
            rb.AddTorque(Vector3.up * -rotSpeed, ForceMode.VelocityChange);
        if (Input.GetKey(KeyCode.D))
            rb.AddTorque(Vector3.up * rotSpeed, ForceMode.VelocityChange);
    }

    private void FixedUpdate()
    {
        if(rb.velocity.magnitude > 15f)
            rb.velocity = rb.velocity.normalized * 15f;  
    }
}
