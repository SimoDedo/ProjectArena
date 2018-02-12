using ExperimentObjects;
using JsonObjects;
using Polimi.GameCollective.Connectivity;
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

    private bool logOffline;
    private bool logOnline;
    private bool logGame;
    private bool logStatistics;

    // List of cases the current player has to play.
    private List<Case> caseList;
    // Directory for this esperiment files.
    private string experimentDirectory;
    // Directory for the surveys.
    private string surveysDirectory;

    // Label of the current game log.
    private string logLabel;
    // Stream writer of the current game log.
    private StreamWriter logStream;
    // String of the current game log.
    private string logString;
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

    // Label of the current statistic log.
    private string statisticsLabel;
    // Stream writer of the current statistic log.
    private StreamWriter statisticsStream;
    // String of the current statistic log.
    private string statisticsString;
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
    // Is the map flip?
    private bool flip = false;
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

    private Queue<Entry> postQueue;

    void Awake() {
        DontDestroyOnLoad(transform.gameObject);
    }

    void Start() {
        caseList = new List<Case>();

        if (ParameterManager.HasInstance() && ParameterManager.Instance.Export == true) {
            logOffline = true;
        } else {
            logOffline = false;
        }

        if (!logOffline && !logOnline) {
            logGame = false;
            logStatistics = false;
        } else {
            if (logOnline) {
                postQueue = new Queue<Entry>();
                SetCompletionOnline();
            }
            if (logOffline) {
                SetupDirectories();
                if (!logOnline) {
                    SetCompletionOffline();
                }
            }

            jLog = new JsonLog {
                log = ""
            };

            if (logGame) {
                jShoot = new JsonShoot();
                jReload = new JsonReload();
                jPosition = new JsonPosition();
                jSpawn = new JsonSpawn();
                jKill = new JsonKill();
                jHit = new JsonHit();
            }

            if (logStatistics) {
                jGameStatistics = new JsonGameStatistics();
                jTargetStatistics = new JsonTargetStatistics();
            }
        }
    }

    void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Update() {
        if (logOnline) {
            if (!RemoteDataManager.Instance.IsRequestPending && postQueue.Count > 0) {
                RemoteDataManager.Instance.SaveData(postQueue.Dequeue());
            }
        }
    }

    /* EXPERIMENT */

    // Sets up the experiment manager.
    public void Setup(Case tutorial, bool playTutorial, List<Study> studies, int casesPerUsers,
        string experimentName, Case survey, bool playSurvey, bool logOnline, bool logGame,
        bool logStatistics) {
        this.tutorial = tutorial;
        this.playTutorial = playTutorial;
        this.studies = studies;
        this.casesPerUsers = casesPerUsers;
        this.experimentName = experimentName;
        this.survey = survey;
        this.playSurvey = playSurvey;
        this.logOnline = logOnline;
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

        caseList.AddRange(logOnline ? GetCasesOnline(casesPerUsers) :
            GetCasesOffline(casesPerUsers));

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

    // Gets the cases to add in a round-robin fashion (offline).
    private List<Case> GetCasesOffline(int count) {
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

    // Gets the cases to add in a round-robin fashion  (online).
    private List<Case> GetCasesOnline(int count) {
        List<Case> lessPlayedCases = new List<Case>();

        // TODO.

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
        // Create the case directories if needed.
        foreach (Study s in studies) {
            foreach (Case c in s.cases) {
                CreateDirectory(experimentDirectory + "/" + c.caseName);
                File.WriteAllText(@experimentDirectory + "/" + c.caseName + "/" +
                    c.GetCurrentMap().name + ".txt", c.GetCurrentMap().text);
            }
        }

        // Create the survey directory if needed.
        surveysDirectory = experimentDirectory + "/Surveys";
        CreateDirectory(surveysDirectory);
    }

    // Sets the completion (online).
    private void SetCompletionOnline() {
        // TODO.
    }

    // Sets the completion (offline).
    private void SetCompletionOffline() {
        foreach (Study s in studies) {
            foreach (Case c in s.cases) {
                c.completion = (Directory.GetFiles(experimentDirectory + "/"
                    + c.caseName + "/", "*", SearchOption.AllDirectories).Length
                    - 1) / 2;
                s.completion += c.completion;
            }
        }
    }

    // Sets up logging.
    private void SetupLogging() {
        if (logOnline) {
            logLabel = caseList[currentCase].GetCurrentMap().name + "_" + currentTimestamp + "_log";
        }
        if (logOffline) {
            logStream = File.CreateText(experimentDirectory + "/"
                + caseList[currentCase].caseName + "/"
                + caseList[currentCase].GetCurrentMap().name + "_" + currentTimestamp + "_log.json");
            logStream.AutoFlush = true;
        }

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
        if (logOnline) {
            statisticsLabel = caseList[currentCase].GetCurrentMap().name + "_" + currentTimestamp
            + "_statistics";
        }
        if (logOffline) {
            statisticsStream = File.CreateText(experimentDirectory + "/"
            + caseList[currentCase].caseName + "/"
            + caseList[currentCase].GetCurrentMap().name + "_" + currentTimestamp
            + "_statistics.json");
            statisticsStream.AutoFlush = true;
        }
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
        if (logOffline) {
            logStream.WriteLine(log);
        }
        if (logOnline) {
            logString += (log + "\n");
        }
    }

    // Writes a log in the statistics stream.
    public void WriteStatisticsLog(string log) {
        if (logOffline) {
            statisticsStream.WriteLine(log);
        }
        if (logOnline) {
            statisticsString += (log + "\n");
        }
    }

    // Stops logging and saves the log.
    public void StopLogging() {
        if (loggingStatistics) {
            LogGameStatistics();
            if (logOffline) {
                statisticsStream.Close();
            }
            if (logOnline) {
                postQueue.Enqueue(new Entry(statisticsLabel, statisticsString, ""));
                statisticsString = "";
            }
            loggingStatistics = false;
        }

        if (logOffline) {
            logStream.Close();
        }
        if (logOnline) {
            postQueue.Enqueue(new Entry(logLabel, logString, ""));
            logString = "";
        }
        logging = false;
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
        Coord coord = NormalizeFlipCoord(x, z);

        jLog.time = Time.time.ToString("n4");
        jLog.type = "player_shot";
        jShoot.x = coord.x.ToString("n4");
        jShoot.y = coord.z.ToString("n4");
        jShoot.direction = NormalizeFlipAngle(direction).ToString("n4");
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
    public void LogMapInfo(float height, float width, float ts, bool f) {
        tileSize = ts;
        flip = f;

        string infoLog = JsonUtility.ToJson(new JsonMapInfo {
            height = height.ToString(),
            width = width.ToString(),
            tileSize = ts.ToString(),
            flip = f.ToString()
        });

        WriteLog(infoLog);

        if (loggingStatistics) {
            WriteStatisticsLog(infoLog);
        }
    }

    // Logs the position (x and z respectively correspond to row and column in matrix notation).
    public void LogPosition(float x, float z, float direction) {
        Coord coord = NormalizeFlipCoord(x, z);

        jLog.time = Time.time.ToString("n4");
        jLog.type = "player_position";
        jPosition.x = coord.x.ToString("n4");
        jPosition.y = coord.z.ToString("n4");
        jPosition.direction = NormalizeFlipAngle(direction).ToString("n4");
        string log = JsonUtility.ToJson(jLog);
        WriteLog(log.Remove(log.Length - 3) + JsonUtility.ToJson(jPosition) + "}");

        if (loggingStatistics) {
            if (lastPosition.x != -1) {
                float delta = EulerDistance(coord.x, coord.z, lastPosition.x, lastPosition.z);
                totalDistance += delta;
                currentDistance += delta;
            }
            lastPosition.x = coord.x;
            lastPosition.z = coord.z;
        }
    }

    // Logs spawn.
    public void LogSpawn(float x, float z, string spawnedEntity) {
        Coord coord = NormalizeFlipCoord(x, z);

        jLog.time = Time.time.ToString("n4");
        jLog.type = "spawn";
        jSpawn.x = coord.x.ToString("n4");
        jSpawn.y = coord.z.ToString("n4");
        jSpawn.spawnedEntity = spawnedEntity;
        string log = JsonUtility.ToJson(jLog);
        WriteLog(log.Remove(log.Length - 3) + JsonUtility.ToJson(jSpawn) + "}");

        if (loggingStatistics) {
            targetSpawn = Time.time;
            initialTargetPosition.x = coord.x;
            initialTargetPosition.z = coord.z;
            initialPlayerPosition = lastPosition;
        }
    }

    // Logs a kill.
    public void LogKill(float x, float z, string killedEnitiy, string killerEntity) {
        Coord coord = NormalizeFlipCoord(x, z);

        jLog.time = Time.time.ToString("n4");
        jLog.type = "kill";
        jKill.x = coord.x.ToString("n4");
        jKill.y = coord.z.ToString("n4");
        jKill.killedEntity = killedEnitiy;
        jKill.killerEntity = killerEntity;
        string log = JsonUtility.ToJson(jLog);
        WriteLog(log.Remove(log.Length - 3) + JsonUtility.ToJson(jKill) + "}");

        if (loggingStatistics) {
            LogTargetStatistics(coord.x, coord.z);
            killCount++;
            mediumKillTime += (Time.time - lastTargetSpawn - mediumKillTime) / killCount;
            mediumKillDistance += (currentDistance - mediumKillDistance) / killCount;
            currentDistance = 0;
            lastTargetSpawn = targetSpawn;
        }
    }

    // Logs a hit.
    public void LogHit(float x, float z, string hittedEntity, string hitterEntity, int damage) {
        Coord coord = NormalizeFlipCoord(x, z);

        jLog.time = Time.time.ToString("n4");
        jLog.type = "hit";
        jHit.x = coord.x.ToString("n4");
        jHit.y = coord.z.ToString("n4");
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

    /* SURVEY*/

    // Tells if I need to save the survey.
    public bool MustSaveSurvey() {
        return !File.Exists(surveysDirectory + "/survey.json") && logOffline;
    }

    // Save survey. This has to be done once.
    public void SaveSurvey(string survey) {
        File.WriteAllText(surveysDirectory + "/survey.json", survey);
    }

    // Saves answers and informations about the experiment.
    public void SaveAnswers(string answers) {
        string info = JsonUtility.ToJson(new JsonInfo {
            experimentName = experimentName,
            playedMaps = GetCurrentCasesArray()
        });
        if (logOnline) {
            postQueue.Enqueue(new Entry(currentTimestamp + "_survey.json", statisticsString, ""));
        }
        if (logOffline) {
            File.WriteAllText(surveysDirectory + "/" + currentTimestamp + "_survey.json",
                info + "\n" + answers);
        }
    }

    // Returns the played cases in an array.
    public string[] GetCurrentCasesArray() {
        try {
            string[] maps = new string[casesPerUsers];
            for (int i = 0; i < casesPerUsers; i++) {
                maps[i] = playTutorial ? caseList[i + 1].GetCurrentMap().name :
                    caseList[i].GetCurrentMap().name;
            }
            return maps;
        } catch {
            return null;
        }
    }

    /* SUPPORT FUNCTIONS */

    // Creates a directory if needed.
    private void CreateDirectory(string directory) {
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    // Returns the euler distance.
    private float EulerDistance(float x1, float y1, float x2, float y2) {
        return Mathf.Sqrt(Mathf.Pow(x1 - x2, 2) + Mathf.Pow(y1 - y2, 2));
    }

    // Normalizes the coordinates and flips them if needed.
    private Coord NormalizeFlipCoord(float x, float z) {
        x /= tileSize;
        z /= tileSize;

        if (flip) {
            return new Coord {
                x = z,
                z = x
            };
        } else {
            return new Coord {
                x = x,
                z = z
            };
        }
    }

    // Normalizes and, if needed, flips an angle with respect to the y = -x axis.
    private float NormalizeFlipAngle(float angle) {
        angle = NormalizeAngle(angle);

        if (flip) {
            angle = NormalizeAngle(angle + 45);
            angle = NormalizeAngle(-1 * angle - 45);
        }

        return angle;
    }

    // If an angle is negative it makes it positive.
    private float NormalizeAngle(float angle) {
        return (angle < 0) ? (360 + angle % 360) : (angle % 360);
    }

}