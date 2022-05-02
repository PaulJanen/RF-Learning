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

public class CrawlerClient : MonoBehaviour
{
    private bool receiveMessage = true;
    public string tcp = "tcp://localhost:5555";

    public int _port = 5555;
    Action callback;

    public CrawlerAgent2 agent;
    public ResponseSocket _server;

    // Start is called before the first frame update
    void Start()
    {
        ForceDotNet.Force();
        NetMQConfig.Linger = new TimeSpan(0, 0, 1);

        _server = new ResponseSocket();
        _server.Options.Linger = new TimeSpan(0, 0, 1);
        _server.Bind($"tcp://*:{_port}");

        Assert.IsNotNull(_server);
        receiveMessage = true;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (receiveMessage)
            ReceiveMessage();
    }

    void ReceiveMessage()
    {
        string recv = _server.ReceiveFrameString();
        Data data = JsonUtility.FromJson<Data>(recv);
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

    private void ResetCommand()
    {
        agent.OnEpisodeBegin();
        agent.CollectObservations();
        receiveMessage = true;

        Data data = new Data();
        data.command = "Reset";
        data.state = agent.currentStateData.ToArray();
        string send = JsonUtility.ToJson(data);
        _server.SendFrame(send);
    }
    
    private void StepCommand(Data data)
    {
        receiveMessage = false;
        agent.ActionReceived(data.actions.ToList());
        agent.stepCallBack = SendStepInfo;
        Debug.Log("callback initialized, " + agent.decisionStep);
    }

    private void DoneTrainingCommand()
    {
        Debug.Log("done training!");
        receiveMessage = false;
        Data data = new Data();
        string send = JsonUtility.ToJson(data);
        _server.SendFrame(send);
    }

    private void SendStepInfo()
    {
        receiveMessage = true;
        agent.stepCallBack = null;
        Data data = new Data();
        agent.CollectObservations();
        data.state = agent.currentStateData.ToArray();
        data.reward = agent.m_Reward;
        data.done = agent.done;
        data.command = "Step";
        agent.m_Reward = 0;
        string send = JsonUtility.ToJson(data);
        Debug.Log("sending step info");
        _server.SendFrame(send);
        Debug.Log("step info sent");
    }


    void ReceiveMessageDeprecated()
    {
        double[] inputVecotr = new double[] { };
        double[] outputVector;
        
        ForceDotNet.Force(); // this line is needed to prevent unity freeze after one use, not sure why yet
        using (ResponseSocket client = new ResponseSocket(tcp))
        {
            Debug.Log("Start");
            client.Options.Linger = TimeSpan.Zero;
            client.Bind(tcp);
            string message = client.ReceiveFrameString();
            Debug.Log("Received: " + message);
      
            message = "Client is talking";
            client.SendFrame(message);
            //Debug.Log("Sending");
            //var byteArray = new byte[inputVecotr.Length * sizeof(double)];
            //Buffer.BlockCopy(inputVecotr, 0, byteArray, 0, byteArray.Length);
            //client.SendFrame(byteArray);

            //byte[] outputBytes = client.ReceiveFrameBytes();
            //outputVector = new double[outputBytes.Length / sizeof(double)];
            //Buffer.BlockCopy(outputBytes, 0, outputVector, 0, outputBytes.Length);
        }
        Debug.Log("Finished");
        NetMQConfig.Cleanup();
    }
    
    void OnDisable()
    {
        _server?.Dispose();
        NetMQConfig.Cleanup(false);
    }
    
}
