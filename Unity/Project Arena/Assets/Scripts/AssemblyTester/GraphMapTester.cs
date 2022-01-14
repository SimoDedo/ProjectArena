using System;
using System.Collections;
using System.IO;
using AssemblyLogging;
using ExperimentObjects;
using JsonObjects.Logging.Game;
using UnityEngine;

namespace AssemblyTester
{
    public class GraphMapTester : MonoBehaviour
    {
        [SerializeField] private GameObject botPrefab;
        [SerializeField] private string mapPath;
        [SerializeField] private string bot1ParamsPath;
        [SerializeField] private string bot2ParamsPath;
        [SerializeField] private bool[] activeGunsBot1;
        [SerializeField] private bool[] activeGunsBot2;
        [SerializeField] private MapManager mapManager;
        [SerializeField] private SpawnPointManager spawnPointManager;
        [SerializeField] private int numExperiments = 1;
        [SerializeField] private string experimentName = "experiment";

        // Size of a maps tile.
        private const float TILE_SIZE = 1;

        private GraphTesterGameManager manager;
        private GameResultsAnalyzer analyzer;

        private int experimentNumber;


        private void Awake()
        {
            #if !UNITY_EDITOR
                Time.captureFramerate = 30;
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            #endif
            var args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                if (arg.StartsWith("-bot1file=")) bot1ParamsPath = arg.Substring(10);
                if (arg.StartsWith("-bot2file=")) bot2ParamsPath = arg.Substring(10);
                if (arg.StartsWith("-numExperiments=")) numExperiments = int.Parse(arg.Substring(16));
                if (arg.StartsWith("-experimentName=")) experimentName = arg.Substring(16);
            }

            analyzer = new GameResultsAnalyzer();
            analyzer.Setup();

            StartNewExperimentGameEvent.Instance.AddListener(NewExperimentStarted);
            ExperimentEndedGameEvent.Instance.AddListener(ExperimentEnded);

            StartNewExperiment();

            // Register start and end experiment events, so that I can finalize the analyzer and
            // reset it
        }

        private void StartNewExperiment()
        {
            manager = gameObject.AddComponent<GraphTesterGameManager>();
            var bot1Params = LoadBotCharacteristics(bot1ParamsPath);
            var bot2Params = LoadBotCharacteristics(bot2ParamsPath);
            manager.SetParameters(
                botPrefab,
                bot1Params,
                activeGunsBot1,
                bot2Params,
                activeGunsBot2,
                mapPath,
                mapManager,
                spawnPointManager
            );
        }

        private void ExperimentEnded()
        {
            Debug.Log("Experiment num " + +experimentNumber + " ended!");

            manager.StopGame();
            Destroy(manager);
            
            ExportResults(analyzer.CompileResults(), experimentName, experimentNumber);

            experimentNumber++;

            analyzer.Reset();

            if (experimentNumber >= numExperiments)
            {
                Application.Quit();
            }
            else
            {
                mapManager.ResetMap();
                spawnPointManager.Reset();

                StartCoroutine(WaitAndStart());
            }
        }

        private static void ExportResults(string compileResults, string experimentName, int experimentNum)
        {
            var exportPath = Application.persistentDataPath + "/Export";
            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }

            var filePath = exportPath + "/" + experimentName + experimentNum + ".json";
            try
            {
                using var writer = new StreamWriter(filePath);
                writer.Write(compileResults);
            }
            catch (Exception e)
            {
                Debug.LogError("Couldn't export results!, error " + e.Message);
            }
        }

        private IEnumerator WaitAndStart()
        {
            yield return new WaitForSeconds(1);
            mapManager.ManageMap(true);
            StartNewExperiment();
        }

        private void NewExperimentStarted()
        {
            Debug.Log("Experiment num " + experimentNumber + " started!");
        }

        private static BotCharacteristics LoadBotCharacteristics(string botFilename)
        {
            var importPath = Application.persistentDataPath + "/Import";
            if (!Directory.Exists(importPath))
            {
                Directory.CreateDirectory(importPath);
            }

            var filePath = importPath + "/" + botFilename;
            try
            {
                using var reader = new StreamReader(filePath);
                var botParams = reader.ReadToEnd();
                return JsonUtility.FromJson<BotCharacteristics>(botParams);
            }
            catch (Exception)
            {
                var rtn = BotCharacteristics.Default;
                try
                {
                    var json = JsonUtility.ToJson(rtn, true);
                    using var writer = new StreamWriter(filePath);
                    writer.Write(json);
                    writer.Close();
                }
                catch (Exception)
                {
                    // Ignored, could not generate default file.
                }

                return rtn;
            }
        }

        // Logs the position (x and z respectively correspond to row and column in matrix notation).
        private void LogPosition(PositionInfo info)
        {
            Debug.Log("Log position");
            var coord = NormalizeFlipCoord(info.x, info.z);

            // var jPosition = new JsonPosition(Time.time - logStart, info.entityID, coord.x, coord.z,
            //     NormalizeAngle(info.dir));
            // jGameLog.positionLogs.Add(jPosition);
            //
            // if (lastPositions.ContainsKey(info.entityID))
            // {
            //     var lastPosition = lastPositions[info.entityID];
            //     var delta = EulerDistance(coord.x, coord.z, lastPosition.x, lastPosition.y);
            //
            //     if (totalDistances.ContainsKey(info.entityID))
            //         totalDistances[info.entityID] += delta;
            //     else
            //         totalDistances[info.entityID] = delta;
            //
            //     if (distancesBetweenKills.ContainsKey(info.entityID))
            //         distancesBetweenKills[info.entityID] += delta;
            //     else
            //         distancesBetweenKills[info.entityID] = delta;
            // }
            //
            // lastPositions[info.entityID] = new Vector2(coord.x, coord.z);
        }

        // Normalizes the coordinates and flips them if needed.
        private static Coord NormalizeFlipCoord(float x, float z)
        {
            x /= TILE_SIZE;
            z /= TILE_SIZE;

            return new Coord {x = x, z = z};
        }

        // If an angle is negative it makes it positive.
        private static float NormalizeAngle(float angle)
        {
            return (angle < 0) ? (360 + angle % 360) : (angle % 360);
        }

        // Returns the euler distance.
        private static float EulerDistance(float x1, float y1, float x2, float y2)
        {
            return Mathf.Sqrt(Mathf.Pow(x1 - x2, 2) + Mathf.Pow(y1 - y2, 2));
        }
    }
}