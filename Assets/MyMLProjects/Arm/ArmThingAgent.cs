using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using Unity.MLAgentsExamples;
using UnityEngine;

public class ArmThingAgent : Agent2
{
    public float targetReachDistance = 2.5f;
    public Rigidbody topHierarchyBodyPartRb;

    [Header("Body Parts")]
    [Space(10)]
    public List<Transform> fingersTop;
    public List<Transform> fingersMiddle;
    public List<Transform> fingersBottom;
    

    [Header("Target")]
    [Space(10)]
    public Transform TargetPrefab;

    [Header("RayPoints")]
    public List<RayPerceptionSensorComponent3D> rays;

    Vector3 dirToTarget;
    Matrix4x4 targetDirMatrix;
    Quaternion lookRotation;
    const float m_maxWalkingSpeed = 0.5f;

    protected override void Initialize()
    {
        base.Initialize();
        jdController.SetupBodyPart(topHierarchyBodyPart);

        for (int i = 0; i < fingersTop.Count; i++)
        {
            jdController.SetupBodyPart(fingersTop[i]);
        }

        for (int i = 0; i < fingersMiddle.Count; i++)
        {
            jdController.SetupBodyPart(fingersMiddle[i]);
        }

        for (int i = 0; i < fingersBottom.Count; i++)
        {
            jdController.SetupBodyPart(fingersBottom[i]);
        }

        if (testingModel == false)
            FreezeRigidBody(true);

        //ActionReceived(new List<double>(new double[1000]));
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();


        if (trainingEnvironment == true && testingModel == false)
        {
            //Random start rotation to help generalize
            topHierarchyBodyPart.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
        }
    }

    protected override void SpawnTarget()
    {
        if (trainingEnvironment == false || testingModel == true)
            return;

        if (targetTransform != null)
            Destroy(targetTransform.gameObject);

        Vector3 instPos = transform.parent.position;
        //instPos.y += 3f;
        targetTransform = Instantiate(TargetPrefab, instPos, Quaternion.identity, transform.parent);
    }

    public override void CollectObservations()
    {
        currentStateData = new List<double>();

        jdController.GetCurrentJointForces();
        dirToTarget = targetTransform.position - stabilizingPivot.position;
        lookRotation = Quaternion.LookRotation(dirToTarget);
        targetDirMatrix = Matrix4x4.TRS(Vector3.zero, lookRotation, Vector3.one);

        RaycastHit hit;
        float maxRaycastDist = 10;
        if (Physics.Raycast(stabilizingPivot.position, Vector3.down, out hit, maxRaycastDist))
        {
            currentStateData.Add(hit.distance / maxRaycastDist);
        }
        else
            currentStateData.Add(1);

        var bodyForwardRelativeToLookRotationToTarget = targetDirMatrix.inverse.MultiplyVector(stabilizingPivot.forward);
        currentStateData.Add(bodyForwardRelativeToLookRotationToTarget.x);
        currentStateData.Add(bodyForwardRelativeToLookRotationToTarget.y);
        currentStateData.Add(bodyForwardRelativeToLookRotationToTarget.z);

        var bodyUpRelativeToLookRotationToTarget = targetDirMatrix.inverse.MultiplyVector(stabilizingPivot.up);
        currentStateData.Add(bodyUpRelativeToLookRotationToTarget.x);
        currentStateData.Add(bodyUpRelativeToLookRotationToTarget.y);
        currentStateData.Add(bodyUpRelativeToLookRotationToTarget.z);

        var cubeForward = orientationCube.transform.forward;

        //velocity we want to match
        var velGoal = cubeForward * m_maxWalkingSpeed;
        //ragdoll's avg vel
        var avgVel = GetAvgVelocity();

        //current ragdoll velocity. normalized
        //currentStateData.Add(Vector3.Distance(velGoal, avgVel));
        //avg body vel relative to cube
        Vector3 values = orientationCube.transform.InverseTransformDirection(avgVel);

        currentStateData.Add(values.x);
        currentStateData.Add(values.y);
        currentStateData.Add(values.z);
        //vel goal relative to cube
        values = orientationCube.transform.InverseTransformDirection(velGoal);
        currentStateData.Add(values.x);
        currentStateData.Add(values.y);
        currentStateData.Add(values.z);
        //rotation delta
        Quaternion QuaternionValues = Quaternion.FromToRotation(stabilizingPivot.forward, cubeForward);
        currentStateData.Add(QuaternionValues.x);
        currentStateData.Add(QuaternionValues.y);
        currentStateData.Add(QuaternionValues.z);
        currentStateData.Add(QuaternionValues.w);

        //Add pos of target relative to orientation cube
        values = orientationCube.transform.InverseTransformPoint(targetTransform.transform.position);
        currentStateData.Add(values.x);
        currentStateData.Add(values.y);
        currentStateData.Add(values.z);

        foreach (var bodyPart in jdController.bodyPartsList)
        {
            if (bodyPart.rb.transform != topHierarchyBodyPart)
            {
                CollectObservationBodyPart(bodyPart);
            }
        }

        GetSensorInfo();
    }

    private void CollectObservationBodyPart(BodyPart2 bp)
    {
        //GROUND CHECK
        if (bp.groundContact.touchingGround)
            currentStateData.Add(1);
        else
            currentStateData.Add(0);

        var velocityRelativeToLookRotationToTarget = targetDirMatrix.inverse.MultiplyVector(bp.rb.velocity);
        currentStateData.Add(velocityRelativeToLookRotationToTarget.x);
        currentStateData.Add(velocityRelativeToLookRotationToTarget.y);
        currentStateData.Add(velocityRelativeToLookRotationToTarget.z);

        var angularVelocityRelativeToLookRotationToTarget = targetDirMatrix.inverse.MultiplyVector(bp.rb.angularVelocity);
        currentStateData.Add(angularVelocityRelativeToLookRotationToTarget.x);
        currentStateData.Add(angularVelocityRelativeToLookRotationToTarget.y);
        currentStateData.Add(angularVelocityRelativeToLookRotationToTarget.z);

        var localPosRelToBody = stabilizingPivot.InverseTransformPoint(bp.rb.position);
        currentStateData.Add(localPosRelToBody.x);
        currentStateData.Add(localPosRelToBody.y);
        currentStateData.Add(localPosRelToBody.z);
        currentStateData.Add(bp.currentXNormalizedRot); // Current x rot
        currentStateData.Add(bp.currentYNormalizedRot); // Current y rot
        currentStateData.Add(bp.currentZNormalizedRot); // Current z rot
        currentStateData.Add(bp.currentStrength / jdController.maxJointForceLimit);
    }

    void GetSensorInfo()
    {
        for (int l = 0; l < rays.Count; l++)
        {
            RayPerceptionInput spec = rays[l].GetRayPerceptionInput();
            RayPerceptionOutput obs = RayPerceptionSensor.Perceive(spec);

            for (int i = 0; i < obs.RayOutputs.Length; i++)
            {
                currentStateData.Add(obs.RayOutputs[i].HitFraction);
                //Debug.Log("sensor: " + i + " fraction: " + obs.RayOutputs[i].HitFraction);
            }
        }
    }

    public override void ActionReceived(List<double> actionBuffers)
    {
        var bpDict = jdController.bodyPartsDict;
        var i = -1;
        int actionCount = 0;

        // Pick a new target joint rotation
        //Debug.Log(actionBuffers.Count);
        for (int l = 0; l < fingersTop.Count; l++)
        {
            bpDict[fingersTop[l]].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], 0);
            actionCount += 2;
        }

        for(int l = 0; l < fingersMiddle.Count; l++)
        {
            bpDict[fingersMiddle[l]].SetJointTargetRotation(actionBuffers[++i], 0, 0);
            actionCount += 1;
        }

        for (int l = 0; l < fingersBottom.Count; l++)
        {
            bpDict[fingersBottom[l]].SetJointTargetRotation(actionBuffers[++i], 0, 0);
            actionCount += 1;
        }

        // Update joint strength
        for (int l = 0; l < fingersTop.Count; l++)
        {
            bpDict[fingersTop[l]].SetJointStrength(actionBuffers[++i]);
            actionCount += 1;
        }

        for (int l = 0; l < fingersMiddle.Count; l++)
        {
            bpDict[fingersMiddle[l]].SetJointStrength(actionBuffers[++i]);
            actionCount += 1;
        }

        for (int l = 0; l < fingersBottom.Count; l++)
        {
            bpDict[fingersBottom[l]].SetJointStrength(actionBuffers[++i]);
            actionCount += 1;
        }

        //Debug.Log(actionCount);
    }

    void FixedUpdate()
    {
        if (freezeBody || done || (testingModel && trainingEnvironment == false))
            return;
        decisionStep += 1;

        UpdateOrientationObjects();

        if (BackFlipDetected())
        {
            SetReward(-1);
            EndEpisode();
            return;
        }
        Vector3 cubeForward = orientationCube.transform.forward;
        Vector3 rbVel = GetAvgVelocity();

        //Gizmos.color = Color.red;
        //Gizmos.DrawRay(transform.position, cubeForward * m_maxWalkingSpeed);
        //This is what we want:
        Debug.DrawRay(stabilizingPivot.position, cubeForward * m_maxWalkingSpeed, Color.yellow);
        //Gizmos.color = Color.blue;
        //Gizmos.DrawRay(transform.position, rbVel);
        //This is what we have:
        Debug.DrawRay(stabilizingPivot.position, rbVel, Color.blue);


        //This reward will approach 1 if it matches perfectly and approach zero as it deviates
        var matchSpeedReward = GetMatchingVelocityReward(cubeForward * m_maxWalkingSpeed, rbVel);
        AddReward(matchSpeedReward * 0.2f);


        //This reward will approach 1 if it faces the target direction perfectly and approach zero as it deviates
        float movingTowardsDot = Vector3.Dot(rbVel, cubeForward);
        AddReward(movingTowardsDot * 0.3f);

        Vector3 cubeForwardProjection = cubeForward;
        cubeForwardProjection.y = 0;
        Vector3 stabilizingPivotProjection = stabilizingPivot.forward;
        stabilizingPivotProjection.y = 0;
        float lookAtTargetReward = (Vector3.Dot(cubeForwardProjection, stabilizingPivotProjection) + 1) * .5F;
        AddReward(movingTowardsDot * lookAtTargetReward * 0.2f);



        float distanceToTarget = Vector3.Distance(stabilizingPivot.position, targetTransform.position);
        if (distanceToTarget < targetReachDistance && trainingEnvironment == true)
            TouchedTarget();

        if (stepCallBack != null && decisionStep >= decisionPeriod)
        {
            decisionStep = 0;
            stepCallBack();
        }
    }

    public float GetMatchingVelocityReward(Vector3 velocityGoal, Vector3 actualVelocity)
    {
        //distance between our actual velocity and goal velocity
        var velDeltaMagnitude = Mathf.Clamp(Vector3.Distance(actualVelocity, velocityGoal), 0, m_maxWalkingSpeed);

        //return the value on a declining sigmoid shaped curve that decays from 1 to 0
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates
        return Mathf.Pow(1 - Mathf.Pow(velDeltaMagnitude / m_maxWalkingSpeed, 2), 2);
    }

    Vector3 GetAvgVelocity()
    {
        Vector3 velSum = Vector3.zero;

        //ALL RBS
        int numOfRb = 0;
        foreach (var item in jdController.bodyPartsList)
        {
            numOfRb++;
            velSum += item.rb.velocity;
        }

        var avgVel = velSum / numOfRb;

        return avgVel;
    }



    public void TouchedTarget()
    {
        AddReward(20f);
        TargetController targetController = targetTransform.GetComponent<TargetController>();
        if (targetController.respawnIfTouched)
            targetController.MoveTargetToRandomPosition();
    }

    bool BackFlipDetected()
    {
        float dotProd = Vector3.Dot(stabilizingPivot.up, Vector3.up);
        if (1-Mathf.Abs(dotProd) > 0.80f)
            return true;
        return false;
    }
}
