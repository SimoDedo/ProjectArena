using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// This class manages the UI of the experiment menu.
/// </summary>
public class ExperimentMenuUIManager : MonoBehaviour {

    [Header("Menu")] [SerializeField] private GameObject menu;
    [SerializeField] private GameObject loading;

    [Header("Other")] [SerializeField] private RotateTranslateByAxis backgroundScript;

    // Use this for initialization
    void Start() {
        Cursor.lockState = CursorLockMode.None;

        backgroundScript.SetRotation(ParameterManager.Instance.BackgroundRotation);
    }

    // Loads the level indicated by the Experiment Manager.
    public void Play() {
        SceneManager.LoadScene(ExperimentManager.Instance.GetNextScene());
    }

    public void SetLoadingVisible(bool visible) {
        if (visible) {
            menu.SetActive(false);
            loading.SetActive(true);
        } else {
            menu.SetActive(true);
            loading.SetActive(false);
        }
    }

}