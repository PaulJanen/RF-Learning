using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantMouth : MonoBehaviour
{
    public Action callback;
    public bool caughtFood;
    public void Restart()
    {
        callback = null;
        caughtFood = false;
    }


    void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.tag == "food")
        {
            caughtFood = true;
            if (callback != null)
            {
                callback();
            }
        }
    }

    void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject.tag == "food")
        {
            caughtFood = false;
        }
    }

}
