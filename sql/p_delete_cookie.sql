SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_delete_cookie]
@sessionId uniqueidentifier
AS
BEGIN
  SET NOCOUNT ON
  IF EXISTS (SELECT 1
               FROM t_session s
	           WHERE s.sessionId = @sessionId)
    DELETE t_session
      WHERE sessionId = @sessionId;
END
GO
grant exec on p_delete_cookie to vexteams24_user
go
