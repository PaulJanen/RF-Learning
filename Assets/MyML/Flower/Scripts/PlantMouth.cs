using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantMouth : MonoBehaviour
{
    public Action callback;
    public Action foodWasReleased;
    public bool caughtFood;
    public void Restart()
    {
        callback = null;
        foodWasReleased = null;
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
            
            if(foodWasReleased != null)
                foodWasReleased();
        }
    }

}
