SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_is_shortUrl_registered]
@shortUrl varchar(16),
@isAlreadyRegistered bit OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SET @isAlreadyRegistered = 0
  IF EXISTS (SELECT 1
               FROM t_url u
			   WHERE u.shortUrl = @shortUrl)
	SET @isAlreadyRegistered = 1
END
GO
grant exec on p_is_shortUrl_registered to vexteams24_user
go
