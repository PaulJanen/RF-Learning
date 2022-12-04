
using System;
using UnityEngine;

public class Fly : TargetBase
{
    public Action touchedGround;
    public Action flyWasConsumed;

    protected bool isBeingConsumed = false;

    public bool IsBeingConsumed
    {
        get { return isBeingConsumed; }
        protected set { isBeingConsumed = value; }
    }


    void FixedUpdate()
    {
        UpdateBehaviour();
    }

    public virtual void UpdateBehaviour()
    {

    }

    public virtual void StartBeingConsumed()
    { 

    }

    public virtual void StopBeingConsumed()
    {

    }

    public virtual void DestroyObject()
    {

    }
}
