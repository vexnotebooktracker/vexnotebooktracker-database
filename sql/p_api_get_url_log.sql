SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_api_get_url_log]
@sessionId uniqueidentifier,
@logId int
AS
BEGIN
  SET NOCOUNT ON;
  SELECT l.logId,
         l.urlId,
         FORMAT(l.visitDateTime,'yyyy-MM-dd hh:mm') visitDateTime,
		 ISNULL(l.ipAddress, 'N/A') ipAddress,
		 ISNULL(l.hostName, 'N/A') hostName,
		 ISNULL(l.deviceType, 'N/A') deviceType,
         ISNULL(l.browserName, 'N/A') browserName,
         ISNULL(l.browserVersion, 'N/A') browserVersion,
         ISNULL(l.operatingSystem, 'N/A') operatingSystem,
         ISNULL(l.referringUrl, 'N/A') referringUrl
    FROM t_urlLog l
	WHERE l.logId = @logId
END
GO
grant exec on p_api_get_url_log to vexteams24_user
go
