using System;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentManager : MonoBehaviour {

    [Header("Experiment")] [SerializeField] private List<Study> studies;
    [SerializeField] private int casesPerUsers;

    [Header("Tutorial")] [SerializeField] private string tutorialScene;
    [SerializeField] private bool playTutorial;

    // List of cases the current player has to play.
    private Queue<Case> caseQueue;

    void Start () {
        caseQueue = new Queue<Case>();
    }
	
    // Creates a new list of cases for the player to play.
    private void CreateNewQueue() {
        if (playTutorial) {
            caseQueue.Enqueue(new Case {
                scene = tutorialScene
            });
        }

        for (int i = 0; i < casesPerUsers; i++) {
            caseQueue.Enqueue(GetCase());
        }
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

    // Get the next scene to be played.
    public string GetNextScene() {
        if (caseQueue.Count == 0)
            CreateNewQueue();
        
        Case currentCase = caseQueue.Dequeue();

        return currentCase.scene;
    }

    [Serializable]
    private class Study {
        public string studyName;
        public List<Case> cases;
        [NonSerialized] public int completion;
    }

    [Serializable]
    private class Case {
        public string fileName;
        public string scene;
        [NonSerialized] public int completion;
    }

}