using System;
using System.Collections;
using AssemblyLogging;
using UnityEngine;

/// <summary>
/// BotGameManager is an implementation of GameManager. The bot game mode consists in a deathmatch
/// between two bots. Each time a bot kills his opponent he scores one point. If a bot
/// kills himself he loses one point. Who has more points when time runs up wins.
/// </summary>
public class GraphTesterGameManager : GameManager
{
    // ReSharper disable once InconsistentNaming
    private const int TOTAL_HEALTH_BOT = 100;
    private bool[] activeGunsBot1;
    private bool[] activeGunsBot2;

    private GameObject bot1;
    private GameObject bot2;
    private BotCharacteristics bot1Params;
    private BotCharacteristics bot2Params;

    private AIEntity ai1;
    private AIEntity ai2;

    private int playerKillCount;
    private int opponentKillCount;

    private const int BOT1_ID = 1;
    private const int BOT2_ID = 2;

    private void Awake()
    {
        if (generateOnly) throw new ArgumentException("Generate only is unsupported!");
    }

    public void SetParameters(
        GameObject botPrefab,
        BotCharacteristics bot1Params,
        bool[] activeGunsBot1,
        BotCharacteristics bot2Params,
        bool[] activeGunsBot2,
        string mapPath,
        MapManager mapManager,
        SpawnPointManager spawnPointManager,
        int gameDuration = 600,
        int readyDuration = 1,
        int scoreDuration = 0,
        float respawnDuration = 3
    )
    {
        this.gameDuration = gameDuration;
        this.readyDuration = readyDuration;
        this.scoreDuration = scoreDuration;
        this.respawnDuration = respawnDuration;
        
        this.bot1Params = bot1Params;
        this.bot2Params = bot2Params;

        bot1 = Instantiate(botPrefab);
        this.activeGunsBot1 = activeGunsBot1;
        ai1 = bot1.GetComponent<AIEntity>();

        bot2 = Instantiate(botPrefab);
        this.activeGunsBot2 = activeGunsBot2;
        ai2 = bot2.GetComponent<AIEntity>();

        mapManagerScript = mapManager;
        spawnPointManagerScript = spawnPointManager;
        mapManagerScript.SetTextFile(mapPath);
    }

    private void Update()
    {
        if (!IsReady() && mapManagerScript.IsReady() && spawnPointManagerScript.IsReady())
        {
            StartNewExperimentGameEvent.Instance.Raise();

            // Generate the map.
            mapManagerScript.ManageMap(true);

            // Set the spawn points.
            spawnPointManagerScript.SetSpawnPoints(mapManagerScript.GetSpawnPoints());

            // Spawn the player and the opponent.
            Spawn(bot1);
            Spawn(bot2);

            ai1.SetCharacteristics(bot1Params);
            ai1.SetEnemy(ai2);
            ai1.SetupEntity(TOTAL_HEALTH_BOT, activeGunsBot1, this, BOT1_ID);
            ai1.SetupLogging();

            ai2.SetCharacteristics(bot2Params);
            ai2.SetEnemy(ai1);
            ai2.SetupEntity(TOTAL_HEALTH_BOT, activeGunsBot2, this, BOT2_ID);
            ai2.SetupLogging();
            startTime = Time.time;

            SetReady(true);
        } else if (IsReady())
        {
            ManageGame();
        }
    }

    // Updates the phase of the game.
    protected override void UpdateGamePhase()
    {
        var passedTime = (int) (Time.time - startTime);
        if (Application.isBatchMode) Debug.Log("Time passed: " + passedTime + " out of " + gameDuration);
        switch (gamePhase)
        {
            case -1:
                // Disable the contenders movement and interactions, activate the ready UI, set the 
                // name of the players and set the phase.
                ai1.SetInGame(false);
                ai2.SetInGame(false);
                gamePhase = 0;
                break;
            case 0 when passedTime >= readyDuration:
                Debug.Log("In fase started");
                // Enable the contenders movement and interactions, activate the fight UI, set the 
                // kills to zero and set the phase.
                ai1.SetInGame(true);
                ai2.SetInGame(true);
                gamePhase = 1;
                break;
            case 1 when passedTime >= readyDuration + gameDuration:
                Debug.Log("Final score: " + playerKillCount + ", " + opponentKillCount);
                // Disable the contenders movement and interactions, activate the score UI, set the 
                // winner and set the phase.
                ai1.SetInGame(false);
                ai2.SetInGame(false);
                gamePhase = 2;
                break;
            case 2 when passedTime >= readyDuration + gameDuration + scoreDuration:
                ExperimentEndedGameEvent.Instance.Raise();
                gamePhase = 3;
                break;
        }
    }

    // Manages the gamed depending on the current time.
    protected override void ManageGame()
    {
        UpdateGamePhase();
    }

    // Adds a kill to the kill counters.
    public override void AddScore(int killerIdentifier, int killedID)
    {
        if (killerIdentifier == killedID)
        {
            if (killerIdentifier == BOT1_ID)
            {
                playerKillCount--;
            } else
            {
                opponentKillCount--;
            }
        } else
        {
            if (killerIdentifier == BOT1_ID)
            {
                playerKillCount++;
            } else
            {
                opponentKillCount++;
            }
        }
    }

    // Sets the color of the UI.
    public override void SetUIColor(Color c) { }

    // Pauses and unpauses the game.
    public override void Pause()
    {
        throw new InvalidOperationException();
    }

    // Menages the death of an entity.
    public override void ManageEntityDeath(GameObject g, Entity e)
    {
        // Start the respawn process.
        StartCoroutine(WaitForRespawn(g, e));
    }

    // Respawns an entity, but only if the game phase is still figth.
    private IEnumerator WaitForRespawn(GameObject g, Entity e)
    {
        yield return new WaitForSeconds(respawnDuration);

        if (gamePhase == 1)
        {
            Spawn(g);
            e.Respawn();
        }
    }

    public void StopGame()
    {
        Destroy(bot1);
        Destroy(bot2);
    }
}