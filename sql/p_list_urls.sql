SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_list_urls]
@sessionId uniqueidentifier = NULL
AS
BEGIN
  SET NOCOUNT ON;
  DECLARE @userId int = 0
  IF @sessionId IS NULL
    RETURN
  SELECT @userId = u.userId
    FROM t_session s
	  JOIN t_user u ON s.userId = u.userId
	WHERE s.sessionId = @sessionId
  IF @userId = 0
    RETURN

-- SET @userId = 1
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
		 , CASE rt.redirectionTypeId
		   WHEN 1 THEN 'Redirect'
		   WHEN 2 THEN 'IFRAME'
		   WHEN 3 THEN 'API'
		   ELSE 'N/A'
		 END redirectionType
		 -- uncomment
		 /*, CASE
		     WHEN u.preventViewingOnMobile = 1 THEN 'Yes'
		     ELSE 'No'
		   END as preventViewingOnMobile*/
		 ,  u.preventViewingOnMobile

		 , ISNULL(vc.viewCount,0) noOfViews
		 -- remove this.
		 -- check public HttpResponseMessage Get() in UrlController.cs in API project.
		 , CAST(@userId AS int) userId
		 , rt.redirectionTypeId
		 , t.teamId
	  FROM t_url u
	    JOIN t_team t ON u.teamId = t.teamId
		JOIN t_redirectionType rt ON u.redirectionTypeId = rt.redirectionTypeId
		LEFT JOIN temp_viewCounts vc ON u.urlId = vc.urlId
	  WHERE u.userId = @userId
END
GO
grant exec on p_list_urls to vexteams24_user
go
