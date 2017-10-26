using UnityEngine;

public class DuelGameManager : GameManager {

    [Header("Contenders")] [SerializeField] private GameObject player;
    [SerializeField] private GameObject opponent;
    [SerializeField] private string playerName = "Player 1";
    [SerializeField] private string opponentName = "Player 2";
    [SerializeField] private int totalHealthPlayer = 100;
    [SerializeField] private int totalHealthOpponent = 100;
    [SerializeField] private bool[] activeGunsPlayer;
    [SerializeField] private bool[] activeGunsOpponent;

    [Header("Duel variables")] [SerializeField] protected GameObject duelGameUIManager;

    private DuelGameUIManager duelGameUIManagerScript;

    private Player playerScript;
    private Opponent opponentScript;

    private int playerKillCount = 0;
    private int opponentKillCount = 0;

    private int playerID = 1;
    private int opponentID = 2;

    private void Start() {
        /* #if UNITY_EDITOR
            UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        #endif */

        mapManagerScript = mapManager.GetComponent<MapManager>();
        spawnPointManagerScript = spawnPointManager.GetComponent<SpawnPointManager>();
        duelGameUIManagerScript = duelGameUIManager.GetComponent<DuelGameUIManager>();

        playerScript = player.GetComponent<Player>();
        opponentScript = opponent.GetComponent<Opponent>();

        duelGameUIManagerScript.CompleteDefade();
    }

    private void Update() {
        if (!IsReady() && mapManagerScript.IsReady() && spawnPointManagerScript.IsReady() && duelGameUIManagerScript.IsReady()) {
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

    // Updates the phase of the game.
    protected override void UpdateGamePhase() {
        int passedTime = (int)(Time.time - startTime);

        if (gamePhase == -1) {
            // Disable the contenders movement and interactions, activate the ready UI, set the name of the players and set the phase.
            playerScript.SetInGame(false);
            opponentScript.SetInGame(false);
            duelGameUIManagerScript.ActivateReadyUI();
            duelGameUIManagerScript.SetPlayersName(playerName, opponentName);
            duelGameUIManagerScript.SetReadyUI();
            gamePhase = 0;
        } else if (gamePhase == 0 && passedTime >= readyDuration) {
            // Enable the contenders movement and interactions, activate the figth UI, set the kills to zero and set the phase.
            playerScript.SetInGame(true);
            opponentScript.SetInGame(true);
            duelGameUIManagerScript.SetKills(0, 0);
            duelGameUIManagerScript.ActivateFigthUI();
            gamePhase = 1;
        } else if (gamePhase == 1 && passedTime >= readyDuration + gameDuration) {
            // Disable the contenders movement and interactions, activate the score UI, set the winner and set the phase.
            playerScript.SetInGame(false);
            opponentScript.SetInGame(false);
            duelGameUIManagerScript.ActivateScoreUI();
            duelGameUIManagerScript.SetScoreUI(playerKillCount, opponentKillCount);
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
                duelGameUIManagerScript.SetCountdown((int)(startTime + readyDuration - Time.time));
                break;
            case 1:
                // Update the time.
                duelGameUIManagerScript.SetTime((int)(startTime + readyDuration + gameDuration - Time.time));
                break;
            case 2:
                // Do nothing.
                break;
        }
    }

    // Adds a kill to the kill counters.
    public override void AddScore(int killerIdentifier, int killedID) {
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

        duelGameUIManagerScript.SetKills(playerKillCount, opponentKillCount);
    }

    public override void AddScore(int score) { }

}