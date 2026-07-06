using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;

namespace NotebookTracker {
  /// <summary>
  /// Handles user authentication by processing email and password from a plain HTML form,
  /// encrypting the password, and validating credentials against the database.
  /// This class can be compiled as a DLL and used in any .NET web application.
  /// </summary>
  public class LoginHandler {
    // Connection string
    private readonly string _connectionString;

    // Constants for session management
    //private const string SESSION_COOKIE_NAME = "NotebookTracker";
    private readonly string _loginPageUrl;

    // Cookie keys
    //private const string SESSION_ID_KEY = "SessionId";
    // private const string RETURN_URL_KEY = "ReturnUrl";

    /// <summary>
    /// Constructor that accepts a specific connection string
    /// </summary>
    /// <param name="connectionString">Database connection string</param>
    /// <param name="loginPageUrl">URL of the login page for redirects</param>
    public LoginHandler(string connectionString, string loginPageUrl = "/login.html") {
      _connectionString = connectionString;
      _loginPageUrl = loginPageUrl;
    }

    /// <summary>
    /// Processes authentication by either validating login credentials or checking existing session
    /// </summary>
    /// <param name="request">The HTTP request object</param>
    /// <param name="response">The HTTP response object for setting cookies</param>
    /// <param name="server">The HTTP server object for transferring to pages</param>
    /// <param name="redirectOnSuccess">Path to transfer to if login is successful</param>
    /// <returns>GUID if authenticated, null otherwise</returns>
    public Guid? ProcessAuthentication(HttpRequest request, HttpResponse response, HttpServerUtility server, string redirectOnSuccess = null) {
      if (request == null) { // || request.HttpMethod != "POST") {
        return null;
      }
      //try {
      Guid? sessionId = null;

      Utils.LogDebug("ProcessAuthentication: Current Script: " + request.Url.AbsoluteUri);

      // Generate device fingerprint
      string deviceFingerprint = GenerateDeviceFingerprint(request);
      string ipAddress = GetClientIPAddress(request);
      string currentPath = request.Path.ToLower();
      string physicalPath = request.PhysicalPath;
      string executingFile = Path.GetFileName(physicalPath);
      bool isTransferedFromLogin = false;

      try {
        if (HttpContext.Current != null ) {
          object transferValue = HttpContext.Current.Session["TransferredFromLogin"];
          if (transferValue != null) {
            isTransferedFromLogin = Convert.ToBoolean(transferValue);
          }
        }
      }
      catch (Exception) {
        // Log the exception if needed
        Utils.LogDebug("catch for TransferredFromLogin");
      }

      Utils.LogDebug("ProcessAuthentication: currentPath: " + currentPath);
      Utils.LogDebug("ProcessAuthentication: e: " + request.Form["e"]);
      Utils.LogDebug("ProcessAuthentication: p: " + request.Form["p"]);
      Utils.LogDebug("ProcessAuthentication: executingFile: " + executingFile);
      // First, check if email and password were submitted via POST
      if (isTransferedFromLogin == false && request.HttpMethod == "POST" && currentPath.EndsWith("login.aspx") && !string.IsNullOrEmpty(request.Form["e"]) && !string.IsNullOrEmpty(request.Form["p"])) {
        // Extract email and password from the plain HTML form
        string email = request.Form["e"];
        string password = request.Form["p"];
        // Validate input
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) {
          RedirectToLogin(response, server, request);
          return null;
        }

        // Validate email format
        if (!IsValidEmail(email)) {
          RedirectToLogin(response, server, request);
          return null;
        }

        // Validate password length
        if (password.Length < 8) {
          RedirectToLogin(response, server, request);
          return null;
        }

        Utils.LogDebug("ProcessAuthentication: calling AuthenticateUser()");
        // Attempt authentication with credentials
        sessionId = AuthenticateUser(email, password, ipAddress, deviceFingerprint);
        Utils.LogDebug("ProcessAuthentication: immediately after AuthenticateUser()");
      }
      else {
        // No credentials submitted, check for existing session cookie
        string sessionIdStr = GetCookieValue(request, AppConstants.SESSION_ID_KEY);
        Utils.LogDebug("ProcessAuthentication: else: sessionIdStr: " + sessionIdStr);
        if (!string.IsNullOrEmpty(sessionIdStr)) {
          // Try to parse the session GUID
          Guid parsedSessionId;
          Utils.LogDebug("ProcessAuthentication: else: parsing sessionId");
          Guid.TryParse(sessionIdStr, out parsedSessionId);
          sessionId = parsedSessionId;
          //  if (Guid.TryParse(sessionIdStr, out parsedSessionId)) {
          //  // Validate and extend the session
          Utils.LogDebug("ProcessAuthentication: else: validating n extending session. parsed session id: " + parsedSessionId.ToString());
          //  // sessionId = GetCookieValue(parsedSessionId), ipAddress, deviceFingerprint);

          // Call ValidateAndExtendSession() with empty strings for ipAddress and deviceFingerprint
          Guid? validatedSessionId = ValidateAndExtendSession(sessionId.Value, "", "");

          // Check if the returned sessionId is valid
          if (!validatedSessionId.HasValue) {
            // If not valid, redirect to login page
            RedirectToLogin(response, server, request);
          }
          //}
        }
      }

      // Set cookie if login was successful
      if (sessionId.HasValue) {
        // Save session ID to cookie
        Utils.LogDebug("ProcessAuthentication: setting cookie. sessionId: " + sessionId.Value.ToString());
        SetCookieValue(response, request, AppConstants.SESSION_ID_KEY, sessionId.Value.ToString());

        Utils.LogDebug("ProcessAuthentication: redirectOnSuccess: " + redirectOnSuccess);
        // Perform server-side transfer if a redirect path was provided
        if (!string.IsNullOrEmpty(redirectOnSuccess) && server != null) {
          // Server.Transfer maintains the original URL in the browser
          Utils.LogDebug("ProcessAuthentication: transferring to: " + redirectOnSuccess);
          HttpContext.Current.Items["TransferredFromLogin"] = true;
          //server.Transfer(redirectOnSuccess, true);
          response.Redirect(redirectOnSuccess);
        }
        Utils.LogDebug("Returning sessionId from ProcessAuthentication 1: " + sessionId.ToString());
        return sessionId;
      }
      else {
        Utils.LogDebug("ProcessAuthentication: auth fail");
        // Authentication failed, redirect to login page if server was provided
        if (server != null) {
          Utils.LogDebug("ProcessAuthentication: auth fail, server != null");
          // Authentication failed, redirect to login page
          RedirectToLogin(response, server, request);
        }
        return null;
      }
    }

    /// <summary>
    /// Hashes a password using BCrypt with automatically generated salt
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>BCrypt hash as binary data</returns>
    public byte[] HashPassword(string password) {
      // BCrypt workfactor - higher values make hashing slower but more secure
      // Values between 10-12 are good for most applications
      int workFactor = 12;

      // BCrypt automatically generates and incorporates a unique salt
      // This produces a 60-character string including salt, cost parameter, and hash
      string bcryptHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor);

      // Convert to binary for storage
      return Encoding.UTF8.GetBytes(bcryptHash);
    }

    /// <summary>
    /// Verifies a password against a stored BCrypt hash
    /// </summary>
    /// <param name="password">The plain text password to verify</param>
    /// <param name="storedHash">The stored hash from the database</param>
    /// <returns>True if password matches, false otherwise</returns>
    private bool VerifyPassword(string password, byte[] storedHash) {
      // Convert binary hash back to string format required by BCrypt
      string hashString = Encoding.UTF8.GetString(storedHash);

      // BCrypt verify - this handles all the salt extraction and comparison
      return BCrypt.Net.BCrypt.Verify(password, hashString);
    }

    /// <summary>
    /// Authenticates user by retrieving the stored hash and verifying the password
    /// </summary>
    /// <param name="email">User's email</param>
    /// <param name="password">Plain text password to verify</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="deviceFingerprint">Device fingerprint</param>
    /// <returns>GUID if authenticated, null otherwise</returns>
    public Guid? AuthenticateUser(string email, string password, string ipAddress, string deviceFingerprint) {
      // Step 1: Retrieve the stored hash for the given email
      Utils.LogDebug("calling GetStoredHash()");
      byte[] storedHash = GetStoredHash(email);

      // If no hash found, authentication fails
      if (storedHash == null) {
        return null;
      }
      Utils.LogDebug("calling VerifyPassword()");
      // Step 2: Verify the password against the retrieved hash
      bool isVerified = VerifyPassword(password, storedHash);

      // If verification fails, authentication fails
      if (!isVerified) {
        Utils.LogDebug("AuthenticateUser() verify hash failed.");
        return null;
      }

      // Step 3: Password verified, create a new session
      Utils.LogDebug("AuthenticateUser(): verified. calling CreateUserSession()");
      return CreateUserSession(email, ipAddress, deviceFingerprint);
    } // AuthenticateUser

    /// <summary>
    /// Retrieves the stored password hash for a user by email
    /// </summary>
    /// <param name="email">User's email</param>
    /// <returns>Stored hash as byte array, or null if user not found</returns>
    private byte[] GetStoredHash(string email) {
      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_get_user_password_hash", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          command.Parameters.Add("@email", SqlDbType.VarChar, 254).Value = email;
          Utils.LogDebug("GetStoredHash() email: " + email);

          try {
            connection.Open();

            // Execute and get result
            object result = command.ExecuteScalar();

            if (result != null && result != DBNull.Value) {
              Utils.LogDebug("GetStoredHash() got hash. ");
              return (byte[])result;
            }
            Utils.LogDebug("GetStoredHash() returning null");
            return null;
          }
          catch (Exception ex) {
            Utils.LogDebug("GetStoredHash() exception: " + ex.Message);
            return null;
          }
        }
      }
    }

    /// <summary>
    /// Creates a new session for the user
    /// </summary>
    /// <param name="email">User's email</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="deviceFingerprint">Device fingerprint</param>
    /// <returns>Session GUID if successful, null otherwise</returns>
    private Guid? CreateUserSession(string email, string ipAddress, string deviceFingerprint) {
      Utils.LogDebug("CreateUserSession(): starting new session");
      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_add_user_session", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          command.Parameters.Add("@email", SqlDbType.VarChar, 254).Value = email;
          command.Parameters.Add("@ipAddress", SqlDbType.VarChar, 50).Value = ipAddress;
          command.Parameters.Add("@deviceFingerprint", SqlDbType.VarChar, 255).Value = deviceFingerprint;

          // Add output parameter for the session GUID
          SqlParameter outputParameter = new SqlParameter("@sessionId", SqlDbType.UniqueIdentifier);
          outputParameter.Direction = ParameterDirection.Output;
          command.Parameters.Add(outputParameter);

          try {
            connection.Open();
            command.ExecuteNonQuery();

            // Check if we got a valid GUID back
            if (outputParameter.Value != null && outputParameter.Value != DBNull.Value) {
              Utils.LogDebug("  CreateUserSession(): session GUID" + outputParameter.Value.ToString());
              return (Guid)outputParameter.Value;
            }

            Utils.LogDebug("  CreateUserSession(): NO session GUID returned.");
            return null;
          }
          catch (Exception) {
            // Log error here if needed
            Utils.LogDebug("  CreateUserSession(): EXCEPTION.");
            return null;
          }
        }
      }
    }

    /// <summary>
    /// Validates an existing session and extends it if valid
    /// </summary>
    /// <param name="sessionId">The session GUID to validate</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="deviceFingerprint">Device fingerprint</param>
    /// <returns>Session GUID if valid, null otherwise</returns>
    public Guid? ValidateAndExtendSession(Guid sessionId, string ipAddress, string deviceFingerprint) {
      Utils.LogDebug("ValidateAndExtendSession(): " + sessionId.ToString());
      // Get the current HttpRequest from the context
      HttpRequest request = HttpContext.Current.Request;

      // Check if ipAddress is empty and get it from GetClientIPAddress if needed
      if (string.IsNullOrEmpty(ipAddress)) {
        ipAddress = GetClientIPAddress(request);
      }

      // Check if deviceFingerprint is empty and get it from GenerateDeviceFingerprint if needed
      if (string.IsNullOrEmpty(deviceFingerprint)) {
        deviceFingerprint = GenerateDeviceFingerprint(request);
      }

      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_validate_and_extend_session", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          command.Parameters.Add("@sessionId", SqlDbType.UniqueIdentifier).Value = sessionId;
          command.Parameters.Add("@ipAddress", SqlDbType.VarChar, 50).Value = ipAddress;
          command.Parameters.Add("@deviceFingerprint", SqlDbType.VarChar, 255).Value = deviceFingerprint;

          // Output parameter to indicate if session is valid
          SqlParameter validParameter = new SqlParameter("@isSessionValid", SqlDbType.Bit);
          validParameter.Direction = ParameterDirection.Output;
          command.Parameters.Add(validParameter);

          try {
            connection.Open();
            command.ExecuteNonQuery();
            Utils.LogDebug("ValidateAndExtendSession: try: is session valid?" + (validParameter.Value).ToString());

            // Check if the session is valid
            if (validParameter.Value != null && validParameter.Value != DBNull.Value && (bool)validParameter.Value) {
              // Session is valid and has been extended
              return sessionId;
            }

            // Session is invalid or expired
            return null;
          }
          catch (Exception) {
            Utils.LogDebug("ValidateAndExtendSession: catch");
            // Log error here if needed
            return null;
          }
        }
      }
    }

    /// <summary>
    /// Gets the application cookie data as a dictionary
    /// </summary>
    /// <param name="request">The HTTP request</param>
    /// <returns>Dictionary of cookie values or empty dictionary if cookie doesn't exist</returns>
    public Dictionary<string, string> GetCookieData(HttpRequest request) {
      Dictionary<string, string> cookieData = new Dictionary<string, string>();

      if (request != null && request.Cookies[AppConstants.SESSION_COOKIE_NAME] != null) {
        string cookieValue = request.Cookies[AppConstants.SESSION_COOKIE_NAME].Value;
        if (!string.IsNullOrEmpty(cookieValue)) {
          try {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            cookieData = serializer.Deserialize<Dictionary<string, string>>(
                HttpUtility.UrlDecode(cookieValue));
          }
          catch {
            // If deserialization fails, return empty dictionary
            cookieData = new Dictionary<string, string>();
          }
        }
      }

      return cookieData;
    }

    /// <summary>
    /// Sets a value in the application cookie
    /// </summary>
    /// <param name="response">The HTTP response</param>
    /// <param name="request">The HTTP request</param>
    /// <param name="key">Key name</param>
    /// <param name="value">Value to store</param>
    /// <param name="isSecure">Whether the cookie should be marked as secure (HTTPS only)</param>
    public void SetCookieValue(HttpResponse response, HttpRequest request, string key, string value, bool isSecure = true) {
      // Get existing cookie data
      Dictionary<string, string> cookieData = GetCookieData(request);

      // Update or add the key-value pair
      if (cookieData.ContainsKey(key))
        cookieData[key] = value;
      else
        cookieData.Add(key, value);

      // Serialize and save the cookie
      Utils.LogDebug("SetCookieValue(): Calling SaveCookie(): key: " + key + ". value: " + value);
      SaveCookie(response, cookieData, isSecure);
    }

    /// <summary>
    /// Gets a value from the application cookie
    /// </summary>
    /// <param name="request">The HTTP request</param>
    /// <param name="key">Key name</param>
    /// <returns>Value or null if key doesn't exist</returns>
    public string GetCookieValue(HttpRequest request, string key) {
      Dictionary<string, string> cookieData = GetCookieData(request);

      string value = null;
      if (cookieData.ContainsKey(key))
        value = cookieData[key];
      // For debugging
      Utils.LogDebug("GetCookieValue(" + key + "): " + value);

      return value;
    }

    /// <summary>
    /// Removes a value from the application cookie
    /// </summary>
    /// <param name="response">The HTTP response</param>
    /// <param name="request">The HTTP request</param>
    /// <param name="key">Key to remove</param>
    /// <param name="isSecure">Whether the cookie should be marked as secure (HTTPS only)</param>
    public void RemoveCookieValue(HttpResponse response, HttpRequest request, string key, bool isSecure = true) {
      Utils.LogDebug("RemoveCookie()");
      Dictionary<string, string> cookieData = GetCookieData(request);
      if (cookieData.ContainsKey(key)) {
        Utils.LogDebug("Remove cookie value: " + key);
        cookieData.Remove(key);
        SaveCookie(response, cookieData, isSecure);
      }
    }

    /// <summary>
    /// Clears all cookie data
    /// </summary>
    /// <param name="response">The HTTP response</param>
    public void ClearCookie(HttpResponse response) {
      Utils.LogDebug("ClearCookie()");
      HttpCookie cookie = new HttpCookie(AppConstants.SESSION_COOKIE_NAME);
      cookie.Expires = DateTime.Now.AddDays(-1); // Expire the cookie
      cookie.HttpOnly = true;
      cookie.Secure = true;
      response.Cookies.Add(cookie);
    }

    /// <summary>
    /// Serializes and saves the cookie data
    /// </summary>
    /// <param name="response">The HTTP response</param>
    /// <param name="cookieData">Dictionary of values to save</param>
    /// <param name="isSecure">Whether the cookie should be marked as secure (HTTPS only)</param>
    private void SaveCookie(HttpResponse response, Dictionary<string, string> cookieData, bool isSecure) {
      Utils.LogDebug("SaveCookie(): begin");
      JavaScriptSerializer serializer = new JavaScriptSerializer();
      string cookieValue = HttpUtility.UrlEncode(serializer.Serialize(cookieData));
      Utils.LogDebug("SaveCookie(): cookiedValue: " + cookieValue);
      HttpCookie cookie = new HttpCookie(AppConstants.SESSION_COOKIE_NAME);
      cookie.Value = cookieValue;
      cookie.HttpOnly = true;
      cookie.Secure = isSecure;
      // No expiration date set = session cookie, will be deleted when browser closes            
      response.Cookies.Add(cookie);
    }

    /// <summary>
    /// Stores the current URL as a return URL in the cookie
    /// </summary>
    /// <param name="request">The HTTP request</param>
    /// <param name="response">The HTTP response</param>
    /// <param name="loginPageUrl">The login page URL to avoid storing it as a return URL</param>
    public void SaveCurrentUrl(HttpRequest request, HttpResponse response, string loginPageUrl) {
      return;
      //// Don't save the login page as a return URL
      //if (request.Url.AbsolutePath.ToLower().Contains(loginPageUrl.ToLower()))
      //    return;

      //// Get the full URL of the current request
      //string returnUrl = request.Url.AbsolutePath;

      //// Add query string parameters if present
      //if (!string.IsNullOrEmpty(request.QueryString.ToString())) {
      //    returnUrl += "?" + request.QueryString.ToString();
      //}

      //// For debugging - add to log or temporary visible element
      //utils.LogDebug("Saving returnUrl: " + returnUrl);

      //// Store the return URL in our single cookie
      //SetCookieValue(response, request, RETURN_URL_KEY, returnUrl);
    }

    /// <summary>
    /// Redirects to the login page
    /// </summary>
    /// <param name="response">The HTTP response object</param>
    /// <param name="server">The HTTP server object</param>
    private void RedirectToLogin(HttpResponse response, HttpServerUtility server, HttpRequest request) {
      if (response != null) {
        // Clear the session values, but keep return URL
        string returnUrl = GetCookieValue(request: null, AppConstants.RETURN_URL_KEY);
        ClearCookie(response);

        // If we have a return URL, add it back to cookie
        if (!string.IsNullOrEmpty(returnUrl)) {
          Dictionary<string, string> cookieData = new Dictionary<string, string>();
          cookieData.Add(AppConstants.RETURN_URL_KEY, returnUrl);
          SaveCookie(response, cookieData, true);
        }

        if ((request.Url.AbsoluteUri).Contains("/login")) {
          HttpContext.Current.Items["SkipLoginAuthentication"] = true;
        }

        // Redirect to login page
        if (server != null) {
          try {
            Utils.LogDebug("RedirectToLogin: server != null, transfer to: " + _loginPageUrl);
            server.Transfer(_loginPageUrl);
          }
          catch {
            // If Transfer fails (e.g., page not found), use Redirect
            Utils.LogDebug("RedirectToLogin: server != null, CATCH redirect to: " + _loginPageUrl);
            response.Redirect(_loginPageUrl);
          }
        }
        else {
          Utils.LogDebug("RedirectToLogin: server != null ELSE, CATCH redirect to: " + _loginPageUrl);
          response.Redirect(_loginPageUrl);
        }
      }
    }

    /// <summary>
    /// Generates a device fingerprint from various browser and device characteristics
    /// </summary>
    /// <param name="request">The HTTP request object</param>
    /// <returns>A string representing the device fingerprint</returns>
    public string GenerateDeviceFingerprint(HttpRequest request) {
      Utils.LogDebug("In GenerateDeviceFingerprint");
      if (request == null)
        return "unknown";

      StringBuilder fingerprint = new StringBuilder();

      // Add user agent
      string userAgent = request.UserAgent ?? "unknown";
      fingerprint.Append(userAgent);

      // Add accepted languages
      string acceptLanguage = request.Headers["Accept-Language"] ?? "unknown";
      fingerprint.Append("|").Append(acceptLanguage);

      // Add screen info from cookies if available
      string screenInfo = "unknown";
      string screenCookieValue = GetCookieValue(request, "screen_info");
      if (!string.IsNullOrEmpty(screenCookieValue)) {
        screenInfo = screenCookieValue;
      }
      else if (request.Cookies["screen_info"] != null) {
        screenInfo = request.Cookies["screen_info"].Value;
      }
      fingerprint.Append("|").Append(screenInfo);

      // Add timezone offset from cookies if available
      string timezoneOffset = "unknown";
      string timezoneCookieValue = GetCookieValue(request, "timezone_offset");
      if (!string.IsNullOrEmpty(timezoneCookieValue)) {
        timezoneOffset = timezoneCookieValue;
      }
      else if (request.Cookies["timezone_offset"] != null) {
        timezoneOffset = request.Cookies["timezone_offset"].Value;
      }
      fingerprint.Append("|").Append(timezoneOffset);

      // Add platform info
      string platform = request.Browser?.Platform ?? "unknown";
      fingerprint.Append("|").Append(platform);

      // Add browser capabilities
      string browserCapabilities = request.Browser?.Type ?? "unknown";
      fingerprint.Append("|").Append(browserCapabilities);

      // Generate a hash of all the collected information
      using (SHA256 sha256 = SHA256.Create()) {
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fingerprint.ToString()));

        // Convert to hex string
        StringBuilder hashString = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++) {
          hashString.Append(hashBytes[i].ToString("x2"));
        }
        Utils.LogDebug("Last line in GenerateDeviceFingerprint: hashString" + hashString.ToString());
        return hashString.ToString();
      }
    }

    /// <summary>
    /// Gets the client's IP address from the request
    /// </summary>
    /// <param name="request">The HTTP request object</param>
    /// <returns>The client's IP address</returns>
    public string GetClientIPAddress(HttpRequest request) {
      string ipAddress = request.ServerVariables["HTTP_X_FORWARDED_FOR"];

      if (string.IsNullOrEmpty(ipAddress)) {
        ipAddress = request.ServerVariables["REMOTE_ADDR"];
      }
      else {
        // If we have a comma-separated list of IP addresses, take the first one
        string[] addresses = ipAddress.Split(',');
        if (addresses.Length > 0) {
          ipAddress = addresses[0].Trim();
        }
      }
      return ipAddress ?? "unknown";
    }

    /// <summary>
    /// Validates if the provided string is a properly formatted email address
    /// Validate email before sending to the database. Database does very simple validation only.
    /// </summary>
    /// <param name="email">Email address to validate</param>
    /// <returns>True if valid email format, false otherwise</returns>
    private bool IsValidEmail(string email) {
      if (string.IsNullOrWhiteSpace(email))
        return false;
      try {
        // Use .NET's built-in validation
        var addr = new System.Net.Mail.MailAddress(email);
        return addr.Address == email;
      }
      catch {
        return false;
      }
    }

    /// <summary>
    /// Validates password meets strength requirements
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>True if password meets all requirements, false otherwise</returns>
    public bool ValidatePasswordStrength(string password) {
      if (string.IsNullOrEmpty(password) || password.Length < 8) {
        return false;
      }

      // Check for at least one lowercase letter
      if (!Regex.IsMatch(password, @"[a-z]")) {
        return false;
      }

      // Check for at least one uppercase letter
      if (!Regex.IsMatch(password, @"[A-Z]")) {
        return false;
      }

      // Check for at least one number
      if (!Regex.IsMatch(password, @"[0-9]")) {
        return false;
      }

      // Check for at least one special character
      if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]")) {
        return false;
      }

      return true;
    }
  }
}