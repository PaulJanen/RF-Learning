using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(JointDriveController2))] // Required to set joint forces
public class Agent2 : MonoBehaviour
{
    protected const int decisionPeriod = 5;
    public List<double> currentStateData;
    public double m_Reward;
    public bool done;
    public int decisionStep = 0;
    public Action stepCallBack;
    protected JointDriveController2 jdController;
    public bool freezeBody = false;
    public Transform stabilizingPivot;
    public Transform targetTransform;
    public Transform topHierarchyBodyPart;
    public bool trainingEnvironment = true;
    public bool testingModel = false;

    //This will be used as a stabilized model space reference point for observations
    //Because ragdolls can move erratically during training, using a stabilized reference transform improves learning
    public OrientationCubeController2 orientationCube;

    private void Awake()
    {
        Initialize();
    }

    protected virtual void Initialize()
    {
        jdController = GetComponent<JointDriveController2>();
        currentStateData = new List<double>();
        orientationCube.Initialize(stabilizingPivot);
    }

    public virtual void OnEpisodeBegin()
    {
        done = false;
        freezeBody = false;
        decisionStep = 0;

        foreach (var bodyPart in jdController.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }
        SpawnTarget();
        orientationCube.UpdateOrientation(stabilizingPivot, targetTransform);
    }

    public virtual void CollectObservations()
    {

    }

    public virtual void ActionReceived(List<double> actionBuffers)
    {

    }

    protected virtual void SpawnTarget()
    {

    }

    public virtual void EndEpisode()
    {
        if (trainingEnvironment == false)
            return;

        done = true;
        freezeBody = true;
        decisionStep = 0;
        if (stepCallBack != null)
        {
            stepCallBack();
        }
    }

    public void SetReward(float reward)
    {
        //Utilities.DebugCheckNanAndInfinity(reward, "reward", "SetReward");
        //m_CumulativeReward += reward - m_Reward;
        m_Reward = reward;
        //m_Reward += reward;
    }

    public void AddReward(float increment)
    {
        //Utilities.DebugCheckNanAndInfinity(increment, "increment", "AddReward");
        m_Reward += increment;
        //m_CumulativeReward += increment;
    }

    protected void UpdateOrientationObjects()
    {
        orientationCube.UpdateOrientation(stabilizingPivot, targetTransform);
    }

    public void FreezeRigidBody(bool freeze)
    {
        TargetBase targetBase = targetTransform.GetComponent<TargetBase>();
        if (targetBase != null)
            targetBase.FreezeRigidBody(freeze);

        for (int i = 0; i < jdController.bodyPartsList.Count; i++)  
        {
            if (freeze)
            {
                freezeBody = true;
                if (jdController.bodyPartsList[i].isAlreadyFroozen == false)
                {
                    jdController.bodyPartsList[i].isAlreadyFroozen = true;
                    jdController.bodyPartsList[i].SaveVelocity();
                    jdController.bodyPartsList[i].rb.constraints = RigidbodyConstraints.FreezePosition;
                    jdController.bodyPartsList[i].rb.isKinematic = true;
                }
            }
            else
            {
                freezeBody = false;
                if (jdController.bodyPartsList[i].isAlreadyFroozen == true)
                {
                    jdController.bodyPartsList[i].isAlreadyFroozen = false;
                    jdController.bodyPartsList[i].rb.isKinematic = false;
                    jdController.bodyPartsList[i].rb.constraints = RigidbodyConstraints.None;
                    jdController.bodyPartsList[i].LoadSavedVelocity();
                }
            }
        }
    }
}
