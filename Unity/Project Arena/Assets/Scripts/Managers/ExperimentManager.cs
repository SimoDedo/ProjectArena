using ExperimentObjects;
using JsonObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ExperimentManager allows to manage experiments. An experiment is composed of different studies 
/// (a set of maps), each one composed by cases (a set of map varaitions). Each time a new
/// experiment is requested, a list of cases from the less played study is provided to the user
/// to be played. A tutorial and a survey scene can be added at the beginning and at the end of
/// the experiment, respectevely.
/// </summary>
public class ExperimentManager : SceneSingleton<ExperimentManager> {

    private Case tutorial;
    private bool playTutorial;

    private List<Study> studies;
    private int casesPerUsers;
    private string experimentName;

    private Case survey;
    private bool playSurvey;

    private bool logGame;
    private bool logStatistics;

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
    // Size of a maps tile.
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

    private bool logging = false;
    private bool loggingStatistics = false;

    void Start() {
        caseList = new List<Case>();

        if (ParameterManager.HasInstance() && ParameterManager.Instance.Export == false) {
            logGame = false;
            logStatistics = false;
        }

        if (logGame || logStatistics) {
            SetupDirectories();
        }
    }

    void Awake() {
        DontDestroyOnLoad(transform.gameObject);
    }

    void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /* EXPERIMENT */

    // Sets up the experiment manager.
    public void Setup(Case tutorial, bool playTutorial, List<Study> studies, int casesPerUsers,
        string experimentName, Case survey, bool playSurvey, bool logGame, bool logStatistics) {
        this.tutorial = tutorial;
        this.playTutorial = playTutorial;
        this.studies = studies;
        this.casesPerUsers = casesPerUsers;
        this.experimentName = experimentName;
        this.survey = survey;
        this.playSurvey = playSurvey;
        this.logGame = logGame;
        this.logStatistics = logStatistics;
    }

    // Creates a new list of cases for the player to play.
    private void CreateNewList() {
        currentTimestamp = GetTimeStamp();
        caseList.Clear();

        if (playTutorial) {
            caseList.Add(tutorial);
        }

        caseList.AddRange(GetCases(casesPerUsers));

        if (playSurvey) {
            caseList.Add(survey);
        }

        caseList.Add(new Case {
            scene = SceneManager.GetActiveScene().name
        });

        currentCase = 0;
    }

    // Returns a well formatted timestamp.
    private string GetTimeStamp() {
        DateTime now = System.DateTime.Now;
        return now.Year + "-" + now.Month + "-" + now.Day + "-" + now.Hour + "-" + now.Minute +
            "-" + now.Second;
    }

    // Gets the cases to add in a round-robin fashion.
    private List<Case> GetCases(int count) {
        List<Case> lessPlayedCases = new List<Case>();

        // Get the least played study.
        int minValue = studies[0].completion;
        int minIndex = 0;

        for (int i = 0; i < studies.Count; i++) {
            if (studies[i].completion < minValue) {
                minValue = studies[i].completion;
                minIndex = i;
            }
        }

        // Get the least played cases in the least played study.
        if (count < studies[minIndex].cases.Count) {
            studies[minIndex].cases = studies[minIndex].cases.OrderBy(o => o.completion).ToList();
            for (int i = 0; i < count; i++) {
                studies[minIndex].cases[i].RandomizeCurrentMap();
                lessPlayedCases.Add(studies[minIndex].cases[i]);
                studies[minIndex].cases[i].completion++;
                studies[minIndex].completion++;
            }
        } else {
            foreach (Case c in studies[minIndex].cases) {
                c.RandomizeCurrentMap();
                lessPlayedCases.Add(c);
                c.completion++;
                studies[minIndex].completion++;
            }
        }

        // Randomize the play order.
        Shuffle(lessPlayedCases);

        return lessPlayedCases;
    }

    // Retuns the next scene to be played.
    public string GetNextScene() {
        currentCase++;

        if (logging) {
            StopLogging();
        }

        if (currentCase == caseList.Count || caseList.Count == 0) {
            CreateNewList();
        }

        Case c = caseList[currentCase];

        ParameterManager.Instance.Flip = currentCase % 2 == 0 ? true : false;
        ParameterManager.Instance.GenerationMode = 4;
        ParameterManager.Instance.MapDNA = (c.maps == null || c.maps.Count == 0) ? "" :
            c.GetCurrentMap().text;

        return c.scene;
    }

    // Shuffles a list.
    void Shuffle<T>(IList<T> list) {
        var random = new System.Random();
        int n = list.Count;

        while (n > 1) {
            n--;
            int k = random.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
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
        System.IO.File.WriteAllText(surveysDirectory + "/" + currentTimestamp + "_survey.json",
            info + "\n" + answers);
    }

    // Returns the played cases in an array.
    public string[] GetCurrentCasesArray() {
        string[] maps = new string[casesPerUsers];
        for (int i = 0; i < casesPerUsers; i++) {
            maps[i] = playTutorial ? caseList[i + 1].GetCurrentMap().name :
                caseList[i].GetCurrentMap().name;
        }
        return maps;
    }

    /* LOGGING */

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (logGame && !(playTutorial && currentCase == 0) && !(playSurvey
            && currentCase == caseList.Count - 2) && currentCase != caseList.Count - 1) {
            SetupLogging();
        }
    }

    // Sets up the directories.
    private void SetupDirectories() {
        experimentDirectory = Application.persistentDataPath + "/Export/" + experimentName;
        CreateDirectory(experimentDirectory);
        // Create the maps directory if needed.
        foreach (Study s in studies) {
            foreach (Case c in s.cases) {
                CreateDirectory(experimentDirectory + "/" + c.GetCurrentMap().name);
                System.IO.File.WriteAllText(@experimentDirectory + "/" + c.GetCurrentMap().name
                    + "/" + c.GetCurrentMap().name + ".txt", c.GetCurrentMap().text);
                c.completion = (Directory.GetFiles(experimentDirectory + "/"
                    + c.GetCurrentMap().name + "/", "*", SearchOption.AllDirectories).Length
                    - 1) / 2;
                s.completion += c.completion;
            }
        }

        // Create the survey directory if needed.
        surveysDirectory = experimentDirectory + "/Surveys";
        CreateDirectory(surveysDirectory);
    }

    // Sets up logging.
    private void SetupLogging() {
        logStream = File.CreateText(experimentDirectory + "/"
            + caseList[currentCase].GetCurrentMap().name + "/"
            + caseList[currentCase].GetCurrentMap().name + "_" + currentTimestamp + "_log.json");
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
        if (gm != null) {
            gm.LoggingHandshake();
        }

        if (logStatistics) {
            SetupStatisticsLogging();
        }
    }

    // Sets up statistics logging.
    private void SetupStatisticsLogging() {
        statisticsStream = File.CreateText(experimentDirectory + "/"
            + caseList[currentCase].GetCurrentMap().name + "/"
            + caseList[currentCase].GetCurrentMap().name + "_" + currentTimestamp
            + "_statistics.json");
        statisticsStream.AutoFlush = true;

        jGameStatistics = new JsonGameStatistics();
        jTargetStatistics = new JsonTargetStatistics();
    }

    // Starts logging.
    public void StartLogging() {
        logging = true;

        if (logStatistics) {
            loggingStatistics = true;
        }

        foreach (MonoBehaviour monoBehaviour in FindObjectsOfType(typeof(MonoBehaviour))) {
            ILoggable logger = monoBehaviour as ILoggable;
            if (logger != null) {
                logger.SetupLogging();
            }
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
        if (loggingStatistics) {
            LogGameStatistics();
            statisticsStream.Close();
            loggingStatistics = false;
        }

        logging = false;

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
    public void LogShot(float x, float z, float direction, int gunId, int ammoInCharger,
        int totalAmmo) {
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

        if (loggingStatistics) {
            shotCount++;
        }
    }

    // Logs info about the maps.
    public void LogMapInfo(float height, float width, float ts) {
        string infoLog = JsonUtility.ToJson(new JsonMapInfo {
            height = height.ToString(),
            width = width.ToString(),
            tileSize = ts.ToString(),
        });

        WriteLog(infoLog);

        if (loggingStatistics) {
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

        if (loggingStatistics) {
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

        if (loggingStatistics) {
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

        if (loggingStatistics) {
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

        if (loggingStatistics) {
            hitCount++;
        }
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
        jGameStatistics.accuracy = (shotCount > 0) ? (hitCount / (float)shotCount).ToString("n4")
            : "0";
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

}