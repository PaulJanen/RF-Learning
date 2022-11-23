using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GivePorts : MonoBehaviour
{

    int firstPort = 5555;

    public int GetPort()
    {
        int port = firstPort;
        firstPort += 1;
        return port;
    }
}
