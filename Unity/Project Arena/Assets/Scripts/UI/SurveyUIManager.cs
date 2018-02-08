using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// This class manages the UI of the survey. A survey is a sequence of screens that starts with an
/// introduction, continues with some questions and ends with a thanks.
/// </summary>
public class SurveyUIManager : MonoBehaviour {

    [Header("Survey fields")] [SerializeField] private GameObject introduction;
    [SerializeField] private GameObject[] questions;
    [SerializeField] private GameObject thanks;

    [Header("Other")] [SerializeField] private RotateTranslateByAxis backgroundScript;

    private ExperimentManager experimentManagerScript;

    private int currentQuestion = 0;
    private string survey = "";
    private string answers = "";

    private void Start() {
        experimentManagerScript = GameObject.Find("Experiment Manager").
            GetComponent<ExperimentManager>();
        backgroundScript.SetRotation(ParameterManager.Instance.BackgroundRotation);
    }

    public void Submit() {
        SaveValues();
        Quit();
    }

    // Updates the values of the survey.
    private void UpdateValues() {
        if (experimentManagerScript.MustSaveSurvey()) {
            survey += questions[currentQuestion].GetComponent<CheckboxQuestion>().GetJsonQuestion();
            if (currentQuestion < questions.Length - 1) {
                survey += "\n";
            }
        }
        answers += questions[currentQuestion].GetComponent<CheckboxQuestion>().GetJsonAnswer();
        if (currentQuestion < questions.Length - 1) {
            answers += "\n";
        }
    }

    // Saves the values of the survey.
    private void SaveValues() {
        if (experimentManagerScript.MustSaveSurvey()) {
            experimentManagerScript.SaveSurvey(survey);
        }
        experimentManagerScript.SaveAnswers(answers);
    }

    // Shows the first question.
    public void FirstQuestion() {
        introduction.SetActive(false);
        questions[currentQuestion].SetActive(true);
    }

    // Shows the next question.
    public void NextQuestion() {
        UpdateValues();

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
        ParameterManager.Instance.BackgroundRotation = backgroundScript.GetRotation();
        SceneManager.LoadScene(experimentManagerScript.GetNextScene());
    }

}
