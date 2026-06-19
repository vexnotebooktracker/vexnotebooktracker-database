SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_api_update_user_profile]
@sessionId UNIQUEIDENTIFIER,
@email VARCHAR(254),
@firstName VARCHAR(128),
@lastName VARCHAR(128),
@teamNoWithLetter VARCHAR(10),
@phone VARCHAR(20) = NULL,
@newPasswordHash VARBINARY(255) = NULL,
@deviceId VARCHAR(50) = '',
@roleId tinyint = NULL,
@returnStatus BIT OUTPUT,
@message VARCHAR(1024) OUTPUT
AS
BEGIN
SET NOCOUNT ON;

DECLARE @userId INT,
        @currentEmail VARCHAR(254),
		@teamNo int,
		@teamId int,
	    @teamLetter char(1)
SET @returnStatus = 0
BEGIN TRY
  SELECT @userId = ms.UserId,
         @currentEmail = u.Email
    FROM t_mobile_session ms
      JOIN t_user u ON ms.UserId = u.UserId
    WHERE ms.SessionId = @sessionId

    IF @@ROWCOUNT != 1
    BEGIN
      SET @message = 'Invalid or expired session. Please login again.'
      RETURN
    END;

    IF LEN(LTRIM(RTRIM(@email))) = 0 OR
	   LEN(LTRIM(RTRIM(@firstName))) = 0 OR
	   LEN(LTRIM(RTRIM(@lastName))) = 0 OR
	   LEN(LTRIM(RTRIM(@teamNoWithLetter))) = 0 OR
	   ISNULL(@roleId,0) NOT IN (2,3)
    BEGIN
      SET @message = 'Email, first name, last name, team number and role are required.'
      RETURN;
    END;

    IF @email != @currentEmail AND
        EXISTS (SELECT 1
                   FROM t_user
                   WHERE Email = @email AND UserId != @userId)
        BEGIN
          SET @message = 'Email address is already registered to another account.'
          RETURN;
        END;

    IF @teamNoWithLetter NOT LIKE '[0-9]%' OR @teamNoWithLetter LIKE '%[^0-9A-Z]%'
    BEGIN
      SET @message = 'Invalid team number format. Use digits followed by optional letter (e.g., 123A).'
      RETURN
    END

    SELECT @teamNo = t.teamNo,
	       @teamLetter = t.teamLetter
	  FROM dbo.fn_extract_team_no(@teamNoWithLetter) t
	IF ISNULL(@teamNo,0) <=  0
	  RAISERROR('* Invalid Team No. It should be 123A format.', 16, 1)

	SELECT @teamId = t.teamId
	  FROM t_team t
	  WHERE t.teamNo = @teamNo
	    AND t.teamLetter = ISNULL(@teamLetter,'')
	IF @@ROWCOUNT != 1
	  BEGIN
	    INSERT t_team (teamNo, teamLetter)
	      VALUES (@teamNo, CASE WHEN ISNULL(@teamLetter,'') = '' THEN ' ' ELSE @teamLetter END)
        SET @teamId = SCOPE_IDENTITY()
	  END

    BEGIN TRANSACTION
    UPDATE t_user
    SET Email = @email,
        FirstName = @firstName,
        LastName = @lastName,
        teamId = @teamId,
        Phone = @phone,
        Password = CASE
            WHEN @newPasswordHash IS NOT NULL THEN @newPasswordHash
            ELSE Password
        END,
		roleId = ISNULL(@roleId, roleId)
    WHERE userId = @userId

    IF @@ROWCOUNT = 0
    BEGIN
      ROLLBACK TRANSACTION;
      SET @message = 'Failed to update user profile. User not found or inactive.'
      RETURN;
    END;

    UPDATE t_mobile_session
      SET LastActivity = GETUTCDATE()
      WHERE SessionId = @sessionId;

    COMMIT TRANSACTION;
    SELECT @returnStatus = 1, @message = 'Profile updated successfully.'
END TRY
BEGIN CATCH
  IF @@TRANCOUNT > 0
    ROLLBACK TRANSACTION;
    SELECT @returnStatus = 1, @message = 'An error occurred while updating your profile: ' + ERROR_MESSAGE();
END CATCH;
END
GO
grant exec on p_api_update_user_profile to vexteams24_user
go
