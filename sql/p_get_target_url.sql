SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_get_target_url]
@shortUrl varchar(20),
@targetUrl varchar(1024) = '' OUTPUT,
@redirectionType tinyint = 1 OUTPUT,
@preventMobileView bit = 1 OUTPUT,
@isExpired bit = 0 OUTPUT,
@isDisabled bit = 0 OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SELECT @targetUrl = '', @redirectionType = 1, @preventMobileView = 1, @isExpired = 0, @isDisabled = 0
  SELECT @redirectionType = u.redirectionTypeId,
         @targetUrl = u.targetUrl,
		 @preventMobileView = ISNULL(u.preventViewingOnMobile,1),
		 @isExpired = CAST(CASE WHEN u.endDate < GETDATE() THEN 1 ELSE 0 END AS bit),
		 @isDisabled = 0 /* will be implemented later. */
	  FROM t_url u
	  WHERE u.shortUrl_randomString = @shortUrl
  IF @@ROWCOUNT != 1
    SELECT @targetUrl = '', @redirectionType = 1, @preventMobileView = 1, @isExpired = 0, @isDisabled = 0
END
GO
grant exec on p_get_target_url to vexteams24_user
go
