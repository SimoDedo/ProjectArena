using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.UI;

public class OnlineDataManager : MonoBehaviour {

	[Header("Server Information")]
	public string m_server = "https://data.polimigamecollective.org";
	public string m_directory = "scopone";
	public string m_get_script = "get.php";
	public string m_post_script = "post.php";
	private string m_filename_save_queue = "queue.json";
	private const string m_datafield="?data=";

	/// <summary>
	/// set to true when OnApplicationQuit is called
	/// </summary>
	private bool is_application_quitting = false;

    [System.Serializable]
    public class Entry {
        public string label { get; set; }
        public string data { get; set; } 
        public string comment { get; set; }

        public Entry() {}

		public Entry(string _label="", string _data="", string _comment="") {
			label = _label;
			data = _data;
			comment = _comment;
		}
	}

	Queue<Entry>	queue = new Queue<Entry> ();
	Queue<string>	result = new Queue<string> ();
	string			str_queue_path = string.Empty;

    int current_item = 0;
    public Text m_DebugScreen;
    private bool m_DebugStatusChanged = false;

	#region Singleton
	private static OnlineDataManager _instance = null;
	public static OnlineDataManager instance { get { return _instance; } }

	// Singleton
	void InitSingleton() {
		if (_instance == null) {
			_instance = this;
			DontDestroyOnLoad (gameObject);
		} else {
			Destroy (gameObject);
		}
	}
	#endregion

	void Awake() {

		// init the singleton
		InitSingleton ();

		// set the path to the queue file
		str_queue_path = Path.Combine (Application.persistentDataPath, m_filename_save_queue);

        current_item = 0;
	}

	void Update() {
		if (Input.GetKeyDown (KeyCode.A)) {
			SaveOnlineData ("test1", "pinco pallo", "cavolo");
		}

        if (Input.GetKeyDown(KeyCode.Q))
        {
            queue.Enqueue(new Entry("test" + current_item.ToString(), "pinco" + current_item.ToString(), "cavolo" + current_item.ToString()));
 
        }

        
    }

    void DebugUpdate()
    {


    }
	private string m_url_update;
	private string m_url_result;
	private string m_url_last_entry;
	private string m_url_last_n;
	private string m_url_all;
	private bool m_is_result_ready = false;
	private bool m_has_submitted_request = false;

	public bool isResultReady { get { return m_is_result_ready; } }
	public bool isWorking { get { return m_has_submitted_request; } }
	public string returnedString { get { return m_url_result; } }

	#region SaveOnlineData
	void SaveOnlineData(string label, string data, string comment) {
		
		string save = label+"|"+data+"|"+comment;

		/// manages only one request at once
		/// if more than one arrives, then it enters a queue
		if (m_has_submitted_request) {
			queue.Enqueue (new Entry (label, data, comment));
			return;
		}

		m_has_submitted_request = true;
		m_url_update = SimpleServerConnection.GeneratePostURL (save);
		StartCoroutine(SubmitPost(GeneratePostURL (save)));
	}
	#endregion

	// get data from the website 
	void Get (string data)
	{
		m_url_update = ServerConnection.GenerateGetURL (data);

		m_url_result = "";

		StartCoroutine(SubmitPost(m_url_update));	
	}
		
	#region GetOnlineData
	public void GetLastEntry() {

		/// manages only one request at once
		if (m_has_submitted_request)
			return;

		m_has_submitted_request = true;

		string load = "load|last";

		m_url_result = "";

		Get (load);

	}

	public void GetLastEntries(int n) {

		/// manages only one request at once
		if (m_has_submitted_request)
			return;

		m_has_submitted_request = true;

		string load = "load|"+n.ToString();

		m_url_result = "";

		Get (load);

	}

	// "load|all" is the command sent to the php script which requires all the data
	public void GetAllEntries() {

		/// manages only one request at once
		if (m_has_submitted_request)
			return;

		m_has_submitted_request = true;

		string load = "load|all";

		m_url_result = "";

		Get (load);

	}
	#endregion

	IEnumerator SubmitPost(string url_update)
	{
		m_is_result_ready = false;

		m_url_result = "";

		while (m_url_result == "") {

			WWW www = new WWW (url_update);

			yield return www;

			m_url_result = www.text;
		}

		m_is_result_ready = true;

		if (queue.Count == 0) {
			Debug.Log ("Queue is empty, leaving the coroutine");
			m_has_submitted_request = false;
		} else {
			Debug.Log ("Queue has other requests");

		}
	}

	public string GeneratePostURL(string data) {
		string encrypted = WWW.EscapeURL(data);		
		string url_update =
			m_server+"/"+
			m_directory+"/"+
			m_post_script+
			m_datafield+encrypted;
		return url_update;
	}

	public string GenerateGetURL(string data) {
		string encrypted = WWW.EscapeURL(data);		
		string url_update =
			m_server+"/"+
			m_directory+"/"+
			m_get_script+
			m_datafield+encrypted;
		return url_update;
	}

	#region LoadAndSaveQueue

	void OnEnable() {
		LoadQueue ();
	}

	private void LoadQueue() {
		string fn = Path.Combine (Application.persistentDataPath, m_filename_save_queue);

		if (File.Exists (fn)) {
			string json_data = File.ReadAllText (fn);
			queue = JsonUtility.FromJson<Queue<Entry>>(json_data);
		}
	}
		
	void OnApplicationQuit() {
		is_application_quitting = true;
	}

	void OnDisable() {

		// when the application quit
		if (is_application_quitting)
			SaveQueue();

	}

	private void SaveQueue() {
		
		string fn = Path.Combine (Application.persistentDataPath, m_filename_save_queue);
		string str_queue = JsonUtility.ToJson(queue);

		File.WriteAllText (fn, str_queue);
	}

	#endregion
}
