using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicFlyControllerForCal : DynamicFlyController, ISpawner
{
    public override void UpdateBehaviour()
    {
        Vector3 newDir = (target.position - transform.position).normalized;
        newDir.y = 0;
        rb.AddForce(newDir * forceMagnitude, ForceMode.Force);
    }

    public new void InitializeSpawnedObj(Transform parent, Spawner spawner, Transform target)
    {
        this.target = target;
        glowParticles.gameObject.SetActive(particlesEnabled);
    }
}
