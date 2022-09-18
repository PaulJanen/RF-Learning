using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WitchHutAgent : Agent2
{

    [Header("Body Parts")]
    [Space(10)]
    public Transform hut;
    public Transform LeftTopLeg;
    public Transform LeftBottomLeg;
    public Transform LeftLegToeBack;
    public Transform LeftLegToeLeft;
    public Transform LeftLegToeMiddle;
    public Transform LeftLegToeRight;
    public Transform RightTopLeg;
    public Transform RightBottomLeg;
    public Transform RightLegToeBack;
    public Transform RightLegToeLeft;
    public Transform RightLegToeMiddle;
    public Transform RightLegToeRight;

    //This will be used as a stabilized model space reference point for observations
    //Because ragdolls can move erratically during training, using a stabilized reference transform improves learning
    public OrientationCubeController2 m_OrientationCube;

    public override void Initialize()
    {
        m_OrientationCube.Initialize(hut);
        base.Initialize();

    }
}
