SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_delete_url]
@sessionId uniqueidentifier,
@urlId int,
@returnStatus int = 1 OUTPUT
/* ExecuteNonQuery() in client doesn't capture value from RETURN */
AS
BEGIN
  /*
   Delete the URL and any existing history.
  */
  SET NOCOUNT ON
  SET @returnStatus = 1
  /* Does the URL belong to the user of the given session? */
  IF NOT EXISTS (SELECT 1
                   FROM t_session s
				     JOIN t_url u ON s.userId = u.userId
	               WHERE s.sessionId = @sessionId
				     AND u.urlId = @urlId)
     RETURN
  DELETE t_urlLog
    WHERE urlId = @urlId;
  DELETE t_url
    WHERE urlId = @urlId;
  SET @returnStatus = 0
END
GO
grant exec on p_delete_url to vexteams24_user
go
