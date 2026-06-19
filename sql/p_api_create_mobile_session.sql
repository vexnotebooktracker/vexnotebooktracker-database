SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_api_create_mobile_session]
    @userId INT,
    @deviceId VARCHAR(50),
    @appInstanceId UNIQUEIDENTIFIER,
    @deviceFingerprint VARCHAR(100),
    @ipAddress VARCHAR(39),
    @deviceInfo NVARCHAR(2048),
    @refreshToken VARCHAR(200),
    @sessionId UNIQUEIDENTIFIER OUTPUT,
    @returnStatus BIT OUTPUT,
    @message VARCHAR(255) OUTPUT
AS
BEGIN
  SET NOCOUNT ON
  SELECT @sessionId = NEWID(), @returnStatus = 0;
  INSERT t_mobile_session (SessionId, UserId, DeviceId, AppInstanceId, DeviceFingerprint, IPAddress, DeviceInfo, RefreshToken, ExpiresAt)
    SELECT @sessionId, @userId, @deviceId, @appInstanceId, @deviceFingerprint, @ipAddress, @deviceInfo, @refreshToken, DATEADD(DAY, 30, GETUTCDATE())
  IF @@ROWCOUNT = 1
    SELECT @returnStatus = 1, @message = CAST(@sessionId AS VARCHAR(255));
END
GO
grant exec on p_api_create_mobile_session to vexteams24_user
go
