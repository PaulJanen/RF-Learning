using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent2 : MonoBehaviour
{
    public List<double> currentStateData;
    public double m_Reward;
    public bool done;
    public int decisionStep = 0;
    public Action stepCallBack;
    protected JointDriveController2 jdController;
    public bool stopTraining = false;


    public virtual void OnEpisodeBegin()
    {

    }

    public virtual void CollectObservations()
    {

    }

    public virtual void ActionReceived(List<double> actionBuffers)
    {

    }

    public virtual void EndEpisode()
    {
        done = true;
        stopTraining = true;
        decisionStep = 0;
        Debug.Log("end episode");
        if (stepCallBack != null)
        {
            Debug.Log("callback called");
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


    public void FreezeRigidBody(bool freeze)
    {
        for (int i = 0; i < jdController.bodyPartsList.Count; i++)  
        {
            if (freeze)
            {
                stopTraining = true;
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
                Debug.Log("unfreeze body");
                stopTraining = false;
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
