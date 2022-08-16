
using UnityEngine;

public class FlySpawner : Spawner
{
    public Fly prefabToSpawn;
    public Transform parent;
    public Vector3 minPos;
    public Vector3 maxPos;

    public override Transform Spawn()
    {
        float x = Random.Range(minPos.x, maxPos.x) + parent.position.x;
        float y = Random.Range(minPos.y, maxPos.y) + parent.position.y;
        float z = Random.Range(minPos.z, maxPos.z) + parent.position.z;
        prefab = Instantiate(prefabToSpawn.gameObject, new Vector3(x,y,z), Quaternion.identity);
        prefab.GetComponent<ISpawner>().InitializeSpawnedObj(parent, this);
        return prefab.transform;
    }
}
