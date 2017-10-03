using UnityEngine;

// The game manager manages the game, it passes itself to the player.

public class GameManager : CoreComponent {

    [SerializeField] private GameObject mapManager;
    [SerializeField] private GameObject spawnPointManager;
    [SerializeField] private GameObject gameGUIManager;

    [SerializeField] private GameObject player;

    [SerializeField] private string playerName = "Player 1";
    [SerializeField] private string opponentName = "Player 2";
    [SerializeField] private int gameDuration = 600;

    // Do I have to assemble the map?
    [SerializeField] private bool assembleMap = true;

    private MapManager mapManagerScript;
    private SpawnPointManager spawnPointManagerScript;
    private GameGUIManager gameGUIManagerScript;

    private PlayerController playerControllerScript;

    // Last time acquired.
    private int pastTime;
    // Current phase of the game: 0 = ready, 1 = figth, 2 = score.
    private int gamePhase;

    private void Start () {
        /* #if UNITY_EDITOR
            UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        #endif */

        mapManagerScript = mapManager.GetComponent<MapManager>();
        spawnPointManagerScript = spawnPointManager.GetComponent<SpawnPointManager>();
        gameGUIManagerScript = gameGUIManager.GetComponent<GameGUIManager>();

        playerControllerScript = player.GetComponent<PlayerController>();
    }

    private void Update () {
        if (!IsReady() && mapManagerScript.IsReady() && spawnPointManagerScript.IsReady() && gameGUIManagerScript.IsReady()) {
            // Generate the map.
            mapManagerScript.ManageMap(assembleMap);

            if (assembleMap) {
                // Set the spawn points.
                spawnPointManagerScript.SetSpawnPoints(mapManagerScript.GetSpawnPoints());

                player.transform.position = spawnPointManagerScript.GetSpawnPosition() + Vector3.up * 3f;
                playerControllerScript.LockCursor();
            }

            pastTime = (int) Time.time;

            SetReady(true);

            playerControllerScript.SetMovementEnabled(true);
        } else if (IsReady()) {
            // Update the elapsed time.
        }
    }

}