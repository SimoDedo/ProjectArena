using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AssemblyLogging;
using JsonObjects.Logging.Game;
using Newtonsoft.Json;
using UnityEngine;

namespace AssemblyTester
{
    public class GraphMapTester : MonoBehaviour
    {
        [SerializeField] private GameObject botPrefab;
        [SerializeField] private string mapName;
        [SerializeField] private string bot1ParamsFilenamePrefix;
        [SerializeField] private string bot2ParamsFilenamePrefix;
        [SerializeField] private MapManager mapManager;
        [SerializeField] private SpawnPointManager spawnPointManager;
        [SerializeField] private int numExperiments = 1;
        [SerializeField] private string experimentName = "experiment";
        private const int GAME_LENGTH = 600;
        private string importPath;
        private string mapsPath;
        private string botsPath;

        private const int BOT1_ID = 1;
        private const int BOT2_ID = 2;

        // Size of a maps tile.
        private const float TILE_SIZE = 1;

        private GraphTesterGameManager manager;
        private GameResultsAnalyzer analyzer;

        private int experimentNumber;

        private readonly List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();

        private void Awake()
        {
            #if !UNITY_EDITOR
                Time.captureFramerate = 30;
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            #endif
            var args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                if (arg.StartsWith("-bot1file=")) bot1ParamsFilenamePrefix = arg.Substring(10);
                if (arg.StartsWith("-bot2file=")) bot2ParamsFilenamePrefix = arg.Substring(10);
                if (arg.StartsWith("-numExperiments=")) numExperiments = int.Parse(arg.Substring(16));
                if (arg.StartsWith("-experimentName=")) experimentName = arg.Substring(16);
                if (arg.StartsWith("-mapName=")) mapName = arg.Substring(9);
            }

            importPath = Application.persistentDataPath + "/Import/";
            mapsPath = importPath + "Maps/";
            botsPath = importPath + "Bots/";

            if (!Directory.Exists(importPath))
            {
                Directory.CreateDirectory(importPath);
            }

            if (!Directory.Exists(mapsPath))
            {
                Directory.CreateDirectory(mapsPath);
            }

            if (!Directory.Exists(botsPath))
            {
                Directory.CreateDirectory(botsPath);
            }

            analyzer = new GameResultsAnalyzer(BOT1_ID, BOT2_ID);
            analyzer.Setup();

            StartNewExperimentGameEvent.Instance.AddListener(NewExperimentStarted);
            ExperimentEndedGameEvent.Instance.AddListener(ExperimentEnded);

            StartNewExperiment();

            // Register start and end experiment events, so that I can finalize the analyzer and
            // reset it
        }

        private void StartNewExperiment()
        {
            analyzer.Reset();

            manager = gameObject.AddComponent<GraphTesterGameManager>();
            var (bot1Params, activeGunsBot1) = LoadBotCharacteristics(botsPath, bot1ParamsFilenamePrefix);
            var (bot2Params, activeGunsBot2) = LoadBotCharacteristics(botsPath, bot2ParamsFilenamePrefix);

            var mapPath = mapsPath + mapName + ".txt";
            if (!File.Exists(mapPath))
            {
                Debug.LogWarning("File " + mapPath + " doesn't exist!");
                mapPath = null;
            }

            manager.SetParameters(botPrefab,
                BOT1_ID,
                bot1Params,
                activeGunsBot1,
                BOT2_ID,
                bot2Params,
                activeGunsBot2,
                mapPath,
                mapManager,
                spawnPointManager,
                GAME_LENGTH,
                respawnDuration: 1.5f
            );
        }

        private void ExperimentEnded()
        {
            Debug.Log("Experiment num " + +experimentNumber + " ended!");

            manager.StopGame();
            Destroy(manager);

            // TODO provide correct length 
            results.Add(analyzer.CompileResults(GAME_LENGTH));

            experimentNumber++;
            
            if (experimentNumber >= numExperiments)
            {
                ExportResults(JsonConvert.SerializeObject(results), experimentName);
                Application.Quit();
            }
            else
            {
                mapManager.ResetMap();
                spawnPointManager.Reset();

                StartCoroutine(WaitAndStart());
            }
        }

        private static void ExportResults(string compileResults, string experimentName)
        {
            var exportPath = Application.persistentDataPath + "/Export/" + experimentName;
            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }

            var filePath = exportPath + "/" + "result.json";
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

        private static Tuple<BotCharacteristics, bool[]> LoadBotCharacteristics(string botsPath, string botFilename)
        {
            var paramsFile = botsPath + botFilename + "params.json";
            var botParams = BotCharacteristics.Default;
            try
            {
                using var reader = new StreamReader(paramsFile);
                botParams = JsonConvert.DeserializeObject<BotCharacteristics>(reader.ReadToEnd());
            }
            catch (Exception)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(botParams, Formatting.Indented);
                    using var writer = new StreamWriter(paramsFile);
                    writer.Write(json);
                    writer.Close();
                }
                catch (Exception)
                {
                    // Ignored, could not generate default file.
                }
            }

            var gunsFile = botsPath + botFilename + "guns.json";
            var guns = new[] {true, true, true, true};
            try
            {
                using var reader = new StreamReader(gunsFile);
                guns = JsonConvert.DeserializeObject<bool[]>(reader.ReadToEnd());
            }
            catch (Exception)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(guns);
                    using var writer = new StreamWriter(gunsFile);
                    writer.Write(json);
                    writer.Close();
                }
                catch (Exception)
                {
                    // Ignored, could not generate default file.
                }
            }

            return new Tuple<BotCharacteristics, bool[]>(botParams, guns);
        }
    }
}