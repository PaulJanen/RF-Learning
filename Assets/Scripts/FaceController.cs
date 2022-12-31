using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class FaceController : MonoBehaviour
{
    [Serializable]
    public enum FaceEmotions
    {
        casual = 0,
        disgusted = 1
    }
    public FaceEmotions faceEmotions;

    private GameObject currentFace;
    public GameObject casualFace;
    public GameObject disgustedFace;


    private void Start()
    {
        currentFace = casualFace;
    }

    public void ChangeFace(string emotion)
    {
        faceEmotions = (FaceEmotions)Enum.Parse(typeof(FaceEmotions), emotion);
        currentFace.SetActive(false);
        switch (faceEmotions)
        {
            case FaceEmotions.disgusted:
                currentFace = disgustedFace; 
                break;
            case FaceEmotions.casual:
                currentFace = casualFace;
                break;

        }

        currentFace.SetActive(true);
    }
}
