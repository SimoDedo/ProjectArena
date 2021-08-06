using System;
using UnityEngine;

// TODO the circularQueue is dependent on the FPS of the game. Bad refresh rate will cause the positions
// to go even more in the past
public class PositionTracker : MonoBehaviour
{
    private Transform t;

    // Considering 60 FPS, we save position up to the half a second ago
    private Vector3[] positionsCircularQueue = new Vector3[30];
    private int queueHead = 0;

    private void Start()
    {
        t = transform;
    }

    private void Update()
    {
        positionsCircularQueue[++queueHead % 30] = t.position;
    }

    public Vector3 GetPositionFromIndex(int index)
    {
        return positionsCircularQueue[(queueHead - index + 30) % 30];
    }
}