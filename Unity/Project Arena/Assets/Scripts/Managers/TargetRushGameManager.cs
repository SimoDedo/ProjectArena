using System;
using UnityEngine;

public class TargetRushGameManager : GameManager {

    [Header("Contenders")] [SerializeField] private GameObject player;
    [SerializeField] private string playerName = "Player 1";
    [SerializeField] private int totalHealthPlayer = 100;
    [SerializeField] private bool[] activeGunsPlayer;
    [SerializeField] private Wave[] waveList;

    [Header("Target Rush variables")] [SerializeField] protected GameObject targetRushGameUIManager;

    private TargetRushGameUIManager targetRushGameUIManagerScript;

    private Player playerScript;
    private int playerScore = 0;
    private int playerID = 1;

    private void Start() {
        /* #if UNITY_EDITOR
            UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        #endif */

        mapManagerScript = mapManager.GetComponent<MapManager>();
        spawnPointManagerScript = spawnPointManager.GetComponent<SpawnPointManager>();
        targetRushGameUIManagerScript = targetRushGameUIManager.GetComponent<TargetRushGameUIManager>();

        playerScript = player.GetComponent<Player>();

        targetRushGameUIManagerScript.Fade(0.5f, 1f, true, 0.5f);
    }

    private void Update() {
        if (!IsReady() && mapManagerScript.IsReady() && spawnPointManagerScript.IsReady() && targetRushGameUIManagerScript.IsReady()) {
            // Generate the map.
            mapManagerScript.ManageMap(true);

            // Set the spawn points.
            if (!generateOnly)
                generateOnly = spawnPointManagerScript.SetSpawnPoints(mapManagerScript.GetSpawnPoints());

            if (!generateOnly) {
                // Spawn the player.
                Spawn(player);

                // Setup the contenders.
                playerScript.SetupEntity(totalHealthPlayer, activeGunsPlayer, this, playerID);

                playerScript.LockCursor();
                startTime = Time.time;
            }

            SetReady(true);
        } else if (IsReady() && !generateOnly) {
            ManageGame();
        }
    }

    // Updates the phase of the game.
    protected override void UpdateGamePhase() {
        int passedTime = (int)(Time.time - startTime);

        if (gamePhase == -1) {
            // Disable the player movement and interactions, activate the ready UI, set the name of the players and set the phase.
            playerScript.SetInGame(false);
            // TODO
            targetRushGameUIManagerScript.ActivateReadyUI();
            gamePhase = 0;
        } else if (gamePhase == 0 && passedTime >= readyDuration) {
            // Enable the player movement and interactions, activate the figth UI, set the kills to zero and set the phase.
            targetRushGameUIManagerScript.Fade(0.5f, 0f, false, 0.25f);
            // TODO
            playerScript.SetInGame(true);
            targetRushGameUIManagerScript.ActivateFigthUI();
            gamePhase = 1;
        } else if (gamePhase == 1 && passedTime >= readyDuration + gameDuration) {
            // Disable the player movement and interactions, activate the score UI, set the winner and set the phase.
            playerScript.SetInGame(false);
            // TODO
            targetRushGameUIManagerScript.ActivateScoreUI();
            gamePhase = 2;
        } else if (gamePhase == 2 && passedTime >= readyDuration + gameDuration + scoreDuration) {
            Application.Quit();
        }
    }

    // Manages the gamed depending on the current time.
    protected override void ManageGame() {
        UpdateGamePhase();

        switch (gamePhase) {
            case 0:
                // Update the countdown.
                targetRushGameUIManagerScript.SetCountdown((int)(startTime + readyDuration - Time.time));
                break;
            case 1:
                // Update the time.
                // TODO
                // Menage the waves.
                // TODO
                // Pause or unpause if needed.
                if (Input.GetKeyDown(KeyCode.Escape))
                    Pause();
                break;
            case 2:
                // Do nothing.
                break;
        }
    }
    // Sets the color of the UI.
    public override void SetUIColor(Color c) {
        targetRushGameUIManagerScript.SetColorAll(c);
    }

    public override void AddScore(int killerIdentifier, int killedID) { }

    public override void AddScore(int score) {
        playerScore += score;
    }

    // Pauses and unpauses the game.
    public override void Pause() {
        if (isPaused) {
            targetRushGameUIManagerScript.Fade(0f, 0.5f, true, 0.25f);
            targetRushGameUIManagerScript.ActivatePauseUI(false);
            playerScript.EnableInput(true);
        } else {
            targetRushGameUIManagerScript.Fade(0f, 0.5f, false, 0.25f);
            targetRushGameUIManagerScript.ActivatePauseUI(true);
            playerScript.EnableInput(false);
        }

        isPaused = !isPaused;
    }

    // Target object. 
    [Serializable]
    private struct Wave {
        // Target prefab. 
        public Target[] targetList;
    }

    [Serializable]
    // Target prefab. 
    private struct Target {
        // Prefab of the target.
        public GameObject prefab;
        // Number of prefabs to be spawn.
        public int count;
    }

}