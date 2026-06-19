SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_get_urls]
@sessionId uniqueidentifier
AS
BEGIN
  SET NOCOUNT ON;
  DECLARE @userId int = 0

  /* get the user ID from the session */
  SELECT @userId = s.userId
    FROM t_session s
	WHERE s.sessionId = @sessionId
	  AND s.lastAccessTime > DATEADD(SECOND, -300, GETDATE())
  IF @@ROWCOUNT != 1
    RETURN

  /* get view count, return with other details*/
  ; WITH urlViewCount (urlId, viewCount) AS
  (
  SELECT l.urlId, COUNT(1)
    FROM t_urlLog l
	  JOIN t_url u ON l.urlId = u.urlId
	WHERE u.userId = @userId
    GROUP BY l.urlId
  )
  SELECT u.urlId,
         CONCAT(t.teamNo, t.teamLetter) Team,
         u.shortUrl,
		 u.targetUrl,
		 FORMAT(u.startDate,'yyyy-MM-dd hh:mm') startDate,
		 FORMAT(u.endDate,'yyyy-MM-dd hh:mm') endDate,
		 CASE rt.redirectionType
		   WHEN 1 THEN 'Redirect'
		   WHEN 2 THEN 'IFRAME'
		   WHEN 3 THEN 'API'
		   ELSE 'N/A'
		 END redirectionType,
		 CASE WHEN u.preventViewingOnMobile = 1 THEN 'Yes'
		      ELSE 'No'
		 END as preventViewingOnMobile,
		 ISNULL(c.viewCount,0) noOfViews
    FROM t_url u
	  INNER JOIN t_team t ON u.teamId = t.teamId
	  INNER JOIN t_redirectionType rt ON u.redirectionTypeId = rt.redirectionTypeId
	  LEFT JOIN urlViewCount c ON u.urlId = c.urlId
	WHERE u.userId = @userId
	ORDER BY Team, u.startDate DESC;
END
GO
grant exec on p_get_urls to vexteams24_user
go
