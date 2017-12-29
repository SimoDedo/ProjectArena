using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
	public Text	m_text_all_entries;
	public Text	m_text_last_entry;
	public Text	m_text_last_n_entries;

	public Text m_text_label;
	public Text m_text_data;
	public Text m_text_comment;

	public InputField m_if_label;
	public InputField m_if_data;
	public InputField m_if_comment;

	public Button m_button_all_entries;
	public Button m_button_last_entry;
	public Button m_button_last_n_entries;
	public Button m_button_save;

	private bool	m_request_all_entries = false;
	private bool	m_request_last_entry = false;
	private bool	m_request_last_n_entries = false;
	private bool	m_request_save = false;

	private bool	m_submitted_request = false;

	private ManageSavedData m_data = null;


	public void Start() {

		m_data = GameObject.FindObjectOfType<ManageSavedData> ();

		if (m_data == null)
			DisableButtons ();
		else EnableButtons ();
	}

	public void GetLastEntryButton() {
		if (!m_data.isWorking) {
			DisableButtons ();
			m_data.GetLastEntry ();
			m_request_last_entry = true;
			m_submitted_request = true;
		}
	}

	public void GetLastNEntriesButton() {
		if (!m_data.isWorking) {
			DisableButtons ();
			m_data.GetLastEntries (2);
			m_request_last_n_entries = true;
			m_submitted_request = true;
		}
	}

	public void GetAllEntriesButton() {
		if (!m_data.isWorking) {
			DisableButtons ();
			m_data.GetAllEntries ();
			m_request_all_entries = true;
			m_submitted_request = true;
		}
	}

	public void SaveButton() {
		if (!m_data.isWorking) {
			DisableButtons ();
			m_data.SaveData (m_text_label.text,m_text_data.text,m_text_comment.text);
			m_request_save = true;
			m_submitted_request = true;
		}
	}

	void Update() {
		
		// there is not an ongoing request do nothing
		if (!m_submitted_request)
			return;

		// there is an ongoing request, was it 
		if (m_data.isResultReady) {
			if (m_request_last_entry) {
				m_text_last_entry.text = m_data.returnedString;
				m_submitted_request = false;
				m_request_last_entry = false;
				EnableButtons ();
			} else if (m_request_last_n_entries) {
				m_text_last_n_entries.text = m_data.returnedString;
				m_submitted_request = false;
				m_request_last_n_entries = false;
				EnableButtons ();
			} else if (m_request_all_entries) {
				m_text_all_entries.text = m_data.returnedString;
				m_submitted_request = false;
				m_request_all_entries = false;
				EnableButtons ();
			} else if (m_request_save) {
				m_if_label.text = "...";
				m_if_data.text = "...";
				m_if_comment.text = "...";
				m_submitted_request = false;
				m_request_save = false;
				EnableButtons ();
			}

		}
	}

	void DisableButtons() {
		SetButtons (false);
	}

	void EnableButtons() {
		SetButtons (true);
	}

	void SetButtons(bool flag) {
		m_button_all_entries.enabled = flag;
		m_button_last_entry.enabled = flag;
		m_button_last_n_entries.enabled = flag;
		m_button_save.enabled = flag;
	}
}
