using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NimbleTest : MonoBehaviour
{

    public List<ConfigurableJoint> joint;
    public float maxJointSpring;
    public float jointDampen;
    public float maxJointForceLimit;
    public float massScale = 1;
    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < joint.Count; i++)
        {
            JointDrive jd = new JointDrive
            {
                positionSpring = maxJointSpring,
                positionDamper = jointDampen,
                maximumForce = maxJointForceLimit
            };
            joint[i].slerpDrive = jd;
            joint[i].massScale = massScale;
        }
    }
}
