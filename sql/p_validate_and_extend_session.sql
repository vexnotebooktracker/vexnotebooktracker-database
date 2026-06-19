SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_validate_and_extend_session]
@sessionId UNIQUEIDENTIFIER,
@ipAddress VARCHAR(50),
@deviceFingerprint VARCHAR(255),
@isSessionValid BIT OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SET @isSessionValid = 0
  DECLARE @lastAccessTime datetime

    -- Get the last activity time for this session and verify IP and fingerprint
  SELECT @lastAccessTime = s.lastAccessTime
    FROM t_session s
    WHERE sessionId = @sessionId
      AND ipAddress = @IPAddress
      AND deviceFingerprint = @deviceFingerprint

  -- Check if session exists and is within timeout period
  IF @lastAccessTime IS NOT NULL AND DATEDIFF(SECOND, @lastAccessTime, GETDATE()) <= 1200
    BEGIN
      UPDATE t_session
        SET lastAccessTime = GETDATE()
        WHERE sessionId = @sessionId
      SET @isSessionValid = 1
    END
END
GO
grant exec on p_validate_and_extend_session to vexteams24_user
go
