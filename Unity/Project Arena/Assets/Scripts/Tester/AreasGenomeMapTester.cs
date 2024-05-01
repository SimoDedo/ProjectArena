using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AI;
using Logging;
using Managers;
using Managers.Map;
using Managers.Mode;
using Maps.MapAssembler;
using Maps.MapGenerator;
using Newtonsoft.Json;
using UnityEngine;

namespace Tester
{
    /// <summary>
    /// Deals with instantiating an experiment to test a specific map generated from a given Area list.
    /// </summary>
    public class AreasGenomeMapTester : MonoBehaviour
    {
        private const int DEFAULT_GAME_LENGTH = 240;

        private const int BOT1_ID = 1;
        private const int BOT2_ID = 2;

        [SerializeField] private string baseDataFolderPath;
        [SerializeField] private GameObject botPrefab;
        [SerializeField] private string genomeName;
        [SerializeField] private string bot1ParamsFilenamePrefix;
        [SerializeField] private float bot1SkillLevel = 1.0f;
        [SerializeField] private string bot2ParamsFilenamePrefix;
        [SerializeField] private float bot2SkillLevel = 1.0f;
        [SerializeField] private SLMapManager mapManager;
        [SerializeField] private AreaGenomeMapGenerator genomeMapGenerator;
        [SerializeField] private MapAssembler mapAssembler;
        [SerializeField] private SpawnPointManager spawnPointManager;
        [SerializeField] private int numExperiments = 1;
        [SerializeField] private string folderName;
        [SerializeField] private string experimentName = "experiment";
        [SerializeField] private int gameLength = DEFAULT_GAME_LENGTH;
        [SerializeField] private bool saveMap ;
        [SerializeField] private bool logPositions;
        [SerializeField] private bool logKillDistances;
        [SerializeField] private bool logDeathPositions;

        private readonly List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();
        private GameResultsAnalyzer analyzer;
        private PositionAnalyzer positionAnalyzer;
        private KillDistanceAnalyzer killDistanceAnalyzer;
        private DeathPositionAnalyzer deathPositionAnalyzer;
        private string botsPath;

        private int experimentNumber;
        private string importPath;

        private GenomeTesterGameManager manager;
        private string genomesPath;

        
        private void Awake()
        {
            if (Application.isBatchMode)
            {
                Time.captureFramerate = 24;
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            }

            var args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                if (arg.StartsWith("-dataFolderPath=")) baseDataFolderPath = arg.Substring(16);
                if (arg.StartsWith("-bot1file=")) bot1ParamsFilenamePrefix = arg.Substring(10);
                if (arg.StartsWith("-bot1skill=")) bot1SkillLevel = float.Parse(arg.Substring(11));
                if (arg.StartsWith("-bot2file=")) bot2ParamsFilenamePrefix = arg.Substring(10);
                if (arg.StartsWith("-bot2skill=")) bot2SkillLevel = float.Parse(arg.Substring(11));
                if (arg.StartsWith("-numExperiments=")) numExperiments = int.Parse(arg.Substring(16));
                if (arg.StartsWith("-folderName=")) folderName = arg.Substring(12);
                if (arg.StartsWith("-experimentName=")) experimentName = arg.Substring(16);
                if (arg.StartsWith("-areaFilename=")) genomeName = arg.Substring(14);
                if (arg.StartsWith("-gameLength=")) gameLength = int.Parse(arg.Substring(12));
                if (arg.StartsWith("-saveMap")) saveMap = true;
                if (arg.StartsWith("-logPositions")) logPositions = true;
                if (arg.StartsWith("-logKillDistances")) logKillDistances = true;
                if (arg.StartsWith("-logDeathPositions")) logDeathPositions = true;
            }

            if (string.IsNullOrEmpty(baseDataFolderPath))
            {
                baseDataFolderPath = Application.persistentDataPath;
            }
            importPath = Path.Combine(baseDataFolderPath, "Import");
            genomesPath = Path.Combine(importPath, "Genomes");
            botsPath = Path.Combine(importPath, "Bots");

            if (!Directory.Exists(genomesPath)) Directory.CreateDirectory(genomesPath);
            if (!Directory.Exists(botsPath)) Directory.CreateDirectory(botsPath);

            analyzer = new GameResultsAnalyzer(BOT1_ID, BOT2_ID);
            analyzer.Setup();
            if (logPositions)
            {
                positionAnalyzer = new PositionAnalyzer(BOT1_ID, BOT2_ID);
                positionAnalyzer.Setup();
            }

            if (logKillDistances)
            {
                killDistanceAnalyzer = new KillDistanceAnalyzer(BOT1_ID, BOT2_ID);
                killDistanceAnalyzer.Setup();
            }

            if (logDeathPositions)
            {
                deathPositionAnalyzer = new DeathPositionAnalyzer(BOT1_ID, BOT2_ID);
                deathPositionAnalyzer.Setup();
            }            

            StartNewExperimentGameEvent.Instance.AddListener(NewExperimentStarted);
            ExperimentEndedGameEvent.Instance.AddListener(ExperimentEnded);

            if (saveMap)
            {
                SaveMapTextGameEvent.Instance.AddListener(SaveMap);
            }

            StartNewExperiment();

            // Register start and end experiment events, so that I can finalize the analyzer and
            // reset it
        }
        
        private void SaveMap(string map)
        {
            ExportResults(baseDataFolderPath, map, folderName, "map_" + experimentName, ".txt");
            SaveMapTextGameEvent.Instance.RemoveListener(SaveMap);
        }

        private void StartNewExperiment()
        {
            analyzer.Reset();
            
            var bot1JsonParams = ReadFromFile<JSonBotCharacteristics>(Path.Combine(botsPath, bot1ParamsFilenamePrefix + "_params.json"));
            var bot1Params = new BotCharacteristics(bot1SkillLevel, bot1JsonParams);
            
            var bot2JsonParams = ReadFromFile<JSonBotCharacteristics>(Path.Combine(botsPath, bot2ParamsFilenamePrefix + "_params.json"));
            var bot2Params = new BotCharacteristics(bot2SkillLevel, bot2JsonParams);

            var activeGunsBot1 = ReadFromFile<bool[]>(Path.Combine(botsPath, bot1ParamsFilenamePrefix + "_guns.json"));
            var activeGunsBot2 = ReadFromFile<bool[]>(Path.Combine(botsPath, bot2ParamsFilenamePrefix + "_guns.json"));

            var genome = ReadFromFile<AreasGenome>(Path.Combine(genomesPath, folderName, genomeName));
            genomeMapGenerator.SetGenome(genome);
            mapAssembler.SetMapScale(genome.mapScale);
            
            manager = gameObject.AddComponent<GenomeTesterGameManager>();
            manager.SetParameters(
                botPrefab,
                BOT1_ID,
                bot1Params,
                activeGunsBot1,
                BOT2_ID,
                bot2Params,
                activeGunsBot2,
                mapManager,
                spawnPointManager,
                gameLength,
                respawnDuration: 3.0f
            );
        }

        private void ExperimentEnded()
        {
            Debug.Log("Experiment num " + +experimentNumber + " ended!");

            manager.StopGame();
            Destroy(manager);
            
            results.Add(analyzer.CompileResults(gameLength));

            experimentNumber++;

            if (experimentNumber >= numExperiments)
            {
                if (logKillDistances)
                {
                    var (positions1, positions2) = killDistanceAnalyzer.CompileResultsAsCSV();
                    ExportResults(baseDataFolderPath, positions1, folderName, "kill_distances_" + experimentName + "_bot1", ".csv");
                    ExportResults(baseDataFolderPath, positions2, folderName, "kill_distances_" + experimentName + "_bot2", ".csv");
                }
                if (logDeathPositions)
                {
                    var (positions1, positions2) = deathPositionAnalyzer.CompileResultsAsCSV();
                    ExportResults(baseDataFolderPath, positions1, folderName, "death_positions_" + experimentName + "_bot1", ".csv");
                    ExportResults(baseDataFolderPath, positions2, folderName, "death_positions_" + experimentName + "_bot2", ".csv");
                }

                if (logPositions)
                {
                    var (positions1, positions2) = positionAnalyzer.CompileResultsAsCSV();
                    ExportResults(baseDataFolderPath, positions1, folderName, "position_" + experimentName + "_bot1", ".csv");
                    ExportResults(baseDataFolderPath, positions2, folderName, "position_" + experimentName + "_bot2", ".csv");
                }

                ExportResults(baseDataFolderPath, JsonConvert.SerializeObject(results), folderName, "final_results_" + experimentName);
                Application.Quit();
            }
            else
            {
                mapManager.ResetMap();
                spawnPointManager.Reset();

                StartCoroutine(WaitAndStart());
            }
        }

        private static void ExportResults(string folderPath, string compileResults, string folderName, string experimentName, string extension = ".json")
        {
            var exportPath = Path.Combine(folderPath, "Export", folderName, experimentName);
            
            var filePath = exportPath + extension;
            try
            {
                Debug.Log("Writing to file " + filePath);
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
            yield return new WaitForSeconds(0.2f);
            StartNewExperiment();
        }

        private void NewExperimentStarted()
        {
            Debug.Log("Experiment num " + experimentNumber + " started!");
        }

        private static T ReadFromFile<T>(string filePath)
        {
                Debug.Log("Reading from " + filePath);
                using var reader = new StreamReader(filePath);
                return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
        }

        private static Tuple<BotCharacteristics, bool[]> LoadBotCharacteristics(string botsPath, string botFilename)
        {
            var paramsFile = Path.Combine(botsPath, botFilename + "_params.json");
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

// FIXME I'd like this to be a readonly struct, but there are some problems with serialization in that way...
[Serializable]
public class AreasGenome
{
    public int width;
    public int height;
    public float mapScale;
    public Area[] areas;

    public AreasGenome()
    {
        
    }
    
    public AreasGenome(int width, int height, float mapScale, Area[] areas)
    {
        this.width = width;
        this.height = height;
        this.mapScale = mapScale;
        this.areas = areas;
    }
}
