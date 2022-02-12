using System;
using System.Collections.Generic;
using System.IO;
using Logging;
using Logging.Logging;
using Logging.Logging.Game;
using Logging.Logging.Statistics;
using Managers.Mode;
using Others;
using UnityEngine;
using UnityEngine.SceneManagement;


// TODO Remove this file 

namespace AI
{
    /// <summary>
    /// ExperimentManager allows to manage experiments. An experiment is composed of different studies
    /// (a set of maps), each one composed by cases (a set of map variations). Each time a new
    /// experiment is requested, a list of cases from the less played study is provided to the user
    /// to be played. A tutorial and a survey scene can be added at the beginning and at the end of
    /// the experiment, respectively. When ExperimentManager is used to log online data, before creating
    /// a new list of cases or before saving data on the server, the experiment completion is retrieved
    /// from the server. When sending data to the server this information is stored in the comment field
    /// of each entry as the sum of the retrieved completion and the completion progress stored locally.
    /// </summary>
    public class AIMapTestingLogger : MonoBehaviour, ILoggable
    {
        public string experimentName;
        public string testID = GetTimeStamp();

        // Current distance.
        private readonly Dictionary<int, float> distancesBetweenKills = new Dictionary<int, float>();

        // Total hits.
        private readonly Dictionary<int, int> hitsTaken = new Dictionary<int, int>();
        private readonly Dictionary<int, Vector2> initialPositions = new Dictionary<int, Vector2>();

        // Total destoryed targets.
        private readonly Dictionary<int, int> killCounts = new Dictionary<int, int>();

        // Position of the player.
        private readonly Dictionary<int, Vector2> lastPositions = new Dictionary<int, Vector2>();

        // Total shots.
        private readonly Dictionary<int, int> shotCounts = new Dictionary<int, int>();

        // Total distance.
        private readonly Dictionary<int, float> totalDistances = new Dictionary<int, float>();

        // Directory for this esperiment files.
        private string experimentDirectory;

        // Is the map flip?
        private bool flip;

        // Label of the current game log.
        private string gameLabel;

        // Support object to format the log.
        private JsonGameLog jGameLog;

        // Support object to format the log.
        private JsonAIStatisticsLog jStatisticsLog;

        // Length of the current statistics log.

        // Start time of the log.
        private float logStart;

        // Label of the current map log.
        private string mapLabel;

        // Label of the current statistic log.
        private string statisticsLabel;

        // Size of a maps tile.
        private float tileSize = 1;

        private void Awake()
        {
            var args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
                if (arg.StartsWith("-experiment="))
                    experimentName = arg.Substring(12);

            experimentName ??= "unnamed experiment";
            SetupDirectories();
            PositionInfoGameEvent.Instance.AddListener(LogPosition);
            GameInfoGameEvent.Instance.AddListener(LogGameInfo);
            MapInfoGameEvent.Instance.AddListener(LogMapInfo);
            ReloadInfoGameEvent.Instance.AddListener(LogReload);
            ShotInfoGameEvent.Instance.AddListener(LogShot);
            SpawnInfoGameEvent.Instance.AddListener(LogSpawn);
            KillInfoGameEvent.Instance.AddListener(LogKill);
            HitInfoGameEvent.Instance.AddListener(LogHit);
            SaveMapTextGameEvent.Instance.AddListener(SaveMapText);
        }


        // Sets up logging.
        public void SetupLogging()
        {
            var gm = FindObjectOfType(typeof(GameManager)) as GameManager;
            if (gm != null) gm.LoggingHandshake();

            gameLabel = SceneManager.GetActiveScene().name + "_game";
            statisticsLabel = SceneManager.GetActiveScene().name + "_statistics";
            mapLabel = SceneManager.GetActiveScene().name + "_map";
        }

        private void SaveMapText(string textMap)
        {
            if (textMap != null)
                File.WriteAllText(experimentDirectory + "/" + mapLabel + ".txt", textMap);
        }

        /* EXPERIMENT */

        // Returns a well formatted timestamp.
        private static string GetTimeStamp()
        {
            return DateTime.Now.ToString("yy") + DateTime.Now.ToString("MM") +
                   DateTime.Now.ToString("dd") + DateTime.Now.ToString("HH") +
                   DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") +
                   DateTime.Now.ToString("ff");
        }


        /* LOGGING */

        // Sets up the directories.
        private void SetupDirectories()
        {
            experimentDirectory = Application.persistentDataPath + "/Export/" + experimentName;
            CreateDirectory(experimentDirectory);
        }

        // Starts loggingGame.
        public void StartLogging()
        {
            logStart = Time.time;

            jGameLog = new JsonGameLog(testID);
            jStatisticsLog = new JsonAIStatisticsLog(testID);
            foreach (var o in FindObjectsOfType(typeof(MonoBehaviour)))
            {
                var monoBehaviour = (MonoBehaviour) o;
                if (monoBehaviour is ILoggable logger) logger.SetupLogging();
            }
        }

        // Stops loggingGame and saves the log.
        public void StopLogging()
        {
            // Save the statistics log, if any.
            LogGameStatistics();

            var log = JsonUtility.ToJson(jStatisticsLog);

            File.WriteAllText(experimentDirectory + "/" + statisticsLabel + ".json", log);

            // Save the game log, if any.

            log = JsonUtility.ToJson(jGameLog);

            if (jGameLog.logPart > 0) gameLabel += "_" + jGameLog.logPart;

            File.WriteAllText(experimentDirectory + "/" + gameLabel + ".json", log);
        }


        // Logs info about the maps.
        private void LogMapInfo(MapInfo info)
        {
            tileSize = info.ts;
            flip = info.f;

            var currentMap = SceneManager.GetActiveScene().name;
            jGameLog.mapInfo = new JsonMapInfo(currentMap, info.height, info.width, tileSize, flip);
            jStatisticsLog.mapInfo = new JsonMapInfo(currentMap, info.height, info.width, tileSize, flip);
        }

        // Logs info about the maps.
        private void LogGameInfo(GameInfo info)
        {
            jGameLog.gameInfo = new JsonGameInfo(info.gameDuration, info.scene, experimentName);
            jStatisticsLog.gameInfo = new JsonGameInfo(info.gameDuration, info.scene, experimentName);
        }

        // Logs reload.
        private void LogReload(ReloadInfo info)
        {
            var jReload = new JsonReload(Time.time - logStart, info.ownerId, info.gunId,
                info.ammoInCharger, info.totalAmmo);
            jGameLog.reloadLogs.Add(jReload);
        }

        // Logs the shot.
        private void LogShot(ShotInfo info)
        {
            var coord = NormalizeFlipCoord(info.x, info.z);

            var jShot = new JsonShot(Time.time - logStart, coord.x, coord.z, NormalizeFlipAngle(info.direction),
                info.ownerId, info.gunID, info.ammoInCharger, info.totalAmmo);
            jGameLog.shotLogs.Add(jShot);

            if (shotCounts.ContainsKey(info.ownerId))
                shotCounts[info.ownerId]++;
            else
                shotCounts[info.ownerId] = 1;
        }

        // Logs the position (x and z respectively correspond to row and column in matrix notation).
        private void LogPosition(PositionInfo info)
        {
            var coord = NormalizeFlipCoord(info.x, info.z);

            var jPosition = new JsonPosition(Time.time - logStart, info.entityID, coord.x, coord.z,
                NormalizeFlipAngle(info.dir));
            jGameLog.positionLogs.Add(jPosition);

            if (lastPositions.ContainsKey(info.entityID))
            {
                var lastPosition = lastPositions[info.entityID];
                var delta = EulerDistance(coord.x, coord.z, lastPosition.x, lastPosition.y);

                if (totalDistances.ContainsKey(info.entityID))
                    totalDistances[info.entityID] += delta;
                else
                    totalDistances[info.entityID] = delta;

                if (distancesBetweenKills.ContainsKey(info.entityID))
                    distancesBetweenKills[info.entityID] += delta;
                else
                    distancesBetweenKills[info.entityID] = delta;
            }

            lastPositions[info.entityID] = new Vector2(coord.x, coord.z);
        }

        // Logs spawn.
        private void LogSpawn(SpawnInfo info)
        {
            var coord = NormalizeFlipCoord(info.x, info.z);

            var jSpawn = new JsonSpawn(Time.time - logStart, coord.x, coord.z,
                info.entityId, info.spawnEntity);
            jGameLog.spawnLogs.Add(jSpawn);

            initialPositions[info.entityId] = new Vector2(coord.x, coord.z);
        }

        // Logs a kill.
        private void LogKill(KillInfo info)
        {
            var coord = NormalizeFlipCoord(info.x, info.z);

            var jKill = new JsonKill(Time.time - logStart, coord.x, coord.z, info.killedEntityID, info.killedEntity,
                info.killerEntityID, info.killerEntity);
            jGameLog.killLogs.Add(jKill);

            if (killCounts.ContainsKey(info.killerEntityID))
                killCounts[info.killerEntityID]++;
            else
                killCounts[info.killerEntityID] = 1;

            distancesBetweenKills[info.killerEntityID] = 0;
        }

        // Logs a hit.
        private void LogHit(HitInfo info)
        {
            var coord = NormalizeFlipCoord(info.x, info.z);

            var jHit = new JsonHit(Time.time - logStart, coord.x, coord.z,
                info.hitEntityID,
                info.hitEntity,
                info.hitterEntityID,
                info.hitterEntity,
                info.damage);
            jGameLog.hitLogs.Add(jHit);

            if (hitsTaken.ContainsKey(info.hitEntityID))
                hitsTaken[info.hitEntityID]++;
            else
                hitsTaken[info.hitEntityID] = 1;
        }


        // Logs statistics about the game.
        private void LogGameStatistics()
        {
            var finalStatisticsMap = new Dictionary<int, JsonFinalStatistics>();
            // I'll be using initialPositions to count total number of entities.

            var entities = new List<int>(initialPositions.Keys);

            foreach (var entity in entities)
            {
                var shotCount = shotCounts.ContainsKey(entity) ? shotCounts[entity] : 0;
                var hitCount = hitsTaken.ContainsKey(entity) ? hitsTaken[entity] : 0;
                var totalDistance = totalDistances.ContainsKey(entity) ? totalDistances[entity] : 0;
                var killCount = killCounts.ContainsKey(entity) ? killCounts[entity] : 0;

                var statistic = new JsonFinalStatistics(shotCount, hitCount,
                    shotCount > 0 ? hitCount / (float) shotCount : 0,
                    totalDistance, killCount > 0 ? jStatisticsLog.gameInfo.duration / killCount : 0,
                    killCount > 0 ? totalDistance / killCount : 0);
                finalStatisticsMap[entity] = statistic;
            }

            jStatisticsLog.finalStatistics = finalStatisticsMap;
        }

        /* SURVEY*/

        /* SUPPORT FUNCTIONS */

        // Creates a directory if needed.
        private static void CreateDirectory(string directory)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        // Returns the euler distance.
        private static float EulerDistance(float x1, float y1, float x2, float y2)
        {
            return Mathf.Sqrt(Mathf.Pow(x1 - x2, 2) + Mathf.Pow(y1 - y2, 2));
        }

        // Normalizes the coordinates and flips them if needed.
        private Coord NormalizeFlipCoord(float x, float z)
        {
            x /= tileSize;
            z /= tileSize;

            if (flip)
                return new Coord
                {
                    x = z,
                    z = x
                };

            return new Coord
            {
                x = x,
                z = z
            };
        }

        // Normalizes and, if needed, flips an angle with respect to the y = -x axis.
        private float NormalizeFlipAngle(float angle)
        {
            angle = NormalizeAngle(angle);

            if (flip)
            {
                angle = NormalizeAngle(angle + 45);
                angle = NormalizeAngle(-1 * angle - 45);
            }

            return angle;
        }

        // If an angle is negative it makes it positive.
        private static float NormalizeAngle(float angle)
        {
            return angle < 0 ? 360 + angle % 360 : angle % 360;
        }
    }
}