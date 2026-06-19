SET ANSI_NULLS ON
GO
CREATE PROCEDURE [dbo].[p_add_user_session]
@email varchar(254),
@ipAddress varchar(50),
@deviceFingerprint varchar(255),
@sessionId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
  -- Generate a new session ID
  SET @SessionID = NEWID()
  -- Get the user and role Ids
  DECLARE @userId int
  SELECT @userId = u.userId
	FROM t_user u
	WHERE u.email = @email
  DELETE t_session
    WHERE userId = @userId;

  INSERT t_session (sessionId, userId, lastAccessTime, ipAddress, deviceFingerprint)
   VALUES (@sessionId, @userId, GETDATE(), ISNULL(@ipAddress,''), ISNULL(@deviceFingerprint,''))
END
GO
grant exec on p_add_user_session to vexteams24_user
go
