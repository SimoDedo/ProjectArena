using UnityEngine;
using System.Collections;
//using System.Web;

public class Leaderboard : MonoBehaviour {
	
	// keys for encrypting
	static private string sKy = "lkirwf897+22#bbtrm8814z5qq=498j5"; //32 chr shared ascii string (32 * 8 = 256 bit)
	static private string sIV = "741952hheeyy66#cs!9hjv887mxx7@8y"; //32 chr shared ascii string (32 * 8 = 256 bit)
	// key added to data for hashing
	static private string sHc = "DKud_64$D:ZUg=c i}d1XG|_?KkE o(S(V!U)*d c_tJNp_]>+`CT2:sd>]&W}~d"; 
	
	// RJ Encoding keys for getting the data from the server
	static private string sKy_get = "nE1pg04e8830117cyYL8IKCYLNvVp1yI"; // 32 * 8 = 256 bit key
	static private string sIV_get = "p8A84403WB8V2SVnj5758Xw8YW836z5y"; // 32 * 8 = 256 bit iv
	// hashcode
	static private string sHc_get = "CWzthZEK5vhvNrmL3dF1VLqYXU0BVMgqL0M8Km7I2b0v2WQ71JpzU62KrBn8X71z";	
	
	// 
	public string url = "http://lb.pierlucalanzi.net";
	public string dir = "globulandia";
	public string php_post = "post.php";
	public string php_get = "get.php";
	private string sMsg="?msg=";
	private string sData="&data=";
	private string url_update;
	private string url_result;
	
	// Use this for initialization
	void Start () {
		Debug.Log("Test for Getter");
	//IEnumerator Start () {

		//string id1 = "fid|nick|cognome|nome|scuola|120|100|M|45|game|data|ora";
		//Post (id1);
		
		string id2 = "fight|200";
		Get (id2);		
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	// submit data to the website
	//IEnumerator Post (string data)
	void Post (string data)
	{
		string encrypted = WWW.EscapeURL(RJEncryption.EncryptRJ256(sKy, sIV, data));		
		string hash_check = WWW.EscapeURL(RJEncryption.Md5Sum(data+"|"+sHc));
		
		//Debug.LogError("ENCRYPTED="+RJEncryption.EncryptRJ256(sKy,sIV,data));
		//Debug.LogError("DECRYPTED="+RJEncryption.DecryptRJ256(sKy,sIV,WWW.UnEscapeURL(encrypted)));
			
		url_update = url+"/"+dir+"/"+php_post+sMsg+encrypted+sData+hash_check;
			
		// apply the url encoding to re-encode special characters etc. 
	
		Debug.Log("Started the corouting <" + url_update + ">");
		
		url_result = "No result so far";
		Debug.Log("==>"+url_result.Length + " <"+url_result+">");
		
        //renderer.material.mainTexture = www.texture;
		StartCoroutine(SubmitPost2(url_update));	
		
	}
	
	//
	// get data from the website for a game with a limit on the number of rows
	//
	void Get (string data)
	{
		string encrypted = WWW.EscapeURL(RJEncryption.EncryptRJ256(sKy_get, sIV_get, data));		
		string hash_check = WWW.EscapeURL(RJEncryption.Md5Sum(data+"|"+sHc_get));

		url_update = url+"/"+dir+"/"+php_get+sMsg+encrypted+sData+hash_check;
			
		// apply the url encoding to re-encode special characters etc. 
		Debug.Log("Started the corouting");
		
		url_result = "No result so far";
		Debug.Log("GET ==>"+url_result.Length + " <"+url_result+">");
		
        //renderer.material.mainTexture = www.texture;
		StartCoroutine(SubmitPost2(url_update));	
	}

	IEnumerator SubmitPost()
	{
		
		Debug.LogError("URL <"+url_update+">");
		
		WWW www = new WWW(url_update);
				
		yield return www;
		
		Debug.Log("==>"+www.text.Length + " <"+www.text+">");
		
		url_result = www.text;
	}

	IEnumerator SubmitPost2(string url_update)
	{
		
		Debug.LogError("URL ==>> <"+url_update+">");
		
		WWW www = new WWW(url_update);
						
		yield return www;
		
		//Debug.Log("==>"+www.text.Length + " <"+www.text+">");
		
		url_result = www.text;
	}

	void Check()
	{	
		string str = "Hello! My name is PLL.";
		string estr = DESEncryption.SimpleTripleDes(str);
		estr = RJEncryption.EncryptRJ256(sKy, sIV,str);
		
		//Debug.LogError("URL="+url+"/"+dir+"/"+php_post+message+estr);
		//Debug.Log ("CHECK="+RJEncryption.DecryptRJ256(sKy,sIV,estr));
        //WWW www = new WWW(url+dir+php_post+message+estr);
        //yield return www;
        //renderer.material.mainTexture = www.texture;
	}

}
