using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticFlyController : Fly, ISpawner
{
    private Spawner spawner;
    public void InitializeSpawnedObj(Transform parent, Spawner spawner, Transform target)
    {
        this.spawner = spawner;
    }

    void OnCollisionStay(Collision other)
    {
        if (other.gameObject.tag == "ground" || other.gameObject.tag == "mouth")
        {
            //transform.DOScale(0f, 1f).OnComplete(DestroyObject);
            //DestroyObject();
        }
    }


    public void DestroyObject()
    {
        Destroy(transform.gameObject);
    }
}
