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
    public List<double> currentStateData;

    private bool stopTraining = false;
    public const int decisionPeriod = 5;


    //The direction an agent will walk during training.
    [Header("Food and mouth managers")]
    public FlyController flyController;
    public FlySpawner foodSpawner;
    public PlantMouth mouth;
    public bool spawnFood;
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
    public OrientationCubeController m_OrientationCube;

    //The indicator graphic gameobject that points towards the target
    DirectionIndicator m_DirectionIndicator;
    JointDriveController2 m_JdController;


    Vector3 m_DirToTarget;
    Matrix4x4 m_TargetDirMatrix;
    Quaternion m_LookRotation;
    float m_MovingTowardsDot;
    float m_FacingDot;

    private void Awake()
    {
        //Time.fixedDeltaTime = 0.01333f;
        //Time.maximumDeltaTime = 0.15f;

        //Initialize();
    }

    public void Initialize()
    {
        SpawnTarget(); //spawn target
        //m_DirToTarget = m_Target.position - pot.position;

        //m_DirectionIndicator = GetComponentInChildren<DirectionIndicator>();
        m_JdController = GetComponent<JointDriveController2>();
        currentStateData = new List<double>();
        stopTraining = true;

        //Setup each body part
        m_JdController.SetupBodyPart(pot);
        m_JdController.SetupBodyPart(stemBottom);
        m_JdController.SetupBodyPart(stemMiddle);
        m_JdController.SetupBodyPart(stemTop);
        m_JdController.SetupBodyPart(mouthUp);
        m_JdController.SetupBodyPart(mouthDown);

    }

    /// <summary>
    /// Spawns a target prefab at pos
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="pos"></param>
    void SpawnTarget()
    {
        if(spawnFood)
            foodSpawner.Spawn();
    }

    /// <summary>
    /// Loop over body parts and reset them to initial conditions.
    /// </summary>
    public void OnEpisodeBegin()
    {
        done = false;
        stopTraining = false;
        decisionStep = 0;
        foreach (var bodyPart in m_JdController.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }

        //Random start rotation to help generalize
        pot.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);

    }

    public override void EndEpisode()
    {
        foodSpawner.Restart();
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

        var velocityRelativeToLookRotationToTarget = m_TargetDirMatrix.inverse.MultiplyVector(bp.rb.velocity);
        currentStateData.Add(velocityRelativeToLookRotationToTarget.x);
        currentStateData.Add(velocityRelativeToLookRotationToTarget.y);
        currentStateData.Add(velocityRelativeToLookRotationToTarget.z);

        var angularVelocityRelativeToLookRotationToTarget = m_TargetDirMatrix.inverse.MultiplyVector(bp.rb.angularVelocity);
        currentStateData.Add(angularVelocityRelativeToLookRotationToTarget.x);
        currentStateData.Add(angularVelocityRelativeToLookRotationToTarget.y);
        currentStateData.Add(angularVelocityRelativeToLookRotationToTarget.z);

        if (bp.rb.transform != pot)
        {
            var localPosRelToBody = pot.InverseTransformPoint(bp.rb.position);
            currentStateData.Add(localPosRelToBody.x);
            currentStateData.Add(localPosRelToBody.y);
            currentStateData.Add(localPosRelToBody.z);
            currentStateData.Add(bp.currentXNormalizedRot); // Current x rot
            currentStateData.Add(bp.currentYNormalizedRot); // Current y rot
            currentStateData.Add(bp.currentZNormalizedRot); // Current z rot

            currentStateData.Add(bp.currentStrength / m_JdController.maxJointForceLimit);
        }
    }

    /// <summary>
    /// Loop over body parts to add them to observation.
    /// </summary>
    public void CollectObservations()
    {
        currentStateData = new List<double>();

        m_JdController.GetCurrentJointForces();
        //m_DirToTarget = m_Target.position - pot.position;
        //m_LookRotation = Quaternion.LookRotation(m_DirToTarget);
        m_TargetDirMatrix = Matrix4x4.TRS(Vector3.zero, m_LookRotation, Vector3.one);

        RaycastHit hit;
        float maxRaycastDist = 10;
        if (Physics.Raycast(pot.position, Vector3.down, out hit, maxRaycastDist))
        {
            currentStateData.Add(hit.distance / maxRaycastDist);
        }
        else
            currentStateData.Add(1);

        var bodyForwardRelativeToLookRotationToTarget = m_TargetDirMatrix.inverse.MultiplyVector(pot.forward);
        currentStateData.Add(bodyForwardRelativeToLookRotationToTarget.x);
        currentStateData.Add(bodyForwardRelativeToLookRotationToTarget.y);
        currentStateData.Add(bodyForwardRelativeToLookRotationToTarget.z);

        var bodyUpRelativeToLookRotationToTarget = m_TargetDirMatrix.inverse.MultiplyVector(pot.up);
        currentStateData.Add(bodyUpRelativeToLookRotationToTarget.x);
        currentStateData.Add(bodyUpRelativeToLookRotationToTarget.y);
        currentStateData.Add(bodyUpRelativeToLookRotationToTarget.z);



        var cubeForward = m_OrientationCube.transform.forward;

        //velocity we want to match
        //var velGoal = cubeForward * TargetWalkingSpeed;
        //ragdoll's avg vel
        var avgVel = GetAvgVelocity();

        //current ragdoll velocity. normalized
        //currentStateData.Add(Vector3.Distance(velGoal, avgVel));
        //avg body vel relative to cube
        Vector3 values = m_OrientationCube.transform.InverseTransformDirection(avgVel);
        /*
        currentStateData.Add(values.x);
        currentStateData.Add(values.y);
        currentStateData.Add(values.z);
        //vel goal relative to cube
        values = m_OrientationCube.transform.InverseTransformDirection(velGoal);
        currentStateData.Add(values.x);
        currentStateData.Add(values.y);
        currentStateData.Add(values.z);
        //rotation delta
        Quaternion QuaternionValues = Quaternion.FromToRotation(body.forward, cubeForward);
        currentStateData.Add(QuaternionValues.x);
        currentStateData.Add(QuaternionValues.y);
        currentStateData.Add(QuaternionValues.z);
        currentStateData.Add(QuaternionValues.w);

        //Add pos of target relative to orientation cube
        values = m_OrientationCube.transform.InverseTransformPoint(m_Target.transform.position);
        currentStateData.Add(values.x);
        currentStateData.Add(values.y);
        currentStateData.Add(values.z);
        */

        foreach (var bodyPart in m_JdController.bodyPartsList)
        {
            CollectObservationBodyPart(bodyPart);
        }
    }

    public void ActionReceived(List<double> actionBuffers)
    {
        // The dictionary with all the body parts in it are in the jdController
        var bpDict = m_JdController.bodyPartsDict;
        var i = -1;
        // Pick a new target joint rotation
        bpDict[pot].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], 0);
        bpDict[stemBottom].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], 0);
        bpDict[stemMiddle].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], 0);
        bpDict[stemTop].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], 0);
        bpDict[mouthUp].SetJointTargetRotation(actionBuffers[++i], 0, 0);
        bpDict[mouthDown].SetJointTargetRotation(actionBuffers[++i], 0, 0);


        // Update joint strength
        bpDict[pot].SetJointStrength(actionBuffers[++i]);
        bpDict[stemBottom].SetJointStrength(actionBuffers[++i]);
        bpDict[stemMiddle].SetJointStrength(actionBuffers[++i]);
        bpDict[stemTop].SetJointStrength(actionBuffers[++i]);
        bpDict[mouthUp].SetJointStrength(actionBuffers[++i]);
        bpDict[mouthDown].SetJointStrength(actionBuffers[++i]);
    }

    /*
    void FixedUpdate()
    {
        if (stopTraining)
            return;
        decisionStep += 1;

        UpdateOrientationObjects();

        // If enabled the feet will light up green when the foot is grounded.
        // This is just a visualization and isn't necessary for function
        if (useFootGroundedVisualization)
        {
            foot0.material = m_JdController.bodyPartsDict[leg0Lower].groundContact.touchingGround
                ? groundedMaterial
                : unGroundedMaterial;
            foot1.material = m_JdController.bodyPartsDict[leg1Lower].groundContact.touchingGround
                ? groundedMaterial
                : unGroundedMaterial;
            foot2.material = m_JdController.bodyPartsDict[leg2Lower].groundContact.touchingGround
                ? groundedMaterial
                : unGroundedMaterial;
            foot3.material = m_JdController.bodyPartsDict[leg3Lower].groundContact.touchingGround
                ? groundedMaterial
                : unGroundedMaterial;
        }

        var cubeForward = m_OrientationCube.transform.forward;

        // Set reward for this step according to mixture of the following elements.
        // a. Match target speed
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates

        var matchSpeedReward = GetMatchingVelocityReward(cubeForward * TargetWalkingSpeed, GetAvgVelocity());
        /*
        // b. Rotation alignment with target direction.
        //This reward will approach 1 if it faces the target direction perfectly and approach zero as it deviates
        var lookAtTargetReward = (Vector3.Dot(cubeForward, body.forward) + 1) * .5F;
        AddReward(matchSpeedReward * lookAtTargetReward);
        */
/*
        foreach (var bodyPart in m_JdController.bodyPartsList)
        {
            if (bodyPart.targetContact && !done && bodyPart.targetContact.touchingTarget)
            {
                TouchedTarget();
            }
        }

        m_DirToTarget = m_Target.position - body.position;
        m_MovingTowardsDot = Vector3.Dot(m_JdController.bodyPartsDict[body].rb.velocity, m_DirToTarget.normalized);
        AddReward(0.03f * m_MovingTowardsDot);

        AddReward(0.03f * matchSpeedReward);

        m_FacingDot = Vector3.Dot(m_DirToTarget.normalized, body.forward);
        AddReward(0.01f * m_FacingDot);


        if (stepCallBack != null && decisionStep >= decisionPeriod)
        {
            decisionStep = 0;
            stepCallBack();
        }
    }
*/


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
        foreach (var item in m_JdController.bodyPartsList)
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
        AddReward(1f);
        EndEpisode();
    }

    public void AddReward(float increment)
    {
        //Utilities.DebugCheckNanAndInfinity(increment, "increment", "AddReward");
        m_Reward += increment;
        //m_CumulativeReward += increment;
    }

    public void StopTraining()
    {
        stepCallBack = null;
        stopTraining = true;
    }

    public void FreezeRigidBody(bool freeze)
    {
        for (int i = 0; i < m_JdController.bodyPartsList.Count; i++)
        {
            if (freeze)
            {
                stopTraining = true;
                if (m_JdController.bodyPartsList[i].isAlreadyFroozen == false)
                {
                    m_JdController.bodyPartsList[i].isAlreadyFroozen = true;
                    m_JdController.bodyPartsList[i].SaveVelocity();
                    m_JdController.bodyPartsList[i].rb.constraints = RigidbodyConstraints.FreezePosition;
                    m_JdController.bodyPartsList[i].rb.isKinematic = true;
                }
            }
            else
            {
                stopTraining = false;
                if (m_JdController.bodyPartsList[i].isAlreadyFroozen == true)
                {
                    m_JdController.bodyPartsList[i].isAlreadyFroozen = false;
                    m_JdController.bodyPartsList[i].rb.isKinematic = false;
                    m_JdController.bodyPartsList[i].rb.constraints = RigidbodyConstraints.None;
                    m_JdController.bodyPartsList[i].LoadSavedVelocity();
                }
            }
        }
    }
}
