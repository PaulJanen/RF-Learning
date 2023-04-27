using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralSpawner : Spawner
{
    public Transform centralPoint;
    public Transform pan;
    public float minSpawnRadius = 1f;
    public float maxSpawnRadius = 1f;
    public float highDistanceMin = 0f;
    public float highDistanceMax = 0f;
    public float angle = 45f;

    public override Transform Spawn()
    {
        //Vector3 x = Random.Range(minPos.x, maxPos.x) * parent.right;
        //Vector3 y = Random.Range(minPos.y, maxPos.y) * parent.up;
        //Vector3 z = Random.Range(minPos.z, maxPos.z) * parent.forward;
        //Vector3 finalPos = x + y + z + parent.position;
        float rad = Random.Range(0, angle) * Mathf.Deg2Rad;
        Vector3 position = centralPoint.right * Mathf.Sin(rad) + centralPoint.forward * Mathf.Cos(rad);
        float spawnRadius = Random.Range(minSpawnRadius, maxSpawnRadius);
        Vector3 finalPos = centralPoint.position + position * spawnRadius;

        finalPos.y = centralPoint.position.y +
            Mathf.Lerp(highDistanceMin, highDistanceMax, (spawnRadius- minSpawnRadius) /(maxSpawnRadius - minSpawnRadius));
        //Vector3 direction = Random.onUnitSphere;
        //direction.y = 0f;
        //Vector3 finalPos = (direction.normalized * Random.Range(minSpawnRadius, maxSpawnRadius)) + parent.position;
        pan.position = finalPos;
        return pan;
    }
}
