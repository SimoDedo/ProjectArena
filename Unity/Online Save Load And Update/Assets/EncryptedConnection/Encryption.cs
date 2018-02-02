using UnityEngine;
using System.Collections;
using System.Security.Cryptography;
using System.Text;

using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;



public class Encryption : MonoBehaviour {

	
    public static void RunSnippet()
    {
        // ENCRYPTING
		//string result = "";
        string result1 = Encrypt("Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas.");
        Debug.Log("CODIFICA   <"+result1+">");
        
        // DECRYPTING
        string result2 = Decrypt(result1);
		Debug.Log("DECODIFICA <"+result2+">");
    }
	
	/*
    static string SimpleTripleDes(string Data) {
        byte[] key = Encoding.ASCII.GetBytes("passwordDR0wSS@P6660juht");
        byte[] iv = Encoding.ASCII.GetBytes("password");
        byte[] data = Encoding.ASCII.GetBytes(Data);
        byte[] enc = new byte[0];
        TripleDES tdes = TripleDES.Create();
        tdes.IV = iv;
        tdes.Key = key;
        tdes.Mode = CipherMode.CBC;
        tdes.Padding = PaddingMode.Zeros;
        ICryptoTransform ict = tdes.CreateEncryptor();
        enc = ict.TransformFinalBlock(data, 0, data.Length);
        return ByteArrayToString(enc);
    }
	/*
    
    static string SimpleTripleDesDecrypt(string Data) {
        byte[] key = Encoding.ASCII.GetBytes("passwordDR0wSS@P6660juht");
        byte[] iv = Encoding.ASCII.GetBytes("password");
        byte[] data = StringToByteArray(Data);
        byte[] enc = new byte[0];
        TripleDES tdes = TripleDES.Create();
        tdes.IV = iv;
        tdes.Key = key;
        tdes.Mode = CipherMode.CBC;
        tdes.Padding = PaddingMode.Zeros;
        ICryptoTransform ict = tdes.CreateDecryptor();
        enc = ict.TransformFinalBlock(data, 0, data.Length);
        return Encoding.ASCII.GetString(enc);
    }
    
    public static string ByteArrayToString(byte[] ba) {
        string hex = BitConverter.ToString(ba);
        return hex.Replace("-","");
    }
    
    public static byte[] StringToByteArray(string hex) {
        int NumberChars = hex.Length;
        byte[] bytes = new byte[NumberChars / 2];
        for (int i = 0; i < NumberChars; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }*/
    
	// 
	void Awake()
	{
		/*
		RunSnippet();	
		
		string iv = "45287112549354892144548565456541";
		string key = "anjueolkdiwpoida";
		string cypher = "u+rIlHB/2rrT/u/qFInnlEkg2unhizsNzGVb9O54sP8=";
		string temp = DecryptRJ256(Decode(cypher), key, iv);
		Debug.Log("OUTPUT="+temp);
		
		byte[] encrypted = MyEncrypt("This is my encrypted message", Decode(key), Decode(iv));
		string dec = DecryptRJ256(encrypted, key, iv);
		
		Debug.Log("DECRYPTED <"+dec+">");
		*/
		
	}
	
	/*
	 * 
	 * Encrypt/Decrypt da MSDN
	 * http://msdn.microsoft.com/en-us/library/system.security.cryptography.cryptostream.aspx
	 * 
	 */

	void MSDNSnippet()
	{
		try
        {

            string original = "Here is some data to encrypt!";

            // Create a new instance of the Rijndael // class.  This generates a new key and initialization  // vector (IV). 
			using (Rijndael myRijndael = Rijndael.Create())
            {
                // Encrypt the string to an array of bytes. 
				byte[] encrypted = MSDNEncryptStringToBytes(original, myRijndael.Key, myRijndael.IV);

                // Decrypt the bytes to a string. 
				string roundtrip = MSDNDecryptStringFromBytes(encrypted, myRijndael.Key, myRijndael.IV);

                //Display the original data and the decrypted data.
                Debug.Log("Original:   "+original);
                Debug.Log("Round Trip: "+roundtrip);
            }

        }
        catch (Exception e)
        {
            //
			Debug.LogError("Error: " + e.Message);
        }		
	}
	
    static byte[] MSDNEncryptStringToBytes(string plainText, byte[] Key, byte[] IV)
    {
        // Check arguments. 
		if (plainText == null || plainText.Length <= 0)
            throw new ArgumentNullException("plainText");
        if (Key == null || Key.Length <= 0)
            throw new ArgumentNullException("Key");
        if (IV == null || IV.Length <= 0)
            throw new ArgumentNullException("Key");
        byte[] encrypted;
        // Create an Rijndael object // with the specified key and IV. 
		using (Rijndael rijAlg = Rijndael.Create())
        {
            rijAlg.Key = Key;
            rijAlg.IV = IV;

            // Create a decrytor to perform the stream transform.
            ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

            // Create the streams used for encryption. 
			using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    encrypted = msEncrypt.ToArray();
                }
            }
        }

        // Return the encrypted bytes from the memory stream. 
		return encrypted;

    }

    static string MSDNDecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
    {
        // Check arguments. 
		if (cipherText == null || cipherText.Length <= 0)
            throw new ArgumentNullException("cipherText");
        if (Key == null || Key.Length <= 0)
            throw new ArgumentNullException("Key");
        if (IV == null || IV.Length <= 0)
            throw new ArgumentNullException("Key");

        // Declare the string used to hold // the decrypted text. 
		string plaintext = null;

        // Create an Rijndael object // with the specified key and IV. 
		using (Rijndael rijAlg = Rijndael.Create())
        {
            rijAlg.Key = Key;
            rijAlg.IV = IV;

            // Create a decrytor to perform the stream transform.
            ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

            // Create the streams used for decryption. 
			using (MemoryStream msDecrypt = new MemoryStream(cipherText))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {

                        // Read the decrypted bytes from the decrypting stream // and place them in a string.
                        plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }

        }

        return plaintext;

    }	
	
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	public static string Encrypt (string toEncrypt)
	{
		byte[] keyArray = UTF8Encoding.UTF8.GetBytes ("12345678901234567890123456789012");
		// 256-AES key
		byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes (toEncrypt);
		RijndaelManaged rDel = new RijndaelManaged ();
		rDel.Key = keyArray;
		rDel.Mode = CipherMode.ECB;
		// http://msdn.microsoft.com/en-us/library/system.security.cryptography.ciphermode.aspx
		rDel.Padding = PaddingMode.PKCS7;
		// better lang support
		ICryptoTransform cTransform = rDel.CreateEncryptor ();
		byte[] resultArray = cTransform.TransformFinalBlock (toEncryptArray, 0, toEncryptArray.Length);
		return Convert.ToBase64String (resultArray, 0, resultArray.Length);
	}

	public static string Decrypt (string toDecrypt)
	{
		byte[] keyArray = UTF8Encoding.UTF8.GetBytes ("12345678901234567890123456789012");
		// AES-256 key
		byte[] toEncryptArray = Convert.FromBase64String (toDecrypt);
		RijndaelManaged rDel = new RijndaelManaged ();
		rDel.Key = keyArray;
		rDel.Mode = CipherMode.ECB;
		// http://msdn.microsoft.com/en-us/library/system.security.cryptography.ciphermode.aspx
		rDel.Padding = PaddingMode.PKCS7;
		// better lang support
		ICryptoTransform cTransform = rDel.CreateDecryptor ();
		byte[] resultArray = cTransform.TransformFinalBlock (toEncryptArray, 0, toEncryptArray.Length);
		return UTF8Encoding.UTF8.GetString (resultArray);
	}
	
	public byte[] Decode(string str)
    {
       var decbuff = Convert.FromBase64String(str);
       return decbuff;
    }

    static public String DecryptRJ256(byte[] cypher, string KeyString, string IVString)
    {
        var sRet = "";
        var encoding = new UTF8Encoding();
        var Key = encoding.GetBytes(KeyString);
        var IV = encoding.GetBytes(IVString);

	    using (var rj = new RijndaelManaged())
	    {
	        try
	        {
	            rj.Padding = PaddingMode.PKCS7;
	            rj.Mode = CipherMode.CBC;
	            rj.KeySize = 256;
	            rj.BlockSize = 256;
	            rj.Key = Key;
	            rj.IV = IV;
	            var ms = new MemoryStream(cypher);
	
	            using (var cs = new CryptoStream(ms, rj.CreateDecryptor(Key, IV), CryptoStreamMode.Read))
	            {
	                using (var sr = new StreamReader(cs))
	                {
	                    sRet = sr.ReadLine();
	                }
	            }
	        }
	        finally
	        {
	            rj.Clear();
	        }			
        }

    	return sRet;
    }

	//static public String EncryptRJ256(byte[] cypher, string KeyString, string IVString)
/*	static public String EncryptRJ256(string input, string KeyString, string IVString)
    {
        var sRet = "";
        var encoding = new UTF8Encoding();
        var Key = encoding.GetBytes(KeyString);
        var IV = encoding.GetBytes(IVString);

	    using (var rj = new RijndaelManaged())
	    {
	        try
	        {
	            rj.Padding = PaddingMode.PKCS7;
	            rj.Mode = CipherMode.CBC;
	            rj.KeySize = 256;
	            rj.BlockSize = 256;
	            rj.Key = Key;
	            rj.IV = IV;
	            var ms = new MemoryStream(cypher);
	
	            using (var cs = new CryptoStream(ms, rj.CreateDecryptor(Key, IV), CryptoStreamMode.Read))
	            {
	                using (var sr = new StreamReader(cs))
	                {
	                    sRet = sr.ReadLine();
	                }
	            }
	        }
	        finally
	        {
	            rj.Clear();
	        }			
        }

    	return sRet;
    }*/
	
	    //static byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV)
	    static byte[] MyEncrypt(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments. 
			if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            // Create an RijndaelManaged object // with the specified key and IV. 
			using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption. 
				using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream. 
			return encrypted;

        }


}
