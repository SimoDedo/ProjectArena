using System;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
#endif

namespace Tester
{
    /// <summary>
    /// A MonoBehaviour able to dispatch to the correct scene depending on the command line parameters.
    /// </summary>
    public class ExperimentDispatcher : MonoBehaviour
    {
        private const string EXPERIMENT_PARAM = "-experimentType=";

        private const string AI_TESTER_TEST_NAME = "AI_TESTER";
        private const string AI_TESTER_TEST_SCENE = "AITester";
        private const string GENOME_TESTER_TEST_NAME = "BOT_GENOME_TESTER";
        private const string GENOME_TESTER_TEST_SCENE = "GenomeTester";

        private void Awake()
        {
#if !UNITY_EDITOR
                Time.captureFramerate = 30;
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
#endif

            if (!Application.isBatchMode)
            {
                QuitWithError("This scene should be played in batch mode!");
                return;
            }

            var args = Environment.GetCommandLineArgs();
            string experimentType = null;
            foreach (var arg in args)
                if (arg.StartsWith(EXPERIMENT_PARAM))
                    experimentType = arg.Substring(EXPERIMENT_PARAM.Length);

            if (experimentType == null)
            {
                // Debug.LogError("You must supply the type of experiment to run with " + EXPERIMENT_PARAM);
                // This doesn't work...

                QuitWithError("You must supply the type of experiment to run with " + EXPERIMENT_PARAM);
                return;
            }

            switch (experimentType)
            {
                case AI_TESTER_TEST_NAME:
                    SceneManager.LoadScene(AI_TESTER_TEST_SCENE);
                    break;
                case GENOME_TESTER_TEST_NAME:
                    SceneManager.LoadScene(GENOME_TESTER_TEST_SCENE);
                    break;

                default:
                    QuitWithError(
                        "Unknown experiment type " + experimentType + "\n" +
                        "Known experiments are:\n" +
                        // ROCKET_SUICIDE_TEST_NAME + ",\n"
                        GENOME_TESTER_TEST_NAME + ",\n"
                    );
                    return;
            }
        }

        private static void QuitWithError(string errorMessage)
        {
            // TODO Understand how to quit batchmode
            Application.Quit(-1);
        }
    }
}