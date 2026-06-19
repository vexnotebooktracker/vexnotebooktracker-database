SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_api_get_urls]
@sessionId uniqueidentifier
AS
BEGIN
  SET NOCOUNT ON;
  DECLARE @userId int = 0
  IF @sessionId IS NULL
    RETURN
  SELECT @userId = u.userId
    FROM t_mobile_session s
	  JOIN t_user u ON s.userId = u.userId
	WHERE s.sessionId = @sessionId
  IF @userId = 0
    RETURN
; WITH temp_viewCounts AS (
    SELECT l.urlId, COUNT(1) viewCount
      FROM t_url u
	    JOIN t_urlLog l ON u.urlId = l.urlId
	  WHERE u.userId = @userId
	  GROUP BY l.urlId)
  SELECT   u.urlId
         , CONCAT(t.teamNo, t.teamLetter) Team
         , CONCAT(u.shortUrl, '_', u.randomString) shortUrl
		 , u.targetUrl
		 , FORMAT(u.startDate,'yyyy-MM-dd hh:mm') startDate
		 , FORMAT(u.endDate,'yyyy-MM-dd hh:mm') endDate
		 , rt.redirectionType
		 , CASE
		     WHEN u.preventViewingOnMobile = 1 THEN 'No'
		     ELSE 'Yes'
		   END as allowMobileView
		 , convert(bit,1) allowViewingOnMobile
		 , ISNULL(vc.viewCount,0) noOfViews
		 , preventViewingOnMobile
	  FROM t_url u
	    JOIN t_team t ON u.teamId = t.teamId
		JOIN t_redirectionType rt ON u.redirectionTypeId = rt.redirectionTypeId
		LEFT JOIN temp_viewCounts vc ON u.urlId = vc.urlId
	  WHERE u.userId = @userId
END
GO
grant exec on p_api_get_urls to vexteams24_user
go
