using System;
using System.Collections;
using UnityEngine;

// The game manager manages the game, it passes itself to the player.

public class GameManager : CoreComponent {


    [Header("Managers")] [SerializeField] private GameObject mapManager;
    [SerializeField] private GameObject spawnPointManager;
    [SerializeField] private GameObject gameUIManager;

    [Header("Game")] [SerializeField] private bool generateOnly;
    [SerializeField] private int gameDuration = 600;
    [SerializeField] private int readyDuration = 3;
    [SerializeField] private int scoreDuration = 10;
    [SerializeField] private float respawnDuration = 3;

    [Header("Contenders")] [SerializeField] private GameObject player;
    [SerializeField] private GameObject opponent;
    [SerializeField] private string playerName = "Player 1";
    [SerializeField] private string opponentName = "Player 2";
    [SerializeField] private int totalHealthPlayer = 100;
    [SerializeField] private int totalHealthOpponent = 100;
    [SerializeField] private bool[] activeGunsPlayer;
    [SerializeField] private bool[] activeGunsOpponent;

    private MapManager mapManagerScript;
    private SpawnPointManager spawnPointManagerScript;
    private GameUIManager gameUIManagerScript;

    private Player playerScript;
    private Opponent opponentScript;

    private int playerKillCount = 0;
    private int opponentKillCount = 0;

    private int playerID = 1;
    private int opponentID = 2;

    // Time at which the game started.
    private float startTime;
    // Current phase of the game: 0 = ready, 1 = figth, 2 = score.
    private int gamePhase = -1;

    private void Start() {
        /* #if UNITY_EDITOR
            UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        #endif */

        mapManagerScript = mapManager.GetComponent<MapManager>();
        spawnPointManagerScript = spawnPointManager.GetComponent<SpawnPointManager>();
        gameUIManagerScript = gameUIManager.GetComponent<GameUIManager>();

        playerScript = player.GetComponent<Player>();
        opponentScript = opponent.GetComponent<Opponent>();
    }

    private void Update() {
        if (!IsReady() && mapManagerScript.IsReady() && spawnPointManagerScript.IsReady() && gameUIManagerScript.IsReady()) {
            // Generate the map.
            mapManagerScript.ManageMap(true);

            // Set the spawn points.
            if (!generateOnly)
                generateOnly = spawnPointManagerScript.SetSpawnPoints(mapManagerScript.GetSpawnPoints());

            if (!generateOnly) {
                // Spawn the player and the opponent.
                Spawn(player);
                Spawn(opponent);

                // Setup the contenders.
                playerScript.SetupEntity(totalHealthPlayer, activeGunsPlayer, this, playerID);
                opponentScript.SetupEntity(totalHealthOpponent, activeGunsOpponent, this, opponentID);

                playerScript.LockCursor();
                startTime = Time.time;
            }

            SetReady(true);
        } else if (IsReady() && !generateOnly) {
            ManageGame();
        }
    }

    // Moves a gameobject to a free spawn point.
    public void Spawn(GameObject g) {
        g.transform.position = spawnPointManagerScript.GetSpawnPosition() + Vector3.up * 3f;
    }

    // Respawns an entity, but only if the game phase is still figth.
    public IEnumerator WaitForRespawn(GameObject g, Entity e) {
        yield return new WaitForSeconds(respawnDuration);

        if (gamePhase == 1) {
            Spawn(g);
            e.Respawn();
        }
    }

    // Manages the gamed depending on the current time.
    private void ManageGame() {
        UpdateGamePhase();

        switch (gamePhase) {
            case 0:
                // Update the countdown.
                gameUIManagerScript.SetCountdown((int)(startTime + readyDuration - Time.time));
                break;
            case 1:
                // Update the time.
                gameUIManagerScript.SetTime((int)(startTime + readyDuration + gameDuration - Time.time));
                break;
            case 2:
                // Do nothing.
                break;
        }
    }

    // Updates the phase of the game.
    private void UpdateGamePhase() {
        int passedTime = (int)(Time.time - startTime);

        if (gamePhase == -1) {
            // Disable the contenders movement and interactions, activate the ready UI, set the name of the players and set the phase.
            playerScript.SetInGame(false);
            opponentScript.SetInGame(false);
            gameUIManagerScript.ActivateReadyUI();
            gameUIManagerScript.SetPlayersName(playerName, opponentName);
            gameUIManagerScript.SetReadyUI();
            gamePhase = 0;
        } else if (gamePhase == 0 && passedTime >= readyDuration) {
            // Enable the contenders movement and interactions, activate the figth UI, set the kills to zero and set the phase.
            playerScript.SetInGame(true);
            opponentScript.SetInGame(true);
            gameUIManagerScript.SetKills(0, 0);
            gameUIManagerScript.ActivateFigthUI();
            gamePhase = 1;
        } else if (gamePhase == 1 && passedTime >= readyDuration + gameDuration) {
            // Disable the contenders movement and interactions, activate the score UI, set the winner and set the phase.
            playerScript.SetInGame(false);
            opponentScript.SetInGame(false);
            gameUIManagerScript.ActivateScoreUI();
            gameUIManagerScript.SetScoreUI(playerKillCount, opponentKillCount);
            gamePhase = 2;
        } else if (gamePhase == 2 && passedTime >= readyDuration + gameDuration + scoreDuration) {
            Application.Quit();
        }
    }

    // Adds a kill to the kill counters.
    public void AddKill(int killerIdentifier, int killedID) {
        if (killerIdentifier == killedID) {
            if (killerIdentifier == playerID)
                playerKillCount--;
            else
                opponentKillCount--;
        } else {
            if (killerIdentifier == playerID)
                playerKillCount++;
            else
                opponentKillCount++;
        }

        gameUIManagerScript.SetKills(playerKillCount, opponentKillCount);
    }

}