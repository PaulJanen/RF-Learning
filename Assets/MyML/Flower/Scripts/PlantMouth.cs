using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantMouth : MonoBehaviour
{
    public Action callback;

    public void Restart()
    {
        callback = null;
    }


    void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.tag == "food")
        {
            Debug.Log("caught food");
            if(callback != null)
            {
                callback();
                callback = null;
            }
        }
    }

}
