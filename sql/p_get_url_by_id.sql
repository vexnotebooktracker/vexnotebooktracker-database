SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_get_url_by_id]
@urlId INT = 0
AS
BEGIN
  SET NOCOUNT ON;
  SET @urlId = ISNULL(@urlId,0)
  SELECT urlId, userId, teamId, shortUrl, targetUrl, startDate, endDate, redirectionTypeId, preventViewingOnMobile
    FROM t_url
    WHERE urlId = @urlId;
END
GO
grant exec on p_get_url_by_id to vexteams24_user
go
