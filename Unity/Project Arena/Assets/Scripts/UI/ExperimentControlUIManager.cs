using JsonObjects.Logging;
using JsonObjects.Logging.Game;
using JsonObjects.Logging.Statistics;
using JsonObjects.Logging.Survey;
using Polimi.GameCollective.Connectivity;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// This class manages the UI of the experiment control menu. The UI changes depending on the
/// current version and on the build platform. This class allows to start a new experiment and
/// download all the logs of the current experiment.
/// </summary>
public class ExperimentControlUIManager : MonoBehaviour {

    [Header("UI")] [SerializeField] private Button exitButton;
    [SerializeField] private Button downloadAllButton;
    [SerializeField] private Button resetCompletionButton;
    [SerializeField] private Button onlineExperimentButton;
    [SerializeField] private Button offlineExperimentButton;
    [SerializeField] private Button completeExperimentButton;
    [SerializeField] private Button directoryButton;
    [SerializeField] private RotateTranslateByAxis backgroundScript;

    [Header("Experiment")] [SerializeField] private string experimentScene;

    private string downloadDirectory;
    private bool exitMustQuit;

    void Awake() {
        Cursor.lockState = CursorLockMode.None;

        if (ParameterManager.HasInstance()) {
            backgroundScript.SetRotation(ParameterManager.Instance.BackgroundRotation);
            if (ParameterManager.Instance.Version == ParameterManager.BuildVersion.COMPLETE) {
                SetExitButton(false);
            } else {
                SetExitButton(true);
            }
        } else {
            SetExitButton(true);
        }

        if (Application.platform != RuntimePlatform.WebGLPlayer) {
            offlineExperimentButton.interactable = true;
            completeExperimentButton.interactable = true;
            directoryButton.interactable = true;
            downloadAllButton.interactable = true;
            downloadDirectory = Application.persistentDataPath + "/Downloads";
            if (!Directory.Exists(downloadDirectory))
                Directory.CreateDirectory(downloadDirectory);
        }
    }

    public void Exit() {
        if (exitMustQuit) {
            Application.Quit();
        } else {
            ParameterManager.Instance.BackgroundRotation = backgroundScript.GetRotation();
            SceneManager.LoadScene("Menu");
        }
    }

    public void ResetCompletion() {
        SetButtonsInteractable(false);
        StartCoroutine(ResetCompletionAttempt());
    }

    public void DowloadAll() {
        SetButtonsInteractable(false);
        StartCoroutine(DowloadAllAttempt());
    }

    private IEnumerator ResetCompletionAttempt() {
        RemoteDataManager.Instance.SaveData(ConnectionSettings.SERVER_RESET_LABEL, "", "");

        while (!RemoteDataManager.Instance.IsResultReady) {
            yield return new WaitForSeconds(0.25f);
        }

        SetButtonsInteractable(true);
    }

    private IEnumerator DowloadAllAttempt() {
        RemoteDataManager.Instance.GetLastEntry();

        while (!RemoteDataManager.Instance.IsResultReady) {
            yield return new WaitForSeconds(0.25f);
        }

        try {
            int downloadCount = JsonUtility.FromJson<JsonCompletionTracker>(
                RemoteDataManager.Instance.Result.Split('|')[4]).logsCount;

            List<JsonStatisticsLog> statisticsLogs = new List<JsonStatisticsLog>();
            List<JsonGameLog> gameLogs = new List<JsonGameLog>();
            List<JsonStatisticsLog> refinedStatisticsLogs = new List<JsonStatisticsLog>();
            List<JsonGameLog> refinedGameLogs = new List<JsonGameLog>();

            RemoteDataManager.Instance.GetLastEntries(downloadCount);

            while (!RemoteDataManager.Instance.IsResultReady) {
                yield return new WaitForSeconds(0.25f);
            }

            string[] results = RemoteDataManager.Instance.Result.Split('\n');

            foreach (string result in results) {
                string[] resultFields = result.Split('|');
                if (resultFields.Length == 6) {
                    if (resultFields[2] != ConnectionSettings.SERVER_COMPLETION_LABEL &&
                        resultFields[2] != ConnectionSettings.SERVER_RESET_LABEL) {

                        switch (resultFields[2]) {
                            case ConnectionSettings.SERVER_GAME_LABEL:
                                gameLogs.Add(
                                    JsonUtility.FromJson<JsonGameLog>(resultFields[3]));
                                break;
                            case ConnectionSettings.SERVER_STATISTICS_LABEL:
                                statisticsLogs.Add(
                                    JsonUtility.FromJson<JsonStatisticsLog>(resultFields[3]));
                                break;
                            case ConnectionSettings.SERVER_ANSWERS_LABEL:
                                JsonAnswers jAnswers =
                                    JsonUtility.FromJson<JsonAnswers>(resultFields[3]);
                                File.WriteAllText(downloadDirectory + "/" + jAnswers.testID +
                                    "_answers" + ".json", resultFields[3]);
                                break;
                        }


                    }
                }
            }

            // Merge the game logs.
            foreach (JsonGameLog gameLog in gameLogs) {
                if (gameLog.logPart == 0) {
                    JsonGameLog refinedGameLog = gameLog;

                    foreach (JsonGameLog gl in gameLogs) {
                        bool started = false;

                        if (gl.testID == gameLog.testID && gl.mapInfo.name == gameLog.mapInfo.name
                            && gl.logPart > 0) {
                            if (!started) {
                                started = true;
                            }
                            refinedGameLog.hitLogs.AddRange(gl.hitLogs);
                            refinedGameLog.killLogs.AddRange(gl.killLogs);
                            refinedGameLog.positionLogs.AddRange(gl.positionLogs);
                            refinedGameLog.reloadLogs.AddRange(gl.reloadLogs);
                            refinedGameLog.shotLogs.AddRange(gl.shotLogs);
                            refinedGameLog.spawnLogs.AddRange(gl.spawnLogs);
                        } else {
                            // When I stop seing inherent logs I stop.
                            if (started) {
                                break;
                            }
                        }
                    }

                    refinedGameLog.hitLogs.Sort((p, q) => p.timestamp.CompareTo(q.timestamp));
                    refinedGameLog.killLogs.Sort((p, q) => p.timestamp.CompareTo(q.timestamp));
                    refinedGameLog.positionLogs.Sort((p, q) => p.timestamp.CompareTo(q.timestamp));
                    refinedGameLog.reloadLogs.Sort((p, q) => p.timestamp.CompareTo(q.timestamp));
                    refinedGameLog.shotLogs.Sort((p, q) => p.timestamp.CompareTo(q.timestamp));
                    refinedGameLog.spawnLogs.Sort((p, q) => p.timestamp.CompareTo(q.timestamp));

                    refinedGameLogs.Add(refinedGameLog);
                }
            }

            // Merge the statistics logs.
            foreach (JsonStatisticsLog statisticsLog in statisticsLogs) {
                if (statisticsLog.logPart == 0) {
                    JsonStatisticsLog refinedStatisticLog = statisticsLog;

                    foreach (JsonStatisticsLog sl in statisticsLogs) {
                        bool started = false;

                        if (sl.testID == statisticsLog.testID &&
                            sl.mapInfo.name == statisticsLog.mapInfo.name && sl.logPart > 0) {
                            if (!started) {
                                break;
                            }
                            refinedStatisticLog.finalStatistics = sl.finalStatistics;
                            refinedStatisticLog.targetStatisticsLogs.AddRange(
                                sl.targetStatisticsLogs);
                        } else {
                            // When I stop seing inherent logs I stop.
                            if (started) {
                                break;
                            }
                        }
                    }

                    refinedStatisticLog.targetStatisticsLogs.Sort((p, q) =>
                        p.timestamp.CompareTo(q.timestamp));
                    refinedStatisticsLogs.Add(refinedStatisticLog);
                }
            }

            gameLogs.Clear();
            statisticsLogs.Clear();

            // Generate the statistics from the game log if they have not been retrieved or if they
            // have not been saved correctly (no target kills logged).
            foreach (JsonGameLog gameLog in refinedGameLogs) {
                JsonStatisticsLog statisticsLog = null;

                foreach (JsonStatisticsLog sl in refinedStatisticsLogs) {
                    if (sl.testID == gameLog.testID && sl.mapInfo.name == gameLog.mapInfo.name) {
                        statisticsLog = sl;
                        break;
                    }
                }

                if (statisticsLog == null) {
                    statisticsLog = new JsonStatisticsLog(gameLog.testID) {
                        mapInfo = gameLog.mapInfo,
                        gameInfo = gameLog.gameInfo,
                        finalStatistics = new JsonFinalStatistics(0, 0, 0, 0, 0, 0)
                    };
                }

                if (statisticsLog.targetStatisticsLogs.Count == 0) {
                    List<JsonTargetStatistics> targetStatistics = new List<JsonTargetStatistics>();

                    foreach (JsonKill k in gameLog.killLogs) {
                        targetStatistics.Add(new JsonTargetStatistics((float)k.timestamp, 0, 0, 0,
                            0, (float)k.x, (float)k.y, 0, 0, 0));
                    }

                    statisticsLog.targetStatisticsLogs = targetStatistics;

                    statisticsLog.finalStatistics.totalHits = gameLog.hitLogs.Count;
                    statisticsLog.finalStatistics.totalShots = gameLog.shotLogs.Count;
                    statisticsLog.finalStatistics.accuracy =
                        (statisticsLog.finalStatistics.totalShots == 0) ? 0 :
                        statisticsLog.finalStatistics.totalHits /
                        (statisticsLog.finalStatistics.totalShots * 1f);
                    statisticsLog.finalStatistics.coveredDistance = 0;
                    statisticsLog.finalStatistics.mediumKillTime =
                        (statisticsLog.targetStatisticsLogs.Count == 0) ? 0 :
                        (statisticsLog.gameInfo.duration * 1f) /
                        statisticsLog.targetStatisticsLogs.Count;
                    statisticsLog.finalStatistics.mediumKillDistance =
                        (statisticsLog.targetStatisticsLogs.Count == 0) ? 0 :
                        statisticsLog.finalStatistics.coveredDistance / 
                        statisticsLog.targetStatisticsLogs.Count;
                }
            }

            // Save the game logs.
            foreach (JsonGameLog gameLog in refinedGameLogs) {
                File.WriteAllText(downloadDirectory + "/" + gameLog.testID + "_" +
                    gameLog.mapInfo.name + "_game.json", JsonUtility.ToJson(gameLog));
            }

            // Save the statistics logs.
            foreach (JsonStatisticsLog statisticsLog in refinedStatisticsLogs) {
                File.WriteAllText(downloadDirectory + "/" + statisticsLog.testID + "_" +
                    statisticsLog.mapInfo.name + "_statistics.json",
                    JsonUtility.ToJson(statisticsLog));
            }
        } finally {
            SetButtonsInteractable(true);
        }
    }

    public void OpenDataDirectory() {
        System.Diagnostics.Process.Start("explorer.exe",
            "/select," + Application.persistentDataPath.Replace(@"/", @"\"));
    }

    private void SetButtonsInteractable(bool interactable) {
        if (Application.platform != RuntimePlatform.WebGLPlayer) {
            offlineExperimentButton.interactable = interactable;
            completeExperimentButton.interactable = interactable;
            directoryButton.interactable = interactable;
        }
        resetCompletionButton.interactable = interactable;
        downloadAllButton.interactable = interactable;
        onlineExperimentButton.interactable = interactable;
        exitButton.interactable = interactable;
    }

    private void SetExitButton(bool mustQuit) {
        if (mustQuit) {
            exitButton.GetComponentInChildren<Text>().text = "Quit";
            if (Application.platform != RuntimePlatform.WebGLPlayer) {
                exitButton.interactable = true;
            } else {
                exitButton.interactable = false;
            }
        } else {
            exitButton.GetComponentInChildren<Text>().text = "Back";
        }

        exitMustQuit = mustQuit;
    }

    public void LoadOnlineExperiment() {
        ParameterManager.Instance.LogOnline = true;
        ParameterManager.Instance.LogOffline = false;
        ParameterManager.Instance.LogSetted = true;
        ParameterManager.Instance.ExperimentControlScene = SceneManager.GetActiveScene().name;
        ParameterManager.Instance.BackgroundRotation = backgroundScript.GetRotation();
        SceneManager.LoadScene(experimentScene);
    }

    public void LoadOfflineExperiment() {
        ParameterManager.Instance.LogOnline = false;
        ParameterManager.Instance.LogOffline = true;
        ParameterManager.Instance.LogSetted = true;
        ParameterManager.Instance.ExperimentControlScene = SceneManager.GetActiveScene().name;
        ParameterManager.Instance.BackgroundRotation = backgroundScript.GetRotation();
        SceneManager.LoadScene(experimentScene);
    }

    public void LoadCompleteExperiment() {
        ParameterManager.Instance.LogOnline = true;
        ParameterManager.Instance.LogOffline = true;
        ParameterManager.Instance.LogSetted = true;
        ParameterManager.Instance.ExperimentControlScene = SceneManager.GetActiveScene().name;
        ParameterManager.Instance.BackgroundRotation = backgroundScript.GetRotation();
        SceneManager.LoadScene(experimentScene);
    }

}