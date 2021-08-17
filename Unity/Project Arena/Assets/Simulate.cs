using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows running with update interval fixed. Coupled with running the build with
/// -batchmode -nographics, it makes the whole game run faster without loss of quality in
/// pathfinding or any other kind of logic that requires some time. 
/// </summary>
public class Simulate : MonoBehaviour
{

    public float captureDeltaTime = 0.33f;
    public float timescale = 2;

    private float oldCaptureDeltaTime;
    private float oldTimescale;
    void Update()
    {
        Debug.Log("Update with " + Time.deltaTime);
        if (captureDeltaTime != oldCaptureDeltaTime)
            Time.captureDeltaTime = captureDeltaTime;
        if (timescale != oldTimescale)
            Time.timeScale = timescale;

        oldTimescale = timescale;
        oldCaptureDeltaTime = captureDeltaTime;
    }

    private void FixedUpdate()
    {
        Debug.Log("FixedUpdate with " + Time.fixedDeltaTime);
    }
}
