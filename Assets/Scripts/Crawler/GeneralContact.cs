using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralContact : MonoBehaviour
{
    public Agent2 agent;

    public bool touchingTag;
    public string contanctTag = "agent"; // Tag of ground object.

    /// <summary>
    /// Check for collision with ground, and optionally penalize agent.
    /// </summary>
    void OnCollisionEnter(Collision col)
    {
        if (col.transform.CompareTag(contanctTag))
        {
            touchingTag = true;
        }
    }

    private void OnCollisionStay(Collision col)
    {
        if (col.transform.CompareTag(contanctTag))
        {
            touchingTag = true;
        }
    }

    /// <summary>
    /// Check for end of ground collision and reset flag appropriately.
    /// </summary>
    void OnCollisionExit(Collision other)
    {
        if (other.transform.CompareTag(contanctTag))
        {
            touchingTag = false;
        }
    }
}
