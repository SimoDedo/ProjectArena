using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// The game manager manages the game, it passes itself to the player.

public abstract class GameManager : CoreComponent, ILoggable {

    [Header("Managers")] [SerializeField] protected GameObject mapManager;
    [SerializeField] protected GameObject spawnPointManager;

    [Header("Game")] [SerializeField] protected bool generateOnly;
    [SerializeField] protected int gameDuration = 600;
    [SerializeField] protected int readyDuration = 3;
    [SerializeField] protected int scoreDuration = 10;
    [SerializeField] protected float respawnDuration = 3;

    protected MapManager mapManagerScript;
    protected SpawnPointManager spawnPointManagerScript;

    // Time at which the game started.
    protected float startTime;
    // Current phase of the game: 0 = ready, 1 = figth, 2 = score.
    protected int gamePhase = -1;
    // Is the game paused?
    protected bool isPaused = false;

    // Do I have to log?
    protected bool logging = false;
    // Experiment manager.
    private ExperimentManager experimentManagerScript;
    // Support object to format the log.
    private JsonLog jLog;
    // Support object to format the log.
    private JsonKill jKill;
    // Support object to format the log.
    private JsonSpawn jSpawn;

    // Moves a gameobject to a free spawn point.
    public void Spawn(GameObject g) {
        g.transform.position = spawnPointManagerScript.GetSpawnPosition() + Vector3.up * 3f;
        // Log if needed.
        if (logging)
            LogSpawn(g.transform.position.x, g.transform.position.z, g.gameObject.name);
    }

    // Menages the death of an entity.
    public abstract void MenageEntityDeath(GameObject g, Entity e);

    protected abstract void UpdateGamePhase();

    protected abstract void ManageGame();

    public abstract void AddScore(int i, int j);

    public abstract void SetUIColor(Color c);

    public abstract void Pause();

    public IEnumerator FreezeTime(float wait, bool mustPause) {
        if (mustPause)
            yield return new WaitForSeconds(wait);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void Quit() {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        LoadNextScene("Menu");
    }

    // Loads the next scene
    private void LoadNextScene(string def) {
        if (GameObject.Find("Experiment Manager") && GameObject.Find("Parameter Manager")) {
            ParameterManager pm = GameObject.Find("Parameter Manager").GetComponent<ParameterManager>();
            ExperimentManager em = GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>();
            SceneManager.LoadScene(em.GetNextScene(pm));
        } else {
            SceneManager.LoadScene(def);
        }
    }

    // Setups stuff for the logging.
    public void SetupLogging(ExperimentManager em) {
        experimentManagerScript = em;

        experimentManagerScript.WriteLog(JsonUtility.ToJson(new JsonInfo {
            height = mapManagerScript.GetMapGenerator().GetHeight().ToString(),
            width = mapManagerScript.GetMapGenerator().GetHeight().ToString(),
            tileSize = mapManagerScript.GetMapGenerator().GetSquareSize().ToString(),
        }));

        jLog = new JsonLog {
            log = ""
        };

        jKill = new JsonKill();
        jSpawn = new JsonSpawn();

        logging = true;
    }

    // Logs spawn.
    private void LogSpawn(float x, float z, string name) {
        jLog.time = Time.time.ToString("n4");
        jLog.type = "spawn";
        jSpawn.x = x.ToString();
        jSpawn.y = z.ToString();
        jSpawn.spawnedEntity = name;
        string log = JsonUtility.ToJson(jLog);
        experimentManagerScript.WriteLog(log.Remove(log.Length - 3) + JsonUtility.ToJson(jSpawn) + "}");
    }

    // Logs a kill.
    protected void LogKill() {
        jLog.time = Time.time.ToString("n4");
        jLog.type = "kill";
        jKill.x = "";
        jKill.y = "";
        jKill.killedEntity = "";
        jKill.killerEntity = "";
        string log = JsonUtility.ToJson(jLog);
        experimentManagerScript.WriteLog(log.Remove(log.Length - 3) + JsonUtility.ToJson(jKill) + "}");
    }

    private class JsonLog {
        public string time;
        public string type;
        public string log;
    }

    private class JsonInfo {
        public string height;
        public string width;
        public string tileSize;
    }

    private class JsonKill {
        public string x;
        public string y;
        public string killedEntity;
        public string killerEntity;
    }

    private class JsonSpawn {
        public string x;
        public string y;
        public string spawnedEntity;
    }

}