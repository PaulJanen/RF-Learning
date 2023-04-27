using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Truck : MonoBehaviour, IInitialize
{
    public float maxWeight;
    public float currentWeight;
    public float fuel;
    public float tireDegradation;

    public LogisticsNode target;

    public void Initialize()
    {
        
    }
}
