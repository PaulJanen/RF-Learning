using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(RapidlyChangeTargRotation());
    }

    // Update is called once per frame
   
    
    IEnumerator RapidlyChangeTargRotation()
    {
        ConfigurableJoint joint = GetComponent<ConfigurableJoint>();
        float timeWait = 0f;
        Vector3 newPos = Vector3.zero;
        while(true)
        {
            timeWait = Random.Range(0.1f, 0.1f);
            newPos = new Vector3(
            Random.Range(joint.lowAngularXLimit.limit, joint.highAngularXLimit.limit)
            , Random.Range(0, joint.angularYLimit.limit)
            , Random.Range(0, joint.angularZLimit.limit)
            );
            joint.targetRotation = Quaternion.Euler(newPos);
            yield return Time.deltaTime;
        }
    }
}
