using System;
using System.Configuration;

namespace NotebookTracker {
  /// <summary>
  /// Contains application-wide constants and settings
  /// </summary>
  public static class AppConstants {

    public static readonly bool DEBUG_MODE = false;

    // Session cookie name from Web.config or default value if not found
    public static readonly string SESSION_COOKIE_NAME =
        ConfigurationManager.AppSettings["SessionCookieName"] ?? "NotebookTracker";

    public static readonly string SESSION_ID_KEY = ConfigurationManager.AppSettings["SessionId"] ?? "SessionId";
    public static readonly string RETURN_URL_KEY = ConfigurationManager.AppSettings["ReturnUrl"] ?? "ReturnUrl";

    // Session timeout in minutes from Web.config or default value if not found
    public static readonly int SESSION_TIMEOUT_MINUTES =
        Convert.ToInt32(ConfigurationManager.AppSettings["SessionTimeoutMinutes"] ?? "5");

    // Database connection string name
    public static readonly string DEFAULT_CONNECTION_STRING = "DefaultConnection";

    // Login page URL
    public static readonly string LOGIN_PAGE_URL = "/login.html";

    // Common URLs
    public static readonly string HOME_PAGE_URL = "/";
    public static readonly string ERROR_PAGE_URL = "/error.aspx";
  } // AppConstants
}
