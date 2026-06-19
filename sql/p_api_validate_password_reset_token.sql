SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_api_validate_password_reset_token]
@resetToken VARCHAR(100),
@isValid BIT OUTPUT,
@email VARCHAR(254) OUTPUT,
@message VARCHAR(255) OUTPUT
AS
BEGIN
  SET NOCOUNT ON
  SELECT @isValid = 0, @email = '', @message = '';
  SELECT @email = u.email
    FROM t_user u
    WHERE u.temporaryPasswordKey = @resetToken
      AND u.temporaryPasswordExpirationDate > GETUTCDATE()
  IF @@ROWCOUNT = 1
    SELECT @isValid = 1, @message = 'Token is valid'
  ELSE
    SELECT @isValid = 0, @message = 'Invalid or expired token';
END
go
grant exec on p_api_validate_password_reset_token to vexteams24_user
go
