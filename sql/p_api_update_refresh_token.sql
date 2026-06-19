SET ANSI_NULLS ON
GO
-- 2. Update refresh token for a session
CREATE OR ALTER PROCEDURE [dbo].[p_api_update_refresh_token]
    @sessionId uniqueidentifier,
    @newRefreshToken VARCHAR(255),
    @returnStatus BIT OUTPUT,
    @message VARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    -- Check if session exists and is active
    IF NOT EXISTS (SELECT 1 FROM t_mobile_app_session WHERE sessionId = @sessionId AND isActive = 1)
    BEGIN
        SET @returnStatus = 0;
        SET @message = 'Session not found or inactive';
        RETURN;
    END

    -- Update refresh token
    UPDATE t_mobile_app_session
    SET refreshToken = @newRefreshToken,
        lastActivity = GETUTCDATE()
    WHERE sessionId = @sessionId;

    IF @@ROWCOUNT > 0
    BEGIN
        SET @returnStatus = 1;
        SET @message = 'Refresh token updated successfully';
    END
    ELSE
    BEGIN
        SET @returnStatus = 0;
        SET @message = 'Failed to update refresh token';
    END
END
GO
grant exec on p_api_update_refresh_token to vexteams24_user
go
