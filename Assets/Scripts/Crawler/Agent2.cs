using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent2 : MonoBehaviour
{
    public double m_Reward;
    public bool done;
    public int decisionStep = 0;
    public Action stepCallBack;

    public void SetReward(float reward)
    {
        //Utilities.DebugCheckNanAndInfinity(reward, "reward", "SetReward");
        //m_CumulativeReward += reward - m_Reward;
        m_Reward = reward;
        //m_Reward += reward;
    }

    public virtual void EndEpisode()
    {
        done = true;
        decisionStep = 0;
        if (stepCallBack != null)
        {
            stepCallBack();
        }
    }
}
