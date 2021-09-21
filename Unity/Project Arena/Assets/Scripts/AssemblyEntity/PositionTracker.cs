using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// TODO the circularQueue is dependent on the FPS of the game. Bad refresh rate will cause the positions
// to go even more in the past
public class PositionTracker : MonoBehaviour
{
    private Transform t;

    // Considering 60 FPS, we save position up to the half a second ago
    private List<Tuple<Vector3, float>> positions = new List<Tuple<Vector3, float>>();
    private const float MEMORY_WINDOW = 0.5f;
    
    private void Start()
    {
        t = transform;
    }

    private void LateUpdate()
    {
        UpdateList();
    }

    public Vector3 GetPositionFromDelay(float delay)
    {
        UpdateList();
        var timeToSearch = Time.time - Mathf.Max(0, delay);
        // Step 1: find the next time instant saved after the delay
        var (afterPosition, afterTime) = positions.First(it => it.Item2 >= timeToSearch);
        // Step 2: find the previous time instant saved, if present, otherwise choose previous point again

        var (beforePosition, beforeTime) = positions.First().Item2 < timeToSearch ? positions.Last(it => it.Item2 < timeToSearch) : new Tuple<Vector3, float>(afterPosition, timeToSearch);

        // Step 3: interpolate the two

        var interpolatedPos = Vector3.Lerp(beforePosition, afterPosition,
            (timeToSearch - beforeTime) / (afterTime - beforeTime));

        // Return interpolation
        return interpolatedPos;
    }

    private void UpdateList()
    {
        if (positions.Count != 0 && positions.Last().Item2 == Time.time)
            positions.RemoveAt(positions.Count-1);

        positions.Add(new Tuple<Vector3, float>(t.position, Time.time));
        positions = positions.Where(it => it.Item2 > Time.time - MEMORY_WINDOW).ToList();
    }
}