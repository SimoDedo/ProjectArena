using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExperimentMenuUIManager : MonoBehaviour {

    [Header("Menu fields")] [SerializeField] private Button backButton;

    [Header("Other")] [SerializeField] private GameObject experimentManagerPrefab;
    [SerializeField] private RotateTranslateByAxis backgroundScript;

    private ExperimentManager experimentManagerScript;
    private ParameterManager parameterManagerScript;

    // Use this for initialization
    void Start () {
        Cursor.lockState = CursorLockMode.None;

        if (GameObject.Find("Parameter Manager")) {
            parameterManagerScript = GameObject.Find("Parameter Manager").GetComponent<ParameterManager>();
            backgroundScript.SetRotation(parameterManagerScript.GetBackgroundRotation());
        }

        if (!GameObject.Find("Experiment Manager"))
            InstantiateExperimentManager();
        else
            experimentManagerScript = GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>();
    }

	void Update () {
        if (Input.GetKey("e") && Input.GetKey("s") && Input.GetKey("c"))
            backButton.interactable = true;
    }

    // Instantiates the experiment manager.
    private void InstantiateExperimentManager() {
        GameObject instance = Instantiate(experimentManagerPrefab);
        instance.name = experimentManagerPrefab.name;
        experimentManagerScript = instance.GetComponent<ExperimentManager>();
    }

    // Loads the level indicated by the Experiment Manager.
    public void Play() {
        // SceneManager.LoadScene("Menu");
    }

    // Goes back to the main menu.
    public void Back() {
        if (parameterManagerScript != null)
            parameterManagerScript.SetBackgroundRotation(backgroundScript.GetRotation());

        Destroy(experimentManagerScript.gameObject);
        SceneManager.LoadScene("Menu");
    }

}