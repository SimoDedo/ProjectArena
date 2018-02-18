using JsonObjects;
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
        }

        downloadDirectory = Application.persistentDataPath + "/Downloads";
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
        RemoteDataManager.Instance.SaveData("PA_RESET", "", "");

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
                    if (resultFields[2] != "PA_COMPLETION" && resultFields[2] != "PA_RESET") {
                        File.WriteAllText(downloadDirectory + "/" + resultFields[2] + ".json",
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
            if (Application.platform == RuntimePlatform.OSXPlayer ||
                Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.LinuxPlayer) {
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
        ParameterManager.Instance.ExperimentControlScene = SceneManager.GetActiveScene().name;
        ParameterManager.Instance.BackgroundRotation = backgroundScript.GetRotation();
        SceneManager.LoadScene(experimentScene);
    }

    public void LoadOfflineExperiment() {
        ParameterManager.Instance.LogOnline = false;
        ParameterManager.Instance.LogOffline = true;
        ParameterManager.Instance.ExperimentControlScene = SceneManager.GetActiveScene().name;
        ParameterManager.Instance.BackgroundRotation = backgroundScript.GetRotation();
        SceneManager.LoadScene(experimentScene);
    }

    public void LoadCompleteExperiment() {
        ParameterManager.Instance.LogOnline = true;
        ParameterManager.Instance.LogOffline = true;
        ParameterManager.Instance.ExperimentControlScene = SceneManager.GetActiveScene().name;
        ParameterManager.Instance.BackgroundRotation = backgroundScript.GetRotation();
        SceneManager.LoadScene(experimentScene);
    }

}