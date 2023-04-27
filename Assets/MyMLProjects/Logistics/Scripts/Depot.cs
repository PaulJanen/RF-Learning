using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Depot : LogisticsNode, IInitialize
{

    public int truckCount = 0;
    public Truck truckPrefab;
    public List<Truck> trucks;


    public void Initialize()
    {
        
    }

    Truck SpawnTrucks()
    {
        Truck truck = Instantiate(truckPrefab, transform.position, Quaternion.identity);
        return truck;
    }

    void CreatePayload()
    {
        for (int i = 0; i < truckCount; i++)
        {
            Truck truck = SpawnTrucks();
            
        }
    }
}
