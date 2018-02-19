using JsonObjects;
using JsonObjects.Game;
using JsonObjects.Statistics;
using JsonObjects.Survey;
using Polimi.GameCollective.Connectivity;
using System.Collections;
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

    void Start() {
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

        if (Application.platform == RuntimePlatform.WebGLPlayer) {
            offlineExperimentButton.interactable = false;
            completeExperimentButton.interactable = false;
            directoryButton.interactable = false;
        } else {
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
                        string fileName = "";

                        switch (resultFields[2]) {
                            case ConnectionSettings.SERVER_GAME_LABEL:
                                JsonGameLog jGameLog =
                                    JsonUtility.FromJson<JsonGameLog>(resultFields[3]);
                                fileName = jGameLog.testID + "_" + jGameLog.mapInfo.name +
                                    "_game_" + jGameLog.logPart;
                                break;
                            case ConnectionSettings.SERVER_STATISTICS_LABEL:
                                JsonStatisticsLog jStatisticsLog =
                                    JsonUtility.FromJson<JsonStatisticsLog>(resultFields[3]);
                                fileName = jStatisticsLog.testID + "_" + jStatisticsLog.mapInfo.name
                                    + "_" + "statistics";
                                break;
                            case ConnectionSettings.SERVER_ANSWERS_LABEL:
                                JsonAnswers jAnswers =
                                    JsonUtility.FromJson<JsonAnswers>(resultFields[3]);
                                fileName = jAnswers.testID + "_answers";
                                break;
                        }

                        File.WriteAllText(downloadDirectory + "/" + fileName + ".json",
                            resultFields[3]);
                    }
                }
            }
        } finally {
            SetButtonsInteractable(true);
        }
    }

    public void OpenDataDirectory() {
        System.Diagnostics.Process.Start("explorer.exe",
            "/select," + downloadDirectory.Replace(@"/", @"\"));
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