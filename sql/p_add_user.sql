SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_add_user]
@firstName nvarchar(128),
@lastName nvarchar(128),
@email varchar(254),
@phone varchar(17) = NULL,
@roleId tinyint = 2,
/* web submits @teamNo, App submits @teamNoWithLetter */
@teamNo varchar(10) = NULL,
@teamNoWithLetter varchar(10) = NULL,
@password varbinary(100),
/* mobile specific */
@deviceId VARCHAR(50) = NULL,
@appInstanceId UNIQUEIDENTIFIER = NULL,
@ipAddress VARCHAR(39) = NULL,
@deviceInfo nVARCHAR(2048) = NULL,
/* end mobile specific */
@returnStatus bit = 0 OUTPUT,
@message varchar(1024) = '' OUTPUT,
@userId INT = 0 OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  DECLARE -- @teamNumber int = 0,
          @teamNoWithoutLetter int,
		  @teamLetter varchar(1) = '',
		  @teamId int

  SELECT @returnStatus = 0, @message = '', @teamNoWithLetter = COALESCE(@teamNoWithLetter, @teamNo)
  BEGIN TRY
    /* Is email existing? */
    IF EXISTS (SELECT 1
	            FROM t_user u
	            WHERE u.email = @email)
      IF @@ROWCOUNT != 1
	    RAISERROR('Specified email is already registered, please use another.', 16, 1)

    /* Is role existing? */
    IF NOT EXISTS (SELECT 1
	                 FROM t_role r
	                 WHERE r.roleId = @roleId)
      IF @@ROWCOUNT != 1
	    RAISERROR('Invalid role was specified.', 16, 1)

    /* Extract team number and letter. */
    SELECT @teamNoWithoutLetter = t.teamNo,
	       @teamLetter = t.teamLetter
	  FROM dbo.fn_extract_team_no(@teamNoWithLetter) t
	IF ISNULL(@teamNoWithoutLetter,0) <=  0
	  RAISERROR('* Invalid Team No. It should be 123A format.', 16, 1)
    IF @teamLetter IS NULL OR @teamLetter = ''
	  SET @teamLetter = ' '

    IF @roleId = 2
	  BEGIN
	    SELECT @teamId = t.teamId
	      FROM t_team t
	      WHERE t.teamNo = @teamNoWithoutLetter
	        AND t.teamLetter = @teamLetter
	    IF @@ROWCOUNT != 1
	      BEGIN
	        INSERT t_team (teamNo, teamLetter)
	          VALUES (@teamNoWithoutLetter, CASE WHEN ISNULL(@teamLetter,'') = '' THEN ' ' ELSE @teamLetter END)
            SET @teamId = SCOPE_IDENTITY()
	     END
	  END
	ELSE IF @roleId = 3
	  BEGIN
	    ;WITH LetterSequence AS (
           SELECT 65 AS CharCode  -- ASCII code for 'A'
           UNION ALL
           SELECT CharCode + 1
             FROM LetterSequence
             WHERE CharCode < 90  -- ASCII code for 'Z'
        )
        INSERT t_team (teamNo, teamLetter)
          SELECT @teamNoWithoutLetter, CHAR(CharCode)
            FROM LetterSequence
			WHERE NOT EXISTS (SELECT 1
                                FROM t_team
                                WHERE teamNo = @teamNoWithoutLetter
                                  AND teamLetter = CHAR(CharCode)
                             )
            OPTION (MAXRECURSION 26)
		SELECT @teamId = (SELECT t.teamId FROM t_team t WHERE t.teamNo = @teamNoWithoutLetter AND t.teamLetter = 'A')
	  END
    INSERT t_user (firstName, lastName, email, phone, roleId, teamId, password, isEmailConfirmed, deviceId, appInstanceId, ipAddress, deviceInfo)
	  VALUES (@firstName, @lastName, @email, @phone, @roleId, @teamId, @password, 0, @deviceId, @appInstanceId, @ipAddress, @deviceInfo)

	SET @userId = SCOPE_IDENTITY()
	SET @returnStatus = 1
	SET @message = '<div>You have been registered successfully. <a href="./registerUrl">Generate Notebook URL</a></div>'
	SELECT @returnStatus, @message
  END TRY
  BEGIN CATCH
    SELECT @returnStatus = 0, @message = ERROR_MESSAGE()
  END CATCH
END
GO
grant exec on p_add_user to vexteams24_user
go
