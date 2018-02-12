using ExperimentObjects;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ExperimentInitializer allows to define experiments.
/// </summary>
public class ExperimentInitializer : MonoBehaviour {

    [Header("Tutorial")] [SerializeField] private Case tutorial;
    [SerializeField] private bool playTutorial;

    [Header("Experiment")] [SerializeField] private List<Study> studies;
    [SerializeField] private int casesPerUsers;
    [SerializeField] private string experimentName;

    [Header("Survey")] [SerializeField] private Case survey;
    [SerializeField] private bool playSurvey;

    [Header("Logging")] [SerializeField] private bool logOnline;
    [SerializeField] private bool logGame;
    [SerializeField] private bool logStatistics;

    void Start() {
        ExperimentManager.Instance.Setup(tutorial, playTutorial, studies, casesPerUsers,
            experimentName, survey, playSurvey, logOnline, logGame, logStatistics);
    }

}
