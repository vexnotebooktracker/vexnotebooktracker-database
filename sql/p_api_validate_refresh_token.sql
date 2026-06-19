SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_api_validate_refresh_token]
    @sessionId uniqueidentifier,
    @refreshToken VARCHAR(255),
    @isValid BIT OUTPUT,
    @userId INT OUTPUT,
    @email VARCHAR(255) OUTPUT,
    @teamNoWithLetter VARCHAR(10) OUTPUT,
    @firstName VARCHAR(50) OUTPUT,
    @lastName VARCHAR(50) OUTPUT,
    @tokenAgeMinutes INT OUTPUT,
    @message VARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @createdAt DATETIME;
    DECLARE @isActive BIT;

    -- Check if session exists and refresh token matches
    SELECT
        @userId = ms.userId,
        @createdAt = ms.createdAt,
        @isActive = ms.isActive
    FROM t_mobile_session ms
    WHERE ms.sessionId = @sessionId
      AND ms.refreshToken = @refreshToken
      AND ms.isActive = 1;

    IF @userId IS NULL
    BEGIN
        SET @isValid = 0;
        SET @message = 'Invalid refresh token or session';
        RETURN;
    END

    -- Check if session is expired (older than 30 days)
    IF DATEDIFF(DAY, @createdAt, GETUTCDATE()) > 30
    BEGIN
        SET @isValid = 0;
        SET @message = 'Refresh token expired';

        -- Deactivate expired session
        UPDATE t_mobile_session
        SET isActive = 0
        WHERE sessionId = @sessionId;

        RETURN;
    END

    -- Get user details
    SELECT
        @email = u.email,
        @teamNoWithLetter = CONCAT(t.teamNo,t.teamLetter),
        @firstName = u.firstName,
        @lastName = u.lastName
    FROM t_user u
	  join t_team t on u.teamId = t.teamId
    WHERE u.userId = @userId -- AND u.isActive = 1;

    IF @email IS NULL
    BEGIN
        SET @isValid = 0;
        SET @message = 'User account not found or inactive';
        RETURN;
    END

    -- Calculate token age in minutes
    SET @tokenAgeMinutes = DATEDIFF(MINUTE, @createdAt, GETUTCDATE());

    SET @isValid = 1;
    SET @message = 'Refresh token valid';
END
GO
grant exec on p_api_validate_refresh_token to vexteams24_user
go
