using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyController : MonoBehaviour
{
    public BoxCollider plantsCatchBoundaries;
    public Rigidbody rb;
    public float forceMagnitude = 20f;
    public float maxVelocity = 10f;

    private float actionInterval = 0.5f;
    private float nextActionTime;
    private Vector3 currentDir = Vector3.zero;
    private float glidingDrag = 5f;
    private float evadingDrag = 10f;

    private void Awake()
    {
        nextActionTime = Time.time;
        currentDir = Vector3.zero;
        rb.AddTorque(Vector3.right*100f,ForceMode.Impulse);
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        nextActionTime = Random.Range(0.75f * actionInterval, 1.25f * actionInterval) + Time.time;
        Vector3 newDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
        currentDir = (currentDir+newDir*0.5f).normalized;
        rb.drag = evadingDrag;
        if (plantsCatchBoundaries.bounds.Contains(transform.position) == false)
        {
            currentDir = (plantsCatchBoundaries.transform.position - transform.position).normalized;
            currentDir.y = 0;
            rb.drag = glidingDrag;
        }

        rb.AddForce(currentDir * forceMagnitude, ForceMode.Force);

        /*if (plantsCatchBoundaries.bounds.Contains(transform.position) == false)
        {
            nextActionTime = Time.time;
        }*/
    }

    void OnCollisionStay(Collision other)
    {
        if(other.gameObject.tag == "ground")
        {
            Destroy(transform.gameObject);
        }
    }
}
