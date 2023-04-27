
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    //protected List<GameObject> instantiatedPrefabs;
    protected GameObject prefab;

    public virtual Transform Spawn()
    {
        return null;
    }
   

    public void Restart()
    {
        if(prefab != null)
            Destroy(prefab);
    }
}
