SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_generate_unique_random_string]
@randomStringLength tinyint = 4,
@randomString varchar(20) OUTPUT
AS
BEGIN
  /* shortUrl is varchar(16), so at minimum the length would be 4. */
  /* allows 65^4 = 17,850,625 combinations. */
  SET NOCOUNT ON
  DECLARE @validChars CHAR(65) = N'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.'
  DECLARE @charLen tinyint = LEN(@validChars),
          @maxAttempts tinyint = 255,
          @attempts tinyint = 0,
          @i tinyint = 1;
  WHILE @attempts < @maxAttempts
  BEGIN
    SET @randomString = '';
    SET @i = 1;
    WHILE @i <= @randomStringLength
      BEGIN
        SET @randomString += SUBSTRING(@validChars, CAST(CEILING(RAND() * @charLen) AS tinyint), 1);
        SET @i += 1;
      END
    IF NOT EXISTS (SELECT 1
	                 FROM t_url u
				     WHERE u.randomString = @randomString)
      RETURN
    SET @attempts += 1
  END
  RETURN
END
GO
grant exec on p_generate_unique_random_string to vexteams24_user
go
