using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using UnityEngine;

public class RJEncryption {

    public static void RunRJSnippet() {
        // Shared 256 bit Key and IV. 32 chr shared ascii string (32 * 8 = 256 bit).
        const string sKy = "lkirwf897+22#bbtrm8814z5qq=498j5";
        const string sIV = "741952hheeyy66#cs!9hjv887mxx7@8y";

        var sTextVal = "Here is the data to encrypt.";

        var eText = EncryptRJ256(sKy, sIV, sTextVal);
        var dText = DecryptRJ256(sKy, sIV, eText);

        Debug.Log("The encrypted text is '" + eText + "'.");
        Debug.Log("The decrypted text is '" + dText + "'.");
    }

    public static string DecryptRJ256(string prm_key, string prm_iv, string prm_text_to_decrypt) {
        var sEncryptedString = prm_text_to_decrypt;

        var myRijndael = new RijndaelManaged() {
            Padding = PaddingMode.Zeros,
            Mode = CipherMode.CBC,
            KeySize = 256,
            BlockSize = 256
        };

        var key = Encoding.ASCII.GetBytes(prm_key);
        var IV = Encoding.ASCII.GetBytes(prm_iv);

        var decryptor = myRijndael.CreateDecryptor(key, IV);

        var sEncrypted = Convert.FromBase64String(sEncryptedString);

        var fromEncrypt = new byte[sEncrypted.Length];

        var msDecrypt = new MemoryStream(sEncrypted);
        var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);

        csDecrypt.Read(fromEncrypt, 0, fromEncrypt.Length);

        return (Encoding.ASCII.GetString(fromEncrypt));
    }

    public static string EncryptRJ256(string prm_key, string prm_iv, string prm_text_to_encrypt) {

        var sToEncrypt = prm_text_to_encrypt;

        var myRijndael = new RijndaelManaged() {
            Padding = PaddingMode.Zeros,
            Mode = CipherMode.CBC,
            KeySize = 256,
            BlockSize = 256
        };

        var key = Encoding.ASCII.GetBytes(prm_key);
        var IV = Encoding.ASCII.GetBytes(prm_iv);

        var encryptor = myRijndael.CreateEncryptor(key, IV);

        var msEncrypt = new MemoryStream();
        var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

        var toEncrypt = Encoding.ASCII.GetBytes(sToEncrypt);

        csEncrypt.Write(toEncrypt, 0, toEncrypt.Length);
        csEncrypt.FlushFinalBlock();

        var encrypted = msEncrypt.ToArray();

        return (Convert.ToBase64String(encrypted));
    }

    // MD5 (http://wiki.unity3d.com/index.php?title=MD5).
    static public string Md5Sum(string strToEncrypt) {
        System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
        byte[] bytes = ue.GetBytes(strToEncrypt);

        // Encrypt bytes.
        System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
        byte[] hashBytes = md5.ComputeHash(bytes);

        // Convert the encrypted bytes back to a string (base 16).
        string hashString = "";

        for (int i = 0; i < hashBytes.Length; i++) {
            hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
        }

        return hashString.PadLeft(32, '0');
    }

}
