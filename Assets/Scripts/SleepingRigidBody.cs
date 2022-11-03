using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SleepingRigidBody : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        GetComponent<Rigidbody>().Sleep();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
