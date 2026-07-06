using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace NotebookTracker {
  public static class Utils {
    public static void LogDebug(string message) {
      return; // hard-coded return for prod, no debugging.
      bool debugMode = false;

      try {
        string configDebugMode = System.Configuration.ConfigurationManager.AppSettings["DEBUG_MODE"];
        if (!string.IsNullOrEmpty(configDebugMode)) {
          bool.TryParse(configDebugMode, out debugMode);
        }
        else {
          debugMode = AppConstants.DEBUG_MODE;
        }
      }
      catch {
        debugMode = AppConstants.DEBUG_MODE;
      }
      // Return immediately if debug mode is disabled
      if (!debugMode) {
        return;
      }

      string logPath = AppDomain.CurrentDomain.BaseDirectory + "App_Data\\debug_log.txt";
      CheckAndAppendToFile(logPath, message);
    } // LogDebug

    public static void LogError(string message) {
      string logPath = AppDomain.CurrentDomain.BaseDirectory + "App_Data\\errors.log";
      CheckAndAppendToFile(logPath, message);
    } // LogError

    private static void CheckAndAppendToFile(string logPath, string message) {
      try {
        // Make sure the directory exists
        string directory = System.IO.Path.GetDirectoryName(logPath);
        if (!System.IO.Directory.Exists(directory)) {
          System.IO.Directory.CreateDirectory(directory);
        }

        using (System.IO.StreamWriter sw = System.IO.File.AppendText(logPath)) {
          sw.WriteLine(DateTime.Now.ToString() + ": " + message);
        }
      }
      catch { } // Suppress errors in logging
    } // LogError

    // Get decrypted connection string
    public static string GetConnectionString(string name) {
      string encryptedString = ConfigurationManager.ConnectionStrings[name].ConnectionString;
      return CryptDecrypt.DecryptConnectionString(encryptedString);
    } // GetConnectionString
  } // utils
} // NotebookTracker
