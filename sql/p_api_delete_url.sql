SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_api_delete_url]
@sessionId uniqueidentifier,
@urlId int,
@returnStatus BIT OUTPUT,
@message VARCHAR(1024) OUTPUT
AS
BEGIN
  /*
   Delete the URL and any existing history.
  */
  SET NOCOUNT ON
  SET @returnStatus = 0
  SET @message = ''
  IF NOT EXISTS (SELECT 1
                   FROM t_session s
				     JOIN t_url u ON s.userId = u.userId
	               WHERE s.sessionId = @sessionId
				     AND u.urlId = @urlId
				  UNION ALL
				  SELECT 1
                   FROM t_mobile_session s
				     JOIN t_url u ON s.userId = u.userId
	               WHERE s.sessionId = @sessionId
				     AND u.urlId = @urlId
				)
    BEGIN
	  SELECT @returnStatus = 0, @message = 'URL/Session not found.'
      RETURN
	END
  DELETE t_urlLog
    WHERE urlId = @urlId;
  DELETE t_url
    WHERE urlId = @urlId;
  IF @@ROWCOUNT > 0
    SELECT @returnStatus = 1, @message = 'URL deleted successfully'
  ELSE
    SELECT @returnStatus = 0, @message = 'Failed to delete URL'
END
GO
grant exec on p_api_delete_url to vexteams24_user
go
