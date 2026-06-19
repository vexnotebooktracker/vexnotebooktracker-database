SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_add_urlLog]
@requestedURL varchar(2048),
@referringURL varchar(500),
@ipAddress varchar(50),
@hostname varchar(128),
@browserName varchar(64),
@browserVersion varchar(64),
@deviceType varchar(16),
@operatingSystem Nvarchar(32)
AS
BEGIN
  SET NOCOUNT ON;
  DECLARE @urlId int = 0
  SELECT @urlId = u.urlId
    FROM t_url u
	WHERE u.shortUrl_randomString = @requestedURL
  INSERT t_urlLog (urlId, visitDateTime, referringURL, ipAddress, hostname, deviceType, browserName, browserVersion, operatingSystem )
    VALUES (ISNULL(@urlId,0), GETDATE(), @referringURL, @ipAddress, @hostname, @deviceType, @browserName, @browserVersion, @operatingSystem);
END
GO
grant exec on p_add_urlLog to vexteams24_user
go
