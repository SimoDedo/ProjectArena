using UnityEngine;
using UnityEngine.SceneManagement;

public class SurveyUIManager : MonoBehaviour {

    [Header("Survey fields")] [SerializeField] private GameObject introduction;
    [SerializeField] private GameObject[] questions;
    [SerializeField] private GameObject thanks;

    [Header("Other")] [SerializeField] private RotateTranslateByAxis backgroundScript;

    private ParameterManager parameterManagerScript;
    private int currentQuestion = 0;

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

    // Shows the first question.
    public void FirstQuestion() {
        introduction.SetActive(false);
        questions[currentQuestion].SetActive(true);
    }

    // Shows the next question.
    public void NextQuestion() {
        questions[currentQuestion].SetActive(false);

        if (currentQuestion < questions.Length - 1) {
            currentQuestion++;
            questions[currentQuestion].SetActive(true);
        } else {
            thanks.SetActive(true);
        }
    }

    // Returns to the experiment menu.
    private void Quit() {
        parameterManagerScript.SetBackgroundRotation(backgroundScript.GetRotation());
        SceneManager.LoadScene(GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>().GetNextScene(parameterManagerScript));
    }

}
