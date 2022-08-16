using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class DynamicFlyController : Fly, ISpawner
{
    public bool particlesEnabled;
    public BoxCollider plantsCatchBoundaries;
    
    public GameObject deathParticles;
    public ParticleSystem glowParticles;
    public ParticleSystem chargingParticle;
    private float playbackTime;
    private IEnumerator disableParticle;
    [SerializeField]
    private float disableParticleAfter;

    public float forceMagnitude = 20f;
    public bool moveTowardsPlant = true;

    private float actionInterval = 0.5f;
    private float nextActionTime;
    private Vector3 currentDir = Vector3.zero;
    [SerializeField]
    private float glidingDrag = 5f;
    [SerializeField]
    private float evadingDrag = 10f;
    
    private IEnumerator consumed;
    private Vector3 initialSize;
    private Vector3 finallSize;
    private float timeElapsed;    
    public float shrinkLength = 1f;

    private void Awake()
    {
        nextActionTime = Time.time;
        currentDir = Vector3.zero;
        rb.AddTorque(Vector3.right*100f,ForceMode.Impulse);
        initialSize = transform.localScale;
        timeElapsed = 0;
        finallSize = Vector3.one * 0.6f;
        playbackTime = 0;
        disableParticleAfter = 0.2f;
    }

    public void InitializeSpawnedObj(Transform parent, Spawner spawner)
    {
        if(moveTowardsPlant==true)
            plantsCatchBoundaries = parent.GetComponent<PlantAgent>().catchBoundaries;
        glowParticles.gameObject.SetActive(particlesEnabled);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (moveTowardsPlant == true)
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

    void OnCollisionEnter(Collision other)
    {
        if(other.gameObject.tag == "ground" || other.gameObject.tag == "pot")
        {
            if(touchedGround!=null)
                touchedGround();
        }
        else if(other.gameObject.tag == "mouth")
        {

        }
    }

    public override void StartBeingConsumed()
    {
        isBeingConsumed = true;

        if(disableParticle!= null)
            StopCoroutine(disableParticle);

        if (particlesEnabled)
            chargingParticle.time = playbackTime;
            chargingParticle.gameObject.SetActive(true);

        consumed = ShrinkObject();
        StartCoroutine(consumed);
    }

    public override void StopBeingConsumed()
    {
        disableParticle = DisableParticle();
        StartCoroutine(disableParticle);
        StopCoroutine(consumed);
        isBeingConsumed = false;
    }

    IEnumerator ShrinkObject()
    {
        while (timeElapsed < shrinkLength)
        {
            Vector3 newSize = Vector3.Lerp(initialSize, finallSize, timeElapsed / shrinkLength);
            transform.localScale = newSize;
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        if(flyWasConsumed!=null)
            flyWasConsumed();

        DestroyObject();
    }

    IEnumerator DisableParticle()
    {
        float deltaTime = 0f;
        while (deltaTime < disableParticleAfter)
        {
            playbackTime = chargingParticle.time;
            deltaTime += Time.deltaTime;
            yield return null;
        }

        playbackTime = chargingParticle.time;
        if(particlesEnabled)
            chargingParticle.gameObject.SetActive(false);
    }

    public override void DestroyObject()
    {
        if(disableParticle!=null)
            StopCoroutine(disableParticle);
        if (particlesEnabled)
        {
            chargingParticle.gameObject.SetActive(false);
            Instantiate(deathParticles, transform.position, Quaternion.identity);
        }
        Destroy(transform.gameObject);
    }
}
