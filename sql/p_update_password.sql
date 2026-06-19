SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_update_password]
@token uniqueidentifier,
@password varbinary(100),
@returnStatus bit = 0 OUTPUT,
@message varchar(1024) = '' OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  IF EXISTS (SELECT 1 FROM t_user u WHERE u.temporaryPasswordKey = @token AND u.temporaryPasswordExpirationDate < DATEADD(HOUR, -1, GETUTCDATE()))
    BEGIN
	  SET @returnStatus = 0
	  SET @message = 'Reset password request is more than an hour old. You may want to send the request again.'
	  RETURN
	END
  UPDATE t_user
    SET password = @password,
	    temporaryPasswordKey = NULL,
		temporaryPasswordExpirationDate = NULL
	WHERE temporaryPasswordKey = @token
  SET @returnStatus = 1
END
GO
grant exec on p_update_password to vexteams24_user
go
