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

    public UnityEvent rightAPressed0Time;
    public UnityEvent rightAPressed1Time;
    public UnityEvent rightAPressed2Time;
    public UnityEvent rightAPressed3Time;
    public UnityEvent rightAPressed4Time;
    public int pressCount;

    private void Awake()
    {
        pressCount = 0;
    }

    void Start()
    {
        HVRInputManager.Instance.RightController.PrimaryButtonState.activated = APressed;
    }


    private void APressed()
    {
        if (pressCount == 0)
            rightAPressed0Time.Invoke();
            if (pressCount == 1)
            rightAPressed1Time.Invoke();
        else if(pressCount == 2)
            rightAPressed2Time.Invoke();
        else if(pressCount==3)
            rightAPressed3Time.Invoke();
        else if(pressCount==4)
            rightAPressed4Time.Invoke();
        pressCount++;
    }
}
