using HurricaneVR.Framework.ControllerInput;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class VrInputEvents : MonoBehaviour
{
    //HVRInputManager.Instance.RightController.SecondaryButtonState.Active B button
    //HVRInputManager.Instance.RightController.PrimaryButtonState.Active   A button
    // Start is called before the first frame update

    public List<UnityEvent> pressEvents= new List<UnityEvent>();

    public int pressCount;


    void Start()
    {
        HVRInputManager.Instance.RightController.PrimaryButtonState.activated = APressed;
    }


    private void APressed()
    {
        if( pressEvents.Count > pressCount)
        {
            Debug.Log("press: " + pressCount);
            pressEvents[pressCount].Invoke();
        }
        pressCount++;
    }
}
