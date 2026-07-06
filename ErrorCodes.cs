namespace NotebookTracker {
  /// <summary>
  /// Defines error codes for the API to use in error responses and logging
  /// </summary>
  public static class ErrorCodes {
    // Database access errors (10000-10999)
    public const int DB_GENERAL_ERROR = 10001;
    public const int DB_CONNECTION_ERROR = 10002;
    public const int DB_QUERY_ERROR = 10003;

    // Role-related errors (11000-11999)
    public const int ROLE_RETRIEVE_ERROR = 11001;
    public const int ROLE_NOT_FOUND = 11002;

    // RedirectionType-related errors (12000-12999)
    public const int REDIRECTION_RETRIEVE_ERROR = 12001;
    public const int REDIRECTION_NOT_FOUND = 12002;

    // Authentication/Authorization errors (13000-13999)
    public const int AUTH_INVALID_TOKEN = 13001;
    public const int AUTH_EXPIRED_TOKEN = 13002;
    public const int AUTH_INSUFFICIENT_PERMISSIONS = 13003;

    // Validation errors (14000-14999)
    public const int VALIDATION_INVALID_INPUT = 14001;
    public const int VALIDATION_MISSING_REQUIRED_FIELD = 14002;

    // Server errors (15000-15999)
    public const int SERVER_GENERAL_ERROR = 15001;
    public const int SERVER_CONFIGURATION_ERROR = 15002;

    // Dashboard-related errors (16000-16999)
    public const int DASHBOARD_RETRIEVE_ERROR = 16001;

    // User-related errors (17000-17999)
    public const int USER_REGISTRATION_ERROR = 17001;
  }
}
