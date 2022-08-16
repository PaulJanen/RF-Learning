using System.Collections;
using System.Collections.Generic;
using Unity.MLAgentsExamples;
using UnityEngine;
using UnityEngine.Serialization;
public class JointDriveController2 : MonoBehaviour
{
    [Header("Joint Drive Settings")]
    [Space(10)]
    public float maxJointSpring;

    public float jointDampen;
    public float maxJointForceLimit;
    public float massScale = 1;
    float m_FacingDot;

    [HideInInspector] public Dictionary<Transform, BodyPart2> bodyPartsDict = new Dictionary<Transform, BodyPart2>();

    [HideInInspector] public List<BodyPart2> bodyPartsList = new List<BodyPart2>();
    internal const float k_MaxAngularVelocity = 100f;

    /// <summary>
    /// Create BodyPart object and add it to dictionary.
    /// </summary>
    public virtual void SetupBodyPart(Transform t)
    {
        var bp = new BodyPart2
        {
            rb = t.GetComponent<Rigidbody>(),
            joint = t.GetComponent<ConfigurableJoint>(),
            startingPos = t.position,
            startingRot = t.rotation
        };
        bp.rb.maxAngularVelocity = k_MaxAngularVelocity;

        // Add & setup the ground contact script
        bp.groundContact = t.GetComponent<GroundContact2>();
        if (!bp.groundContact)
        {
            bp.groundContact = t.gameObject.AddComponent<GroundContact2>();
            bp.groundContact.agent = gameObject.GetComponent<Agent2>();
        }
        else
        {
            bp.groundContact.agent = gameObject.GetComponent<Agent2>();
        }

        if (bp.joint)
        {
            var jd = new JointDrive
            {
                positionSpring = maxJointSpring,
                positionDamper = jointDampen,
                maximumForce = maxJointForceLimit
            };
            bp.joint.slerpDrive = jd;
            bp.joint.massScale = massScale;
        }

        bp.thisJdController = this;
        bodyPartsDict.Add(t, bp);
        bodyPartsList.Add(bp);
    }

    public void GetCurrentJointForces()
    {
        foreach (var bodyPart in bodyPartsDict.Values)
        {
            if (bodyPart.joint)
            {
                bodyPart.currentJointForce = bodyPart.joint.currentForce;
                bodyPart.currentJointForceSqrMag = bodyPart.joint.currentForce.magnitude;
                bodyPart.currentJointTorque = bodyPart.joint.currentTorque;
                bodyPart.currentJointTorqueSqrMag = bodyPart.joint.currentTorque.magnitude;
                if (Application.isEditor)
                {
                    if (bodyPart.jointForceCurve.length > 1000)
                    {
                        bodyPart.jointForceCurve = new AnimationCurve();
                    }

                    if (bodyPart.jointTorqueCurve.length > 1000)
                    {
                        bodyPart.jointTorqueCurve = new AnimationCurve();
                    }

                    bodyPart.jointForceCurve.AddKey(Time.time, bodyPart.currentJointForceSqrMag);
                    bodyPart.jointTorqueCurve.AddKey(Time.time, bodyPart.currentJointTorqueSqrMag);
                }
            }
        }
    }
}


[System.Serializable]
public class BodyPart2
{
    [Header("Body Part Info")][Space(10)] public ConfigurableJoint joint;
    public Rigidbody rb;
    [HideInInspector] public Vector3 startingPos;
    [HideInInspector] public Quaternion startingRot;

    [Header("Ground & Target Contact")]
    [Space(10)]
    public GroundContact2 groundContact;

    public TargetContact targetContact;

    [FormerlySerializedAs("thisJDController")]
    [HideInInspector] public JointDriveController2 thisJdController;

    [Header("Current Joint Settings")]
    [Space(10)]
    public Vector3 currentEularJointRotation;

    [HideInInspector] public float currentStrength;
    public float currentXNormalizedRot;
    public float currentYNormalizedRot;
    public float currentZNormalizedRot;

    [Header("Other Debug Info")]
    [Space(10)]
    public Vector3 currentJointForce;

    public float currentJointForceSqrMag;
    public Vector3 currentJointTorque;
    public float currentJointTorqueSqrMag;
    public AnimationCurve jointForceCurve = new AnimationCurve();
    public AnimationCurve jointTorqueCurve = new AnimationCurve();

    public Vector3 prvVelocity;
    public Vector3 prvAngularVelocity;
    public bool isAlreadyFroozen = false;

    /// <summary>
    /// Reset body part to initial configuration.
    /// </summary>
    public void Reset(BodyPart2 bp)
    {
        bp.rb.transform.position = bp.startingPos;
        bp.rb.transform.rotation = bp.startingRot;
        bp.rb.velocity = Vector3.zero;
        bp.rb.angularVelocity = Vector3.zero;
        if (bp.groundContact)
        {
            bp.groundContact.touchingGround = false;
        }

        if (bp.targetContact)
        {
            bp.targetContact.touchingTarget = false;
        }
    }

    public void ResetWithInterpolation(BodyPart2 bp)
    {
        joint.targetRotation = Quaternion.Euler(0f, 0f, 0f);//Quaternion.identity;
    }

    /// <summary>
    /// Apply torque according to defined goal `x, y, z` angle and force `strength`.
    /// </summary>
    public void SetJointTargetRotation(double xx, double yy, double zz)
    {
        float x = (float)xx;
        float y = (float)yy;
        float z = (float)zz;

        x = (x + 1f) * 0.5f;
        y = (y + 1f) * 0.5f;
        z = (z + 1f) * 0.5f;

        var xRot = Mathf.Lerp(joint.lowAngularXLimit.limit, joint.highAngularXLimit.limit, x);
        var yRot = Mathf.Lerp(-joint.angularYLimit.limit, joint.angularYLimit.limit, y);
        var zRot = Mathf.Lerp(-joint.angularZLimit.limit, joint.angularZLimit.limit, z);

        currentXNormalizedRot =
            Mathf.InverseLerp(joint.lowAngularXLimit.limit, joint.highAngularXLimit.limit, xRot);
        currentYNormalizedRot = Mathf.InverseLerp(-joint.angularYLimit.limit, joint.angularYLimit.limit, yRot);
        currentZNormalizedRot = Mathf.InverseLerp(-joint.angularZLimit.limit, joint.angularZLimit.limit, zRot);

        joint.targetRotation = Quaternion.Euler(xRot, yRot, zRot);
        currentEularJointRotation = new Vector3(xRot, yRot, zRot);
    }

    public void SetJointStrength(double _strength)
    {
        float strength = (float)_strength;

        var rawVal = (strength + 1f) * 0.5f * thisJdController.maxJointForceLimit;
        var jd = new JointDrive
        {
            positionSpring = thisJdController.maxJointSpring,
            positionDamper = thisJdController.jointDampen,
            maximumForce = rawVal
        };
        joint.slerpDrive = jd;
        currentStrength = jd.maximumForce;
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