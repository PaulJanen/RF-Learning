using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using System;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Data
{
    public string command;
    public double[] state;
    public double[] actions;
    public double reward;
    public bool done;
}

public class Client : MonoBehaviour
{
    private bool receiveMessage = true;

    Action callback;

    public Agent2 agent;
    public ResponseSocket _server;
    public GivePorts portGiver;

    // Start is called before the first frame update
    void Start()
    {
        int _port = portGiver.GetPort();

        if (agent.testingModel == true)
        {
            receiveMessage = false;
            return;
        }

        ForceDotNet.Force();
        NetMQConfig.Linger = new TimeSpan(0, 0, 1);

        _server = new ResponseSocket();
        _server.Options.Linger = new TimeSpan(0, 0, 1);
        _server.Bind($"tcp://*:{_port}");

        Assert.IsNotNull(_server);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (receiveMessage)
        {
            ReceiveMessage();
        }
    }

    void ReceiveMessage()
    {
        string recv = "";
        _server.TryReceiveFrameString(out recv);
        Data data = JsonUtility.FromJson<Data>(recv);
        if (data != null)
        {
            agent.FreezeRigidBody(false);
            receiveMessage = false;
            switch (data.command)
            {
                case "Reset":
                    ResetCommand();
                    break;
                case "Step":
                    StepCommand(data);
                    break;
                case "DoneTraining":
                    DoneTrainingCommand();
                    break;
                default:
                    break;
            }
        }
        else
            agent.FreezeRigidBody(true);
    }

    private void ResetCommand()
    {
        agent.OnEpisodeBegin();
        agent.CollectObservations();

        Data data = new Data();
        data.command = "Reset";
        data.state = agent.currentStateData.ToArray();
        string send = JsonUtility.ToJson(data);
        _server.SendFrame(send);
        receiveMessage = true;
    }
    
    private void StepCommand(Data data)
    {
        if (agent.done == false)
        {
            agent.stepCallBack = SendStepInfo;
            agent.ActionReceived(data.actions.ToList());
        }
        else
            SendStepInfo();
    }

    private void DoneTrainingCommand()
    {
        receiveMessage = false;
        Data data = new Data();
        data.command = "DoneTraining";
        string send = JsonUtility.ToJson(data);
        _server.SendFrame(send);
    }

    private void SendStepInfo()
    {
        agent.stepCallBack = null;
        Data data = new Data();
        agent.CollectObservations();
        data.state = agent.currentStateData.ToArray();
        data.reward = agent.m_Reward;
        data.done = agent.done;
        data.command = "Step";
        agent.m_Reward = 0;
        string send = JsonUtility.ToJson(data);
        _server.SendFrame(send);
        receiveMessage = true;
    }
    
    void OnDisable()
    {
        _server?.Dispose();
        NetMQConfig.Cleanup(false);
    }
    
}
