SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_get_url_history]
@sessionId uniqueidentifier,
@urlId int
AS
BEGIN
  SET NOCOUNT ON;
  DECLARE @viewCount int = 0
  /* Does the URL belong to the user of the given session? */
  IF NOT EXISTS (SELECT 1
                   FROM t_session s
				     JOIN t_url u ON s.userId = u.userId
	               WHERE s.sessionId = @sessionId
				     AND u.urlId = @urlId)
     RETURN

  /* get view count, return with other details*/
  SELECT @viewCount = COUNT(1)
    FROM t_urlLog l
	WHERE l.urlId = @urlId
  SELECT u.urlId,
         CONCAT(t.teamNo, t.teamLetter) Team,
         u.shortUrl,
		 u.targetUrl,
		 FORMAT(u.startDate,'yyyy-MM-dd hh:mm') startDate,
		 FORMAT(u.endDate,'yyyy-MM-dd hh:mm') endDate,
		 rt.redirectionType,
		 CASE WHEN u.preventViewingOnMobile = 1 THEN 'Yes'
		      ELSE 'No'
		 END as preventViewingOnMobile,
		 ISNULL(@viewCount,0) noOfViews
    FROM t_url u
	  INNER JOIN t_team t ON u.teamId = t.teamId
	  INNER JOIN t_redirectionType rt ON u.redirectionTypeId = rt.redirectionTypeId
	WHERE u.urlId = @urlId;

  /* Return URL visit history */
  SELECT FORMAT(l.visitDateTime,'yyyy-MM-dd hh:mm:ss') visitDateTime,
         l.referringUrl,
		 l.ipAddress,
		 l.hostName,
		 l.deviceType,
		 l.operatingSystem,
		 l.browserName,
		 l.browserVersion
    FROM t_urlLog l
	WHERE l.urlId = @urlId
	ORDER BY l.visitDateTime DESC;
END
GO
grant exec on p_get_url_history to vexteams24_user
go
