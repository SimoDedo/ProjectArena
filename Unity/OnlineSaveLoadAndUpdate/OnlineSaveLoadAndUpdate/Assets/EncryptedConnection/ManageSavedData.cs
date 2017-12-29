using UnityEngine;
using System.Collections;
using SimpleJSON;

//using System.Web;

public class ManageSavedData : MonoBehaviour {

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

	// Use this for initialization
	/*void Update () {
		if (Input.GetKeyDown (KeyCode.Alpha1) && !isWorking) {
			Debug.LogError ("SAVE ONE ENTRY");
			SaveData ("Match#1", "{\"lorenzo\":8}", "Semplice JSON");
		} else if (Input.GetKeyDown (KeyCode.Alpha2) && !isWorking) {
			Debug.LogError ("SAVE ONE ENTRY");
			SaveData ("Match#2", "1;Mancala;MCTS;100;", "CSV il formato lo definisci tu");
		} else if (Input.GetKeyDown (KeyCode.Alpha3) && !isWorking) {
			Debug.LogError ("GET LAST ENTRY ENTRY");
			GetLastEntry ();
		} else if (Input.GetKeyDown (KeyCode.Alpha4) && !isWorking) {
			Debug.LogError ("GET ALL ENTRIES");
			GetAllEntries ();
		} else if (Input.GetKeyDown (KeyCode.Alpha5) && !isWorking) {
			Debug.LogError ("GET LAST 2 ENTRIES");
			GetLastEntries (2);
		}
	}*/

	/// <summary>
	/// Saves the data on the server
	/// </summary>
	/// <param name="label">Label used to identify the experiment (e.g., Mancala#1, Aware#2, etc.</param>
	/// <param name="data">The actual data saved anything that can be represented as a string</param>
	/// <param name="comment">Any free comment needed</param>
	public void SaveData(string label, string data, string comment) {
		// "save|" is the command sent to the php script
		string save = "save|"+label+"|"+data+"|"+comment;
		Post (save);
	}

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

	// submit data to the website
	void Post (string data)
	{
		/// manages only one request at once
		if (m_has_submitted_request)
			return;

		m_has_submitted_request = true;

		m_url_update = ServerConnection.GeneratePostURL (data);

		StartCoroutine(SubmitPost(m_url_update));
	}
	
	// get data from the website 
	void Get (string data)
	{
		m_url_update = ServerConnection.GenerateGetURL (data);

		m_url_result = "";

		StartCoroutine(SubmitPost(m_url_update));	
	}
		
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

		m_has_submitted_request = false;

		//Debug.LogError ("DATA RECEIVED " + m_url_result);
	}
}
