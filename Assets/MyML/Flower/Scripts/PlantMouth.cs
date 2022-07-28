using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantMouth : MonoBehaviour
{
    public Action callback;

    private void Restart()
    {
        callback = null;
    }

    private void Update()
    {
        Debug.Log("activeS");
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.tag == "food")
        {
            if(callback != null)
            {
                callback();
                callback = null;
            }
        }
    }

}
