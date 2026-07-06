using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// Compiles KeyMaterial.cs alongside it (see Compile-*.bat) => same key as the
// library. Same scheme: random 16-byte IV prepended, URL-safe Base64.
// Keep this tool and its .exe OUT of any public repo (key compiles in).
namespace NotebookTracker {
    class ConnectionStringEncryptorProgram {
        private const int IvSize = 16;

        static void Main(string[] args) {
            try {
                string connectionString = (args.Length > 0)
                    ? args[0]
                    : Prompt("Enter connection string to encrypt:");

                if (string.IsNullOrWhiteSpace(connectionString)) {
                    Console.WriteLine("Error: Connection string cannot be empty.");
                    Console.WriteLine("Usage: ConnectionStringEncryptor.exe \"your connection string\"");
                    WaitForKeyPress();
                    return;
                }

                string encrypted = EncryptString(connectionString);

                Console.WriteLine("\nEncrypted connection string:");
                Console.WriteLine(encrypted);
                Console.WriteLine("\nFor connections.config:");
                Console.WriteLine("<connectionStrings>");
                Console.WriteLine("  <add name=\"DefaultConnection\" connectionString=\"" + encrypted + "\" providerName=\"System.Data.SqlClient\" />");
                Console.WriteLine("</connectionStrings>");

                // Round-trip self-check so you never paste a value that won't decrypt
                string check = Encoding.UTF8.GetString(DecryptBytes(encrypted));
                Console.WriteLine("\nRound-trip check: " + (check == connectionString ? "PASS" : "FAIL"));

                WaitForKeyPress();
            }
            catch (Exception ex) {
                Console.WriteLine("Error: " + ex.Message);
                WaitForKeyPress();
            }
        }

        // ── URL-safe Base64 (identical to CryptDecrypt) ──
        private static string ToUrlSafeBase64(byte[] bytes) {
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }
        private static byte[] FromUrlSafeBase64(string token) {
            string s = token.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
            return Convert.FromBase64String(s);
        }

        public static string EncryptString(string plainText) {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            using (Aes aes = Aes.Create()) {
                aes.Key = CryptDecrypt.Key;         // shared key from KeyMaterial.cs
                aes.GenerateIV();
                byte[] iv = aes.IV;
                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, iv))
                using (MemoryStream ms = new MemoryStream()) {
                    ms.Write(iv, 0, iv.Length);
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write)) {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                    }
                    return ToUrlSafeBase64(ms.ToArray());
                }
            }
        }

        private static byte[] DecryptBytes(string encrypted) {
            byte[] input = FromUrlSafeBase64(encrypted);
            byte[] iv = new byte[IvSize];
            Buffer.BlockCopy(input, 0, iv, 0, IvSize);
            using (Aes aes = Aes.Create()) {
                aes.Key = CryptDecrypt.Key;
                aes.IV = iv;
                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, iv))
                using (MemoryStream ms = new MemoryStream()) {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write)) {
                        cs.Write(input, IvSize, input.Length - IvSize);
                        cs.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
        }

        private static string Prompt(string label) {
            Console.WriteLine(label);
            return Console.ReadLine();
        }
        private static void WaitForKeyPress() {
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
