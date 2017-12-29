/*
 * 
 * DES Encryption for C# and PHP
 * http://sanity-free.com/forum/viewtopic.php?id=133
 * 
 * UTF8 was originally ASCII
 * 
 */

using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

public class DESEncryption {

	// Use this for initialization
	void Start () {
		RunDESSnippet ();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
    public static void RunDESSnippet()
    {
        // ENCRYPTING
		Debug.Log("ORIGINA <" + "Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas" + ">");
        string result = SimpleTripleDes("Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas");
		Debug.Log("ENCRYPT <" + result + ">");
		        
        // DECRYPTING
        result = SimpleTripleDesDecrypt(result);
		Debug.Log("DECRYPT <"+result+">");
    }
    
    public static string SimpleTripleDes(string Data) {
        byte[] key = Encoding.UTF8.GetBytes("passwordDR0wSS@P6660juht");
        byte[] iv = Encoding.UTF8.GetBytes("password");
        byte[] data = Encoding.UTF8.GetBytes(Data);
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
    
    public static string SimpleTripleDesDecrypt(string Data) {
        byte[] key = Encoding.UTF8.GetBytes("passwordDR0wSS@P6660juht");
        byte[] iv = Encoding.UTF8.GetBytes("password");
        byte[] data = StringToByteArray(Data);
        byte[] enc = new byte[0];
        TripleDES tdes = TripleDES.Create();
        tdes.IV = iv;
        tdes.Key = key;
        tdes.Mode = CipherMode.CBC;
        tdes.Padding = PaddingMode.Zeros;
        ICryptoTransform ict = tdes.CreateDecryptor();
        enc = ict.TransformFinalBlock(data, 0, data.Length);
        return Encoding.UTF8.GetString(enc);
    }
    
    public static string ByteArrayToString(byte[] ba) {
        string hex = BitConverter.ToString(ba);
        return hex.Replace("-","");
    }
    
    public static byte[] StringToByteArray(String hex) {
        int NumberChars = hex.Length;
        byte[] bytes = new byte[NumberChars / 2];
        for (int i = 0; i < NumberChars; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }
}

/*
 * 
 * PHP COUNTERPART
 * 
 * 

<?php
// very simple ASCII key and IV
$key = "passwordDR0wSS@P6660juht";
$iv = "password";

$cipher = mcrypt_module_open(MCRYPT_3DES, '', 'cbc', '');

// ENCRYPTING
printvar(
  SimpleTripleDes('Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas.'),
  'Encrypted:'
);

// DECRYPTING
printvar(
  SimpleTripleDesDecrypt('51196a80db5c51b8523220383de600fd116a947e00500d6b9101ed820d29f198c705000791c07ecc1e090213c688a4c7a421eae9c534b5eff91794ee079b15ecb862a22581c246e15333179302a7664d4be2e2384dc49dace30eba36546793be'),
  'Decrypted:'
);

function SimpleTripleDes($buffer) {
  global $key, $iv, $cipher;
  printvar($buffer, 'Encrypting:');

  // get the amount of bytes to pad
  $extra = 8 - (strlen($buffer) % 8);
  //printvar($extra, 'Padding with n zeros');

  // add the zero padding
  if($extra > 0) {
    for($i = 0; $i < $extra; $i++) {
      $buffer .= "\0";
    }
  }

  mcrypt_generic_init($cipher, $key, $iv);
  $result = bin2hex(mcrypt_generic($cipher, $buffer));
  mcrypt_generic_deinit($cipher);
  return $result;
}

function SimpleTripleDesDecrypt($buffer) {
  global $key, $iv, $cipher;
  printvar($buffer, 'Decrypting:');

  mcrypt_generic_init($cipher, $key, $iv);
  $result = rtrim(mdecrypt_generic($cipher, hex2bin($buffer)), "\0");
  mcrypt_generic_deinit($cipher);
  return $result;
}

function hex2bin($data)
{
  $len = strlen($data);
  return pack("H" . $len, $data);
} 

// HELPER FUNCTIONS

function printvar($var, $label="") {
    print "<pre style=\"border: 1px solid #999; background-color: #f7f7f7; color: #000; overflow: auto; width: auto; text-align: left; padding: 1em;\">" .
        (
            (
                strlen(
                    trim($label)
                )
            ) ? htmlentities($label)."\n===================\n" : ""
        ) .
        htmlentities(print_r($var, TRUE)) . "</pre>";
}
?>

*/
