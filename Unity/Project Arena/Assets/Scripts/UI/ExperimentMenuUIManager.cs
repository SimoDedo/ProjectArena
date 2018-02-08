using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// This class manages the UI of the experiment menu. The esc button is enable by pressing "e", "s"
/// and "c" at the same time.
/// </summary>
public class ExperimentMenuUIManager : MonoBehaviour {

    [Header("Menu fields")] [SerializeField] private Button backButton;

    [Header("Other")] [SerializeField] private GameObject experimentManagerPrefab;
    [SerializeField] private RotateTranslateByAxis backgroundScript;

    private ExperimentManager experimentManagerScript;

    // Use this for initialization
    void Start() {
        Cursor.lockState = CursorLockMode.None;

        backgroundScript.SetRotation(ParameterManager.Instance.BackgroundRotation);

        if (GameObject.Find("Experiment Manager")) {
            experimentManagerScript = GameObject.Find("Experiment Manager").
                GetComponent<ExperimentManager>();
        } else {
            experimentManagerScript = InstantiateManager(experimentManagerPrefab,
                "Experiment Manager").GetComponent<ExperimentManager>();
        }
    }

    void Update() {
        if (Input.GetKey("e") && Input.GetKey("s") && Input.GetKey("c")) {
            backButton.interactable = true;
        }
    }

    // Instantiates a manager.
    private GameObject InstantiateManager(GameObject prefab, string name) {
        GameObject instance = Instantiate(prefab);
        instance.name = name;
        return instance;
    }

    // Loads the level indicated by the Experiment Manager.
    public void Play() {
        SceneManager.LoadScene(experimentManagerScript.GetNextScene());
    }

    // Goes back to the main menu.
    public void Back() {
        ParameterManager.Instance.BackgroundRotation = backgroundScript.GetRotation();

        Destroy(experimentManagerScript.gameObject);
        SceneManager.LoadScene("Menu");
    }

}