using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HurricaneVR.Framework.ControllerInput;

public class CauldronController : MonoBehaviour
{   
    public float forceMagnitude = 10f;
    public Transform paddlePivot;
    public Rigidbody rb;
    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        if(HVRInputManager.Instance.LeftController.PrimaryButtonState.Active)
        {
            Vector3 force = paddlePivot.forward * forceMagnitude;
            rb.AddForce(force);
        }
    }
}
