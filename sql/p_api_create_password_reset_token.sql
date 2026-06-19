SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_api_create_password_reset_token]
    @email VARCHAR(254),
    @returnStatus BIT OUTPUT,
    @message VARCHAR(255) OUTPUT,
    @resetToken NVARCHAR(100) OUTPUT
AS
BEGIN
  SET NOCOUNT ON
    SET @returnStatus = 0;
    SET @message = '';
    SET @resetToken = '';

    -- Check if user exists
    IF NOT EXISTS (SELECT 1 FROM t_user WHERE email = @email)
    BEGIN
        SET @message = 'User not found';
        RETURN;
    END
    -- Generate reset token (simplified - use crypto-secure method in production)
    SET @resetToken = CONVERT(VARCHAR(42), NEWID())
    UPDATE t_user
      SET temporaryPasswordKey = @resetToken,
	      temporaryPasswordExpirationDate = DATEADD(HOUR, 1, GETUTCDATE())
      WHERE email = @email;
    IF @@ROWCOUNT = 1
      SELECT @returnStatus = 1, @message = 'Reset token created successfully';
END;
grant exec on p_api_create_password_reset_token to vexteams24_user
go
