using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    public List<GameObject> environments;
    // Start is called before the first frame update

    public void InitializeEnvironmentRandomly()
    {
        int environment = Random.Range(0, environments.Count);

        for (int i = 0; i < environments.Count; i++)
        {
            if (i == environment)
                environments[i].SetActive(true);
            else
                environments[i].SetActive(false);
        }
    }
}
