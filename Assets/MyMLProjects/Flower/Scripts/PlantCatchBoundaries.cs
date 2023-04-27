using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantCatchBoundaries : MonoBehaviour
{
    public BoxCollider catchBox;
    public List<Transform> foodInBoundarie = new List<Transform>();
    public Fly restingPositionFly;
    public Fly currentActiveFood;


    private void Awake()
    {
        catchBox = GetComponent<BoxCollider>();
        currentActiveFood = restingPositionFly;
    }
    /*
     void OnTriggerEnter(Collider other)
     {
         if (other.gameObject.tag == "food" && other.gameObject != restingPositionFly.gameObject)
         {

             if (foodInBoundarie.Contains(other.transform) == false)
             {
                 foodInBoundarie.Add(other.transform);
                 if (currentActiveFood == restingPositionFly)
                 {
                     Debug.Log("getting food");
                     GetFood();
                 }
             }
         }
     }
    */

  

    /*
    void OnTriggerExit(Collider other)
    {
        if (other.transform == currentActiveFood.transform)
        {
            Debug.Log("exit condition matched");
            currentActiveFood = restingPositionFly;

            if (InsideCatchingBox(currentActiveFood.transform) == false)
            {
                currentActiveFood = restingPositionFly;
            }
        }


    }
    */

    /*
     public Fly GetFood()
     {
         currentActiveFood = restingPositionFly;
         if (foodInBoundarie.Count > 0)
         {
             Transform fly = foodInBoundarie[foodInBoundarie.Count - 1];
             if (fly == null) 
             {
                 foodInBoundarie.RemoveAt(foodInBoundarie.Count - 1);
                 return restingPositionFly;
             }

             if (IsInsideCatchBox(fly) == false)
             {
                 foodInBoundarie.RemoveAt(foodInBoundarie.Count - 1);
                 return restingPositionFly;
             }
             foodInBoundarie.RemoveAt(foodInBoundarie.Count - 1);
             currentActiveFood = fly.GetComponent<Fly>();
             Debug.Log("what?????");
             return currentActiveFood;
         }
         return restingPositionFly;
     }
    */

    public Fly GetFood()
    {
        Collider[] hitColliders = Physics.OverlapBox(gameObject.transform.position, transform.localScale / 2);
        for(int i = 0; i < hitColliders.Length; i++)
        {
            if(hitColliders[i].gameObject.tag == "food")
            {
                currentActiveFood = hitColliders[i].GetComponent<Fly>();
                return currentActiveFood;
            }
        }
        currentActiveFood = restingPositionFly;
        return restingPositionFly;
    }

    public bool InsideCatchingBox(Transform obj)
    {
        Collider[] hitColliders = Physics.OverlapBox(gameObject.transform.position, transform.localScale / 2);
        for (int i = 0; i < hitColliders.Length; i++)
        {
            if (hitColliders[i].transform == obj)
            {
                return true;
            }
        }
        return false;
    }
}
