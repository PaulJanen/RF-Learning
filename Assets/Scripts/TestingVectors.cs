using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestingVectors : MonoBehaviour
{
    // Start is called before the first frame update

    public Transform childObj;
    public Transform otherObj;

    public Transform testObj;
    public Transform testOb2;

    public bool testTRS = false;
    public bool testInverse = false;
    public bool testFromToQuaternion = false;
    public bool testInversePoint = false;
    public bool testAngularVelocity = false;
    public bool testQuaternioLook = false;
    public bool testBackflipDetection = false;
    public bool testUnitSphere = false;

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
        if(testQuaternioLook)
            TestQuaternionLook();
        if(testBackflipDetection)
            TestBackflipDetection();
        if (testUnitSphere)
            TestUnitSphere();
    }

    void TestQuaternionLook()
    {
        Vector3 dir = otherObj.position - transform.position;
        Debug.DrawRay(transform.position, dir);
        Quaternion m_LookRotation = Quaternion.LookRotation(dir);
        transform.rotation = m_LookRotation;
    }

    //https://answers.unity.com/questions/982783/inversetransformpoint-via-matrices.html
    //Matrix4x4 version of Transform.InverseTransformPoint
    //Vector3 localPosition = matrixTrans.inverse.MultiplyPoint3x4(worldPositon);
    void TestRrs()
    {
        Vector3 dir = otherObj.position - transform.position;
        Debug.DrawRay(transform.position, dir);
        Quaternion m_LookRotation = Quaternion.LookRotation(dir);
        Matrix4x4 m_TargetDirMatrix = Matrix4x4.TRS(Vector3.zero, m_LookRotation, Vector3.one);

        childObj.localScale = ExtractScale(m_TargetDirMatrix);
        childObj.rotation = ExtractRotation(m_TargetDirMatrix);
        childObj.position = ExtractPosition(m_TargetDirMatrix);

        Debug.Log("forwad: " + transform.forward);
        var bodyForwardRelativeToLookRotationToTarget = m_TargetDirMatrix.inverse.MultiplyVector(transform.forward);
        //Debug.Log(bodyForwardRelativeToLookRotationToTarget);
        testObj.position = bodyForwardRelativeToLookRotationToTarget;
        testOb2.rotation = Quaternion.LookRotation(bodyForwardRelativeToLookRotationToTarget);
    }

    void TestInverseTransform()
    {
        Debug.Log(transform.InverseTransformDirection(testObj.position));
    }

    void TestInversePoint()
    {
        Debug.Log(transform.InverseTransformPoint(testObj.position));
    }

    void TestBackflipDetection()
    {
        Debug.Log(Vector3.Dot(transform.forward, Vector3.up));
    }

    void TestUnitSphere()
    {
        Vector3 dir = (Random.insideUnitSphere * 90f);
        Vector3 spawnPos = transform.position + dir;
        spawnPos.y = transform.position.y;
        Instantiate(otherObj, spawnPos, Quaternion.identity);
    }

    //https://forum.unity.com/threads/i-dont-understand-this-quaternion-fromtorotation-code-behavior.389229/
    //So, yeah, FromToRotation returns a Quaternion that would rotate the first vector so that it matches the second vector.
    void TestFromToQuaternion()
    {
        Quaternion rotation =  Quaternion.FromToRotation(Vector3.up, otherObj.up);
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

    public Quaternion ExtractRotation(Matrix4x4 matrix)
    {
        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;

        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;

        return Quaternion.LookRotation(forward, upwards);
    }

    public Vector3 ExtractPosition(Matrix4x4 matrix)
    {
        Vector3 position;
        position.x = matrix.m03;
        position.y = matrix.m13;
        position.z = matrix.m23;
        return position;
    }

    public Vector3 ExtractScale(Matrix4x4 matrix)
    {
        Vector3 scale;
        scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
        scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
        scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
        return scale;
    }
}
