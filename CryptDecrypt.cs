using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NotebookTracker {
    // NOTE: 'partial' — the AES Key lives in the git-ignored KeyMaterial.cs.
    // The IV is generated randomly per encryption and prepended to the
    // ciphertext (first 16 bytes). The whole [IV||ciphertext] blob is encoded
    // with URL-SAFE Base64 (RFC 4648 §5): '+' -> '-', '/' -> '_', padding
    // stripped. Tokens are therefore safe in URLs with no HttpUtility.UrlEncode
    // and survive the /iframe -> ProxyHandler.aspx rewrite untouched.
    public partial class CryptDecrypt {

        private const int IvSize = 16; // AES block size (128-bit IV)

        // ── URL-safe Base64 helpers ──

        private static string ToUrlSafeBase64(byte[] bytes) {
            return Convert.ToBase64String(bytes)
                          .Replace('+', '-')
                          .Replace('/', '_')
                          .TrimEnd('=');            // strip padding
        }

        private static byte[] FromUrlSafeBase64(string token) {
            string s = token.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4) {                 // restore padding
                case 2: s += "=="; break;
                case 3: s += "=";  break;
            }
            return Convert.FromBase64String(s);
        }

        // ── Core: single source of truth for the IV-prepend contract ──

        /// <summary>
        /// Encrypts plaintext bytes. Output = URL-safe Base64 of
        /// [16-byte random IV][ciphertext].
        /// </summary>
        private static string EncryptBytes(byte[] plainBytes) {
            using (Aes aes = Aes.Create()) {
                aes.Key = Key;
                aes.GenerateIV();                   // fresh random IV every call
                byte[] iv = aes.IV;

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, iv))
                using (MemoryStream ms = new MemoryStream()) {
                    ms.Write(iv, 0, iv.Length);     // prepend IV
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write)) {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                    }
                    return ToUrlSafeBase64(ms.ToArray());
                }
            }
        }

        /// <summary>Reverses EncryptBytes: reads prepended IV, returns plaintext bytes.</summary>
        private static byte[] DecryptBytes(string encrypted) {
            byte[] input = FromUrlSafeBase64(encrypted);
            if (input.Length < IvSize)
                throw new CryptographicException("Ciphertext too short to contain an IV.");

            byte[] iv = new byte[IvSize];
            Buffer.BlockCopy(input, 0, iv, 0, IvSize);

            using (Aes aes = Aes.Create()) {
                aes.Key = Key;
                aes.IV = iv;
                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, iv))
                using (MemoryStream ms = new MemoryStream()) {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write)) {
                        cs.Write(input, IvSize, input.Length - IvSize); // skip IV
                        cs.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
        }

        // ── Public API ──

        /// <summary>
        /// Decrypts an encrypted connection string.
        /// V2: put the encrypted value (produced by ConnectionStringEncryptor,
        /// same key + prepended-IV + URL-safe-Base64 scheme) into
        /// connections.config and enable the call in Utils.GetConnectionString.
        /// </summary>
        public static string DecryptConnectionString(string encryptedString) {
            return Encoding.UTF8.GetString(DecryptBytes(encryptedString));
        }

        /// <summary>Encrypts a URL with a 5-minute expiry for the proxy handler.</summary>
        public string EncryptUrlWithExpiry(string url) {
            try {
                DateTime expiryTime = DateTime.UtcNow.AddMinutes(5);
                string dataToEncrypt = url + "|" + expiryTime.Ticks.ToString();
                return EncryptBytes(Encoding.UTF8.GetBytes(dataToEncrypt));
            }
            catch (Exception ex) {
                Utils.LogDebug("Error encrypting URL: " + ex.Message);
                throw new Exception("Failed to secure the document URL", ex);
            }
        }

        /// <summary>Decrypts a URL token back to the original URL.</summary>
        public string DecryptUrl(string encryptedToken) {
            try {
                return Encoding.UTF8.GetString(DecryptBytes(encryptedToken));
            }
            catch (Exception ex) {
                Utils.LogDebug("Error decrypting URL: " + ex.Message);
                throw new Exception("Failed to decode the document URL", ex);
            }
        }

        /// <summary>
        /// Decrypts a token to "url|expiryTicks". Returns empty string on failure
        /// (ProxyHandler treats empty as an invalid token).
        /// </summary>
        public string DecryptUrlWithExpiry(string encryptedToken) {
            try {
                return Encoding.UTF8.GetString(DecryptBytes(encryptedToken));
            }
            catch (Exception ex) {
                Utils.LogDebug("Error decrypting URL with expiry: " + ex.Message);
                return string.Empty;
            }
        }
    }
}
