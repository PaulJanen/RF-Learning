using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Sensors;

public class CalAgent : Agent2
{
    public Rigidbody rbBalancedObj;

    public RayPerceptionSensor sensor;


    public float targetReachDistance = 5f;
    public float speed = 10f;
    public float rotSpeed = 10f;

    [Header("Body Parts")]
    [Space(10)]
    public Transform MouthTop;
    public Transform MouthDown;

    public Transform ArmForwardLeft;
    public Transform ArmForwardLeftHand;
    public Transform ArmForwardRight;
    public Transform ArmForwardRightHand;
    public Transform ArmBackwardsRight;
    public Transform ArmBackwardsRightHand;
    public Transform ArmBackwardsLeft;
    public Transform ArmBackwardsLeftHand;

    [Header("RayPoints")]
    public Transform ForwardLeftHandRayPoint;
    public Transform ForwardRightHandRayPoint;
    public Transform BackRightHandRayPoint;
    public Transform BackLeftHandRayPoint;

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

    private Vector3 transformFinalPos;
    private Vector3 balancedObjPrvVelocity;
    private Vector3 balancedObjPrvAngularVelocity;
    private Vector3 balancedObjStartingPos;
    private Quaternion balancedObjStartingRot;
    private bool isPanAlreadyFrozen;
    private float maxPanHeight = 0.7181f;
    private float minPanHeight = 0.6424f;
    private float rayLegth = 0.1f;

    public bool isCooking;


    protected override void Initialize()
    {
        base.Initialize();

        transformFinalPos = transform.position;

        balancedObjStartingPos = rbBalancedObj.transform.position;
        balancedObjStartingRot = rbBalancedObj.transform.rotation;

        jdController.SetupBodyPart(topHierarchyBodyPart);
        jdController.SetupBodyPart(MouthTop);
        jdController.SetupBodyPart(MouthDown);

        jdController.SetupBodyPart(ArmForwardLeft);
        jdController.SetupBodyPart(ArmForwardLeftHand);
        jdController.SetupBodyPart(ArmForwardRight);
        jdController.SetupBodyPart(ArmForwardRightHand);
        jdController.SetupBodyPart(ArmBackwardsRight);
        jdController.SetupBodyPart(ArmBackwardsRightHand);
        jdController.SetupBodyPart(ArmBackwardsLeft);
        jdController.SetupBodyPart(ArmBackwardsLeftHand);

        if (testingModel == false)
            FreezeRigidBody(true);
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();

        rbBalancedObj.transform.position = stabilizingPivot.transform.position + Vector3.up*5f;
        rbBalancedObj.transform.rotation = balancedObjStartingRot;
        isPanAlreadyFrozen = false;

        if (trainingEnvironment == true)
        {
            //Random start rotation to help generalize
            topHierarchyBodyPart.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
            TargetWalkingSpeed = Random.Range(0.1f, maxWalkingSpeed);
        }
    }

    void BeginNewTask()
    {
        int taskId = Random.Range(0, 1);

        if(taskId == 0)
        {
            rbBalancedObj.gameObject.SetActive(false);
            isCooking = false;
        }
        else
        {
            rbBalancedObj.gameObject.SetActive(true);
            isCooking =true;
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
        if (isCooking == false)
            CollectObservationsForChasing();
        else
            CollectObservationsForBalancing();
    }

    void CollectObservationsForChasing()
    {
        currentStateData = new List<double>();

        jdController.GetCurrentJointForces();
        dirToTarget = targetTransform.position - stabilizingPivot.position;
        lookRotation = Quaternion.LookRotation(dirToTarget);
        targetDirMatrix = Matrix4x4.TRS(Vector3.zero, lookRotation, Vector3.one);
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

        GetSensorInfo();

        foreach (var bodyPart in jdController.bodyPartsList)
        {
            CollectObservationBodyPart(bodyPart);
        }
    }

    void CollectObservationsForBalancing()
    {
        currentStateData = new List<double>();
        dirToTarget = rbBalancedObj.worldCenterOfMass - stabilizingPivot.position;
        lookRotation = Quaternion.LookRotation(dirToTarget);
        targetDirMatrix = Matrix4x4.TRS(Vector3.zero, lookRotation, Vector3.one);
        var bodyUpRelativeToLookRotationToTarget = targetDirMatrix.inverse.MultiplyVector(stabilizingPivot.up);
        currentStateData.Add(bodyUpRelativeToLookRotationToTarget.x);
        currentStateData.Add(bodyUpRelativeToLookRotationToTarget.y);
        currentStateData.Add(bodyUpRelativeToLookRotationToTarget.z);
        var bodyForwardRelativeToLookRotationToTarget = targetDirMatrix.inverse.MultiplyVector(stabilizingPivot.forward);
        currentStateData.Add(bodyForwardRelativeToLookRotationToTarget.x);
        currentStateData.Add(bodyForwardRelativeToLookRotationToTarget.y);
        currentStateData.Add(bodyForwardRelativeToLookRotationToTarget.z);

        var balancedObjUpRelativeToLookRotationToTarget = targetDirMatrix.inverse.MultiplyVector(rbBalancedObj.transform.up);
        currentStateData.Add(balancedObjUpRelativeToLookRotationToTarget.x);
        currentStateData.Add(balancedObjUpRelativeToLookRotationToTarget.y);
        currentStateData.Add(balancedObjUpRelativeToLookRotationToTarget.z);

        var balancedObjForwardRelativeToLookRotationToTarget = targetDirMatrix.inverse.MultiplyVector(rbBalancedObj.transform.forward);
        currentStateData.Add(balancedObjForwardRelativeToLookRotationToTarget.x);
        currentStateData.Add(balancedObjForwardRelativeToLookRotationToTarget.y);
        currentStateData.Add(balancedObjForwardRelativeToLookRotationToTarget.z);

        var localPosRelToBody = stabilizingPivot.InverseTransformPoint(rbBalancedObj.worldCenterOfMass);
        currentStateData.Add(localPosRelToBody.x);
        currentStateData.Add(localPosRelToBody.y);
        currentStateData.Add(localPosRelToBody.z);

        var balancedObjVelocityRelativeToLookRotationToTarget = targetDirMatrix.inverse.MultiplyVector(rbBalancedObj.velocity);
        currentStateData.Add(balancedObjVelocityRelativeToLookRotationToTarget.x);
        currentStateData.Add(balancedObjVelocityRelativeToLookRotationToTarget.y);
        currentStateData.Add(balancedObjVelocityRelativeToLookRotationToTarget.z);

        var balancedObjAngularVelocityRelativeToLookRotationToTarget = targetDirMatrix.inverse.MultiplyVector(rbBalancedObj.angularVelocity);
        currentStateData.Add(balancedObjAngularVelocityRelativeToLookRotationToTarget.x);
        currentStateData.Add(balancedObjAngularVelocityRelativeToLookRotationToTarget.y);
        currentStateData.Add(balancedObjAngularVelocityRelativeToLookRotationToTarget.z);

        currentStateData.Add(HandRaycast(ForwardLeftHandRayPoint));
        currentStateData.Add(HandRaycast(ForwardRightHandRayPoint));
        currentStateData.Add(HandRaycast(BackRightHandRayPoint));
        currentStateData.Add(HandRaycast(BackLeftHandRayPoint));

        foreach (var bodyPart in jdController.bodyPartsList)
        {
            CollectObservationBodyPart(bodyPart);
        }

        dirToTarget = transformFinalPos - stabilizingPivot.position;
        lookRotation = Quaternion.LookRotation(dirToTarget);
        targetDirMatrix = Matrix4x4.TRS(Vector3.zero, lookRotation, Vector3.one);
        bodyForwardRelativeToLookRotationToTarget = targetDirMatrix.inverse.MultiplyVector(stabilizingPivot.forward);
        currentStateData.Add(bodyForwardRelativeToLookRotationToTarget.x);
        currentStateData.Add(bodyForwardRelativeToLookRotationToTarget.y);
        currentStateData.Add(bodyForwardRelativeToLookRotationToTarget.z);
    }

    float HandRaycast(Transform origin)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin.position, Vector3.up, out hit, rayLegth))
        {
            if(hit.rigidbody == rbBalancedObj)
                return(hit.distance / rayLegth);
        }

        return 1f;
    }

    void GetSensorInfo()
    {
        var c = GetComponent<RayPerceptionSensorComponent3D>();
        RayPerceptionInput spec = c.GetRayPerceptionInput();
        RayPerceptionOutput obs = RayPerceptionSensor.Perceive(spec);
        for (int i = 0; i < obs.RayOutputs.Length; i++)
        {
            currentStateData.Add(obs.RayOutputs[i].HitFraction);
            //Debug.Log("sensor: " + i + " fraction: " + obs.RayOutputs[i].HitFraction);
        }
    }

    private void CollectObservationBodyPart(BodyPart2 bp)
    {
        /*
        //GROUND CHECK
        if (bp.groundContact.touchingGround)
            currentStateData.Add(1);
        else
            currentStateData.Add(0);
        */
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

        float x = (float)actionBuffers[++i];//((float)actionBuffers[++i] + 1f) * 0.5f;
        float y = (float)actionBuffers[++i];//((float)actionBuffers[++i] + 1f) * 0.5f;

        if (float.IsNaN(x) == false)
            bpDict[topHierarchyBodyPart].rb.AddForce(stabilizingPivot.forward * speed * x, ForceMode.VelocityChange);
        if (float.IsNaN(y) == false)
            bpDict[topHierarchyBodyPart].rb.AddTorque(Vector3.up * rotSpeed * y, ForceMode.VelocityChange);

        bpDict[ArmForwardLeft].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], 0);
        bpDict[ArmForwardLeftHand].SetJointTargetRotation(actionBuffers[++i], 0, 0);
        bpDict[ArmForwardRight].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], 0);
        bpDict[ArmForwardRightHand].SetJointTargetRotation(actionBuffers[++i], 0, 0);
        bpDict[ArmBackwardsRight].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], 0);
        bpDict[ArmBackwardsRightHand].SetJointTargetRotation(actionBuffers[++i], 0, 0);
        bpDict[ArmBackwardsLeft].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], 0);
        bpDict[ArmBackwardsLeftHand].SetJointTargetRotation(actionBuffers[++i], 0, 0);


        bpDict[ArmForwardLeft].SetJointStrength(actionBuffers[++i]);
        bpDict[ArmForwardLeftHand].SetJointStrength(actionBuffers[++i]);
        bpDict[ArmForwardRight].SetJointStrength(actionBuffers[++i]);
        bpDict[ArmForwardRightHand].SetJointStrength(actionBuffers[++i]);
        bpDict[ArmBackwardsRight].SetJointStrength(actionBuffers[++i]);
        bpDict[ArmBackwardsRightHand].SetJointStrength(actionBuffers[++i]);
        bpDict[ArmBackwardsLeft].SetJointStrength(actionBuffers[++i]);
        bpDict[ArmBackwardsLeftHand].SetJointStrength(actionBuffers[++i]);
    }

    void FixedUpdate()
    {
        if (freezeBody || done || testingModel)
            return;
        decisionStep += 1;

        UpdateOrientationObjects();

        if (isCooking)
            RewardForCooking();
        else
            RewardForChasing();
        

        if (stepCallBack != null && decisionStep >= decisionPeriod)
        {
            decisionStep = 0;
            stepCallBack();
        }
    }

    void RewardForCooking()
    {

        if (BackFlipDetected())
        {
            SetReward(-1);
            EndEpisode();
            return;
        }


        float maxDistance = maxPanHeight - minPanHeight;
        float panDistance = rbBalancedObj.worldCenterOfMass.y - minPanHeight;
        float panRelativeDistance = Mathf.Lerp(0f, 1f, (panDistance / maxDistance));
        
        AddReward(0.25f * panRelativeDistance);

        float balancingReward = (Vector3.Dot(Vector3.up, rbBalancedObj.transform.up) + 1) * 0.5f;
        AddReward(0.25f * balancingReward);


        Vector3 dirToTarget = transformFinalPos - stabilizingPivot.position;
        dirToTarget.y = 0f;
        lookRotation = Quaternion.LookRotation(dirToTarget);
        targetDirMatrix = Matrix4x4.TRS(Vector3.zero, lookRotation, Vector3.one);
        Vector3 stabilizingPivotForward = stabilizingPivot.forward;
        stabilizingPivotForward.y = 0f;
        Vector3 bodyForwardRelativeToLookRotationToTarget = targetDirMatrix.inverse.MultiplyVector(stabilizingPivotForward);
        float lookingAtTarget = Vector3.Dot(bodyForwardRelativeToLookRotationToTarget, stabilizingPivot.forward);
        AddReward(lookingAtTarget * 0.2f);

        float distanceToTarget = Vector3.Distance(stabilizingPivot.position, transformFinalPos);
        float distanceToTargetReward = Mathf.Lerp(1, 0, distanceToTarget / 3f);

        AddReward(distanceToTargetReward * 0.2f);        
    }


    void RewardForChasing()
    {
        Vector3 cubeForward = orientationCube.transform.forward;
        Vector3 rbVel = GetAvgVelocity();

        //Gizmos.color = Color.red;
        //Gizmos.DrawRay(transform.position, cubeForward * TargetWalkingSpeed);
        //Debug.DrawRay(transform.position, cubeForward * TargetWalkingSpeed, Color.red);
        //Gizmos.color = Color.blue;
        //Gizmos.DrawRay(transform.position, rbVel);
        //Debug.DrawRay(transform.position, rbVel, Color.blue);


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
        AddReward(lookAtTargetReward * 0.1f);

        float movingTowardsDot = Vector3.Dot(rbVel, cubeForward);
        AddReward(movingTowardsDot * 0.2f);

        float distanceToTarget = Vector3.Distance(stabilizingPivot.position, targetTransform.position);

        if (distanceToTarget < targetReachDistance && trainingEnvironment == true)
            TouchedTarget();
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
        float dotProd = Vector3.Dot(rbBalancedObj.transform.forward, Vector3.up);
        if (dotProd > 0.9)
            return true;
        return false;
    }

    public override void FreezeRigidBody(bool freeze)
    {
        base.FreezeRigidBody(freeze);

        if (freeze)
        {
            if (isPanAlreadyFrozen == false)
            {
                isPanAlreadyFrozen = true;
                balancedObjPrvVelocity = rbBalancedObj.velocity;
                balancedObjPrvAngularVelocity = rbBalancedObj.angularVelocity;
                rbBalancedObj.constraints = RigidbodyConstraints.FreezeAll;
                rbBalancedObj.isKinematic = true;
            }
        }
        else
        {
            isPanAlreadyFrozen = false;
            rbBalancedObj.velocity = balancedObjPrvVelocity;
            rbBalancedObj.angularVelocity = balancedObjPrvAngularVelocity;
            rbBalancedObj.isKinematic = false;
            rbBalancedObj.constraints = RigidbodyConstraints.None;
        }
    }
}
