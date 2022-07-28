using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrientationCubeController2 : MonoBehaviour
{

    public void Initialize(Transform root)
    {
        transform.position = root.position;
    }

    public void UpdateOrientation(Transform rootBP, Transform target)
    {
        var dirVector = target.position - transform.position;
        
        var lookRot =
            dirVector == Vector3.zero
                ? Quaternion.identity
                : Quaternion.LookRotation(dirVector); //get our look rot to the target

        //UPDATE ORIENTATION CUBE POS & ROT
        transform.SetPositionAndRotation(rootBP.position, lookRot);
    }
}
