using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(JointDriveController2))] // Required to set joint forces
public class PlantAgent : Agent2
{
    public const int decisionPeriod = 5;

    //The direction an agent will walk during training.
    [Header("Food and mouth managers")]
    public bool spawnFood;
    public FlySpawner foodSpawner;
    public PlantMouth mouthBottom;
    public PlantMouth mouthTop;
    private Transform food;


    [Header("Body Parts")][Space(10)] 
    public Transform pot;
    public Transform stemBottom;
    public Transform stemMiddle;
    public Transform stemTop;
    public Transform mouthUp;
    public Transform mouthDown;
    public BoxCollider catchBoundaries;

    //This will be used as a stabilized model space reference point for observations
    //Because ragdolls can move erratically during training, using a stabilized reference transform improves learning
    public OrientationCubeController2 m_OrientationCube;

    //The indicator graphic gameobject that points towards the target
    DirectionIndicator m_DirectionIndicator;
    


    Vector3 dirToTarget;
    Matrix4x4 targetDirMatrix;
    Quaternion m_LookRotation;
    float m_MovingTowardsDot;
    float m_FacingDot;

    private void Awake()
    {
        //Time.fixedDeltaTime = 0.01333f;
        //Time.maximumDeltaTime = 0.15f;

        Initialize();
    }

    public void Initialize()
    {

        m_OrientationCube.Initialize(stemTop);
        //m_DirToTarget = m_Target.position - pot.position;

        //m_DirectionIndicator = GetComponentInChildren<DirectionIndicator>();
        jdController = GetComponent<JointDriveController2>();
        currentStateData = new List<double>();
        freezeBody = true;

        //Setup each body part
        jdController.SetupBodyPart(pot);
        jdController.SetupBodyPart(stemBottom);
        jdController.SetupBodyPart(stemMiddle);
        jdController.SetupBodyPart(stemTop);
        jdController.SetupBodyPart(mouthUp);
        jdController.SetupBodyPart(mouthDown);

    }

    /// <summary>
    /// Spawns a target prefab at pos
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="pos"></param>
    void SpawnTarget()
    {
        foodSpawner.Restart();
        if (spawnFood)
            food = foodSpawner.Spawn();
    }

    /// <summary>
    /// Loop over body parts and reset them to initial conditions.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        SpawnTarget();
        mouthBottom.Restart();
        mouthTop.Restart();
        m_OrientationCube.UpdateOrientation(stemTop, food);
        mouthBottom.callback += TouchedTarget;
        mouthTop.callback += TouchedTarget;
        done = false;
        freezeBody = false;
        decisionStep = 0;
        foreach (var bodyPart in jdController.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }
        //Random start rotation to help generalize
        //pot.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
    }

    public override void EndEpisode()
    {
        base.EndEpisode();
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
        dirToTarget = food.position - stemTop.position;
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



        var cubeForward = m_OrientationCube.transform.forward;

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
        Vector3 values = m_OrientationCube.transform.InverseTransformPoint(food.transform.position);
        currentStateData.Add(values.x);
        currentStateData.Add(values.y);
        currentStateData.Add(values.z);
        

        foreach (var bodyPart in jdController.bodyPartsList)
        {
            if (bodyPart.rb.transform != pot)
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
        // Pick a new target joint rotation
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

    
    void FixedUpdate()
    {
        if (freezeBody || done)
            return;
        decisionStep += 1;


        UpdateOrientationObjects();
        // If enabled the feet will light up green when the foot is grounded.
        // This is just a visualization and isn't necessary for function
        
        var cubeForward = m_OrientationCube.transform.forward;

        // Set reward for this step according to mixture of the following elements.
        // a. Match target speed
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates

        //var matchSpeedReward = GetMatchingVelocityReward(cubeForward * TargetWalkingSpeed, GetAvgVelocity());
        
        // b. Rotation alignment with target direction.
        //This reward will approach 1 if it faces the target direction perfectly and approach zero as it deviates
        var lookAtTargetReward = (Vector3.Dot(cubeForward, stemTop.forward) + 1) * .5F;
        AddReward(lookAtTargetReward*0.01f);   
        
        if (stepCallBack != null && decisionStep >= decisionPeriod)
        {
            decisionStep = 0;
            stepCallBack();
        }
    }
    

    void UpdateOrientationObjects()
    {
        m_OrientationCube.UpdateOrientation(stemTop, food);
    }

    /// <summary>
    ///Returns the average velocity of all of the body parts
    ///Using the velocity of the body only has shown to result in more erratic movement from the limbs
    ///Using the average helps prevent this erratic movement
    /// </summary>
    Vector3 GetAvgVelocity()
    {
        Vector3 velSum = Vector3.zero;
        Vector3 avgVel = Vector3.zero;

        //ALL RBS
        int numOfRb = 0;
        foreach (var item in jdController.bodyPartsList)
        {
            numOfRb++;
            velSum += item.rb.velocity;
        }

        avgVel = velSum / numOfRb;
        return avgVel;
    }

    /// <summary>
    /// Normalized value of the difference in actual speed vs goal walking speed.
    /// </summary>


    /// <summary>
    /// Agent touched the target
    /// </summary>
    public void TouchedTarget()
    {

        if(mouthBottom.caughtFood && mouthTop.caughtFood)
        {
            Debug.Log("food caught");
            AddReward(1f);
            EndEpisode();
        }
    }

    public void AddReward(float increment)
    {
        //Utilities.DebugCheckNanAndInfinity(increment, "increment", "AddReward");
        m_Reward += increment;
        //m_CumulativeReward += increment;
    }
    
}
