using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CookingFood : MonoBehaviour
{

    public string contanctTag;
    public bool touchingTag;
    public List<ParticleSystem> particleSystem;
    private float timeElapsed;
    public float stopCookingAfter = 1f;
    IEnumerator stopCookingAfterCoroutine;

    private void Update()
    {
        if (touchingTag)
        {
            if (stopCookingAfterCoroutine != null)
                StopCoroutine(stopCookingAfterCoroutine);

            for (int i = 0; i < particleSystem.Count; i++)
            {
                if(particleSystem[i].isPlaying == false)
                    particleSystem[i].Play();
            }
        } 
        else
        {
            
        }
    }

    private void OnCollisionStay(Collision col)
    {
        if (col.transform.CompareTag(contanctTag))
        {
            touchingTag = true;
            timeElapsed = 0;
        }
    }

    void OnCollisionExit(Collision other)
    {
        if (other.transform.CompareTag(contanctTag))
        {
            touchingTag = false;

            if (stopCookingAfterCoroutine != null)
                StopCoroutine(stopCookingAfterCoroutine);
            stopCookingAfterCoroutine = StopCooking();
            StartCoroutine(stopCookingAfterCoroutine);
        }
    }

    IEnumerator StopCooking()
    {
        while (timeElapsed < stopCookingAfter)
        {
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < particleSystem.Count; i++)
        {
            if (particleSystem[i].isPlaying == true)
                particleSystem[i].Stop();
        }
    }
}
