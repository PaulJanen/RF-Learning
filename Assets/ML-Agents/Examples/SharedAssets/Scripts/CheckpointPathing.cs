using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointPathing : MonoBehaviour
{

    public List<Transform> checkpoints;
    public Transform mark;
    public Transform chaser;
    public float distance = 20f;
    private int index = 0;

    private void Awake()
    {
        UpdateCheckpointMark();
    }

    // Update is called once per frame
    void Update()
    {
        float distanceBetween = Vector3.Distance(mark.position, chaser.position);
        if (distanceBetween < distance)
            UpdateCheckpointMark();
    }


    void UpdateCheckpointMark()
    {
        if(checkpoints.Count < (index + 1))
            return;
        mark.position = checkpoints[index].position;
        index += 1;
    }
}
