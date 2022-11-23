using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestingPhisics : MonoBehaviour
{
    public Transform stabilizingPivot;
    public Transform test;
    public Rigidbody body;
    public Transform lookTest;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        test.position = body.worldCenterOfMass;

        /*
        Vector3 dirToTarget = body.worldCenterOfMass - stabilizingPivot.position;
        Quaternion lookRotation = Quaternion.LookRotation(dirToTarget);
        //stabilizingPivot.rotation = lookRotation;
        lookRotation = Quaternion.LookRotation(dirToTarget);
        Matrix4x4 targetDirMatrix = Matrix4x4.TRS(Vector3.zero, lookRotation, Vector3.one);
        var bodyUpRelativeToLookRotationToTarget = targetDirMatrix.inverse.MultiplyVector(stabilizingPivot.up);
        var bodyForwardRelativeToLookRotationToTarget = targetDirMatrix.inverse.MultiplyVector(stabilizingPivot.forward);
        lookTest.rotation = lookRotation;
        Debug.Log("matrix up: " + bodyUpRelativeToLookRotationToTarget);
        Debug.Log("matrix forward: " + bodyForwardRelativeToLookRotationToTarget);
        //Debug.Log("inverse: " + stabilizingPivot.InverseTransformDirection(dirToTarget));
        //Debug.Log("inverse: " + dirToTarget);
        */

        /*
        var bodyUpRelativeToLookRotationToTarget = targetDirMatrix.inverse.MultiplyVector(stabilizingPivot.up);
        currentStateData.Add(bodyUpRelativeToLookRotationToTarget.x);
        currentStateData.Add(bodyUpRelativeToLookRotationToTarget.y);
        currentStateData.Add(bodyUpRelativeToLookRotationToTarget.z);
        */
    }
}
