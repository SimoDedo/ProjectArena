using System;
using UnityEngine;

// The game manager manages the game, it passes itself to the player.

public class GameManager : CoreComponent {

    [SerializeField] private GameObject mapManager;
    [SerializeField] private GameObject spawnPointManager;
    [SerializeField] private GameObject gameGUIManager;

    [SerializeField] private GameObject player;
    [SerializeField] private GameObject opponent;

    [SerializeField] private int gameDuration = 600;
    [SerializeField] private int readyDuration = 3;
    [SerializeField] private int scoreDuration = 10;

    [SerializeField] private string playerName = "Player 1";
    [SerializeField] private string opponentName = "Player 2";
    [SerializeField] private int totalHealth = 100;
    [SerializeField] private bool[] activeGunsPlayer;

    private MapManager mapManagerScript;
    private SpawnPointManager spawnPointManagerScript;
    private GameGUIManager gameGUIManagerScript;

    private PlayerController playerControllerScript;
    private Opponent opponentScript;

    private int playerKillCount = 0;
    private int opponentKillCount = 0;

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
        gameGUIManagerScript = gameGUIManager.GetComponent<GameGUIManager>();

        playerControllerScript = player.GetComponent<PlayerController>();
        opponentScript = opponent.GetComponent<Opponent>();
    }

    private void Update() {
        if (!IsReady() && mapManagerScript.IsReady() && spawnPointManagerScript.IsReady() && gameGUIManagerScript.IsReady()) {
            // Generate the map.
            mapManagerScript.ManageMap(true);

            // Set the spawn points.
            spawnPointManagerScript.SetSpawnPoints(mapManagerScript.GetSpawnPoints());

            // Spawn the player and the opponent.
            player.transform.position = spawnPointManagerScript.GetSpawnPosition() + Vector3.up * 3f;
            opponent.transform.position = spawnPointManagerScript.GetSpawnPosition() + Vector3.up * 3f;

            // Setup the UI.
            gameGUIManagerScript.SetActiveGuns(activeGunsPlayer);

            // Setup the player.
            playerControllerScript.SetupPlayer(totalHealth, activeGunsPlayer, this);
            // Setup the opponent.
            opponentScript.SetupOpponent(totalHealth, this);

            playerControllerScript.LockCursor();

            startTime = Time.time;

            SetReady(true);
        } else if (IsReady()) {
            ManageGame();
        }
    }

    // Moves a gameobject to a free spawn point.
    public void Respawn(GameObject g) {
        g.transform.position = spawnPointManagerScript.GetSpawnPosition() + Vector3.up * 3f;
        // TODO - Reset life with abstract method, manage wait, respawn counter, ecc.
    }

    // Manages the gamed depending on the current time.
    private void ManageGame() {
        UpdateGamePhase();

        switch (gamePhase) {
            case 0:
                // Update the countdown.
                gameGUIManagerScript.SetCountdown((int)(startTime + readyDuration - Time.time));
                break;
            case 1:
                // Update the time.
                gameGUIManagerScript.SetTime((int)(startTime + readyDuration + gameDuration - Time.time));
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
            // Disable the player movement, activate the ready GUI, set the name of the players and set the phase.
            playerControllerScript.SetMovementEnabled(false);
            gameGUIManagerScript.ActivateReadyGUI();
            gameGUIManagerScript.SetPlayersName(playerName, opponentName);
            gameGUIManagerScript.SetReadyGUI();
            gamePhase = 0;
        } else if (gamePhase == 0 && passedTime >= readyDuration) {
            // Enable the player movement, activate the figth GUI, set the kills to zero and set the phase.
            playerControllerScript.SetMovementEnabled(true);
            gameGUIManagerScript.SetPlayer1Kills(0);
            gameGUIManagerScript.SetPlayer2Kills(0);
            gameGUIManagerScript.ActivateFigthGUI();
            gamePhase = 1;
        } else if (gamePhase == 1 && passedTime >= readyDuration + gameDuration) {
            // Disable the player movement, activate the score GUI, set the winner and set the phase.
            playerControllerScript.SetMovementEnabled(false);
            gameGUIManagerScript.ActivateScoreGUI();
            gameGUIManagerScript.SetScoreGUI(playerKillCount, opponentKillCount);
            gamePhase = 2;
        } else if (gamePhase == 2 && passedTime >= readyDuration + gameDuration + scoreDuration) {
            Application.Quit();
        }
    }

    // FACADE METHOD - Sets the current gun in the UI calling the UI method.
    public void SetCurrentGun(int currentGunIndex) {
        gameGUIManagerScript.SetCurrentGun(currentGunIndex);
    }

    // FACADE METHOD - Starts the reloading cooldown in the UI.
    public void StartReloading(float duration) {
        gameGUIManagerScript.SetCooldown(duration);

    }

    // FACADE METHOD - Stops the reloading.
    public void StopReloading() {
        gameGUIManagerScript.StopReloading();
    }

    // FACADE METHOD - Sets the ammo in the charger.
    public void SetAmmo(int charger, int total) {
        gameGUIManagerScript.SetAmmo(charger, total);
    }

    // FACADE METHOD - Sets the health.
    public void SetHealth(int health, int tot) {
        gameGUIManagerScript.SetHealth(health, tot);
    }

}