using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestingVectors : MonoBehaviour
{
    // Start is called before the first frame update

    public Transform childObj;
    public Transform otherObj;

    public Transform testObj;

    public bool testTRS = false;
    public bool testInverse = false;
    public bool testFromToQuaternion = false;
    public bool testInversePoint = false;
    public bool testAngularVelocity = false;

    void Start()
    {
        
    }

    private void FixedUpdate()
    {
        if(testTRS)
            TestRrs();
        if (testInverse)
            TestInverseTransform();
        if (testFromToQuaternion)
            TestFromToQuaternion();
        if (testInversePoint)
            TestInversePoint();
        if (testAngularVelocity)
            TestAngularVelocity();

    }

    void TestRrs()
    {
        Vector3 dir = otherObj.position - transform.position;
        Debug.DrawRay(transform.position, dir);
        Quaternion m_LookRotation = Quaternion.LookRotation(dir);
        Matrix4x4 m_TargetDirMatrix = Matrix4x4.TRS(Vector3.zero, m_LookRotation, Vector3.one);
        var bodyForwardRelativeToLookRotationToTarget = m_TargetDirMatrix.inverse.MultiplyVector(childObj.forward);
        Debug.Log(bodyForwardRelativeToLookRotationToTarget);
        testObj.position = bodyForwardRelativeToLookRotationToTarget;
    }

    void TestInverseTransform()
    {
        Debug.Log(transform.InverseTransformDirection(testObj.position));
    }

    void TestInversePoint()
    {
        Debug.Log(transform.InverseTransformPoint(testObj.position));
    }

    void TestFromToQuaternion()
    {
        Quaternion rotation =  Quaternion.FromToRotation(transform.up, otherObj.up);
        childObj.rotation = otherObj.rotation;
        transform.rotation = rotation;
        testObj.rotation = rotation;
        Debug.Log(rotation);
    }

    void TestAngularVelocity()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (Input.GetKey("t"))
        {
            rb.angularVelocity = Vector3.up * 1f;
        }
        Debug.Log(rb.angularVelocity);
    }
}
