
using UnityEngine;

public class FlySpawner : Spawner
{
    public Fly prefabToSpawn;
    public Transform parent;
    public Transform target;
    public Vector3 minPos;
    public Vector3 maxPos;
    public float minSpawnRadius = 1f;
    public float maxSpawnRadius = 1f;
    public float angle = 45f;

    public override Transform Spawn()
    {
        //Vector3 x = Random.Range(minPos.x, maxPos.x) * parent.right;
        //Vector3 y = Random.Range(minPos.y, maxPos.y) * parent.up;
        //Vector3 z = Random.Range(minPos.z, maxPos.z) * parent.forward;
        //Vector3 finalPos = x + y + z + parent.position;
        float rad = angle * Mathf.Deg2Rad;
        Vector3 position = parent.right * Mathf.Sin(rad) + parent.forward * Mathf.Cos(rad);
        Vector3 finalPos = parent.position + position * Random.Range(minSpawnRadius, maxSpawnRadius);
        finalPos.y = minPos.y;
        //Vector3 direction = Random.onUnitSphere;
        //direction.y = 0f;
        //Vector3 finalPos = (direction.normalized * Random.Range(minSpawnRadius, maxSpawnRadius)) + parent.position;
        prefab = Instantiate(prefabToSpawn.gameObject, finalPos, Quaternion.identity);
        prefab.GetComponent<ISpawner>().InitializeSpawnedObj(parent, this, target);
        return prefab.transform;
    }
}
