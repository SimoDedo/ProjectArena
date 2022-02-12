using System.Collections.Generic;
using Logging;
using Managers;
using UnityEngine;

namespace Others
{
    /// <summary>
    ///     ExperimentInitializer allows to define experiments.
    /// </summary>
    public class ExperimentInitializer : MonoBehaviour
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

        private ExperimentManager experimentManager;

        private void Awake()
        {
            if (ParameterManager.HasInstance())
            {
                logOnline = ParameterManager.Instance.LogOnline;
                logOffline = ParameterManager.Instance.LogOffline;
            }

            if (experimentManager == null)
            {
                var obj = new GameObject("ExperimentManager");
                experimentManager = obj.AddComponent<ExperimentManager>();
                experimentManager.Setup(tutorial, playTutorial, studies, casesPerUsers,
                    experimentName, survey, playSurvey, logOffline, logOnline, logGame, logStatistics);
            }
        }
    }
}