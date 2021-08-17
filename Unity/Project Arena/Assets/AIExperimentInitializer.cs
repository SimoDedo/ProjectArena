using System.Collections;
using System.Collections.Generic;
using ExperimentObjects;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AIExperimentInitializer : MonoBehaviour
{
    [Header("Tutorial")] [SerializeField] private Case tutorial;
    [SerializeField] private bool playTutorial;

    [Header("Experiment")] [SerializeField]
    private List<Study> studies;

    [SerializeField] private int casesPerUsers;
    [SerializeField] private string experimentName;

    [Header("Survey")] [SerializeField] private Case survey;
    [SerializeField] private bool playSurvey;

    [Header("Logging")] [SerializeField] private bool logOffline;
    [SerializeField] private bool logOnline;
    [SerializeField] private bool logGame;
    [SerializeField] private bool logStatistics;

    void Awake()
    {
        if (ParameterManager.HasInstance()) {
            logOnline = ParameterManager.Instance.LogOnline;
            logOffline = ParameterManager.Instance.LogOffline;
        }
        ParameterManager.Instance.LogSetted = true;
        ParameterManager.Instance.GenerationMode = 5;
        
        if (!ExperimentManager.HasInstance())
        {
            ExperimentManager.Instance.Setup(tutorial, playTutorial, studies, casesPerUsers,
                experimentName, survey, playSurvey, logOffline, logOnline, logGame, logStatistics);
        }
    }

    void Start()
    {
        StartCoroutine(WaitAndStartExperiment());
    }

    private IEnumerator WaitAndStartExperiment()
    {
        yield return new WaitForSeconds(1);
        yield return ExperimentManager.Instance.StartNewExperiment();
    }
}