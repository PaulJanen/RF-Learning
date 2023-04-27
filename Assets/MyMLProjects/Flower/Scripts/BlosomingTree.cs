using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlosomingTree : MonoBehaviour
{

    public List<Rigidbody> food;

    // Start is called before the first frame update
    void Awake()
    {
        for (int i = 0; i < food.Count; i++)
        {
            food[i].drag = Random.Range(27, 32);
            food[i].angularDrag= Random.Range(27, 32);
            Vector3 pos = food[i].position;
            food[i].transform.position = new Vector3(pos.x, Random.Range(10f, 20f), pos.z);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
