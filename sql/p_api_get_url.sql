SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_api_get_url]
@sessionId uniqueidentifier,
@urlId INT = 0
AS
BEGIN
  SET NOCOUNT ON;
  SET @urlId = ISNULL(@urlId,0)
  DECLARE @noOfViews int = 0
  SELECT @noOfViews = COUNT(1)
      FROM t_urlLog l
	  WHERE l.urlId = @urlId
  SELECT u.urlId,
         u.userId,
		 CONCAT(t.teamNo,t.teamLetter) team,
         u.shortUrl,
		 u.targetUrl,
		 u.startDate,
		 u.endDate,
		 rt.redirectionType,
		 CASE WHEN u.preventViewingOnMobile = 1 THEN 'No'
		     WHEN u.preventViewingOnMobile = 0 THEN 'Yes'
			 ELSE 'N/A'
		 END [allowMobileView],
		 @noOfViews noOfViews
    FROM t_url u
	  JOIN t_team t ON u.teamId = t.teamId
	  JOIN t_redirectionType rt ON u.redirectionTypeId = rt.redirectionTypeId
    WHERE u.urlId = @urlId
END
GO
grant exec on p_api_get_url to vexteams24_user
go
