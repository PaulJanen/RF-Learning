using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using System;


public class PlantAgent : Agent2
{
    //The direction an agent will walk during training.
    [Header("Food and mouth managers")]
    
    public FlySpawner flySpawner;
    public Fly fly;
    public PlantMouth mouthBottom;
    public PlantMouth mouthTop;
    public Fly restingPositionFly;


    [Header("Body Parts")][Space(10)] 
    public Transform stemBottom;
    public Transform stemMiddle;
    public Transform stemTop;
    public Transform mouthUp;
    public Transform mouthDown;
    public BoxCollider catchBoundaries;
    public PlantCatchBoundaries plantCatchBoundaries;

    //The indicator graphic gameobject that points towards the target
    DirectionIndicator m_DirectionIndicator;
   
    Vector3 dirToTarget;
    Matrix4x4 targetDirMatrix;
    Quaternion m_LookRotation;
    float m_MovingTowardsDot;
    float m_FacingDot;

    protected override void Initialize()
    {
        //m_DirToTarget = m_Target.position - pot.position;

        //m_DirectionIndicator = GetComponentInChildren<DirectionIndicator>();
        base.Initialize();

        //Setup each body part
        jdController.SetupBodyPart(topHierarchyBodyPart);
        jdController.SetupBodyPart(stemBottom);
        jdController.SetupBodyPart(stemMiddle);
        jdController.SetupBodyPart(stemTop);
        jdController.SetupBodyPart(mouthUp);
        jdController.SetupBodyPart(mouthDown);
        FreezeRigidBody(true);
    }

    /// <summary>
    /// Spawns a target prefab at pos
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="pos"></param>
    protected override void SpawnTarget()
    {
        if (spawnFood)
        {
            flySpawner.Restart();
            targetTransform = flySpawner.Spawn();
            fly = targetTransform.GetComponent<Fly>();
            fly.touchedGround = FoodTouchedGround;
            fly.flyWasConsumed = FoodWasConsumed;
        }
        else
        {
            fly = plantCatchBoundaries.GetFood();
            targetTransform = fly.transform;
            fly.touchedGround = FoodTouchedGround;
            fly.flyWasConsumed = FoodWasConsumed;
        }
    }

    void FoodTouchedGround()
    {
        if (done)
            return;
        //SetReward(-1);
        //EndEpisode();
        SpawnTarget();
    }


    /// <summary>
    /// Loop over body parts and reset them to initial conditions.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();

        if (spawnFood == true)
        {
            //Random start rotation to help generalize
            topHierarchyBodyPart.rotation = Quaternion.Euler(-90f, Random.Range(0.0f, 360.0f), 0);
        }

        mouthBottom.Restart();
        mouthTop.Restart();
        
        mouthTop.callback += FoodBeeingEaten;
        mouthBottom.callback += FoodBeeingEaten;
        mouthTop.foodWasReleased += FoodWasReleased;
        mouthBottom.foodWasReleased += FoodWasReleased;
    }


    /// <summary>
    /// Add relevant information on each body part to observations.
    /// </summary>
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

        
        var localPosRelToBody = stemTop.InverseTransformPoint(bp.rb.position);
        currentStateData.Add(localPosRelToBody.x);
        currentStateData.Add(localPosRelToBody.y);
        currentStateData.Add(localPosRelToBody.z);
        currentStateData.Add(bp.currentXNormalizedRot); // Current x rot
        currentStateData.Add(bp.currentYNormalizedRot); // Current y rot
        currentStateData.Add(bp.currentZNormalizedRot); // Current z rot

        currentStateData.Add(bp.currentStrength / jdController.maxJointForceLimit);
    }

    /// <summary>
    /// Loop over body parts to add them to observation.
    /// </summary>
    public override void CollectObservations()
    {
        currentStateData = new List<double>();

        jdController.GetCurrentJointForces();
        dirToTarget = targetTransform.position - stemTop.position;
        m_LookRotation = Quaternion.LookRotation(dirToTarget);
        targetDirMatrix = Matrix4x4.TRS(Vector3.zero, m_LookRotation, Vector3.one);

        RaycastHit hit;
        float maxRaycastDist = 10;
        if (Physics.Raycast(stemTop.position, Vector3.down, out hit, maxRaycastDist))
        {
            currentStateData.Add(hit.distance / maxRaycastDist);
        }
        else
            currentStateData.Add(1);

        var bodyForwardRelativeToLookRotationToTarget = targetDirMatrix.inverse.MultiplyVector(stemTop.forward);
        currentStateData.Add(bodyForwardRelativeToLookRotationToTarget.x);
        currentStateData.Add(bodyForwardRelativeToLookRotationToTarget.y);
        currentStateData.Add(bodyForwardRelativeToLookRotationToTarget.z);

        var bodyUpRelativeToLookRotationToTarget = targetDirMatrix.inverse.MultiplyVector(stemTop.up);
        currentStateData.Add(bodyUpRelativeToLookRotationToTarget.x);
        currentStateData.Add(bodyUpRelativeToLookRotationToTarget.y);
        currentStateData.Add(bodyUpRelativeToLookRotationToTarget.z);

        if (mouthTop.caughtFood)
            currentStateData.Add(1);
        else
            currentStateData.Add(0);

        if (mouthBottom.caughtFood)
            currentStateData.Add(1);
        else
            currentStateData.Add(0);



        var cubeForward = orientationCube.transform.forward;

        //velocity we want to match
        //var velGoal = cubeForward * TargetWalkingSpeed;
        //ragdoll's avg vel
        //var avgVel = GetAvgVelocity();

        //current ragdoll velocity. normalized
        //currentStateData.Add(Vector3.Distance(velGoal, avgVel));
        //avg body vel relative to cube
        //Vector3 values = m_OrientationCube.transform.InverseTransformDirection(avgVel);
        
        //currentStateData.Add(values.x);
        //currentStateData.Add(values.y);
        //currentStateData.Add(values.z);
        //vel goal relative to cube
        //values = m_OrientationCube.transform.InverseTransformDirection(velGoal);
        //currentStateData.Add(values.x);
        //currentStateData.Add(values.y);
        //currentStateData.Add(values.z);
        //rotation delta
        Quaternion QuaternionValues = Quaternion.FromToRotation(stemTop.forward, cubeForward);
        currentStateData.Add(QuaternionValues.x);
        currentStateData.Add(QuaternionValues.y);
        currentStateData.Add(QuaternionValues.z);
        currentStateData.Add(QuaternionValues.w);

        //Add pos of target relative to orientation cube
        Vector3 values = orientationCube.transform.InverseTransformPoint(targetTransform.transform.position);
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
    }

    public override void ActionReceived(List<double> actionBuffers)
    {
        // The dictionary with all the body parts in it are in the jdController
        var bpDict = jdController.bodyPartsDict;
        var i = -1;

        if (spawnFood == false && targetTransform == plantCatchBoundaries.restingPositionFly.transform)
        {
            foreach (var bodyPart in jdController.bodyPartsDict.Values)
            {
                if (bodyPart != bpDict[topHierarchyBodyPart])
                {
                    bodyPart.ResetWithInterpolation(bodyPart);
                    bodyPart.SetJointStrength(1);
                }
            }

        }
        else
        { // Pick a new target joint rotation
            //bpDict[pot].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], actionBuffers[++i]);
            bpDict[stemBottom].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], actionBuffers[++i]);
            bpDict[stemMiddle].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], actionBuffers[++i]);
            bpDict[stemTop].SetJointTargetRotation(actionBuffers[++i], 0, actionBuffers[++i]);
            bpDict[mouthUp].SetJointTargetRotation(actionBuffers[++i], 0, 0);
            bpDict[mouthDown].SetJointTargetRotation(actionBuffers[++i], 0, 0);


            // Update joint strength
            //bpDict[pot].SetJointStrength(actionBuffers[++i]);
            bpDict[stemBottom].SetJointStrength(actionBuffers[++i]);
            bpDict[stemMiddle].SetJointStrength(actionBuffers[++i]);
            bpDict[stemTop].SetJointStrength(actionBuffers[++i]);
            bpDict[mouthUp].SetJointStrength(actionBuffers[++i]);
            bpDict[mouthDown].SetJointStrength(actionBuffers[++i]);
        }
    }

    
    void FixedUpdate()
    {
        if (freezeBody || done)
            return;
        decisionStep += 1;

        if (spawnFood == false && targetTransform == plantCatchBoundaries.restingPositionFly.transform)
        {
            SpawnTarget();
        }
        UpdateOrientationObjects();
        // If enabled the feet will light up green when the foot is grounded.
        // This is just a visualization and isn't necessary for function
        
        Vector3 cubeForward = orientationCube.transform.forward;

        // Set reward for this step according to mixture of the following elements.
        // a. Match target speed
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates

        //var matchSpeedReward = GetMatchingVelocityReward(cubeForward * TargetWalkingSpeed, GetAvgVelocity());
        
        // b. Rotation alignment with target direction.
        //This reward will approach 1 if it faces the target direction perfectly and approach zero as it deviates
        float lookAtTargetReward = (Vector3.Dot(cubeForward, stabilizingPivot.forward) + 1) * .5F;
        AddReward(lookAtTargetReward*0.1f);

        if (mouthTop.caughtFood)
            AddReward(0.1f);
        if (mouthBottom.caughtFood)
            AddReward(0.1f);
        if (mouthTop.caughtFood && mouthBottom.caughtFood)
            AddReward(1f);

        if (stepCallBack != null && decisionStep >= decisionPeriod)
        {
            decisionStep = 0;
            stepCallBack();
        }
    }

    private void Update()
    {
        if (plantCatchBoundaries.InsideCatchingBox(targetTransform) == false)
        {
            SpawnTarget();
        }
    }

    
    private void FoodBeeingEaten()
    {
        if (mouthBottom.caughtFood && mouthTop.caughtFood)
        {
            if(fly.IsBeingConsumed == false)
            {
                fly.StartBeingConsumed();
            }
        }
    }

    private void FoodWasReleased()
    {
        if (fly.IsBeingConsumed == true)
        {
            fly.StopBeingConsumed();
        }
    }

    void FoodWasConsumed()
    {
        Debug.Log("food was eaten");
        AddReward(20f);
        mouthTop.caughtFood = false;
        mouthBottom.caughtFood = false;
        if(spawnFood)
            SpawnTarget();
        else
        {
            fly = restingPositionFly;
            targetTransform = fly.transform;
            fly.touchedGround = FoodTouchedGround;
            fly.flyWasConsumed = FoodWasConsumed;
        }
    }
}
