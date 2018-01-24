using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExperimentManager : MonoBehaviour {

    [Header("Tutorial")] [SerializeField] private Case tutorial;
    [SerializeField] private bool playTutorial;

    [Header("Experiment")] [SerializeField] private List<Study> studies;
    [SerializeField] private int casesPerUsers;
    [SerializeField] private string experimentName;

    [Header("Survey")] [SerializeField] private Case survey;
    [SerializeField] private bool playSurvey;

    // List of cases the current player has to play.
    private List<Case> caseList;
    // Directory for this esperiment files.
    private string experimentDirectory;
    // Directory for the surveys.
    private string surveysDirectory;

    private int currentCase = -1;
    private string currentTimestamp;

    void Start() {
        caseList = new List<Case>();

        // Create the experiment directory if needed.
        experimentDirectory = Application.persistentDataPath + "/Export/" + experimentName;
        CreateDirectory(experimentDirectory);
        // Create the maps directory if needed.
        foreach (Study s in studies)
            foreach (Case c in s.cases) {
                CreateDirectory(experimentDirectory + "/" + c.map.name);
                System.IO.File.WriteAllText(@experimentDirectory + "/" + c.map.name + "/" + c.map.name + ".txt", c.map.text);
            }
        // Create the survey directory if needed.
        surveysDirectory = experimentDirectory + "/Surveys";
        CreateDirectory(surveysDirectory);
    }

    void Awake() {
        DontDestroyOnLoad(transform.gameObject);
    }

    // Creates a new list of cases for the player to play.
    private void CreateNewList() {
        currentTimestamp = GetTimeStamp();
        caseList.Clear();

        if (playTutorial)
            caseList.Add(tutorial);

        for (int i = 0; i < casesPerUsers; i++)
            caseList.Add(GetCase());

        if (playSurvey)
            caseList.Add(survey);

        caseList.Add(new Case {
            scene = SceneManager.GetActiveScene().name
        });

        currentCase = 0;
    }

    // Returns a well formatted timestamp.
    private string GetTimeStamp() {
        DateTime now = System.DateTime.Now;
        return now.Year + "-" + now.Month + "-" + now.Day + "-" + now.Hour + "-" + now.Minute + "-" + now.Second;
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
        currentCase++;

        if (currentCase == caseList.Count || caseList.Count == 0)
            CreateNewList();

        Case c = caseList[currentCase];

        pm.SetGenerationMode(4);
        pm.SetMapDNA(c.map == null ? "" : c.map.text);

        return c.scene;
    }

    // Creates a directory if needed.
    private void CreateDirectory(string directory) {
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    // Tells if I need to save the survey.
    public bool MustSaveSurvey() {
        return !File.Exists(surveysDirectory + "/survey.json");
    }

    // Save survey. This has to be done once.
    public void SaveSurvey(string survey) {
        System.IO.File.WriteAllText(surveysDirectory + "/survey.json", survey);
    }

    // Saves answers and informations about the experiment.
    public void SaveAnswers(string answers) {
        string info = JsonUtility.ToJson(new JsonInfo {
            experimentName = experimentName,
            playedMaps = GetCurrentCasesArray()
        });
        System.IO.File.WriteAllText(surveysDirectory + "/" + currentTimestamp + "_survey.json", info + "\n" + answers);
    }

    // Returns the played cases in an array.
    public string[] GetCurrentCasesArray() {
        string[] maps = new string[casesPerUsers];
        for (int i = 0; i < casesPerUsers; i++)
            maps[i] = playTutorial ? caseList[i + 1].map.name : caseList[i].map.name;
        return maps;
    }

    private class JsonInfo {
        public string experimentName;
        public string[] playedMaps;
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