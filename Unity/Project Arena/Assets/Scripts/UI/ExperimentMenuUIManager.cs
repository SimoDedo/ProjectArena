using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// This class manages the UI of the experiment menu. The esc button is enable by pressing "e", "s"
/// and "c" at the same time.
/// </summary>
public class ExperimentMenuUIManager : MonoBehaviour {

    [Header("Menu")] [SerializeField] private GameObject menu;
    [SerializeField] private GameObject loading;
    [SerializeField] private Button backButton;

    [Header("Other")] [SerializeField] private RotateTranslateByAxis backgroundScript;

    // Use this for initialization
    void Start() {
        Cursor.lockState = CursorLockMode.None;

        backgroundScript.SetRotation(ParameterManager.Instance.BackgroundRotation);
    }

    void Update() {
        if (Input.GetKey("e") && Input.GetKey("s") && Input.GetKey("c")) {
            backButton.interactable = true;
        }
    }

    // Loads the level indicated by the Experiment Manager.
    public void Play() {
        SceneManager.LoadScene(ExperimentManager.Instance.GetNextScene());
    }

    // Goes back to the main menu.
    public void Back() {
        ParameterManager.Instance.BackgroundRotation = backgroundScript.GetRotation();

        Destroy(ExperimentManager.Instance.gameObject);
        SceneManager.LoadScene("Menu");
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