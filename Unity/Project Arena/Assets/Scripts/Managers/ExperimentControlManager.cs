using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Connectivity;
using Logging.Logging;
using Logging.Logging.Game;
using Logging.Logging.Statistics;
using Logging.Logging.Survey;
using Maps;
using UnityEngine;

// TODO This file was broken. I have tried to fix it, but I don't know if I did that correctly

namespace Managers
{
    /// <summary>
    ///     Allows to start a new experiment and download its logs.
    /// </summary>
    public static class ExperimentControlManager
    {
        // Resets the completion of the experiment to start a new one.
        public static IEnumerator ResetCompletionAttempt()
        {
            RemoteDataManager.Instance.SaveData(ConnectionSettings.SERVER_RESET_LABEL, "", "");

            while (!RemoteDataManager.Instance.IsResultReady) yield return new WaitForSeconds(0.25f);
        }

        // Downloads all the logs of the current experiment and eventually merge them and reconstruct the
        // statistics log.
        public static IEnumerator DowloadAllAttempt(string downloadDirectory, bool mergeLogs)
        {
            RemoteDataManager.Instance.GetLastEntry();

            while (!RemoteDataManager.Instance.IsResultReady) yield return new WaitForSeconds(0.25f);

            int downloadCount;
            try
            {
                downloadCount = JsonUtility.FromJson<JsonCompletionTracker>(
                    RemoteDataManager.Instance.Result.Split('|')[4]).logsCount;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                yield break;
            }

            var statisticsLogs = new List<JsonStatisticsLog>();
            var gameLogs = new List<JsonGameLog>();
            var refinedStatisticsLogs = new List<JsonStatisticsLog>();
            var refinedGameLogs = new List<JsonGameLog>();

            RemoteDataManager.Instance.GetLastEntries(downloadCount);

            while (!RemoteDataManager.Instance.IsResultReady) yield return new WaitForSeconds(0.25f);

            var results = RemoteDataManager.Instance.Result.Split('\n');

            try
            {
                foreach (var result in results)
                {
                    var resultFields = result.Split('|');
                    if (resultFields.Length == 6)
                        if (resultFields[2] != ConnectionSettings.SERVER_COMPLETION_LABEL &&
                            resultFields[2] != ConnectionSettings.SERVER_RESET_LABEL)
                            switch (resultFields[2])
                            {
                                case ConnectionSettings.SERVER_GAME_LABEL:
                                    if (mergeLogs)
                                    {
                                        gameLogs.Add(
                                            JsonUtility.FromJson<JsonGameLog>(resultFields[3]));
                                    }
                                    else
                                    {
                                        var jGameLog =
                                            JsonUtility.FromJson<JsonGameLog>(resultFields[3]);
                                        File.WriteAllText(downloadDirectory + "/" + jGameLog.testID +
                                                          "_" + jGameLog.mapInfo.name + "_game_" + jGameLog.logPart
                                                          + ".json", resultFields[3]);
                                    }

                                    break;
                                case ConnectionSettings.SERVER_STATISTICS_LABEL:
                                    if (mergeLogs)
                                    {
                                        statisticsLogs.Add(
                                            JsonUtility.FromJson<JsonStatisticsLog>(resultFields[3]));
                                    }
                                    else
                                    {
                                        var jStatisticLog =
                                            JsonUtility.FromJson<JsonStatisticsLog>(resultFields[3]);
                                        File.WriteAllText(downloadDirectory + "/" + jStatisticLog.testID
                                                          + "_" + jStatisticLog.mapInfo.name + "_statistics_" +
                                                          jStatisticLog.logPart + ".json",
                                            JsonUtility.ToJson(jStatisticLog));
                                    }

                                    break;
                                case ConnectionSettings.SERVER_ANSWERS_LABEL:
                                    var jAnswers =
                                        JsonUtility.FromJson<JsonAnswers>(resultFields[3]);
                                    File.WriteAllText(downloadDirectory + "/" + jAnswers.testID +
                                                      "_answers" + ".json", resultFields[3]);
                                    break;
                            }
                }

                if (mergeLogs)
                {
                    // Merge the game logs.
                    foreach (var gameLog in gameLogs)
                        if (gameLog.logPart == 0)
                        {
                            var refinedGameLog = gameLog;

                            var count = 0;
                            var lastPart = 0;

                            foreach (var gl in gameLogs)
                                if (gl.testID == gameLog.testID &&
                                    gl.mapInfo.name == gameLog.mapInfo.name && gl.logPart > 0)
                                {
                                    if (gl.logPart > lastPart) lastPart = gl.logPart;

                                    refinedGameLog.hitLogs.AddRange(gl.hitLogs);
                                    refinedGameLog.killLogs.AddRange(gl.killLogs);
                                    refinedGameLog.positionLogs.AddRange(gl.positionLogs);
                                    refinedGameLog.reloadLogs.AddRange(gl.reloadLogs);
                                    refinedGameLog.shotLogs.AddRange(gl.shotLogs);
                                    refinedGameLog.spawnLogs.AddRange(gl.spawnLogs);
                                    count++;
                                }

                            refinedGameLog.hitLogs.Sort((p, q) => p.timestamp.CompareTo(q.timestamp));
                            refinedGameLog.killLogs.Sort((p, q) => p.timestamp.CompareTo(q.timestamp));
                            refinedGameLog.positionLogs.Sort((p, q) => p.timestamp.CompareTo(q.timestamp));
                            refinedGameLog.reloadLogs.Sort((p, q) => p.timestamp.CompareTo(q.timestamp));
                            refinedGameLog.shotLogs.Sort((p, q) => p.timestamp.CompareTo(q.timestamp));
                            refinedGameLog.spawnLogs.Sort((p, q) => p.timestamp.CompareTo(q.timestamp));

                            // Debug.Log(gameLog.testID + "-" + gameLog.mapInfo.name + " -> " +
                            //    lastPart + "/" + count);

                            // Set the part to -1 if the log is incomplete, to 0 if it is complete.
                            if (lastPart != count)
                                refinedGameLog.logPart = -1;
                            else
                                refinedGameLog.logPart = 0;

                            refinedGameLogs.Add(refinedGameLog);
                        }

                    // Merge the statistics logs.
                    foreach (var statisticsLog in statisticsLogs)
                        if (statisticsLog.logPart == 0)
                        {
                            var refinedStatisticLog = statisticsLog;

                            foreach (var sl in statisticsLogs)
                                if (sl.testID == statisticsLog.testID &&
                                    sl.mapInfo.name == statisticsLog.mapInfo.name && sl.logPart > 0)
                                {
                                    refinedStatisticLog.finalStatistics = sl.finalStatistics;

                                    refinedStatisticLog.targetStatisticsLogs.AddRange(
                                        sl.targetStatisticsLogs);
                                }

                            refinedStatisticLog.targetStatisticsLogs.Sort((p, q) =>
                                p.timestamp.CompareTo(q.timestamp));
                            refinedStatisticsLogs.Add(refinedStatisticLog);
                        }

                    gameLogs.Clear();
                    statisticsLogs.Clear();

                    // Generate the statistics from the game log if they have not been retrieved or if 
                    // they have not been saved correctly (no target kills logged).
                    foreach (var gameLog in refinedGameLogs)
                    {
                        JsonStatisticsLog statisticsLog = null;

                        foreach (var sl in refinedStatisticsLogs)
                            if (sl.testID == gameLog.testID && sl.mapInfo.name == gameLog.mapInfo.name)
                            {
                                statisticsLog = sl;
                                break;
                            }

                        if (statisticsLog == null)
                        {
                            statisticsLog = new JsonStatisticsLog(gameLog.testID)
                            {
                                mapInfo = gameLog.mapInfo,
                                gameInfo = gameLog.gameInfo,
                                finalStatistics = new JsonFinalStatistics(0, 0, 0, 0, 0, 0)
                            };
                            refinedStatisticsLogs.Add(statisticsLog);
                        }

                        // Set the log part to know if the statistics are complete.
                        statisticsLog.logPart = gameLog.logPart;

                        if (statisticsLog.targetStatisticsLogs.Count == 0)
                        {
                            var targetStatistics =
                                new List<JsonTargetStatistics>();

                            foreach (var k in gameLog.killLogs)
                                targetStatistics.Add(new JsonTargetStatistics((float) k.timestamp, 0, 0,
                                    0, 0, (float) k.x, (float) k.y, 0, 0, 0));

                            statisticsLog.targetStatisticsLogs = targetStatistics;

                            statisticsLog.finalStatistics.totalHits = gameLog.hitLogs.Count;
                            statisticsLog.finalStatistics.totalShots = gameLog.shotLogs.Count;
                            statisticsLog.finalStatistics.accuracy =
                                statisticsLog.finalStatistics.totalShots == 0
                                    ? 0
                                    : statisticsLog.finalStatistics.totalHits /
                                      (statisticsLog.finalStatistics.totalShots * 1f);
                            statisticsLog.finalStatistics.coveredDistance = 0;
                            statisticsLog.finalStatistics.mediumKillTime =
                                statisticsLog.targetStatisticsLogs.Count == 0
                                    ? 0
                                    : statisticsLog.gameInfo.duration * 1f /
                                      statisticsLog.targetStatisticsLogs.Count;
                            statisticsLog.finalStatistics.mediumKillDistance =
                                statisticsLog.targetStatisticsLogs.Count == 0
                                    ? 0
                                    : statisticsLog.finalStatistics.coveredDistance /
                                      statisticsLog.targetStatisticsLogs.Count;
                        }
                    }

                    // Save the game logs.
                    foreach (var gameLog in refinedGameLogs)
                        File.WriteAllText(downloadDirectory + "/" + gameLog.testID + "_" +
                                          gameLog.mapInfo.name + "_game" +
                                          (gameLog.logPart == -1 ? "_incomplete" : "") +
                                          ".json", JsonUtility.ToJson(gameLog));

                    // Save the statistics logs.
                    foreach (var statisticsLog in refinedStatisticsLogs)
                        File.WriteAllText(downloadDirectory + "/" + statisticsLog.testID + "_" +
                                          statisticsLog.mapInfo.name + "_statistics" +
                                          (statisticsLog.finalStatistics.coveredDistance == 0 ? "_generated" : "") +
                                          (statisticsLog.logPart == -1 ? "_incomplete" : "") + ".json",
                            JsonUtility.ToJson(statisticsLog));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        // Downloads all the logs of the current experiment and creates a dataset for each map with the 
        // set of cordinates of the player.
        public static IEnumerator CreateHeatDatasetAttempt(string downloadDirectory)
        {
            RemoteDataManager.Instance.GetLastEntry();

            while (!RemoteDataManager.Instance.IsResultReady) yield return new WaitForSeconds(0.25f);

            int downloadCount;
            try
            {
                downloadCount = JsonUtility.FromJson<JsonCompletionTracker>(
                    RemoteDataManager.Instance.Result.Split('|')[4]).logsCount;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                yield break;
            }

            RemoteDataManager.Instance.GetLastEntries(downloadCount);

            while (!RemoteDataManager.Instance.IsResultReady) yield return new WaitForSeconds(0.25f);

            var results = RemoteDataManager.Instance.Result.Split('\n');

            var coords = new List<List<Coord>>();
            var mapIndexDictionary = new Dictionary<string, int>();


            try
            {
                foreach (var result in results)
                {
                    var resultFields = result.Split('|');
                    if (resultFields.Length == 6 && resultFields[2] ==
                        ConnectionSettings.SERVER_GAME_LABEL)
                    {
                        var jGameLog =
                            JsonUtility.FromJson<JsonGameLog>(resultFields[3]);

                        // Remove the placement ID from the map name.
                        var name = Regex.Replace(jGameLog.mapInfo.name, @"[\d-]", string.Empty);
                        int index;

                        if (mapIndexDictionary.ContainsKey(name))
                        {
                            index = mapIndexDictionary[name];
                            // Debug.Log("Retrieved index " + index + " for map " + name + ".");
                        }
                        else
                        {
                            index = coords.Count;
                            mapIndexDictionary.Add(name, index);
                            // Debug.Log("Added " + name + " with index " + index + " to the dictionary");
                            coords.Add(new List<Coord>());
                        }

                        foreach (var position in jGameLog.positionLogs)
                            if (jGameLog.mapInfo.flip)
                                coords[index].Add(new Coord(
                                    (int) jGameLog.mapInfo.height - (int) position.y - 1,
                                    (int) jGameLog.mapInfo.width - (int) position.x - 1));
                            else
                                coords[index].Add(new Coord((int) position.x, (int) position.y));
                    }
                }

                for (var i = 0; i < coords.Count; i++)
                {
                    var data = "";

                    foreach (var coord in coords[i]) data += coord.tileX + ";" + coord.tileY + "\n";

                    File.WriteAllText(downloadDirectory + "/heatmap_" +
                                      mapIndexDictionary.FirstOrDefault(x => x.Value == i).Key + ".csv", data);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        // Gets the starting index of a log given its identifier.
        public static IEnumerator GetLogIndexAttempt(string id, int range)
        {
            try
            {
                RemoteDataManager.Instance.GetLastEntries(range);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            while (!RemoteDataManager.Instance.IsResultReady) yield return new WaitForSeconds(0.25f);

            try
            {
                var results = RemoteDataManager.Instance.Result.Split('\n');

                for (var i = results.Length - 1; i >= 0; i--)
                {
                    var resultFields = results[i].Split('|');
                    if (resultFields.Length == 6 && resultFields[2] ==
                        ConnectionSettings.SERVER_GAME_LABEL)
                    {
                        var jGameLog =
                            JsonUtility.FromJson<JsonGameLog>(resultFields[3]);

                        if (jGameLog.testID == id)
                        {
                            Debug.Log("The index of '" + id + "' is " + i + ".");
                            break;
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }
}