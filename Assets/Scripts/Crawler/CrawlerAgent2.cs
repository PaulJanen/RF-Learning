using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(JointDriveController2))] // Required to set joint forces
public class CrawlerAgent2 : Agent2
{

    [Header("Walk Speed")]
    [Range(0.1f, m_maxWalkingSpeed)]
    [SerializeField]
    [Tooltip(
        "The speed the agent will try to match.\n\n" +
        "TRAINING:\n" +
        "For VariableSpeed envs, this value will randomize at the start of each training episode.\n" +
        "Otherwise the agent will try to match the speed set here.\n\n" +
        "INFERENCE:\n" +
        "During inference, VariableSpeed agents will modify their behavior based on this value " +
        "whereas the CrawlerDynamic & CrawlerStatic agents will run at the speed specified during training "
    )]

    //The walking speed to try and achieve
    private float m_TargetWalkingSpeed = m_maxWalkingSpeed;

    public const int decisionPeriod = 5;

    const float m_maxWalkingSpeed = 15; //The max walking speed
    const float m_minWalkingSpeed = 10;
    //The current target walking speed. Clamped because a value of zero will cause NaNs
    public float TargetWalkingSpeed
    {
        get { return m_TargetWalkingSpeed; }
        set { m_TargetWalkingSpeed = Mathf.Clamp(value, .1f, m_maxWalkingSpeed); }
    }

    //The direction an agent will walk during training.
    [Header("Target To Walk Towards")]
    public Transform TargetPrefab; //Target prefab to use in Dynamic envs
    private Transform m_Target; //Target the agent will walk towards during training.

    [Header("Body Parts")][Space(10)] public Transform body;
    public Transform leg0Upper;
    public Transform leg0Lower;
    public Transform leg1Upper;
    public Transform leg1Lower;
    public Transform leg2Upper;
    public Transform leg2Lower;
    public Transform leg3Upper;
    public Transform leg3Lower;

    //This will be used as a stabilized model space reference point for observations
    //Because ragdolls can move erratically during training, using a stabilized reference transform improves learning
    OrientationCubeController m_OrientationCube;

    //The indicator graphic gameobject that points towards the target
    DirectionIndicator m_DirectionIndicator;
    JointDriveController2 m_JdController;

    [Header("Foot Grounded Visualization")]
    [Space(10)]
    public bool useFootGroundedVisualization;

    public MeshRenderer foot0;
    public MeshRenderer foot1;
    public MeshRenderer foot2;
    public MeshRenderer foot3;
    public Material groundedMaterial;
    public Material unGroundedMaterial;

    Vector3 m_DirToTarget;
    Matrix4x4 m_TargetDirMatrix;
    Quaternion m_LookRotation;
    float m_MovingTowardsDot;
    float m_FacingDot;

    private void Awake()
    {
        Time.fixedDeltaTime = 0.01333f;
        Time.maximumDeltaTime = 0.15f;
        
        Initialize();
    }

    public void Initialize()
    {
        SpawnTarget(TargetPrefab, transform.position); //spawn target
        m_DirToTarget = m_Target.position - body.position;
        
        m_OrientationCube = GetComponentInChildren<OrientationCubeController>();
        m_DirectionIndicator = GetComponentInChildren<DirectionIndicator>();
        m_JdController = GetComponent<JointDriveController2>();
        currentStateData = new List<double>();
        stopTraining = true;

        //Setup each body part
        m_JdController.SetupBodyPart(body);
        m_JdController.SetupBodyPart(leg0Upper);
        m_JdController.SetupBodyPart(leg0Lower);
        m_JdController.SetupBodyPart(leg1Upper);
        m_JdController.SetupBodyPart(leg1Lower);
        m_JdController.SetupBodyPart(leg2Upper);
        m_JdController.SetupBodyPart(leg2Lower);
        m_JdController.SetupBodyPart(leg3Upper);
        m_JdController.SetupBodyPart(leg3Lower);
    }

    /// <summary>
    /// Spawns a target prefab at pos
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="pos"></param>
    void SpawnTarget(Transform prefab, Vector3 pos)
    {
        m_Target = Instantiate(prefab, pos, Quaternion.identity, transform.parent);
    }

    /// <summary>
    /// Loop over body parts and reset them to initial conditions.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        done = false;
        stopTraining = false;
        decisionStep = 0;
        foreach (var bodyPart in m_JdController.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }

        //Random start rotation to help generalize
        body.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);

        UpdateOrientationObjects();

        //Set our goal walking speed
        TargetWalkingSpeed = Random.Range(m_minWalkingSpeed, m_maxWalkingSpeed);
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

        if (bp.rb.transform != body)
        {
            var localPosRelToBody = body.InverseTransformPoint(bp.rb.position);
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
    public override void CollectObservations()
    {
        currentStateData = new List<double>();

        m_JdController.GetCurrentJointForces();
        m_DirToTarget = m_Target.position - body.position;
        m_LookRotation = Quaternion.LookRotation(m_DirToTarget);
        m_TargetDirMatrix = Matrix4x4.TRS(Vector3.zero, m_LookRotation, Vector3.one);

        RaycastHit hit;
        float maxRaycastDist = 10;
        if (Physics.Raycast(body.position, Vector3.down, out hit, maxRaycastDist))
        {
            currentStateData.Add(hit.distance / maxRaycastDist);
        }
        else
            currentStateData.Add(1);

        var bodyForwardRelativeToLookRotationToTarget = m_TargetDirMatrix.inverse.MultiplyVector(body.forward);
        currentStateData.Add(bodyForwardRelativeToLookRotationToTarget.x);
        currentStateData.Add(bodyForwardRelativeToLookRotationToTarget.y);
        currentStateData.Add(bodyForwardRelativeToLookRotationToTarget.z);

        var bodyUpRelativeToLookRotationToTarget = m_TargetDirMatrix.inverse.MultiplyVector(body.up);
        currentStateData.Add(bodyUpRelativeToLookRotationToTarget.x);
        currentStateData.Add(bodyUpRelativeToLookRotationToTarget.y);
        currentStateData.Add(bodyUpRelativeToLookRotationToTarget.z);

        

        var cubeForward = m_OrientationCube.transform.forward;

        //velocity we want to match
        var velGoal = cubeForward * TargetWalkingSpeed;
        //ragdoll's avg vel
        var avgVel = GetAvgVelocity();

        //current ragdoll velocity. normalized
        currentStateData.Add(Vector3.Distance(velGoal, avgVel));
        //avg body vel relative to cube
        Vector3 values = m_OrientationCube.transform.InverseTransformDirection(avgVel);
        
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
        

        foreach (var bodyPart in m_JdController.bodyPartsList)
        {
            CollectObservationBodyPart(bodyPart);
        }
    }

    public override void ActionReceived(List<double> actionBuffers)
    {
        // The dictionary with all the body parts in it are in the jdController
        var bpDict = m_JdController.bodyPartsDict;
        var i = -1;
        // Pick a new target joint rotation
        bpDict[leg0Upper].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], 0);
        bpDict[leg1Upper].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], 0);
        bpDict[leg2Upper].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], 0);
        bpDict[leg3Upper].SetJointTargetRotation(actionBuffers[++i], actionBuffers[++i], 0);
        bpDict[leg0Lower].SetJointTargetRotation(actionBuffers[++i], 0, 0);
        bpDict[leg1Lower].SetJointTargetRotation(actionBuffers[++i], 0, 0);
        bpDict[leg2Lower].SetJointTargetRotation(actionBuffers[++i], 0, 0);
        bpDict[leg3Lower].SetJointTargetRotation(actionBuffers[++i], 0, 0);

        // Update joint strength
        bpDict[leg0Upper].SetJointStrength(actionBuffers[++i]);
        bpDict[leg1Upper].SetJointStrength(actionBuffers[++i]);
        bpDict[leg2Upper].SetJointStrength(actionBuffers[++i]);
        bpDict[leg3Upper].SetJointStrength(actionBuffers[++i]);
        bpDict[leg0Lower].SetJointStrength(actionBuffers[++i]);
        bpDict[leg1Lower].SetJointStrength(actionBuffers[++i]);
        bpDict[leg2Lower].SetJointStrength(actionBuffers[++i]);
        bpDict[leg3Lower].SetJointStrength(actionBuffers[++i]);
    }

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
        AddReward(0.01f * m_FacingDot );


        if (stepCallBack != null && decisionStep >= decisionPeriod)
        { 
            decisionStep = 0;
            stepCallBack();
        }
    }

    /// <summary>
    /// Update OrientationCube and DirectionIndicator
    /// </summary>
    void UpdateOrientationObjects()
    {
        m_OrientationCube.UpdateOrientation(body, m_Target);
        if (m_DirectionIndicator)
        {
            m_DirectionIndicator.MatchOrientation(m_OrientationCube.transform);
        }
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
    public float GetMatchingVelocityReward(Vector3 velocityGoal, Vector3 actualVelocity)
    {
        //distance between our actual velocity and goal velocity
        var velDeltaMagnitude = Mathf.Clamp(Vector3.Distance(actualVelocity, velocityGoal), 0, TargetWalkingSpeed);

        //return the value on a declining sigmoid shaped curve that decays from 1 to 0
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates
        return Mathf.Pow(1 - Mathf.Pow(velDeltaMagnitude / TargetWalkingSpeed, 2), 2);
    }

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
}
