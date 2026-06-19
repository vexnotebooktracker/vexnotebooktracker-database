SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_api_terminate_mobile_session]
@sessionId UNIQUEIDENTIFIER,
@deviceId VARCHAR(50),
@returnStatus BIT OUTPUT,
@message VARCHAR(255) OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  DELETE t_mobile_session
    OUTPUT
        deleted.[sessionId],
        deleted.[userId],
        deleted.[deviceId],
        deleted.[appInstanceId],
        deleted.[deviceFingerprint],
        deleted.[iPAddress],
        deleted.[deviceInfo],
        deleted.[refreshToken],
        deleted.[createdAt],
        GETUTCDATE(), -- deleted.[lastActivity],
        deleted.[expiresAt],
        0
    INTO t_mobile_session_archive (
        [sessionId],
        [userId],
        [deviceId],
        [appInstanceId],
        [deviceFingerprint],
        [iPAddress],
        [deviceInfo],
        [refreshToken],
        [createdAt],
        [lastActivity],
        [expiresAt],
        [isActive])
    WHERE sessionId = @sessionId;
    SELECT @returnStatus = CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END,
           @message = CASE WHEN @returnStatus = 1 THEN 'Session terminated' ELSE 'Session not found' END
END
GO
grant exec on p_api_terminate_mobile_session to vexteams24_user
go
