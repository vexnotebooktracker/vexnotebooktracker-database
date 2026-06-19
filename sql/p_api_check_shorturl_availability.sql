SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_api_check_shorturl_availability]
@shortUrl varchar(16) = NULL,
@isAvailable bit OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SET @isAvailable = 1
  IF EXISTS (SELECT 1
               FROM t_url u
			   WHERE u.shortUrl = @shortUrl)
    SET @isAvailable = 0
  ELSE
    SET @isAvailable = 1
END
GO
grant exec on p_api_check_shorturl_availability to vexteams24_user
go
