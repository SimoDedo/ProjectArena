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
    [SerializeField] private bool[] activeWeaponsPlayer;

    // Do I have to assemble the map?
    [SerializeField] private bool assembleMap = true;

    private MapManager mapManagerScript;
    private SpawnPointManager spawnPointManagerScript;
    private GameGUIManager gameGUIManagerScript;

    private PlayerController playerControllerScript;

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
    }

    private void Update() {
        if (!IsReady() && mapManagerScript.IsReady() && spawnPointManagerScript.IsReady() && gameGUIManagerScript.IsReady()) {
            // Generate the map.
            mapManagerScript.ManageMap(assembleMap);

            if (assembleMap) {
                // Set the spawn points.
                spawnPointManagerScript.SetSpawnPoints(mapManagerScript.GetSpawnPoints());

                // Setup the player.
                playerControllerScript.SetupPlayer(totalHealth, activeWeaponsPlayer, this);

                // Spawn the player and the opponent.
                player.transform.position = spawnPointManagerScript.GetSpawnPosition() + Vector3.up * 3f;
                opponent.transform.position = spawnPointManagerScript.GetSpawnPosition() + Vector3.up * 3f;

                playerControllerScript.LockCursor();
            }

            startTime = Time.time;

            SetReady(true);
        } else if (IsReady()) {
            ManageGame();
        }
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
            gameGUIManagerScript.SetActiveWeapons(activeWeaponsPlayer);
            gameGUIManagerScript.SetCurrentWeapon(getLowestActiveWeapon());
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

    // Returns the index of the lowest active weapon.
    private int getLowestActiveWeapon() {
        for (int i = 0; i < activeWeaponsPlayer.GetLength(0); i++) {
            if (activeWeaponsPlayer[i])
                return i;
        }

        return -1;
    }

    // GAME UI MANAGER FACADE METHODS //

    // Sets the current weapon in the UI calling the UI method.
    public void SetCurrentWeapon(int currentWeaponIndex) {
        gameGUIManagerScript.SetCurrentWeapon(currentWeaponIndex);
    }

    // Starts the reloading cooldown in the UI.
    public void StartReloading(float duration) {
        gameGUIManagerScript.SetCooldown(duration);

    }

    // Sets the color of the crosshair.
    public void SetCrosshairNeutral(bool isNeutral) {
        gameGUIManagerScript.SetCrosshairNeutral(isNeutral);
    }

    // Stops the reloading.
    public void StopReloading() {
        gameGUIManagerScript.StopReloading();
    }

    // Sets the ammo in the charger.
    public void SetAmmo(int charger, int total) {
        gameGUIManagerScript.SetAmmo(charger, total);
    }

    // Sets the health.
    public void SetHealth(int health, int tot) {
        gameGUIManagerScript.SetHealth(health, tot);
    }

}