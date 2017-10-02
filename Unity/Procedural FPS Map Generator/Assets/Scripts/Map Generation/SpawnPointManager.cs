using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnPointManager : CoreComponent {

    [SerializeField] private float spawnCooldown = 5f;

    // List of all the spawn points.
    private List<SpawnPoint> spawnPoints;

    // Use this for initialization.
    private void Start() {
        spawnPoints = new List<SpawnPoint>();

        SetReady(true);
    }

    // Sets the spawn points.
    public void SetSpawnPoints(List<GameObject> SPs) {
        foreach (GameObject s in SPs) {
            SpawnPoint newSpawnPoint = new SpawnPoint();
            newSpawnPoint.spawnPosition = s.transform.position;
            newSpawnPoint.lastUsed = Time.time;
            spawnPoints.Add(newSpawnPoint);
        }
    }

    // Returns an available spawn position.
    public Vector3 GetSpawnPosition() {
        List<SpawnPoint> availableSpawnPoints = spawnPoints
            .Where(spawnPoints => spawnPoints.lastUsed > Time.time + spawnCooldown)
            .ToList();

        if (availableSpawnPoints.Count == 0)
            return GetRandomSpawnPoint(spawnPoints).spawnPosition;
        else
            return GetRandomSpawnPoint(availableSpawnPoints).spawnPosition;
    }

    // Returns a random spawn point from a list.
    private SpawnPoint GetRandomSpawnPoint(List<SpawnPoint> SPs) {
        return SPs[UnityEngine.Random.Range(0, SPs.Count)];
    }

    private struct SpawnPoint {
        public Vector3 spawnPosition;
        public float lastUsed;
    }

}