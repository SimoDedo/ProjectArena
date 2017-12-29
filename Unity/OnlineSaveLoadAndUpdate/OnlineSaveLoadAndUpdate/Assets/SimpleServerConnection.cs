using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleServerConnection  {

	private const string php_post = "post.php";
	private const string php_get = "get.php";
	private const string sMsg="?data=";
	private static string url = "https://data.polimigamecollective.org";

	private static string dir = "scopone";

	public static void Init(string _url = "https://data.polimigamecollective.org", string _dir = "scopone") {

	}

	public static string GeneratePostURL(string data) {
		string encrypted = WWW.EscapeURL(data);		
		string url_update =
			SimpleServerConnection.url+"/"+
			SimpleServerConnection.dir+"/"+
			SimpleServerConnection.php_post+
			SimpleServerConnection.sMsg+encrypted;
		return url_update;
	}

	public static string GenerateGetURL(string data) {
		string encrypted = WWW.EscapeURL(data);		
		string url_update = 
			SimpleServerConnection.url+"/"+
			SimpleServerConnection.dir+"/"+
			SimpleServerConnection.php_post+
			SimpleServerConnection.sMsg+encrypted;
		return url_update;
	}
}
