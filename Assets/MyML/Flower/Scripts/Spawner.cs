
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    //protected List<GameObject> instantiatedPrefabs;
    protected GameObject prefab;

    public virtual void Spawn()
    {

    }
   

    public void Restart()
    {
        if(prefab != null)
            Destroy(prefab);
    }
}
