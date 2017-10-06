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
            spawnPoints.Add(new SpawnPoint(s.transform.position, Time.time - spawnCooldown));
        }
    }

    // Returns an available spawn position.
    public Vector3 GetSpawnPosition() {
        List<SpawnPoint> availableSpawnPoints = spawnPoints
            .Where(spawnPoints => Time.time - spawnPoints.lastUsed >= spawnCooldown)
            .ToList();

        if (availableSpawnPoints.Count == 0)
            return GetRandomSpawnPoint(spawnPoints).spawnPosition;
        else
            return GetRandomSpawnPoint(availableSpawnPoints).spawnPosition;
    }

    // Returns a random spawn point from a list.
    private SpawnPoint GetRandomSpawnPoint(List<SpawnPoint> SPs) {
        SpawnPoint sp = SPs[UnityEngine.Random.Range(0, SPs.Count)];
        sp.lastUsed = Time.time;
        return sp;
    }

    private class SpawnPoint {
        public Vector3 spawnPosition;
        public float lastUsed;

        public SpawnPoint(Vector3 sp, float lu) {
            spawnPosition = sp;
            lastUsed = lu;
        }
    }

}