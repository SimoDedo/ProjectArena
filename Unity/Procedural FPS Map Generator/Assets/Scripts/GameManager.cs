using UnityEngine;

public class GameManager : CoreComponent {

    [SerializeField] private GameObject mapManager;
    [SerializeField] private GameObject spawnPointManager;

    [SerializeField] private GameObject player;

    // Do I have to assemble the map?
    [SerializeField] private bool assembleMap = true;

    private MapManager mapManagerScript;
    private SpawnPointManager spawnPointManagerScript;

    private void Start () {
        /* #if UNITY_EDITOR
            UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        #endif */

        mapManagerScript = mapManager.GetComponent<MapManager>();
        spawnPointManagerScript = spawnPointManager.GetComponent<SpawnPointManager>();
    }

    private void Update () {
        if (!IsReady() && mapManagerScript.IsReady() && spawnPointManagerScript.IsReady()) {
            // Generate the map.
            mapManagerScript.ManageMap(assembleMap);

            if (assembleMap) {
                // Set the spawn points.
                spawnPointManagerScript.SetSpawnPoints(mapManagerScript.GetSpawnPoints());

                player.transform.position = spawnPointManagerScript.GetSpawnPosition() + Vector3.up * 3f;
            }

            SetReady(true);
        }
    }

}