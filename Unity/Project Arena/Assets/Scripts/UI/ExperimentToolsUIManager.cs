using JsonObjects;
using Polimi.GameCollective.Connectivity;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ExperimentToolsUIManager : MonoBehaviour {

    [SerializeField] Button quitButton;
    [SerializeField] Button downloadAllButton;
    [SerializeField] Button resetCompletionButton;
    [SerializeField] Button importButton;
    [SerializeField] Button exportButton;

    private string downloadDirectory;
    private string exportDirectory;
    private string importDirectory;

    void Start() {
        if (Application.isEditor) {
            quitButton.gameObject.SetActive(false);
            importButton.interactable = false;
            exportButton.interactable = false;
        }

        exportDirectory = Application.persistentDataPath + "/Export";
        importDirectory = Application.persistentDataPath + "/Import";
        downloadDirectory = exportDirectory + "/ Download";

        if (!Directory.Exists(exportDirectory))
            Directory.CreateDirectory(exportDirectory);
        if (!Directory.Exists(importDirectory))
            Directory.CreateDirectory(importDirectory);
        if (!Directory.Exists(downloadDirectory))
            Directory.CreateDirectory(downloadDirectory);
    }

    public void Exit() {
        Application.Quit();
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

            string[] results = RemoteDataManager.Instance.Result.Split('|');

            for (int i = 0; i < results.Length / 5; i++) {
                if (results[i * 5 + 2] != "PA_COMPLETION" && results[i * 5 + 2] != "PA_RESET") {
                    File.WriteAllText(downloadDirectory + "/" + results[i * 5 + 2] + ".json",
                        results[i * 5 + 3]);
                }
            }
        } finally {
            SetButtonsInteractable(true);
        }
    }

    public void OpenImportFolder() {
        ShowExplorer(importDirectory);
    }

    public void OpenExportFolder() {
        ShowExplorer(exportDirectory);
    }

    private void ShowExplorer(string path) {
        path = path.Replace(@"/", @"\");
        System.Diagnostics.Process.Start("explorer.exe", "/select," + path);
    }

    private void SetButtonsInteractable(bool interactable) {
        resetCompletionButton.interactable = interactable;
        downloadAllButton.interactable = interactable;
        quitButton.interactable = interactable;
        importButton.interactable = interactable;
        exportButton.interactable = interactable;
    }

}