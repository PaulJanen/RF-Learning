using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class FlyController : MonoBehaviour, ISpawner
{
    public BoxCollider plantsCatchBoundaries;
    public Rigidbody rb;
    public float forceMagnitude = 20f;
    public float maxVelocity = 10f;
    public bool stayStill = true;

    private float actionInterval = 0.5f;
    private float nextActionTime;
    private Vector3 currentDir = Vector3.zero;
    private float glidingDrag = 5f;
    private float evadingDrag = 10f;
    private Spawner spawner;

    private void Awake()
    {
        nextActionTime = Time.time;
        currentDir = Vector3.zero;
        rb.AddTorque(Vector3.right*100f,ForceMode.Impulse);
    }

    public void InitializeSpawnedObj(Transform parent, Spawner spawner)
    {
        plantsCatchBoundaries = parent.GetComponent<PlantAgent>().catchBoundaries;
        this.spawner = spawner;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (stayStill == false)
        {
            nextActionTime = Random.Range(0.75f * actionInterval, 1.25f * actionInterval) + Time.time;
            Vector3 newDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            currentDir = (currentDir + newDir * 0.5f).normalized;
            rb.drag = evadingDrag;
            if (plantsCatchBoundaries.bounds.Contains(transform.position) == false)
            {
                currentDir = (plantsCatchBoundaries.transform.position - transform.position).normalized;
                currentDir.y = 0;
                rb.drag = glidingDrag;
            }

            rb.AddForce(currentDir * forceMagnitude, ForceMode.Force);
        }
    }

    void OnCollisionStay(Collision other)
    {
        if(other.gameObject.tag == "ground" || other.gameObject.tag == "mouth")
        {
            //transform.DOScale(0f, 1f).OnComplete(DestroyObject);
        }
    }


    public void DestroyObject()
    {
        Destroy(transform.gameObject);
    }
}
