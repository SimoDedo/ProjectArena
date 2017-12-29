using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerConnection  {

	// keys for encrypting
	private const string sKy = "lkirwf897+22#bbtrm8814z5qq=498j5"; //32 chr shared ascii string (32 * 8 = 256 bit)
	private const string sIV = "741952hheeyy66#cs!9hjv887mxx7@8y"; //32 chr shared ascii string (32 * 8 = 256 bit)

	// key added to data for hashing
	private const string sHc = "DKud_64$D:ZUg=c i}d1XG|_?KkE o(S(V!U)*d c_tJNp_]>+`CT2:sd>]&W}~d"; 

	// RJ Encoding keys for getting the data from the server
	private const string sKy_get = "nE1pg04e8830117cyYL8IKCYLNvVp1yI"; // 32 * 8 = 256 bit key
	private const string sIV_get = "p8A84403WB8V2SVnj5758Xw8YW836z5y"; // 32 * 8 = 256 bit iv
	// hashcode
	private const string sHc_get = "CWzthZEK5vhvNrmL3dF1VLqYXU0BVMgqL0M8Km7I2b0v2WQ71JpzU62KrBn8X71z";	

	// 
	private const string url = "https://data.polimigamecollective.org";
	private const string dir = "mancala";
	private const string php_post = "post.php";
	private const string php_get = "post.php";

	private const string sMsg="?msg=";
	private const string sData="&data=";


	public static string GeneratePostURL(string data) {
		string encrypted = WWW.EscapeURL(RJEncryption.EncryptRJ256(sKy, sIV, data));		
		string hash_check = WWW.EscapeURL(RJEncryption.Md5Sum(data+"|"+sHc));

		string url_update = url+"/"+dir+"/"+php_post+sMsg+encrypted+sData+hash_check;

		return url_update;

	}

	public static string GenerateGetURL(string data) {
		string encrypted = WWW.EscapeURL(RJEncryption.EncryptRJ256(sKy, sIV, data));		
		string hash_check = WWW.EscapeURL(RJEncryption.Md5Sum(data+"|"+sHc));

		string url_update = url+"/"+dir+"/"+php_get+sMsg+encrypted+sData+hash_check;

		return url_update;
	}

}
