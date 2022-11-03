using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgentsExamples;


public class WitchHutAgent : Agent2
{
    public float targetReachDistance = 5f;

    [Header("Body Parts")]
    [Space(10)]
    public Transform LeftTopLeg;
    public Transform LeftBottomLeg;
    public Transform LeftLegFoot;
    public Transform RightTopLeg;
    public Transform RightBottomLeg;
    public Transform RightLegFoot;
    [Header("Target")]
    [Space(10)]
    public Transform TargetPrefab;

    const float maxWalkingSpeed = 15;
    [Range(0.1f, maxWalkingSpeed)]
    [SerializeField]
    private float targetWalkingSpeed = maxWalkingSpeed;
    public float TargetWalkingSpeed
    {
        get { return targetWalkingSpeed; }
        set { targetWalkingSpeed = Mathf.Clamp(value, .1f, maxWalkingSpeed); }
    }

    Vector3 dirToTarget;
    Matrix4x4 targetDirMatrix;
    Quaternion lookRotation;

    protected override void Initialize()
    {      
        base.Initialize();
        jdController.SetupBodyPart(topHierarchyBodyPart);
        jdController.SetupBodyPart(LeftTopLeg);
        jdController.SetupBodyPart(LeftBottomLeg);
        jdController.SetupBodyPart(LeftLegFoot);
        jdController.SetupBodyPart(RightTopLeg);
        jdController.SetupBodyPart(RightBottomLeg);
        jdController.SetupBodyPart(RightLegFoot);

        if(testingModel == false)
            FreezeRigidBody(true);
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        
        if (trainingEnvironment == true)
        {
            //Random start rotation to help generalize
            topHierarchyBodyPart.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
            TargetWalkingSpeed = Random.Range(0.1f, maxWalkingSpeed);
        }
    }

    protected override void SpawnTarget()
    {
        if (trainingEnvironment == false)
            return;

        if (targetTransform != null)
            Destroy(targetTransform.gameObject);

        Vector3 instPos = transform.parent.position;
        instPos.y += 3f;
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
        var velGoal = cubeForward * TargetWalkingSpeed;
        //ragdoll's avg vel
        var avgVel = GetAvgVelocity();

        //current ragdoll velocity. normalized
        currentStateData.Add(Vector3.Distance(velGoal, avgVel));
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
            CollectObservationBodyPart(bodyPart);
        }
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

    public override void ActionReceived(List<double> actionBuffers)
    {
        var bpDict = jdController.bodyPartsDict;
        var i = -1;

        bpDict[topHierarchyBodyPart].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], actionBuffers[++i]);
        bpDict[LeftTopLeg].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], 0);
        bpDict[LeftBottomLeg].SetJointTargetRotation(actionBuffers[++i], 0, 0);
        bpDict[LeftLegFoot].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], actionBuffers[++i]);

        bpDict[RightTopLeg].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], 0);
        bpDict[RightBottomLeg].SetJointTargetRotation(actionBuffers[++i], 0, 0);
        bpDict[RightLegFoot].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], actionBuffers[++i]);

        bpDict[LeftTopLeg].SetJointStrength(actionBuffers[++i]);
        bpDict[LeftBottomLeg].SetJointStrength(actionBuffers[++i]);
        bpDict[LeftLegFoot].SetJointStrength(actionBuffers[++i]);
        bpDict[RightTopLeg].SetJointStrength(actionBuffers[++i]);
        bpDict[RightBottomLeg].SetJointStrength(actionBuffers[++i]);
        bpDict[RightLegFoot].SetJointStrength(actionBuffers[++i]);
    }

    void FixedUpdate()
    {
        if (freezeBody || done || testingModel)
            return;
        decisionStep += 1;

        UpdateOrientationObjects();

        if(BackFlipDetected())
        {
            SetReward(-1);
            EndEpisode();
            return;
        }
        Debug.Log("target walk speed: " + targetWalkingSpeed);
        Vector3 cubeForward = orientationCube.transform.forward;
        Vector3 rbVel = GetAvgVelocity();

        //Gizmos.color = Color.red;
        //Gizmos.DrawRay(transform.position, cubeForward * TargetWalkingSpeed);
        Debug.DrawRay(transform.position, cubeForward * TargetWalkingSpeed, Color.red);
        //Gizmos.color = Color.blue;
        //Gizmos.DrawRay(transform.position, rbVel);
        Debug.DrawRay(transform.position, rbVel, Color.blue);
        

        //This reward will approach 1 if it matches perfectly and approach zero as it deviates
        var matchSpeedReward = GetMatchingVelocityReward(cubeForward * TargetWalkingSpeed, rbVel);
        //AddReward(matchSpeedReward);
        AddReward(matchSpeedReward * 0.2f);

        //This reward will approach 1 if it faces the target direction perfectly and approach zero as it deviates
        Vector3 cubeForwardProjection = cubeForward;
        cubeForwardProjection.y = 0;
        Vector3 stabilizingPivotProjection = stabilizingPivot.forward;
        stabilizingPivotProjection.y = 0;
        float lookAtTargetReward = (Vector3.Dot(cubeForwardProjection, stabilizingPivotProjection) + 1) * .5F;
        AddReward(lookAtTargetReward * 0.4f);

        float movingTowardsDot = Vector3.Dot(rbVel, cubeForward);
        AddReward(movingTowardsDot * 0.2f);

        float distanceToTarget = Vector3.Distance(stabilizingPivot.position, targetTransform.position);
        
        if (distanceToTarget < targetReachDistance && trainingEnvironment == true)
            TouchedTarget();

        if (stepCallBack != null && decisionStep >= decisionPeriod)
        {
            decisionStep = 0;
            stepCallBack();
        }

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

    //normalized value of the difference in avg speed vs goal walking speed.
    public float GetMatchingVelocityReward(Vector3 velocityGoal, Vector3 actualVelocity)
    {
        //distance between our actual velocity and goal velocity
        var velDeltaMagnitude = Mathf.Clamp(Vector3.Distance(actualVelocity, velocityGoal), 0, TargetWalkingSpeed);

        //return the value on a declining sigmoid shaped curve that decays from 1 to 0
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates
        return Mathf.Pow(1 - Mathf.Pow(velDeltaMagnitude / TargetWalkingSpeed, 2), 2);
    }

    public void TouchedTarget()
    {
        AddReward(20f);
        targetTransform.GetComponent<TargetController>().MoveTargetToRandomPosition();
    }

    bool BackFlipDetected()
    {
        float dotProd = Vector3.Dot(stabilizingPivot.forward, Vector3.up);
        if (dotProd > 0.9)
            return true;
        return false;
    }
}
