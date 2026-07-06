using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace NotebookTracker {
  /// <summary>
  /// Data access class for URL shortener application.
  /// This class should be compiled into a separate DLL.
  /// </summary>
  public class DatabaseAccess {
    private readonly string _connectionString;

    /// <summary>
    /// Constructor that retrieves connection string from web.config
    /// </summary>
    public DatabaseAccess() {
      _connectionString = ConfigurationManager.ConnectionStrings[AppConstants.DEFAULT_CONNECTION_STRING].ConnectionString;
    }

    /// <summary>
    /// Constructor with explicit connection string for testing
    /// </summary>
    /// <param name="connectionString">Database connection string</param>
    public DatabaseAccess(string connectionString) {
      _connectionString = connectionString;
    }

    /// <summary>
    /// Adds a new user to the database using p_add_user stored procedure
    /// </summary>
    /// <param name="userData">The user data to add</param>
    /// <returns>True if the operation was successful, otherwise false</returns>
    public SqlResult AddUser(UserData userData) {
      try {
        using (SqlConnection connection = new SqlConnection(_connectionString)) {
          using (SqlCommand command = new SqlCommand("p_add_user", connection)) {
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@email", userData.Email);
            command.Parameters.AddWithValue("@firstName", userData.FirstName);
            command.Parameters.AddWithValue("@lastName", userData.LastName);
            command.Parameters.AddWithValue("@roleId", userData.RoleId);
            command.Parameters.AddWithValue("@teamNo", userData.TeamNo);
            command.Parameters.AddWithValue("@password", userData.Password);
            if (string.IsNullOrEmpty(userData.Phone)) {
              command.Parameters.AddWithValue("@phone", DBNull.Value);
            }
            else {
              command.Parameters.AddWithValue("@phone", userData.Phone);
            }
            SqlParameter successParam = new SqlParameter("@returnStatus", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            command.Parameters.Add(successParam);
            SqlParameter messageParam = new SqlParameter("@message", SqlDbType.VarChar, 1024) { Direction = ParameterDirection.Output };
            command.Parameters.Add(messageParam);

            connection.Open();
            command.ExecuteNonQuery();
            return new SqlResult {
              returnStatus = Convert.ToBoolean(successParam.Value),
              databaseMessage = Convert.ToString(messageParam.Value)
            };
          }
        }
      }
      catch (Exception ex) {
        Utils.LogDebug("p_add_user return status: " + ex.Message);
        return new SqlResult {
          returnStatus = false,
          databaseMessage = ex.Message
        };
      }
    } // AddUser

    public string GetRedirectionTypes(byte? redirectionTypeId = null) {
      string redirectionTypesHtml = string.Empty;
      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_list_html_redirectionTypes", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          if (redirectionTypeId.HasValue) {
            command.Parameters.AddWithValue("@redirectionTypeId", redirectionTypeId.Value);
          }
          SqlParameter outputParameter = new SqlParameter("@redirectionTypesHtml", SqlDbType.VarChar, -1);
          outputParameter.Direction = ParameterDirection.Output;
          command.Parameters.Add(outputParameter);
          connection.Open();
          command.ExecuteNonQuery();
          redirectionTypesHtml = (outputParameter.Value == DBNull.Value) ? string.Empty : (string)outputParameter.Value;
        }
      }
      return redirectionTypesHtml;
    } // GetRedirectionTypes

    public string GetTeams(Guid sessionId, Int32? teamId = null) {
      string teamsHtml = string.Empty;
      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_list_html_teams", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          command.Parameters.Add("@sessionId", SqlDbType.UniqueIdentifier).Value = sessionId;
          if (teamId.HasValue) {
            command.Parameters.AddWithValue("@teamId", teamId.Value);
          }
          SqlParameter outputParameter = new SqlParameter("@teamsHtml", SqlDbType.VarChar, -1);
          outputParameter.Direction = ParameterDirection.Output;
          command.Parameters.Add(outputParameter);
          connection.Open();
          command.ExecuteNonQuery();
          teamsHtml = (outputParameter.Value == DBNull.Value) ? string.Empty : (string)outputParameter.Value;
        }
      }
      return teamsHtml;
    } // GetTeams

    /// <summary>
    /// Checks if a short URL already exists in the database
    /// </summary>
    /// <param name="shortUrl">Short URL to check</param>
    /// <returns>True if short URL exists, otherwise false</returns>
    public bool CheckIfShortUrlExists(string shortUrl) {
      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_is_shortUrl_registered", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          command.Parameters.AddWithValue("@ShortUrl", shortUrl);
          SqlParameter existsParam = new SqlParameter("@isAlreadyRegistered", SqlDbType.Bit) { Direction = ParameterDirection.Output };
          command.Parameters.Add(existsParam);
          connection.Open();
          command.ExecuteNonQuery();
          return Convert.ToBoolean(existsParam.Value);
        }
      }
    } // CheckIfShortUrlExists

    /// <summary>
    /// Returns target URL for the given short URL
    /// </summary>
    /// <param name="shortUrl">Short URL</param>
    /// <returns>Target URL and redirection type</returns>
    public UrlRedirectInfo GetTargetUrl(string shortUrl) {
      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_get_target_url", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          command.Parameters.AddWithValue("@shortUrl", shortUrl);
          SqlParameter targetUrl = new SqlParameter("@targetUrl", SqlDbType.VarChar, 1024) { Direction = ParameterDirection.Output };
          SqlParameter redirectionType = new SqlParameter("@redirectionType", SqlDbType.TinyInt) { Direction = ParameterDirection.Output };
          SqlParameter preventMobileView = new SqlParameter("@preventMobileView", SqlDbType.Bit) { Direction = ParameterDirection.Output };
          SqlParameter isExpired = new SqlParameter("@isExpired", SqlDbType.Bit) { Direction = ParameterDirection.Output };
          SqlParameter isDisabled = new SqlParameter("@isDisabled", SqlDbType.Bit) { Direction = ParameterDirection.Output };
          command.Parameters.Add(targetUrl);
          command.Parameters.Add(redirectionType);
          command.Parameters.Add(preventMobileView);
          command.Parameters.Add(isExpired);
          command.Parameters.Add(isDisabled);
          connection.Open();
          command.ExecuteNonQuery();
          return new UrlRedirectInfo {
            targetUrl = Convert.ToString(targetUrl.Value),
            redirectionType = Convert.ToByte(redirectionType.Value),
            preventMobileView = Convert.ToBoolean(preventMobileView.Value),
            isExpired = Convert.ToBoolean(isExpired.Value),
            isDisabled = Convert.ToBoolean(isDisabled.Value)
          };
        }
      }
    } // UrlRedirectInfo

    //public string GetTargetUrl(string shortUrl) {
    //  using (SqlConnection connection = new SqlConnection(_connectionString)) {
    //    using (SqlCommand command = new SqlCommand("p_get_target_url", connection)) {
    //      command.CommandType = CommandType.StoredProcedure;
    //      command.Parameters.AddWithValue("@shortUrl", shortUrl);
    //      SqlParameter targetUrl = new SqlParameter("@targetUrl", SqlDbType.VarChar, 1024) { Direction = ParameterDirection.Output };
    //      command.Parameters.Add(targetUrl);
    //      connection.Open();
    //      command.ExecuteNonQuery();
    //      return Convert.ToString(targetUrl.Value);
    //    }
    //  }
    //} // GetTargetUrl

    /// <summary>
    /// Saves a URL record to the database
    /// </summary>
    /// <param name="record">URL record to save</param>
    /// <returns>True if successful, otherwise false</returns>
    // Step 2: Modify the SaveUrlRecord method to return this class
    public SqlResult SaveUrlRecord(Guid sessionId, UrlRecord record) {
      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_add_url", connection)) {
          command.CommandType = CommandType.StoredProcedure;

          // Add parameters
          command.Parameters.Add("@sessionId", SqlDbType.UniqueIdentifier).Value = sessionId;
          command.Parameters.AddWithValue("@teamId", record.TeamId);
          command.Parameters.AddWithValue("@shortUrl", record.ShortUrl);
          command.Parameters.AddWithValue("@targetUrl", record.TargetUrl);
          command.Parameters.AddWithValue("@startDate", record.StartDate);
          command.Parameters.AddWithValue("@endDate", record.EndDate);
          command.Parameters.AddWithValue("@redirectionTypeId", record.RedirectionTypeId);
          command.Parameters.AddWithValue("@preventViewingOnMobile", record.PreventViewingOnMobile);

          // Output parameter to indicate success
          SqlParameter successParam = new SqlParameter("@returnStatus", SqlDbType.Bit) { Direction = ParameterDirection.Output };
          command.Parameters.Add(successParam);
          SqlParameter messageParam = new SqlParameter("@message", SqlDbType.VarChar, 2048) { Direction = ParameterDirection.Output };
          command.Parameters.Add(messageParam);

          try {
            connection.Open();
            command.ExecuteNonQuery();
            return new SqlResult {
              returnStatus = Convert.ToBoolean(successParam.Value),
              databaseMessage = Convert.ToString(messageParam.Value)
            };
          }
          catch (Exception ex) {
            // Log error - in a production environment, use proper logging
            Utils.LogError($"Error {ErrorCodes.DB_QUERY_ERROR}: Error in DatabaseAccess.SaveUrlRecord: {ex.ToString()}");
            return new SqlResult {
              returnStatus = false,
              databaseMessage = ex.Message
            };
          }
        }
      }
    } // SaveUrlRecord

    /// <summary>
    /// Executes the p_list_urls stored procedure and returns the results as a DataTable
    /// </summary>
    /// <param name="sessionId">The user's session ID</param>
    /// <returns>DataTable containing URL information or null if an error occurs</returns>
    public DataTable GetUrls(Guid sessionId) {
      Utils.LogDebug("GetUrls: sessionId: " + sessionId.ToString());
      DataTable dt = new DataTable();
      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_list_urls", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          command.Parameters.AddWithValue("@sessionId", sessionId);
          try {
            connection.Open();
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            adapter.Fill(dt);
          }
          catch (Exception ex) {
            Utils.LogDebug("Database error: GetUrls: " + ex.Message);
            return null;
          }
        }
      }
      return dt;
    } // GetUrls

    /// <summary>
    /// Executes the p_delete_url stored procedure to delete a URL
    /// </summary>
    /// <param name="sessionId">The user's session ID</param>
    /// <param name="urlId">The ID of the URL to delete</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool DeleteUrl(Guid sessionId, int urlId) {
      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_delete_url", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          command.Parameters.AddWithValue("@sessionId", sessionId);
          command.Parameters.AddWithValue("@urlId", urlId);
          SqlParameter returnParameter = command.Parameters.Add("@returnStatus", SqlDbType.Int);
          returnParameter.Direction = ParameterDirection.ReturnValue;
          try {
            connection.Open();
            command.ExecuteNonQuery();
            int result = (int)returnParameter.Value;
            Utils.LogDebug("p_delete_url return status: " + result.ToString());
            return result == 0;
          }
          catch (Exception ex) {
            Utils.LogDebug("Database error: DeleteUrl: " + ex.Message);
            return false;
          }
        }
      }
    } // DeleteUrl

    /// <summary>
    /// Store visitor information in the database using a stored procedure
    /// </summary>
    public void SaveVisitorInfoInDatabase(string RequestedURL, string ReferringURL, string IPAddress, string Hostname, string Browser, string BrowserVersion, string DeviceType, string os) {
      try {
        using (SqlConnection connection = new SqlConnection(_connectionString)) {
          connection.Open();
          // Create command to call stored procedure
          using (SqlCommand command = new SqlCommand("p_add_urlLog", connection)) {
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@RequestedURL", RequestedURL);
            command.Parameters.AddWithValue("@referringURL", ReferringURL);
            command.Parameters.AddWithValue("@ipAddress", IPAddress);
            command.Parameters.AddWithValue("@hostname", Hostname);
            command.Parameters.AddWithValue("@browserName", Browser);
            command.Parameters.AddWithValue("@browserVersion", BrowserVersion);
            command.Parameters.AddWithValue("@DeviceType", DeviceType);
            command.Parameters.AddWithValue("@OperatingSystem", os);
            command.ExecuteNonQuery();
          }
        }
      }
      catch (Exception ex) {
        Utils.LogDebug("p_add_urlLog: " + ex.Message);
      }
    } // SaveVisitorInfoInDatabase

    /// <summary>
    /// delete session cookie in the database.
    /// </summary>
    public void DeleteCookie(Guid sessionId) {
      try {
        using (SqlConnection connection = new SqlConnection(_connectionString)) {
          connection.Open();
          using (SqlCommand command = new SqlCommand("p_delete_cookie", connection)) {
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@sessionId", sessionId);
            command.ExecuteNonQuery();
          }
        }
      }
      catch (Exception ex) {
        Utils.LogDebug("p_delete_cookie: " + ex.Message);
      }
    } // DeleteCookie

    /// <summary>
    /// Gets all redirection types from the database
    /// </summary>
    /// <returns>List of redirection types</returns>
    public DataTable GetRedirectionTypesAPI() {
      DataTable dataTable = new DataTable();

      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        SqlCommand command = new SqlCommand("p_api_get_redirectionTypes", connection);
        command.CommandType = CommandType.StoredProcedure;

        try {
          connection.Open();
          SqlDataAdapter adapter = new SqlDataAdapter(command);
          adapter.Fill(dataTable);
        }
        catch (Exception ex) {
          // Log the exception using the existing LogDebug method with error code
          Utils.LogError(string.Format("Error {0}: Error in DatabaseAccess.GetAllRoles: {1}", ErrorCodes.DB_QUERY_ERROR, ex.ToString()));

          // Rethrow with more context while preserving the original exception
          throw new Exception("Error retrieving roles from database", ex);
        }
        finally {
          if (connection.State == ConnectionState.Open)
            connection.Close();
        }
      }
      return dataTable;
    } // GetAllRedirectionTypesAPI

    /// <summary>
    /// Gets all roles from the database
    /// </summary>
    /// <returns>DataTable containing role information</returns>
    public DataTable GetRolesAPI() {
      DataTable dataTable = new DataTable();

      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        SqlCommand command = new SqlCommand("p_api_get_roles", connection);
        command.CommandType = CommandType.StoredProcedure;

        try {
          connection.Open();
          SqlDataAdapter adapter = new SqlDataAdapter(command);
          adapter.Fill(dataTable);
        }
        catch (Exception ex) {
          // Log the exception using the existing LogDebug method with error code
          Utils.LogError(string.Format("Error {0}: Error in DatabaseAccess.GetAllRoles: {1}", ErrorCodes.DB_QUERY_ERROR, ex.ToString()));

          // Rethrow with more context while preserving the original exception
          throw new Exception("Error retrieving roles from database", ex);
        }
        finally {
          if (connection.State == ConnectionState.Open)
            connection.Close();
        }
      }
      return dataTable;
    } // GetRolesAPI

    // <summary>
    /// Gets a URL record by its ID
    /// UPDATED: Now requires sessionId parameter for authorization
    /// </summary>
    /// <param name="sessionId">The user's session ID</param>
    /// <param name="urlId">The URL ID to retrieve</param>
    /// <returns>DataTable containing URL information</returns>
    public DataTable GetUrlAPI(string sessionId, int urlId) {
      if (!Guid.TryParse(sessionId, out Guid sessionGuid)) {
        Utils.LogDebug("GetUrlAPI: Invalid session ID format");
        return new DataTable();
      }

      DataTable dataTable = new DataTable();
      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        SqlCommand command = new SqlCommand("p_api_get_url", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add("@sessionId", SqlDbType.UniqueIdentifier).Value = sessionGuid;  // Added sessionId
        command.Parameters.AddWithValue("@urlId", urlId);
        try {
          connection.Open();
          SqlDataAdapter adapter = new SqlDataAdapter(command);
          adapter.Fill(dataTable);
        }
        catch (Exception ex) {
          Utils.LogDebug($"Error {ErrorCodes.DB_QUERY_ERROR}: Error in DatabaseAccess.GetUrlAPI: {ex.ToString()}");
          throw new Exception("Error retrieving URL from database", ex);
        }
        finally {
          if (connection.State == ConnectionState.Open)
            connection.Close();
        }
      }
      return dataTable;
    }

    /// <summary>
    /// Gets view logs for a specific URL ID
    /// UPDATED: Now requires sessionId parameter for authorization
    /// </summary>
    /// <param name="sessionId">The user's session ID</param>
    /// <param name="urlId">The URL ID to get logs for</param>
    /// <returns>DataTable containing URL view log records for specific URL</returns>
    public DataTable GetUrlViewLogAPI(string sessionId, int urlId) {
      if (!Guid.TryParse(sessionId, out Guid sessionGuid)) {
        Utils.LogDebug("GetUrlViewLogAPI: Invalid session ID format");
        return new DataTable();
      }

      DataTable dataTable = new DataTable();
      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        SqlCommand command = new SqlCommand("p_api_get_url_logs", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add("@sessionId", SqlDbType.UniqueIdentifier).Value = sessionGuid;  // Added sessionId
        command.Parameters.AddWithValue("@urlId", urlId);

        try {
          connection.Open();
          SqlDataAdapter adapter = new SqlDataAdapter(command);
          adapter.Fill(dataTable);
        }
        catch (Exception ex) {
          Utils.LogError(string.Format("Error {0}: Error in DatabaseAccess.GetUrlViewLogAPI: {1}", ErrorCodes.DASHBOARD_RETRIEVE_ERROR, ex.ToString()));
          throw new Exception("Error retrieving URL view logs from database", ex);
        }
        finally {
          if (connection.State == ConnectionState.Open)
            connection.Close();
        }
      }
      return dataTable;
    }

    /// <summary>
    /// Gets full details of a specific log entry by log ID
    /// ✅ UPDATED: Now requires sessionId parameter for authorization
    /// </summary>
    /// <param name="sessionId">The user's session ID</param>
    /// <param name="logId">The log entry ID</param>
    /// <returns>DataTable containing full log entry details</returns>
    public DataTable GetUrlViewLogEntryAPI(string sessionId, int logId) {
      if (!Guid.TryParse(sessionId, out Guid sessionGuid)) {
        Utils.LogDebug("GetUrlViewLogEntryAPI: Invalid session ID format");
        return new DataTable();
      }

      DataTable dataTable = new DataTable();
      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        SqlCommand command = new SqlCommand("p_api_get_url_log", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add("@sessionId", SqlDbType.UniqueIdentifier).Value = sessionGuid;  // ✅ Added sessionId
        command.Parameters.AddWithValue("@logId", logId);

        try {
          connection.Open();
          SqlDataAdapter adapter = new SqlDataAdapter(command);
          adapter.Fill(dataTable);
        }
        catch (Exception ex) {
          Utils.LogError(string.Format("Error {0}: Error in DatabaseAccess.GetUrlViewLogEntryAPI: {1}", ErrorCodes.DASHBOARD_RETRIEVE_ERROR, ex.ToString()));
          throw new Exception("Error retrieving log entry from database", ex);
        }
        finally {
          if (connection.State == ConnectionState.Open)
            connection.Close();
        }
      }
      return dataTable;
    }

    /// <summary>
    /// Executes the p_api_get_urls stored procedure and returns the results as a DataTable
    /// UPDATED: Now requires sessionId parameter for authorization
    /// </summary>
    /// <param name="sessionId">The user's session ID</param>
    /// <returns>DataTable containing URL information or empty table if error occurs</returns>
    public DataTable GetUrlsAPI(string sessionId) {
      if (!Guid.TryParse(sessionId, out Guid sessionGuid)) {
        Utils.LogDebug("GetUrlsAPI: Invalid session ID format");
        return new DataTable();
      }

      Utils.LogDebug("GetUrlsAPI: sessionId: " + sessionGuid.ToString());
      DataTable dt = new DataTable();
      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_api_get_urls", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          command.Parameters.Add("@sessionId", SqlDbType.UniqueIdentifier).Value = sessionGuid;  // Added sessionId
          try {
            connection.Open();
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            adapter.Fill(dt);
          }
          catch (Exception ex) {
            Utils.LogDebug("Database error: GetUrlsAPI: " + ex.Message);
            return new DataTable();
          }
        }
      }
      return dt;
    }

    // <summary>
    /// Deletes a URL record from the database using p_api_delete_url stored procedure
    /// UPDATED: Now requires sessionId parameter for authorization
    /// </summary>
    /// <param name="sessionId">The user's session ID</param>
    /// <param name="urlId">The URL ID to delete</param>
    /// <returns>SqlResult indicating success/failure and message</returns>
    public SqlResult DeleteUrlAPI(string sessionId, int urlId) {
      if (!Guid.TryParse(sessionId, out Guid sessionGuid)) {
        return new SqlResult {
          returnStatus = false,
          databaseMessage = "Invalid session ID format"
        };
      }

      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_api_delete_url", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          command.Parameters.Add("@sessionId", SqlDbType.UniqueIdentifier).Value = sessionGuid;  // Added sessionId
          command.Parameters.AddWithValue("@urlId", urlId);

          SqlParameter successParam = new SqlParameter("@returnStatus", SqlDbType.Bit) { Direction = ParameterDirection.Output };
          command.Parameters.Add(successParam);
          SqlParameter messageParam = new SqlParameter("@message", SqlDbType.VarChar, 1024) { Direction = ParameterDirection.Output };
          command.Parameters.Add(messageParam);

          try {
            connection.Open();
            command.ExecuteNonQuery();
            return new SqlResult {
              returnStatus = Convert.ToBoolean(successParam.Value),
              databaseMessage = Convert.ToString(messageParam.Value)
            };
          }
          catch (Exception ex) {
            Utils.LogError($"Error {ErrorCodes.DB_QUERY_ERROR}: Error in DatabaseAccess.DeleteUrlAPI: {ex.ToString()}");
            return new SqlResult {
              returnStatus = false,
              databaseMessage = ex.Message
            };
          }
        }
      }
    }

    /// <summary>
    /// Adds a new URL record to the database using p_api_add_url stored procedure
    /// UPDATED: Now requires sessionId parameter for authorization
    /// </summary>
    /// <param name="sessionId">The user's session ID</param>
    /// <param name="record">The URL record to add</param>
    /// <returns>SqlResult indicating success/failure and message</returns>
    public SqlResult AddUrlAPI(string sessionId, UrlRecord record) {
      if (!Guid.TryParse(sessionId, out Guid sessionGuid)) {
        return new SqlResult {
          returnStatus = false,
          databaseMessage = "Invalid session ID format"
        };
      }

      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_api_add_url", connection)) {
          command.CommandType = CommandType.StoredProcedure;

          // Add parameters
          command.Parameters.Add("@sessionId", SqlDbType.UniqueIdentifier).Value = sessionGuid;  // Added sessionId
          command.Parameters.AddWithValue("@teamId", record.TeamId);
          command.Parameters.AddWithValue("@shortUrl", record.ShortUrl);
          command.Parameters.AddWithValue("@targetUrl", record.TargetUrl);
          command.Parameters.AddWithValue("@startDate", record.StartDate);
          command.Parameters.AddWithValue("@endDate", record.EndDate);
          command.Parameters.AddWithValue("@redirectionTypeId", record.RedirectionTypeId);
          command.Parameters.AddWithValue("@preventViewingOnMobile", record.PreventViewingOnMobile);

          // Output parameters
          SqlParameter successParam = new SqlParameter("@returnStatus", SqlDbType.Bit) { Direction = ParameterDirection.Output };
          command.Parameters.Add(successParam);
          SqlParameter messageParam = new SqlParameter("@message", SqlDbType.VarChar, 2048) { Direction = ParameterDirection.Output };
          command.Parameters.Add(messageParam);

          try {
            connection.Open();
            command.ExecuteNonQuery();
            return new SqlResult {
              returnStatus = Convert.ToBoolean(successParam.Value),
              databaseMessage = Convert.ToString(messageParam.Value)
            };
          }
          catch (Exception ex) {
            Utils.LogError($"Error {ErrorCodes.DB_QUERY_ERROR}: Error in DatabaseAccess.AddUrlAPI: {ex.ToString()}");
            return new SqlResult {
              returnStatus = false,
              databaseMessage = ex.Message
            };
          }
        }
      }
    }

    /// <summary>
    /// Gets teams for a specific user session from the database
    /// </summary>
    /// <param name="sessionId">The session ID from JWT token</param>
    /// <returns>DataTable containing team information for the user</returns>
    public DataTable GetTeamAPI(Guid sessionId) {
      DataTable dataTable = new DataTable();

      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        SqlCommand command = new SqlCommand("p_api_get_teams", connection);
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add("@sessionId", SqlDbType.UniqueIdentifier).Value = sessionId;
        try {
          connection.Open();
          SqlDataAdapter adapter = new SqlDataAdapter(command);
          adapter.Fill(dataTable);
        }
        catch (Exception ex) {
          // Log the exception using the existing LogError method with error code
          Utils.LogError(string.Format("Error {0}: Error in DatabaseAccess.GetTeamAPI: {1}", ErrorCodes.DB_QUERY_ERROR, ex.ToString()));

          // Rethrow with more context while preserving the original exception
          throw new Exception("Error retrieving teams from database", ex);
        }
        finally {
          if (connection.State == ConnectionState.Open)
            connection.Close();
        }
      }
      return dataTable;
    } // GetTeamAPI

    /// <summary>
    /// Checks if a short URL is available (not already taken)
    /// </summary>
    /// <param name="shortUrl">Short URL to check availability for</param>
    /// <returns>True if available (not taken), false if already exists</returns>
    public bool CheckShortUrlAvailabilityAPI(string shortUrl) {
      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_api_check_shorturl_availability", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          command.Parameters.AddWithValue("@shortUrl", shortUrl);
          SqlParameter availableParam = new SqlParameter("@isAvailable", SqlDbType.Bit) { Direction = ParameterDirection.Output };
          command.Parameters.Add(availableParam);

          try {
            connection.Open();
            command.ExecuteNonQuery();
            return Convert.ToBoolean(availableParam.Value);
          }
          catch (Exception ex) {
            Utils.LogError($"Error {ErrorCodes.DB_QUERY_ERROR}: Error in DatabaseAccess.CheckShortUrlAvailabilityAPI: {ex.ToString()}");
            // If there's an error, assume it's not available (safer approach)
            return false;
          }
        }
      }
    } // CheckShortUrlAvailabilityAPI

    /// <summary>
    /// Checks if an email address is available (not already registered)
    /// </summary>
    /// <param name="email">Email address to check</param>
    /// <returns>True if available, false if already registered</returns>
    public bool CheckEmailAvailabilityAPI(string email) {
      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_api_check_email_availability", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          command.Parameters.AddWithValue("@email", email.ToLower().Trim());

          SqlParameter availableParam = new SqlParameter("@isAvailable", SqlDbType.Bit) { Direction = ParameterDirection.Output };
          command.Parameters.Add(availableParam);

          try {
            connection.Open();
            command.ExecuteNonQuery();
            return Convert.ToBoolean(availableParam.Value);
          }
          catch (Exception ex) {
            Utils.LogError($"Error {ErrorCodes.DB_QUERY_ERROR}: Error in DatabaseAccess.CheckEmailAvailability: {ex.ToString()}");
            // If there's an error, assume email is not available (safer approach)
            return false;
          }
        }
      }
    } // CheckEmailAvailabilityAPI

    /// <summary>
    /// Adds a new mobile user to the database with device information
    /// </summary>
    /// <param name="userData">User data to add</param>
    /// <param name="deviceId">Mobile device ID</param>
    /// <param name="appInstanceId">App instance ID</param>
    /// <param name="ipAddress">User's IP address</param>
    /// <param name="deviceInfo">Device information JSON</param>
    /// <returns>MobileUserResult with success status and user ID</returns>
    public MobileUserResult AddMobileUserAPI(MobileUserData userData, string deviceId, string appInstanceId, string ipAddress, string deviceInfo) {
      try {
        using (SqlConnection connection = new SqlConnection(_connectionString)) {
          using (SqlCommand command = new SqlCommand("p_add_user", connection)) {
            command.CommandType = CommandType.StoredProcedure;

            // User data parameters
            command.Parameters.AddWithValue("@email", userData.Email);
            command.Parameters.AddWithValue("@firstName", userData.FirstName);
            command.Parameters.AddWithValue("@lastName", userData.LastName);
            command.Parameters.AddWithValue("@roleId", userData.RoleId);
            command.Parameters.AddWithValue("@teamNoWithLetter", userData.TeamNoWithLetter);
            command.Parameters.AddWithValue("@password", userData.Password);

            if (string.IsNullOrEmpty(userData.Phone)) {
              command.Parameters.AddWithValue("@phone", DBNull.Value);
            }
            else {
              command.Parameters.AddWithValue("@phone", userData.Phone);
            }

            // Mobile-specific parameters
            command.Parameters.AddWithValue("@deviceId", deviceId);
            command.Parameters.AddWithValue("@appInstanceId", Guid.Parse(appInstanceId));
            command.Parameters.AddWithValue("@ipAddress", ipAddress);
            command.Parameters.AddWithValue("@deviceInfo", deviceInfo);

            // Output parameters
            SqlParameter successParam = new SqlParameter("@returnStatus", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            SqlParameter messageParam = new SqlParameter("@message", SqlDbType.VarChar, 1024) { Direction = ParameterDirection.Output };
            SqlParameter userIdParam = new SqlParameter("@userId", SqlDbType.Int) { Direction = ParameterDirection.Output };

            command.Parameters.Add(successParam);
            command.Parameters.Add(messageParam);
            command.Parameters.Add(userIdParam);

            connection.Open();
            command.ExecuteNonQuery();

            return new MobileUserResult {
              returnStatus = Convert.ToBoolean(successParam.Value),
              databaseMessage = Convert.ToString(messageParam.Value),
              userId = successParam.Value != DBNull.Value && Convert.ToBoolean(successParam.Value) ?
                        Convert.ToInt32(userIdParam.Value) : 0
            };
          }
        }
      }
      catch (Exception ex) {
        Utils.LogError($"Error {ErrorCodes.DB_QUERY_ERROR}: Error in DatabaseAccess.AddMobileUser: {ex.ToString()}");
        return new MobileUserResult {
          returnStatus = false,
          databaseMessage = ex.Message,
          userId = 0
        };
      }
    } // AddMobileUserAPI

    /// <summary>
    /// Deletes a user profile and all associated data
    /// </summary>
    /// <param name="sessionId">The user's session ID</param>
    /// <param name="confirmationText">The confirmation text ("DELETE")</param>
    /// <param name="deviceId">Device ID for additional validation</param>
    /// <param name="reason">Optional reason for deletion</param>
    /// <returns>DeleteProfileResult indicating success/failure and message</returns>
    public DeleteProfileResult DeleteUserProfileAPI(string sessionId, string confirmationText, string deviceId) {
      if (!Guid.TryParse(sessionId, out Guid sessionGuid)) {
        return new DeleteProfileResult {
          returnStatus = false,
          databaseMessage = "Invalid session ID format",
          deletedAt = null
        };
      }

      // Validate confirmation text
      if (string.IsNullOrEmpty(confirmationText) || confirmationText.Trim().ToUpper() != "DELETE") {
        return new DeleteProfileResult {
          returnStatus = false,
          databaseMessage = "Invalid confirmation text. Please type 'DELETE' to confirm.",
          deletedAt = null
        };
      }

      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_api_delete_user_profile", connection)) {
          command.CommandType = CommandType.StoredProcedure;

          command.Parameters.Add("@sessionId", SqlDbType.UniqueIdentifier).Value = sessionGuid;
          command.Parameters.Add("@confirmationText", SqlDbType.VarChar, 10).Value = confirmationText.Trim().ToUpper();
          command.Parameters.Add("@deviceId", SqlDbType.VarChar, 50).Value = deviceId ?? string.Empty;
          SqlParameter successParam = new SqlParameter("@returnStatus", SqlDbType.Bit) { Direction = ParameterDirection.Output };
          SqlParameter messageParam = new SqlParameter("@message", SqlDbType.VarChar, 1024) { Direction = ParameterDirection.Output };
          SqlParameter deletedAtParam = new SqlParameter("@deletedAt", SqlDbType.DateTime) { Direction = ParameterDirection.Output };
          command.Parameters.Add(successParam);
          command.Parameters.Add(messageParam);
          command.Parameters.Add(deletedAtParam);

          try {
            connection.Open();
            command.ExecuteNonQuery();

            var success = Convert.ToBoolean(successParam.Value);
            var message = Convert.ToString(messageParam.Value) ?? string.Empty;
            var deletedAt = deletedAtParam.Value != DBNull.Value ? Convert.ToDateTime(deletedAtParam.Value) : (DateTime?)null;

            Utils.LogDebug($"Delete profile result for session {sessionId}: Success={success}, Message={message}");

            return new DeleteProfileResult {
              returnStatus = success,
              databaseMessage = message,
              deletedAt = deletedAt
            };
          }
          catch (Exception ex) {
            var errorMessage = string.Format("Error {0}: Error in DatabaseAccess.DeleteUserProfile: {1}",
              ErrorCodes.DB_QUERY_ERROR, ex.ToString());
            Utils.LogError(errorMessage);
            return new DeleteProfileResult {
              returnStatus = false,
              databaseMessage = "An error occurred while deleting the profile. Please try again.",
              deletedAt = null
            };
          }
        }
      }
    } // DeleteUserProfileAPI

    /// <summary>
    /// Updates a user's profile information
    /// </summary>
    /// <param name="sessionId">The user's session ID</param>
    /// <param name="email">New email address</param>
    /// <param name="firstName">New first name</param>
    /// <param name="lastName">New last name</param>
    /// <param name="teamNoWithLetter">New team number with letter</param>
    /// <param name="phone">New phone number (can be null)</param>
    /// <param name="newPasswordHash">New password hash (can be null if not changing password)</param>
    /// <param name="deviceId">Device ID for additional validation</param>
    /// <returns>SqlResult indicating success/failure and message</returns>
    public SqlResult UpdateUserProfileAPI(string sessionId, string email, string firstName, string lastName,
      string teamNoWithLetter, string phone, byte[] newPasswordHash, string deviceId, byte roleId) {

      if (!Guid.TryParse(sessionId, out Guid sessionGuid)) {
        return new SqlResult {
          returnStatus = false,
          databaseMessage = "Invalid session ID format"
        };
      }

      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_api_update_user_profile", connection)) {
          command.CommandType = CommandType.StoredProcedure;

          // Input parameters
          command.Parameters.Add("@sessionId", SqlDbType.UniqueIdentifier).Value = sessionGuid;
          command.Parameters.Add("@email", SqlDbType.VarChar, 254).Value = email.ToLower().Trim();
          command.Parameters.Add("@firstName", SqlDbType.VarChar, 128).Value = firstName.Trim();
          command.Parameters.Add("@lastName", SqlDbType.VarChar, 128).Value = lastName.Trim();
          command.Parameters.Add("@teamNoWithLetter", SqlDbType.VarChar, 10).Value = teamNoWithLetter.Trim().ToUpper();
          command.Parameters.Add("@roleId", SqlDbType.TinyInt).Value = roleId;
          command.Parameters.Add("@deviceId", SqlDbType.VarChar, 50).Value = deviceId ?? string.Empty;

          // Optional phone parameter
          if (string.IsNullOrEmpty(phone)) {
            command.Parameters.Add("@phone", SqlDbType.VarChar, 20).Value = DBNull.Value;
          }
          else {
            command.Parameters.Add("@phone", SqlDbType.VarChar, 20).Value = phone.Trim();
          }

          // Optional password parameter
          if (newPasswordHash == null) {
            command.Parameters.Add("@newPasswordHash", SqlDbType.VarBinary, 255).Value = DBNull.Value;
          }
          else {
            command.Parameters.Add("@newPasswordHash", SqlDbType.VarBinary, 255).Value = newPasswordHash;
          }

          // Output parameters
          SqlParameter successParam = new SqlParameter("@returnStatus", SqlDbType.Bit) { Direction = ParameterDirection.Output };
          SqlParameter messageParam = new SqlParameter("@message", SqlDbType.VarChar, 1024) { Direction = ParameterDirection.Output };
          command.Parameters.Add(successParam);
          command.Parameters.Add(messageParam);

          try {
            connection.Open();
            command.ExecuteNonQuery();

            var success = Convert.ToBoolean(successParam.Value);
            var message = Convert.ToString(messageParam.Value) ?? string.Empty;

            Utils.LogDebug($"Update profile result for session {sessionId}: Success={success}, Message={message}");

            return new SqlResult {
              returnStatus = success,
              databaseMessage = message
            };
          }
          catch (Exception ex) {
            var errorMessage = string.Format("Error {0}: Error in DatabaseAccess.UpdateUserProfile: {1}",
              ErrorCodes.DB_QUERY_ERROR, ex.ToString());
            Utils.LogError(errorMessage);
            return new SqlResult {
              returnStatus = false,
              databaseMessage = "An error occurred while updating the profile. Please try again."
            };
          }
        }
      }
    } // UpdateUserProfileAPI

    /// <summary>
    /// Validates a URL creation request
    /// </summary>
    /// <param name="record">The URL record to validate</param>
    /// <returns>Validation result with success status and message</returns>
    public ValidationResult ValidateUrlRecord(UrlRecord record) {
    // Validate targetUrl
    if (string.IsNullOrEmpty(record.TargetUrl)) {
      return new ValidationResult {
        IsValid = false,
        Message = "Target URL is required"
      };
    }

      // Must start with https:// only
      if (!record.TargetUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
        return new ValidationResult {
          IsValid = false,
          Message = "Target URL must start with https://"
        };
      }

      // Check if it's just "https://" without anything after
      if (record.TargetUrl.Equals("https://", StringComparison.OrdinalIgnoreCase)) {
        return new ValidationResult {
          IsValid = false,
          Message = "Please enter a complete URL"
        };
      }

      // Extract the part after "https://"
      string urlAfterProtocol = record.TargetUrl.Substring(8);

      // Check if there's a domain (must contain at least one dot)
      if (string.IsNullOrWhiteSpace(urlAfterProtocol) || !urlAfterProtocol.Contains(".")) {
        return new ValidationResult {
          IsValid = false,
          Message = "Invalid domain format"
        };
      }

      // Split by '/' to separate domain from path
      string[] parts = urlAfterProtocol.Split(new[] { '/' }, 2);
      string path = parts.Length > 1 ? parts[1] : string.Empty;

      // Check if it's just a domain with trailing slash or no path
      if (string.IsNullOrWhiteSpace(path)) {
        return new ValidationResult {
          IsValid = false,
          Message = "Please enter a complete document URL (e.g., https://example.com/document or https://example.com/file?id=123)"
        };
      }

      if (record.TargetUrl.Contains("vexteams.org")) {
      return new ValidationResult {
        IsValid = false,
        Message = "Target URL cannot contain vexteams.org"
      };
    }

      // Validate dates
      if (record.StartDate <= DateTime.UtcNow.AddDays(-1)) {
        return new ValidationResult {
        IsValid = false,
        Message = "Start date must be in the future"
      };
    }

    if (record.EndDate <= record.StartDate) {
      return new ValidationResult {
        IsValid = false,
        Message = "End date must be after start date"
      };
    }

    // Validate shortUrl
    if (string.IsNullOrEmpty(record.ShortUrl)) {
      return new ValidationResult {
        IsValid = false,
        Message = "Short URL is required"
      };
    }

    if (!Regex.IsMatch(record.ShortUrl, @"^[a-zA-Z0-9\-_.]+$")) {
      return new ValidationResult {
        IsValid = false,
        Message = "Short URL can only contain letters, numbers, hyphens, underscores, and periods"
      };
    }

    return new ValidationResult {
      IsValid = true,
      Message = "Validation successful"
    };
  } // ValidateUrlRecord

    /// <summary>
    /// Creates a new mobile session for a user
    /// UPDATED: Now returns real GUID instead of "All good"
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="appInstanceId">App instance identifier</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="deviceInfo">Device information JSON</param>
    /// <param name="refreshToken">Refresh token</param>
    /// <returns>DatabaseResult with real session ID in message</returns>
    public DatabaseResult CreateMobileSession(int userId, string deviceId, string appInstanceId, string ipAddress, string deviceInfo, string refreshToken, string deviceFingerprint) {
      try {
        using (SqlConnection connection = new SqlConnection(_connectionString)) {
          using (SqlCommand command = new SqlCommand("p_api_create_mobile_session", connection)) {
            command.CommandType = CommandType.StoredProcedure;

            // Input parameters
            command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
            command.Parameters.Add("@deviceId", SqlDbType.VarChar, 50).Value = deviceId;
            command.Parameters.Add("@appInstanceId", SqlDbType.UniqueIdentifier).Value = Guid.Parse(appInstanceId);
            command.Parameters.Add("@ipAddress", SqlDbType.VarChar, 39).Value = ipAddress;
            command.Parameters.Add("@deviceInfo", SqlDbType.VarChar, -1).Value = deviceInfo;
            command.Parameters.Add("@refreshToken", SqlDbType.VarChar, 200).Value = refreshToken;
            command.Parameters.Add("@deviceFingerprint", SqlDbType.VarChar, 64).Value = deviceFingerprint ?? string.Empty; // Add this


            // Output parameters
            command.Parameters.Add("@sessionId", SqlDbType.UniqueIdentifier).Direction = ParameterDirection.Output;
            command.Parameters.Add("@returnStatus", SqlDbType.Bit).Direction = ParameterDirection.Output;
            command.Parameters.Add("@message", SqlDbType.VarChar, 255).Direction = ParameterDirection.Output;

            connection.Open();
            command.ExecuteNonQuery();

            return new DatabaseResult {
              returnStatus = (bool)command.Parameters["@returnStatus"].Value,
              databaseMessage = command.Parameters["@sessionId"].Value.ToString(), 
              message = command.Parameters["@message"].Value.ToString()
            };
          }
        }
      }
      catch (Exception ex) {
        var errorMessage = string.Format("Error {0}: Error in DatabaseAccess.CreateMobileSession: {1}",
          ErrorCodes.DB_QUERY_ERROR, ex.ToString());
        Utils.LogError(errorMessage);
        return new DatabaseResult {
          returnStatus = false,
          databaseMessage = "Session creation failed"
        };
      }
    }

    // <summary>
    /// Validates and updates session activity
    /// UPDATED: Now accepts uniqueidentifier for sessionId
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="deviceId">Device ID</param>
    /// <param name="ipAddress">Current IP address</param>
    /// <param name="userAgent">User agent string</param>
    /// <returns>SessionValidationResult with user info and session status</returns>
    public SessionValidationResult ValidateAndUpdateSession(string sessionId, string deviceId, string ipAddress, string userAgent) {
      try {
        // Convert string sessionId to Guid
        if (!Guid.TryParse(sessionId, out Guid sessionGuid)) {
          return new SessionValidationResult {
            IsValid = false,
            Message = "Invalid session ID format"
          };
        }

        using (SqlConnection connection = new SqlConnection(_connectionString)) {
          using (SqlCommand command = new SqlCommand("p_api_validate_session", connection)) {
            command.CommandType = CommandType.StoredProcedure;

            // Use Guid parameter
            command.Parameters.Add("@sessionId", SqlDbType.UniqueIdentifier).Value = sessionGuid;
            command.Parameters.Add("@deviceId", SqlDbType.VarChar, 255).Value = deviceId ?? "";
            command.Parameters.Add("@ipAddress", SqlDbType.VarChar, 45).Value = ipAddress ?? "";
            command.Parameters.Add("@userAgent", SqlDbType.VarChar, 255).Value = userAgent ?? "";

            // Output parameters
            command.Parameters.Add("@isValid", SqlDbType.Bit).Direction = ParameterDirection.Output;
            command.Parameters.Add("@userId", SqlDbType.Int).Direction = ParameterDirection.Output;
            command.Parameters.Add("@email", SqlDbType.VarChar, 254).Direction = ParameterDirection.Output;
            command.Parameters.Add("@TeamNoWithLetter", SqlDbType.VarChar, 10).Direction = ParameterDirection.Output;
            command.Parameters.Add("@firstName", SqlDbType.VarChar, 128).Direction = ParameterDirection.Output;
            command.Parameters.Add("@lastName", SqlDbType.VarChar, 128).Direction = ParameterDirection.Output;
            command.Parameters.Add("@roleId", SqlDbType.TinyInt).Direction = ParameterDirection.Output;
            command.Parameters.Add("@message", SqlDbType.VarChar, 255).Direction = ParameterDirection.Output;

            connection.Open();
            command.ExecuteNonQuery();

            return new SessionValidationResult {
              IsValid = (bool)command.Parameters["@isValid"].Value,
              UserId = (int)command.Parameters["@userId"].Value,
              Email = command.Parameters["@email"].Value.ToString(),
              TeamNoWithLetter = command.Parameters["@TeamNoWithLetter"].Value.ToString(),
              FirstName = command.Parameters["@firstName"].Value.ToString(),
              LastName = command.Parameters["@lastName"].Value.ToString(),
              RoleId = Convert.ToByte(command.Parameters["@roleId"].Value ?? 2),
              Message = command.Parameters["@message"].Value.ToString()
            };
          }
        }
      }
      catch (Exception ex) {
        var errorMessage = string.Format("Error {0}: Error in DatabaseAccess.ValidateAndUpdateSession: {1}",
          ErrorCodes.DB_QUERY_ERROR, ex.ToString());
        Utils.LogError(errorMessage);
        return new SessionValidationResult {
          IsValid = false,
          Message = "Session validation failed"
        };
      }
    }

    /// <summary>
    /// Terminates a mobile session
    /// UPDATED: Now accepts uniqueidentifier for sessionId
    /// </summary>
    /// <param name="sessionId">Session ID to terminate</param>
    /// <param name="deviceId">Device ID for additional validation</param>
    /// <returns>DatabaseResult indicating success/failure</returns>
    public DatabaseResult TerminateMobileSession(string sessionId, string deviceId) {
      if (!Guid.TryParse(sessionId, out Guid sessionGuid)) {
        return new DatabaseResult {
          returnStatus = false,
          databaseMessage = "Invalid session ID format"
        };
      }

      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_api_terminate_mobile_session", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          command.Parameters.Add("@sessionId", SqlDbType.UniqueIdentifier).Value = sessionGuid;  // Use Guid
          command.Parameters.Add("@deviceId", SqlDbType.VarChar, 50).Value = deviceId;

          SqlParameter successParam = new SqlParameter("@returnStatus", SqlDbType.Bit) { Direction = ParameterDirection.Output };
          SqlParameter messageParam = new SqlParameter("@message", SqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
          command.Parameters.Add(successParam);
          command.Parameters.Add(messageParam);

          try {
            connection.Open();
            command.ExecuteNonQuery();
            return new DatabaseResult {
              returnStatus = (bool)successParam.Value,
              databaseMessage = messageParam.Value.ToString()
            };
          }
          catch (Exception ex) {
            var errorMessage = string.Format("Error {0}: Error in DatabaseAccess.TerminateMobileSession: {1}",
              ErrorCodes.DB_QUERY_ERROR, ex.ToString());
            Utils.LogError(errorMessage);
            return new DatabaseResult {
              returnStatus = false,
              databaseMessage = ex.Message
            };
          }
        }
      }
    }

    /// <summary>
    /// Validates refresh token and returns user information
    /// ✅ UPDATED: Now accepts uniqueidentifier for sessionId
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="refreshToken">Refresh token to validate</param>
    /// <returns>RefreshTokenValidationResult with user info and token age</returns>
    public RefreshTokenValidationResult ValidateRefreshToken(string sessionId, string refreshToken) {
      if (!Guid.TryParse(sessionId, out Guid sessionGuid)) {
        return new RefreshTokenValidationResult {
          IsValid = false,
          Message = "Invalid session ID format"
        };
      }

      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_api_validate_refresh_token", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          command.Parameters.Add("@sessionId", SqlDbType.UniqueIdentifier).Value = sessionGuid;  // ✅ Use Guid
          command.Parameters.AddWithValue("@refreshToken", refreshToken);

          SqlParameter isValidParam = new SqlParameter("@isValid", SqlDbType.Bit) { Direction = ParameterDirection.Output };
          SqlParameter userIdParam = new SqlParameter("@userId", SqlDbType.Int) { Direction = ParameterDirection.Output };
          SqlParameter emailParam = new SqlParameter("@email", SqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
          SqlParameter teamNumberParam = new SqlParameter("@TeamNoWithLetter", SqlDbType.VarChar, 10) { Direction = ParameterDirection.Output };
          SqlParameter firstNameParam = new SqlParameter("@firstName", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
          SqlParameter lastNameParam = new SqlParameter("@lastName", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
          SqlParameter tokenAgeParam = new SqlParameter("@tokenAgeMinutes", SqlDbType.Int) { Direction = ParameterDirection.Output };
          SqlParameter messageParam = new SqlParameter("@message", SqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };

          command.Parameters.Add(isValidParam);
          command.Parameters.Add(userIdParam);
          command.Parameters.Add(emailParam);
          command.Parameters.Add(teamNumberParam);
          command.Parameters.Add(firstNameParam);
          command.Parameters.Add(lastNameParam);
          command.Parameters.Add(tokenAgeParam);
          command.Parameters.Add(messageParam);

          try {
            connection.Open();
            command.ExecuteNonQuery();

            var isValid = Convert.ToBoolean(isValidParam.Value);
            var result = new RefreshTokenValidationResult {
              IsValid = isValid,
              Message = Convert.ToString(messageParam.Value) ?? string.Empty
            };

            if (isValid) {
              result.AuthResult = new AuthenticationResult {
                IsValid = true,
                UserId = Convert.ToInt32(userIdParam.Value),
                Email = Convert.ToString(emailParam.Value) ?? string.Empty,
                TeamNoWithLetter = Convert.ToString(teamNumberParam.Value) ?? string.Empty,
                FirstName = Convert.ToString(firstNameParam.Value) ?? string.Empty,
                LastName = Convert.ToString(lastNameParam.Value) ?? string.Empty
              };
              result.TokenAge = TimeSpan.FromMinutes(Convert.ToInt32(tokenAgeParam.Value));
            }

            return result;
          }
          catch (Exception ex) {
            var errorMessage = string.Format("Error {0}: Error in DatabaseAccess.ValidateRefreshToken: {1}",
              ErrorCodes.DB_QUERY_ERROR, ex.ToString());
            Utils.LogError(errorMessage);
            return new RefreshTokenValidationResult {
              IsValid = false,
              Message = "Refresh token validation failed"
            };
          }
        }
      }
    }

    // <summary>
    /// Updates refresh token for a session
    /// ✅ UPDATED: Now accepts uniqueidentifier for sessionId
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="newRefreshToken">New refresh token</param>
    /// <returns>SqlResult indicating success/failure</returns>
    public SqlResult UpdateRefreshToken(string sessionId, string newRefreshToken) {
      if (!Guid.TryParse(sessionId, out Guid sessionGuid)) {
        return new SqlResult {
          returnStatus = false,
          databaseMessage = "Invalid session ID format"
        };
      }

      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_api_update_refresh_token", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          command.Parameters.Add("@sessionId", SqlDbType.UniqueIdentifier).Value = sessionGuid;  // ✅ Use Guid
          command.Parameters.AddWithValue("@newRefreshToken", newRefreshToken);

          SqlParameter successParam = new SqlParameter("@returnStatus", SqlDbType.Bit) { Direction = ParameterDirection.Output };
          SqlParameter messageParam = new SqlParameter("@message", SqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
          command.Parameters.Add(successParam);
          command.Parameters.Add(messageParam);

          try {
            connection.Open();
            command.ExecuteNonQuery();
            return new SqlResult {
              returnStatus = Convert.ToBoolean(successParam.Value),
              databaseMessage = Convert.ToString(messageParam.Value)
            };
          }
          catch (Exception ex) {
            var errorMessage = string.Format("Error {0}: Error in DatabaseAccess.UpdateRefreshToken: {1}",
              ErrorCodes.DB_QUERY_ERROR, ex.ToString());
            Utils.LogError(errorMessage);
            return new SqlResult {
              returnStatus = false,
              databaseMessage = ex.Message
            };
          }
        }
      }
    }

    /// <summary>
    /// Retrieves the stored password hash for a user by email
    /// </summary>
    /// <param name="email">User's email</param>
    /// <returns>Stored hash as byte array, or null if user not found</returns>
    public byte[] GetUserPasswordHash(string email) {
      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_get_user_password_hash", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          command.Parameters.AddWithValue("@email", email);

          try {
            connection.Open();
            object result = command.ExecuteScalar();

            if (result != null && result != DBNull.Value) {
              return (byte[])result;
            }
            return null;
          }
          catch (Exception ex) {
            Utils.LogError($"Error {ErrorCodes.DB_QUERY_ERROR}: Error in DatabaseAccess.GetUserPasswordHash: {ex.ToString()}");
            return null;
          }
        }
      }
    } // GetUserPasswordHash

    /// <summary>
    /// Generates and stores a password reset token for a user
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <returns>SqlResult with reset token in databaseMessage if successful</returns>
    public SqlResult CreatePasswordResetToken(string email) {
      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_api_create_password_reset_token", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          command.Parameters.AddWithValue("@email", email);

          SqlParameter successParam = new SqlParameter("@returnStatus", SqlDbType.Bit) { Direction = ParameterDirection.Output };
          SqlParameter messageParam = new SqlParameter("@message", SqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
          SqlParameter resetTokenParam = new SqlParameter("@resetToken", SqlDbType.VarChar, 100) { Direction = ParameterDirection.Output };

          command.Parameters.Add(successParam);
          command.Parameters.Add(messageParam);
          command.Parameters.Add(resetTokenParam);

          try {
            connection.Open();
            command.ExecuteNonQuery();
            return new SqlResult {
              returnStatus = Convert.ToBoolean(successParam.Value),
              databaseMessage = Convert.ToString(resetTokenParam.Value) ?? Convert.ToString(messageParam.Value)
            };
          }
          catch (Exception ex) {
            Utils.LogError(string.Format("Error {0}: Error in DatabaseAccess.CreatePasswordResetToken: {1}",
                ErrorCodes.DB_QUERY_ERROR, ex.ToString()));
            return new SqlResult {
              returnStatus = false,
              databaseMessage = "Failed to create password reset token"
            };
          }
        }
      }
    }

    /// <summary>
    /// Validates a password reset token
    /// </summary>
    /// <param name="resetToken">Reset token to validate</param>
    /// <returns>SqlResult with user email in databaseMessage if valid</returns>
    public SqlResult ValidatePasswordResetToken(string resetToken) {
      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_api_validate_password_reset_token", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          command.Parameters.AddWithValue("@resetToken", resetToken);

          SqlParameter isValidParam = new SqlParameter("@isValid", SqlDbType.Bit) { Direction = ParameterDirection.Output };
          SqlParameter emailParam = new SqlParameter("@email", SqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
          SqlParameter messageParam = new SqlParameter("@message", SqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };

          command.Parameters.Add(isValidParam);
          command.Parameters.Add(emailParam);
          command.Parameters.Add(messageParam);

          try {
            connection.Open();
            command.ExecuteNonQuery();
            return new SqlResult {
              returnStatus = Convert.ToBoolean(isValidParam.Value),
              databaseMessage = Convert.ToString(emailParam.Value) ?? Convert.ToString(messageParam.Value)
            };
          }
          catch (Exception ex) {
            Utils.LogError(string.Format("Error {0}: Error in DatabaseAccess.ValidatePasswordResetToken: {1}",
                ErrorCodes.DB_QUERY_ERROR, ex.ToString()));
            return new SqlResult {
              returnStatus = false,
              databaseMessage = "Token validation failed"
            };
          }
        }
      }
    }

    /// <summary>
    /// Gets user details by email (called AFTER password verification)
    /// </summary>
    /// <param name="email">User email</param>
    /// <returns>AuthenticationResult with user info</returns>
    public AuthenticationResult GetUserByEmail(string email) {
      using (SqlConnection connection = new SqlConnection(_connectionString)) {
        using (SqlCommand command = new SqlCommand("p_api_get_user_by_email", connection)) {
          command.CommandType = CommandType.StoredProcedure;
          command.Parameters.AddWithValue("@email", email);

          SqlParameter isValidParam = new SqlParameter("@isValid", SqlDbType.Bit) { Direction = ParameterDirection.Output };
          SqlParameter userIdParam = new SqlParameter("@userId", SqlDbType.Int) { Direction = ParameterDirection.Output };
          SqlParameter teamNumberParam = new SqlParameter("@TeamNoWithLetter", SqlDbType.VarChar, 10) { Direction = ParameterDirection.Output };
          SqlParameter firstNameParam = new SqlParameter("@firstName", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
          SqlParameter lastNameParam = new SqlParameter("@lastName", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
          SqlParameter phoneParam = new SqlParameter("@phone", SqlDbType.VarChar, 17) { Direction = ParameterDirection.Output };
          SqlParameter roleIdParam = new SqlParameter("@roleId", SqlDbType.TinyInt) { Direction = ParameterDirection.Output };
          SqlParameter messageParam = new SqlParameter("@message", SqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };

          command.Parameters.Add(isValidParam);
          command.Parameters.Add(userIdParam);
          command.Parameters.Add(teamNumberParam);
          command.Parameters.Add(firstNameParam);
          command.Parameters.Add(lastNameParam);
          command.Parameters.Add(phoneParam);
          command.Parameters.Add(roleIdParam);
          command.Parameters.Add(messageParam);

          try {
            connection.Open();
            command.ExecuteNonQuery();
            // After command.ExecuteNonQuery();
            var roleIdValue = roleIdParam.Value;
            Utils.LogDebug($"Database returned roleId: {roleIdValue} (type: {roleIdValue?.GetType()})");

            var result = new AuthenticationResult {
              IsValid = Convert.ToBoolean(isValidParam.Value),
              UserId = isValidParam.Value != DBNull.Value && Convert.ToBoolean(isValidParam.Value) ? Convert.ToInt32(userIdParam.Value) : 0,
              Email = email,
              TeamNoWithLetter = Convert.ToString(teamNumberParam.Value) ?? string.Empty,
              FirstName = Convert.ToString(firstNameParam.Value) ?? string.Empty,
              LastName = Convert.ToString(lastNameParam.Value) ?? string.Empty,
              Phone = Convert.ToString(phoneParam.Value) ?? string.Empty,
              RoleId = roleIdParam.Value != DBNull.Value ? Convert.ToByte(roleIdParam.Value) : (byte)0,
              Message = Convert.ToString(messageParam.Value) ?? string.Empty
            };
            Utils.LogDebug($"AuthenticationResult created - RoleId: {result.RoleId}, Email: {result.Email}");

            return result;
          }
          catch (Exception ex) {
            Utils.LogError($"Error {ErrorCodes.DB_QUERY_ERROR}: Error in DatabaseAccess.GetUserByEmail: {ex.ToString()}");
            return new AuthenticationResult {
              IsValid = false,
              Message = "Failed to retrieve user information"
            };
          }
        }
      }
    } // GetUserByEmail

    /// <summary>
    /// Updates user password using a reset token
    /// </summary>
    /// <param name="token">Password reset token</param>
    /// <param name="hashedPassword">Hashed password</param>
    /// <returns>SqlResult indicating success/failure and message</returns>
    public SqlResult UpdatePassword(string token, byte[] hashedPassword) {
      try {
        using (SqlConnection connection = new SqlConnection(_connectionString)) {
          using (SqlCommand command = new SqlCommand("p_update_password", connection)) {
            command.CommandType = CommandType.StoredProcedure;

            // Add parameters
            command.Parameters.Add("@token", SqlDbType.UniqueIdentifier).Value = Guid.Parse(token);
            command.Parameters.Add("@password", SqlDbType.VarBinary, 100).Value = hashedPassword;

            // Add output parameters
            SqlParameter successParam = new SqlParameter("@returnStatus", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            SqlParameter messageParam = new SqlParameter("@message", SqlDbType.VarChar, 1024) { Direction = ParameterDirection.Output };
            command.Parameters.Add(successParam);
            command.Parameters.Add(messageParam);

            connection.Open();
            command.ExecuteNonQuery();

            return new SqlResult {
              returnStatus = Convert.ToBoolean(successParam.Value),
              databaseMessage = Convert.ToString(messageParam.Value)
            };
          }
        }
      }
      catch (Exception ex) {
        Utils.LogError($"Error {ErrorCodes.DB_QUERY_ERROR}: Error in DatabaseAccess.UpdatePassword: {ex.ToString()}");
        return new SqlResult {
          returnStatus = false,
          databaseMessage = "Failed to update password"
        };
      }
    } // UpdatePassword
} // DatabaseAccess

public class UrlRecord {
  public string ShortUrl { get; set; }
  public string TargetUrl { get; set; }
  public DateTime StartDate { get; set; }
  public DateTime EndDate { get; set; }
  public byte RedirectionTypeId { get; set; }
  public bool PreventViewingOnMobile { get; set; }
  public int TeamId { get; set; }
} // UrlRecord
public class UserData {
  public string Email { get; set; }
  public string FirstName { get; set; }
  public string LastName { get; set; }
  public string RoleId { get; set; }
  public string TeamNo { get; set; }
  public string Phone { get; set; }
  public byte[] Password { get; set; }
} // UserData
public class SqlResult {
  public bool returnStatus { get; set; }
  public string databaseMessage { get; set; }
} // SqlResult
  // Create a custom class to return multiple values since C# 4.0 doesn't have good Tuple support
public class UrlRedirectInfo {
  public string targetUrl { get; set; }
  public byte redirectionType { get; set; }
  public bool preventMobileView { get; set; }
  public bool isExpired { get; set; }
  public bool isDisabled { get; set; }
  } // UrlRedirectInfo
  /// <summary>
  /// Result of validation operation
  /// </summary>
  public class ValidationResult {
  public bool IsValid { get; set; }
  public string Message { get; set; }
}  // ValidationResult
} // NotebookTracker

/// <summary>
/// Result of session validation operation
/// </summary>
public class SessionValidationResult {
  public bool IsValid { get; set; }
  public int UserId { get; set; }
  public string Email { get; set; }
  public string TeamNoWithLetter { get; set; }
  public string FirstName { get; set; }
  public string LastName { get; set; }
  public byte RoleId { get; set; }
  public string Message { get; set; }
} // SessionValidationResult

/// <summary>
/// Result of user authentication operation
/// </summary>
public class AuthenticationResult {
  public bool IsValid { get; set; }
  public int UserId { get; set; }
  public string Email { get; set; }
  public string TeamNoWithLetter { get; set; }
  public string FirstName { get; set; }
  public string LastName { get; set; }
  public string Phone { get; set; }
  public byte RoleId { get; set; }
  public string Message { get; set; }
} // AuthenticationResult

/// <summary>
/// Result of refresh token validation operation
/// </summary>
public class RefreshTokenValidationResult {
  public bool IsValid { get; set; }
  public string Message { get; set; }
  public AuthenticationResult AuthResult { get; set; }
  public TimeSpan TokenAge { get; set; }
}

public class DatabaseResult {
  public bool returnStatus { get; set; }
  public string databaseMessage { get; set; }
  public string message { get; set; }
}

/// <summary>
/// Result of mobile user creation operation
/// </summary>
public class MobileUserResult {
  public bool returnStatus { get; set; }
  public string databaseMessage { get; set; }
  public int userId { get; set; }
} // MobileUserResult

public class MobileUserData {
  public string Email { get; set; }
  public string FirstName { get; set; }
  public string LastName { get; set; }
  public byte RoleId { get; set; }
  public string TeamNoWithLetter { get; set; }
  public string Phone { get; set; }
  public byte[] Password { get; set; }
}

/// <summary>
/// Result of profile deletion operation
/// </summary>
public class DeleteProfileResult {
  public bool returnStatus { get; set; }
  public string databaseMessage { get; set; }
  public DateTime? deletedAt { get; set; }
} // DeleteProfileResult
