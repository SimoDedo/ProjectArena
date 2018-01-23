using UnityEngine;
using UnityEngine.SceneManagement;

public class SurveyUIManager : MonoBehaviour {

    [Header("Survey fields")]

    [Header("Other")] [SerializeField] private RotateTranslateByAxis backgroundScript;

    private ParameterManager parameterManagerScript;

    private void Start() {
        parameterManagerScript = GameObject.Find("Parameter Manager").GetComponent<ParameterManager>();
        backgroundScript.SetRotation(parameterManagerScript.GetBackgroundRotation());
    }

    public void Submit() {
        StoreValues();
        Quit();
    }

    // Stores the values of the survey.
    private void StoreValues() {
        // TODO.
    }

    // Returns to the experiment menu.
    private void Quit() {
        parameterManagerScript.SetBackgroundRotation(backgroundScript.GetRotation());
        SceneManager.LoadScene(GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>().GetNextScene(parameterManagerScript));
    }

}
