SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_api_delete_user_profile]
    @sessionId UNIQUEIDENTIFIER,
    @confirmationText VARCHAR(10),
    @deviceId VARCHAR(50),
    @returnStatus BIT OUTPUT,
    @message VARCHAR(1024) OUTPUT,
    @deletedAt DATETIME OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @userId INT;
    DECLARE @userEmail VARCHAR(255);
    DECLARE @isValidSession BIT = 0;
    SELECT @returnStatus = 0, @deletedAt = NULL, @message = 'Invalid confirmation text. Please type "DELETE" to confirm.';
    IF UPPER(LTRIM(RTRIM(@confirmationText))) != 'DELETE'
      RETURN;

    SELECT @userId = u.userId, @userEmail = u.email, @isValidSession = 1
      FROM t_mobile_session ms
        INNER JOIN t_user u ON ms.userId = u.userId
    WHERE ms.sessionId = @sessionId

    IF @isValidSession = 0
    BEGIN
        SET @message = 'Invalid or expired session. Please login again.';
        RETURN;
    END

    BEGIN TRY
     BEGIN TRANSACTION;
     INSERT t_user_delete_log (userId, userEmail, deletedAt, deviceId)
       VALUES (@userId, @userEmail, GETUTCDATE(), @deviceId);
	 DELETE t_urlLog
	   WHERE urlId IN (SELECT urlId FROM t_url WHERE userId = @userId)
	 DELETE t_url
	   WHERE userId = @userId
	 DELETE t_mobile_session_archive
	   WHERE userId = @userId
	 DELETE t_mobile_session
	   WHERE userId = @userId
	 DELETE t_user
	   WHERE userId = @userId
     SELECT @message = 'Profile successfully deleted. ', @returnStatus = 1;
     COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
      ROLLBACK TRANSACTION;
      SELECT @returnStatus = 0, @message = 'An error occurred while deleting the profile: ' + ERROR_MESSAGE();
    END CATCH
END
GO
grant exec on p_api_delete_user_profile to vexteams24_user
go
