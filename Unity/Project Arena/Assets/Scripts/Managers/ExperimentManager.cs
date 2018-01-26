using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExperimentManager : MonoBehaviour {

    [Header("Tutorial")] [SerializeField] private Case tutorial;
    [SerializeField] private bool playTutorial;

    [Header("Experiment")] [SerializeField] private List<Study> studies;
    [SerializeField] private int casesPerUsers;
    [SerializeField] private string experimentName;

    [Header("Survey")] [SerializeField] private Case survey;
    [SerializeField] private bool playSurvey;

    [Header("Logging")] [SerializeField] private bool logGame;
    [SerializeField] private bool logStatistics;

    // List of cases the current player has to play.
    private List<Case> caseList;
    // Directory for this esperiment files.
    private string experimentDirectory;
    // Directory for the surveys.
    private string surveysDirectory;

    // Stream writer of the current game log.
    private StreamWriter logStream;
    // Support object to format the log.
    private JsonLog jLog;
    // Support object to fromat the position log.
    private JsonShoot jShoot;
    // Support object to fromat the position log.
    private JsonReload jReload;
    // Support object to fromat the position log.
    private JsonPosition jPosition;
    // Support object to format the log.
    private JsonKill jKill;
    // Support object to format the log.
    private JsonSpawn jSpawn;
    // Support object to format the log.
    private JsonHit jHit;

    // Stream writer of the current statistic log.
    private StreamWriter statisticsStream;
    // Spawn time of current target.
    private float targetSpawn = 0;
    // Spawn time of last target.
    private float lastTargetSpawn = 0;
    // Current distance.
    private float currentDistance = 0;
    // Total distance.
    private float totalDistance = 0;
    // Total shots.
    private int shotCount = 0;
    // Total hits.
    private int hitCount = 0;
    // Total destoryed targets.
    private float killCount = 0;
    // Medium kill time.
    private float mediumKillTime = 0;
    // Medium distance covered to find a target.
    private float mediumKillDistance = 0;
    // Size of a map tile.
    private float tileSize = 1;
    // Position of the player.
    private Vector3 lastPosition = new Vector3(-1, -1, -1);
    // Initial target position.
    private Vector3 initialTargetPosition = new Vector3(-1, -1, -1);
    // Initial player position.
    private Vector3 initialPlayerPosition = new Vector3(-1, -1, -1);
    // Support object to format the log.
    private JsonGameStatistics jGameStatistics;
    // Support object to format the log.
    private JsonTargetStatistics jTargetStatistics;

    private int currentCase = -1;
    private string currentTimestamp;
    private bool logging;

    void Start() {
        caseList = new List<Case>();

        // Create the experiment directory if needed.
        experimentDirectory = Application.persistentDataPath + "/Export/" + experimentName;
        CreateDirectory(experimentDirectory);
        // Create the maps directory if needed.
        foreach (Study s in studies)
            foreach (Case c in s.cases) {
                CreateDirectory(experimentDirectory + "/" + c.map.name);
                System.IO.File.WriteAllText(@experimentDirectory + "/" + c.map.name + "/" + c.map.name + ".txt", c.map.text);
            }
        // Create the survey directory if needed.
        surveysDirectory = experimentDirectory + "/Surveys";
        CreateDirectory(surveysDirectory);
    }

    void Awake() {
        DontDestroyOnLoad(transform.gameObject);
    }

    void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /* EXPERIMENT */

    // Creates a new list of cases for the player to play.
    private void CreateNewList() {
        currentTimestamp = GetTimeStamp();
        caseList.Clear();

        if (playTutorial)
            caseList.Add(tutorial);

        for (int i = 0; i < casesPerUsers; i++)
            caseList.Add(GetCase());

        if (playSurvey)
            caseList.Add(survey);

        caseList.Add(new Case {
            scene = SceneManager.GetActiveScene().name
        });

        currentCase = 0;
    }

    // Returns a well formatted timestamp.
    private string GetTimeStamp() {
        DateTime now = System.DateTime.Now;
        return now.Year + "-" + now.Month + "-" + now.Day + "-" + now.Hour + "-" + now.Minute + "-" + now.Second;
    }

    // Gets the next case to add in a round-robin fashion.
    private Case GetCase() {
        // Get the least played study.
        int minValue = studies[0].completion;
        int minIndex = 0;

        for (int i = 0; i < studies.Count; i++) {
            if (studies[i].completion < minValue) {
                minValue = studies[i].completion;
                minIndex = i;
            }
        }
        studies[minIndex].completion++;

        // Get the least played case in the least played study.
        int minMinValue = studies[minIndex].cases[0].completion;
        int minMinIndex = 0;

        for (int i = 0; i < studies[minIndex].cases.Count; i++) {
            if (studies[minIndex].cases[i].completion < minMinValue) {
                minMinValue = studies[minIndex].cases[i].completion;
                minMinIndex = i;
            }
        }
        studies[minIndex].cases[minMinIndex].completion++;

        return studies[minIndex].cases[minMinIndex];
    }

    // Retuns the next scene to be played.
    public string GetNextScene(ParameterManager pm) {
        currentCase++;

        if (logging)
            StopLogging();

        if (currentCase == caseList.Count || caseList.Count == 0)
            CreateNewList();

        Case c = caseList[currentCase];

        pm.SetGenerationMode(4);
        pm.SetMapDNA(c.map == null ? "" : c.map.text);

        return c.scene;
    }

    /* SURVEY */

    // Tells if I need to save the survey.
    public bool MustSaveSurvey() {
        return !File.Exists(surveysDirectory + "/survey.json");
    }

    // Save survey. This has to be done once.
    public void SaveSurvey(string survey) {
        System.IO.File.WriteAllText(surveysDirectory + "/survey.json", survey);
    }

    // Saves answers and informations about the experiment.
    public void SaveAnswers(string answers) {
        string info = JsonUtility.ToJson(new JsonInfo {
            experimentName = experimentName,
            playedMaps = GetCurrentCasesArray()
        });
        System.IO.File.WriteAllText(surveysDirectory + "/" + currentTimestamp + "_survey.json", info + "\n" + answers);
    }

    // Returns the played cases in an array.
    public string[] GetCurrentCasesArray() {
        string[] maps = new string[casesPerUsers];
        for (int i = 0; i < casesPerUsers; i++)
            maps[i] = playTutorial ? caseList[i + 1].map.name : caseList[i].map.name;
        return maps;
    }

    /* LOGGING */

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (logGame && !(playTutorial && currentCase == 0) && !(playSurvey && currentCase == caseList.Count - 2) && currentCase != caseList.Count - 1)
            SetupLogging();
    }

    // Sets up logging.
    private void SetupLogging() {
        logStream = File.CreateText(experimentDirectory + "/" + caseList[currentCase].map.name + "/" + caseList[currentCase].map.name + "_" + currentTimestamp + "_log.json");
        logStream.AutoFlush = true;

        jLog = new JsonLog {
            log = ""
        };

        jShoot = new JsonShoot();
        jReload = new JsonReload();
        jPosition = new JsonPosition();
        jSpawn = new JsonSpawn();
        jKill = new JsonKill();
        jHit = new JsonHit();

        GameManager gm = FindObjectOfType(typeof(GameManager)) as GameManager;
        if (gm != null)
            gm.LoggingHandshake(this);

        if (logStatistics)
            SetupStatisticsLogging();
    }

    // Sets up statistics logging.
    private void SetupStatisticsLogging() {
        statisticsStream = File.CreateText(experimentDirectory + "/" + caseList[currentCase].map.name + "/" + caseList[currentCase].map.name + "_" + currentTimestamp + "_statistics.json");
        statisticsStream.AutoFlush = true;

        jGameStatistics = new JsonGameStatistics();
        jTargetStatistics = new JsonTargetStatistics();
    }

    // Starts logging.
    public void StartLogging() {
        logging = true;

        foreach (MonoBehaviour monoBehaviour in FindObjectsOfType(typeof(MonoBehaviour))) {
            ILoggable logger = monoBehaviour as ILoggable;
            if (logger != null)
                logger.SetupLogging(this);
        }
    }

    // Writes a log in the log stream.
    public void WriteLog(string log) {
        logStream.WriteLine(log);
    }

    // Writes a log in the statistics stream.
    public void WriteStatisticsLog(string log) {
        statisticsStream.WriteLine(log);
    }

    // Stops logging and saves the log.
    public void StopLogging() {
        if (logStatistics) {
            LogGameStatistics();
            statisticsStream.Close();
            logStatistics = false;
        }

        logStream.Close();
    }

    // Logs reload.
    public void LogRelaod(int gunId, int ammoInCharger, int totalAmmo) {
        jLog.time = Time.time.ToString("n4");
        jLog.type = "player_reload";
        jReload.weapon = gunId.ToString();
        jReload.ammoInCharger = ammoInCharger.ToString();
        jReload.totalAmmo = totalAmmo.ToString();
        string log = JsonUtility.ToJson(jLog);
        WriteLog(log.Remove(log.Length - 3) + JsonUtility.ToJson(jReload) + "}");
    }

    // Logs the shot.
    public void LogShot(float x, float z, float direction, int gunId, int ammoInCharger, int totalAmmo) {
        jLog.time = Time.time.ToString("n4");
        jLog.type = "player_shot";
        jShoot.x = x.ToString("n4");
        jShoot.y = z.ToString("n4");
        jShoot.direction = direction.ToString("n4");
        jShoot.weapon = gunId.ToString();
        jShoot.ammoInCharger = ammoInCharger.ToString();
        jShoot.totalAmmo = totalAmmo.ToString();
        string log = JsonUtility.ToJson(jLog);
        WriteLog(log.Remove(log.Length - 3) + JsonUtility.ToJson(jShoot) + "}");

        if (logStatistics)
            shotCount++;
    }

    // Logs info about the map.
    public void LogMapInfo(float height, float width, float ts) {
        string infoLog = JsonUtility.ToJson(new JsonMapInfo {
            height = height.ToString(),
            width = width.ToString(),
            tileSize = ts.ToString(),
        });

        WriteLog(infoLog);

        if (logStatistics) {
            WriteStatisticsLog(infoLog);
            tileSize = ts;
        }
    }

    // Logs the position.
    public void LogPosition(float x, float z, float direction) {
        jLog.time = Time.time.ToString("n4");
        jLog.type = "player_position";
        jPosition.x = x.ToString("n4");
        jPosition.y = z.ToString("n4");
        jPosition.direction = direction.ToString("n4");
        string log = JsonUtility.ToJson(jLog);
        WriteLog(log.Remove(log.Length - 3) + JsonUtility.ToJson(jPosition) + "}");

        if (logStatistics) {
            if (lastPosition.x != -1) {
                float delta = EulerDistance(x, z, lastPosition.x, lastPosition.z, tileSize);
                totalDistance += delta;
                currentDistance += delta;
            }
            lastPosition.x = x;
            lastPosition.z = z;
        }
    }

    // Logs spawn.
    public void LogSpawn(float x, float z, string spawnedEntity) {
        jLog.time = Time.time.ToString("n4");
        jLog.type = "spawn";
        jSpawn.x = x.ToString("n4");
        jSpawn.y = z.ToString("n4");
        jSpawn.spawnedEntity = spawnedEntity;
        string log = JsonUtility.ToJson(jLog);
        WriteLog(log.Remove(log.Length - 3) + JsonUtility.ToJson(jSpawn) + "}");

        if (logStatistics) {
            targetSpawn = Time.time;
            initialTargetPosition.x = x;
            initialTargetPosition.z = z;
            initialPlayerPosition = lastPosition;
        }
    }

    // Logs a kill.
    public void LogKill(float x, float z, string killedEnitiy, string killerEntity) {
        jLog.time = Time.time.ToString("n4");
        jLog.type = "kill";
        jKill.x = x.ToString("n4");
        jKill.y = z.ToString("n4");
        jKill.killedEntity = killedEnitiy;
        jKill.killerEntity = killerEntity;
        string log = JsonUtility.ToJson(jLog);
        WriteLog(log.Remove(log.Length - 3) + JsonUtility.ToJson(jKill) + "}");

        if (logStatistics) {
            LogTargetStatistics(x, z);
            killCount++;
            mediumKillTime += (Time.time - lastTargetSpawn - mediumKillTime) / killCount;
            mediumKillDistance += (currentDistance - mediumKillDistance) / killCount;
            currentDistance = 0;
            lastTargetSpawn = targetSpawn;
        }
    }

    // Logs a hit.
    public void LogHit(float x, float z, string hittedEntity, string hitterEntity, int damage) {
        jLog.time = Time.time.ToString("n4");
        jLog.type = "hit";
        jHit.x = x.ToString("n4");
        jHit.y = z.ToString("n4");
        jHit.hittedEntity = hittedEntity;
        jHit.hitterEntity = hitterEntity;
        jHit.damage = damage.ToString();
        string log = JsonUtility.ToJson(jLog);
        WriteLog(log.Remove(log.Length - 3) + JsonUtility.ToJson(jHit) + "}");

        if (logStatistics)
            hitCount++;
    }

    // Logs statistics about the performance of the player finding the target.
    private void LogTargetStatistics(float x, float z) {
        jTargetStatistics.playerInitialX = initialPlayerPosition.x.ToString("n4");
        jTargetStatistics.playerInitialY = initialPlayerPosition.z.ToString("n4");
        jTargetStatistics.playerX = lastPosition.x.ToString("n4");
        jTargetStatistics.playerY = lastPosition.z.ToString("n4");
        jTargetStatistics.targetX = x.ToString("n4");
        jTargetStatistics.targetY = z.ToString("n4");
        jTargetStatistics.time = (Time.time - lastTargetSpawn).ToString("n4");
        jTargetStatistics.coveredTileDistance = currentDistance.ToString("n4");
        jTargetStatistics.speed = (currentDistance / (Time.time - lastTargetSpawn)).ToString("n4");
        WriteStatisticsLog(JsonUtility.ToJson(jTargetStatistics));
    }

    // Logs statistics about the game.
    private void LogGameStatistics() {
        jGameStatistics.coveredDistance = totalDistance.ToString("n4");
        jGameStatistics.mediumKillTime = mediumKillTime.ToString("n4");
        jGameStatistics.mediumKillDistance = mediumKillDistance.ToString("n4");
        jGameStatistics.totalShots = shotCount.ToString("n4");
        jGameStatistics.totalHits = hitCount.ToString("n4");
        jGameStatistics.accuracy = (shotCount > 0) ? (hitCount / (float)shotCount).ToString("n4") : "0";
        WriteStatisticsLog(JsonUtility.ToJson(jGameStatistics));
    }

    /* SUPPORT FUNCTIONS */

    // Creates a directory if needed.
    private void CreateDirectory(string directory) {
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    // Returns the normalized euler distance.
    private float EulerDistance(float x1, float y1, float x2, float y2, float normalization) {
        return Mathf.Sqrt(Mathf.Pow(x1 - x2, 2) + Mathf.Pow(y1 - y2, 2)) / normalization;
    }

    /* CUTSOM OBJECTS */

    private class JsonLog {
        public string time;
        public string type;
        public string log;
    }

    private class JsonInfo {
        public string experimentName;
        public string[] playedMaps;
    }

    private class JsonMapInfo {
        public string height;
        public string width;
        public string tileSize;
    }

    private class JsonPosition {
        public string x;
        public string y;
        public string direction;
    }

    private class JsonShoot {
        public string x;
        public string y;
        public string direction;
        public string weapon;
        public string ammoInCharger;
        public string totalAmmo;
    }

    private class JsonReload {
        public string weapon;
        public string ammoInCharger;
        public string totalAmmo;
    }

    private class JsonKill {
        public string x;
        public string y;
        public string killedEntity;
        public string killerEntity;
    }

    private class JsonHit {
        public string x;
        public string y;
        public string hittedEntity;
        public string hitterEntity;
        public string damage;
    }

    private class JsonSpawn {
        public string x;
        public string y;
        public string spawnedEntity;
    }

    private class JsonTargetStatistics {
        public string playerInitialX;
        public string playerInitialY;
        public string playerX;
        public string playerY;
        public string targetX;
        public string targetY;
        public string coveredTileDistance;
        public string time;
        public string speed;
    }

    private class JsonGameStatistics {
        public string totalShots;
        public string totalHits;
        public string accuracy;
        public string coveredDistance;
        public string mediumKillTime;
        public string mediumKillDistance;
    }

    [Serializable]
    private class Study {
        [SerializeField] public string studyName;
        [SerializeField] public List<Case> cases;
        [NonSerialized] public int completion;
    }

    [Serializable]
    private class Case {
        [SerializeField] public TextAsset map;
        [SerializeField] public string scene;
        [NonSerialized] public int completion;
    }

}