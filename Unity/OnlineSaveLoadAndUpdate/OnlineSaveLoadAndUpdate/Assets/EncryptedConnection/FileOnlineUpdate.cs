#define DEBUG

using UnityEngine;
using System.Collections;


public class FileOnlineUpdate : MonoBehaviour {


	/// singleton pattern access
	public static FileOnlineUpdate	instance { get {return _instance; } } 

	/// prefix 
	public static string prefix = "redono-trivia-";

	/// directories
	public static string sourceDirectory = "Resources";
	public static string destDirectory = Application.persistentDataPath;

	/// files and directories
	public static string[] dirs = {"Config", "Images"};
	public static string[] text_files = {"Config/test.json"};

	/// www links
	/// 
	/// http is the main directory where files are stored
	/// 
	public static string http = "http://lb.pierlucalanzi.net/redono/";
	
	private static string	downloaded = string.Empty;
	private static bool		isCheckingVersion { get {return _isCheckingVersion; } } 
	private static bool		isDownloadingFiles { get {return _isDownloadingFiles; } } 
	private static bool		isEverythingDone { get {return _isEverythingDone; } } 

	void Awake () {
		Debug.LogError("SCRIVE DA "+sourceDirectory+"+\nA"+destDirectory);

#if DEBUG
		if (startFresh) PlayerPrefs.DeleteAll();
#endif
		if (IsFirstRun())
		{
			CreateDirectories(dirs);
			CopyTextFilesFromResourcesToSandbox(text_files);
			UnsetFirstRun();
			SetVersion(0);
		}
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnGUI () {
		GUI.Button(new Rect(0,0,100,100f), IsFirstRun().ToString());
	}

	public static void ReadTextFile(string fn, out string ta)
	{
		if (System.IO.File.Exists(Application.persistentDataPath + "/"+fn))
		{
			ta = System.IO.File.ReadAllText(Application.persistentDataPath+"/"+fn);
		} else {
			ta = (Resources.Load(FilenameWithoutExtension(fn)) as TextAsset).text;
		}

		Debug.LogError("READ "+ta);
	}

	#region PRIVATE_FIELDS
	private static FileOnlineUpdate	_instance = null;
	private static bool _isCheckingVersion = false;
	private static bool _isDownloadingFiles = false;
	private static bool _isEverythingDone = false;


	private bool loading = false;

	#endregion

	#region PRIVATE_METHODS
	/// <summary>
	/// Setup the singleton pattern
	/// </summary>
	void SingletonSetup() {
		if (instance == null) 
		{
			_instance = this;
			DontDestroyOnLoad(this);
		} else {
			Destroy (gameObject);
		}
	}

	/// <summary>
	/// Check whether it is the first run of the application
	/// </summary>
	/// <returns><c>true</c> if it is the first run; otherwise, <c>false</c>.</returns>
	bool IsFirstRun() {
		string flag = prefix+"isFirstRun";

		if (PlayerPrefs.HasKey(flag))
			return (((int)PlayerPrefs.GetInt("flag"))==0);
		else {
			return true;
		}
	}

	/// <summary>
	/// Set the first run flag to false
	/// </summary>
	void UnsetFirstRun() {
		string flag = prefix+"isFirstRun";
		PlayerPrefs.SetInt (flag, 1);
	}
	

	int Version() {
		string flag = prefix+"Version";
		if (PlayerPrefs.HasKey(flag))
			return ((int)PlayerPrefs.GetInt("flag"));
		else {
			return 0;
		}
	}

	void SetVersion(int version) {
		string flag = prefix+"Version";
		PlayerPrefs.SetInt("flag", version);
	}

	/// <summary>
	/// create a set of directories in the sandbox
	/// </summary>
	/// <param name="dn">dirList</param>
	void CreateDirectories(string[] dirList)
	{
		foreach (string dn in dirList)
		{
			string dir = Application.persistentDataPath + "/"+dn;
			if(!System.IO.Directory.Exists(dir)) {
				System.IO.Directory.CreateDirectory(dir);
			}
		}
	}

	/// <summary>
	/// Copies the text files from resources to sandbox.
	/// </summary>
	/// <param name="fileList">File list.</param>
	void CopyTextFilesFromResourcesToSandbox(string[] fileList)
	{
		foreach (string fn in fileList)
		{
			if(!System.IO.File.Exists(Application.persistentDataPath + "/"+fn)){

				// filename with no extension
				string fnne = FilenameWithoutExtension(fn);

				TextAsset		xmlAsset = Resources.Load(fnne) as TextAsset;
				string			xmlContent = xmlAsset.text;
				System.IO.File.WriteAllText(Application.persistentDataPath+"/"+fn, xmlContent);
			}
		}
	}

	/// <summary>
	/// Retrieve the text file online from the server 
	/// 
	/// http://answers.unity3d.com/questions/705516/downloading-images-and-text-files-from-server.html
	/// </summary>
	/// <param name="fn">fn</param>
	/// <param name="ta">ta</param>
	public void RetrieveTextFileOnline(string fn, out string ta)
	{
		ta = string.Empty;

//		DownloadTextFiles(http+fn);
	}

	/// downloads all the required files
//	IEnumerator DownloadTextFiles(string[] urls) {
//		WWW www = new WWW(url);
//		yield return www;
//		downloaded = www.text;
//	}


	public static string FilenameWithoutExtension(string fn)
	{
		int fileExtPos = fn.LastIndexOf(".");
		if (fileExtPos >= 0 )
			return fn.Substring(0, fileExtPos);
		else return fn;
	}
	

	#endregion

	#region DEBUG_METHODS
	public bool startFresh = false;
	#endregion
}
