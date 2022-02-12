using UnityEngine;

namespace Connectivity
{
    /// <summary>
    ///     URL-formatting utilities.
    /// </summary>
    public class ServerConnection
    {
        private static readonly string GET_PAGE = "post.php";
        private static readonly string POST_PAGE = "post.php";
        private static readonly string PHP_MESSAGE_VAR = "msg=";
        private static readonly string PHP_HASH_VAR = "data=";
        private readonly string encryptionIV;
        private readonly string encryptionKey;
        private readonly string serverSalt;
        private readonly string url;

        public ServerConnection(string url, string serverSalt, string encryptionKey,
            string encryptionIV)
        {
            this.url = url;
            this.serverSalt = serverSalt;
            this.encryptionKey = encryptionKey;
            this.encryptionIV = encryptionIV;
        }

        public string GenerateGetURL(string data)
        {
            var encrypted = WWW.EscapeURL(Encryption.Encrypt(data, encryptionKey, encryptionIV));
            var hash = WWW.EscapeURL(Encryption.Hash(data + "|" + serverSalt));
            return url + GET_PAGE + "?" + PHP_MESSAGE_VAR + encrypted + "&" + PHP_HASH_VAR + hash;
        }

        public string GeneratePostURL(string data)
        {
            var encrypted = WWW.EscapeURL(Encryption.Encrypt(data, encryptionKey, encryptionIV));
            var hash = WWW.EscapeURL(Encryption.Hash(data + "|" + serverSalt));
            return url + POST_PAGE + "?" + PHP_MESSAGE_VAR + encrypted + "&" + PHP_HASH_VAR + hash;
        }
    }
}