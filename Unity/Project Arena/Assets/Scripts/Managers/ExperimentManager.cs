using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExperimentManager : MonoBehaviour {

    [Header("Tutorial")] [SerializeField] private Case tutorial;
    [SerializeField] private bool playTutorial;

    [Header("Experiment")] [SerializeField] private List<Study> studies;
    [SerializeField] private int casesPerUsers;

    [Header("Survey")] [SerializeField] private Case survey;
    [SerializeField] private bool playSurvey;

    // List of cases the current player has to play.
    private Queue<Case> caseQueue;

    void Start() {
        caseQueue = new Queue<Case>();
    }

    void Awake() {
        DontDestroyOnLoad(transform.gameObject);
    }

    // Creates a new list of cases for the player to play.
    private void CreateNewQueue() {
        if (playTutorial)
            caseQueue.Enqueue(tutorial);

        for (int i = 0; i < casesPerUsers; i++)
            caseQueue.Enqueue(GetCase());

        if (playSurvey)
            caseQueue.Enqueue(survey);

        caseQueue.Enqueue(new Case {
            scene = SceneManager.GetActiveScene().name
        });
    }

    // Gets the next case to add in a round-robin fashion.
    private Case GetCase() {
        // Get the least played study.
        int minValue = studies[0].completion;
        int minIndex = 0;

        for (int i = 0; i < studies.Count; i++) {
            if (studies[i].completion < minValue) {
                minValue = studies[i].completion;
                minIndex = i;
            }
        }
        studies[minIndex].completion++;

        // Get the least played case in the least played study.
        int minMinValue = studies[minIndex].cases[0].completion;
        int minMinIndex = 0;

        for (int i = 0; i < studies[minIndex].cases.Count; i++) {
            if (studies[minIndex].cases[i].completion < minMinValue) {
                minMinValue = studies[minIndex].cases[i].completion;
                minMinIndex = i;
            }
        }
        studies[minIndex].cases[minMinIndex].completion++;

        return studies[minIndex].cases[minMinIndex];
    }

    // Retuns the next scene to be played.
    public string GetNextScene(ParameterManager pm) {
        if (caseQueue.Count == 0)
            CreateNewQueue();

        Case currentCase = caseQueue.Dequeue();

        pm.SetGenerationMode(4);
        pm.SetMapDNA(currentCase.map == null ? "" : currentCase.map.text);

        return currentCase.scene;
    }

    [Serializable]
    private class Study {
        [SerializeField] public string studyName;
        [SerializeField] public List<Case> cases;
        [NonSerialized] public int completion;
    }

    [Serializable]
    private class Case {
        [SerializeField] public TextAsset map;
        [SerializeField] public string scene;
        [NonSerialized] public int completion;
    }

}