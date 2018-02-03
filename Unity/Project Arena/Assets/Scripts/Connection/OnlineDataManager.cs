using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;

public class OnlineDataManager : Singleton<OnlineDataManager> {

    [Header("Server Information")] [SerializeField] private string server = "https://data.polimigamecollective.org";
    [SerializeField] private string directory = "arena";
    [SerializeField] private string getScript = "get.php";
    [SerializeField] private string postScript = "post.php";

    private string filenameSaveQueue = "queue.json";
    private const string dataField = "?data=";

    public Queue<Entry> queue = new Queue<Entry>();
    public Queue<string> result = new Queue<string>();
    public string queuePath = string.Empty;
    public int currentItem = 0;

    private bool isApplicationQuitting = false;

    private string urlUpdates;
    private string urlResults;
    private string urlLastEntry;
    private string urlLastN;
    private string urlAll;
    private bool isResultReady = false;
    private bool hasSubmittedRequest = false;

    public bool IsResultReady { get { return isResultReady; } }
    public bool IsWorking { get { return hasSubmittedRequest; } }
    public string ReturnedString { get { return urlResults; } }

    void Awake() {
        // Set the path to the queue file.
        queuePath = Path.Combine(Application.persistentDataPath, filenameSaveQueue);

        currentItem = 0;
    }

    void OnEnable() {
        LoadQueue();
    }

    void OnApplicationQuit() {
        isApplicationQuitting = true;
    }

    void OnDisable() {
        if (isApplicationQuitting)
            SaveQueue();
    }

    // Saves data on the server. This manages only one request at once, so
    // if more than one arrives, then it enters a queue.
    void SaveOnlineData(string label, string data, string comment) {
        string save = label + "|" + data + "|" + comment;

        if (hasSubmittedRequest) {
            queue.Enqueue(new Entry(label, data, comment));
            return;
        }

        hasSubmittedRequest = true;
        urlUpdates = SimpleServerConnection.GeneratePostURL(save);
        StartCoroutine(SubmitPost(GeneratePostURL(save)));
    }

    // Get data from the server. 
    void Get(string data) {
        urlUpdates = ServerConnection.GenerateGetURL(data);

        urlResults = "";

        StartCoroutine(SubmitPost(urlUpdates));
    }

    // Get last entry. This manages only one request at once.
    public void GetLastEntry() {
        if (hasSubmittedRequest)
            return;

        hasSubmittedRequest = true;

        string load = "load|last";

        urlResults = "";

        Get(load);
    }

    // Get n last entry. This manages only one request at once.
    public void GetLastEntries(int n) {
        if (hasSubmittedRequest)
            return;

        hasSubmittedRequest = true;

        string load = "load|" + n.ToString();

        urlResults = "";

        Get(load);
    }

    // Get all the entries with "load|all".
    public void GetAllEntries() {
        if (hasSubmittedRequest)
            return;

        hasSubmittedRequest = true;

        string load = "load|all";

        urlResults = "";

        Get(load);

    }

    // Submits data to the server.
    IEnumerator SubmitPost(string urlUpdates) {
        isResultReady = false;

        urlResults = "";

        while (urlResults == "") {

            WWW www = new WWW(urlUpdates);

            yield return www;

            urlResults = www.text;
        }

        isResultReady = true;

        if (queue.Count == 0) {
            Debug.Log("Queue is empty, leaving the coroutine");
            hasSubmittedRequest = false;
        } else {
            Debug.Log("Queue has other requests");

        }
    }

    // Generates the post URL.
    public string GeneratePostURL(string data) {
        string encrypted = WWW.EscapeURL(data);
        string urlUpdates =
            server + "/" +
            directory + "/" +
            postScript +
            dataField + encrypted;
        return urlUpdates;
    }

    // Generates the get URL.
    public string GenerateGetURL(string data) {
        string encrypted = WWW.EscapeURL(data);
        string urlUpdates =
            server + "/" +
            directory + "/" +
            getScript +
            dataField + encrypted;
        return urlUpdates;
    }

    // Loads the queue.
    private void LoadQueue() {
        string fn = Path.Combine(Application.persistentDataPath, filenameSaveQueue);

        if (File.Exists(fn)) {
            string json_data = File.ReadAllText(fn);
            queue = JsonUtility.FromJson<Queue<Entry>>(json_data);
        }
    }

    // Saves the queue.
    private void SaveQueue() {
        string fn = Path.Combine(Application.persistentDataPath, filenameSaveQueue);
        string str_queue = JsonUtility.ToJson(queue);

        File.WriteAllText(fn, str_queue);
    }

    [System.Serializable]
    public class Entry {
        public string Label { get; set; }
        public string Data { get; set; }
        public string Comment { get; set; }

        public Entry() { }

        public Entry(string _label = "", string _data = "", string _comment = "") {
            Label = _label;
            Data = _data;
            Comment = _comment;
        }
    }

}