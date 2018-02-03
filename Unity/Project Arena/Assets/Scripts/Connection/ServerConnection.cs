using UnityEngine;

public class ServerConnection {

    // Keys for encrypting. 32 chr shared ascii string (32 * 8 = 256 bit).
    private const string sKy = "lkirwf897+22#bbtrm8814z5qq=498j5";
    private const string sIV = "741952hheeyy66#cs!9hjv887mxx7@8y";

    // Key added to data for hashing.
    private const string sHc = "DKud_64$D:ZUg=c i}d1XG|_?KkE o(S(V!U)*d c_tJNp_]>+`CT2:sd>]&W}~d";

    // RJ Encoding keys for getting the data from the server. 32 * 8 = 256 bit.
    private const string sKyGet = "nE1pg04e8830117cyYL8IKCYLNvVp1yI";
    private const string sIVGet = "p8A84403WB8V2SVnj5758Xw8YW836z5y";
    // Hashcode.
    private const string sHcGet = "CWzthZEK5vhvNrmL3dF1VLqYXU0BVMgqL0M8Km7I2b0v2WQ71JpzU62KrBn8X71z";

    private const string url = "https://data.polimigamecollective.org";
    private const string dir = "arena";
    private const string phpPost = "post.php";
    private const string phpGet = "post.php";

    private const string sMsg = "?msg=";
    private const string sData = "&data=";

    public static string GeneratePostURL(string data) {
        string encrypted = WWW.EscapeURL(RJEncryption.EncryptRJ256(sKy, sIV, data));
        string hashCheck = WWW.EscapeURL(RJEncryption.Md5Sum(data + "|" + sHc));

        string urlUpdate = url + "/" + dir + "/" + phpPost + sMsg + encrypted + sData + hashCheck;

        return urlUpdate;
    }

    public static string GenerateGetURL(string data) {
        string encrypted = WWW.EscapeURL(RJEncryption.EncryptRJ256(sKy, sIV, data));
        string hashCheck = WWW.EscapeURL(RJEncryption.Md5Sum(data + "|" + sHc));

        string urlUpdate = url + "/" + dir + "/" + phpGet + sMsg + encrypted + sData + hashCheck;

        return urlUpdate;
    }

}