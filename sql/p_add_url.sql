SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_add_url]
@sessionId uniqueidentifier,
@teamId int,
@shortUrl varchar(16),
@targetUrl varchar(2048),
@startDate datetime = NULL,
@endDate datetime = NULL,
@redirectionTypeId tinyint = 1,
@preventViewingOnMobile bit = 1,
@returnStatus bit output,
@message varchar(2048) output
AS
BEGIN
  SET NOCOUNT ON;
  /*
   Send back detailed error message for human errors.
   Let the error message be cryptic for non-human errors. Prevent exposing any program bugs or hacking attempts.
  */
  DECLARE @db_teamId int = 0,
		  @db_roleId tinyint = 0,
		  @userId int = 0,
		  @randomString varchar(20) = '',
		  @randomStringLength tinyint

  SELECT @returnStatus = 0, @message = ''
  BEGIN TRY
    SELECT @db_roleId = u.roleId,
	       @db_teamId = u.teamId,
		   @userId    = u.userId
	  FROM t_user u
	    JOIN t_session s ON u.userId = s.userId
	  WHERE s.sessionId = @sessionId
    /* Is valid user? */
    IF @@ROWCOUNT != 1
	  RAISERROR('* Error 0001', 16, 1)

    /* Is valid team? */
    IF NOT EXISTS (SELECT 1 FROM t_team t WHERE t.teamId = @teamId)
	  RAISERROR('* Error 0002', 16, 1)

    /* Does the team belong to user */
    IF @db_roleId = 2 /* team captain */
	  BEGIN
	    IF @teamId != @db_teamId
	      RAISERROR('* Error 0003', 16, 1)
	  END
	ELSE
      IF @db_roleId = 3 /* school/coash/advisor */
	    BEGIN
		  /* Team number must be same, not the letter. */
	      IF NOT EXISTS (SELECT 1
		                   FROM t_team t1
						     JOIN t_team t2 ON t1.teamNo = t2.teamNo
		                   WHERE t1.teamId = @teamId
					         AND t2.teamId = @db_teamId)
	        RAISERROR('* Error 0004', 16, 1)
	    END

    /* Is requested short URL already existing? */
    IF EXISTS (SELECT 1 FROM t_url u WHERE u.shortUrl = @shortUrl)
	  BEGIN
	    SET @message = '* Requested short URL: ' + @shortUrl + ' in use. Please select another.'
	    RAISERROR( @message, 16, 1)
	  END

    /* target URL should NOT be on vexteams.org */
    IF @targetUrl LIKE '%vexteams.org%'
      RAISERROR( 'Target URL can not hosted on vexteams.org.', 16, 1)

    /* target URL must start with http or https */
    IF NOT (@targetUrl LIKE 'http://%'
	        OR @targetUrl LIKE 'https://%')
      RAISERROR( 'Target URL must start with http:// OR https://', 16, 1)

	/* If invalid redirection type, silently default to 1, i.e., redirect */
    IF NOT EXISTS (SELECT 1 FROM t_redirectionType rt WHERE rt.redirectionTypeId = @redirectionTypeId)
	  SET @redirectionTypeId = 1

	/* If invalid start date or start date is earlier than today 12AM, set it to current datetime. */
    IF @startDate IS NULL
	   OR @startDate < CAST(CAST(GETUTCDATE() as DATE) as DATETIME)
	   OR @startDate > @endDate
	  SET @startDate = GETUTCDATE()

	/* Competitions do not expect notebook submission more than two weeks early. */
    IF @endDate IS NULL
	   OR @endDate > DATEADD(DAY, 60, GETUTCDATE())
	   OR @endDate < @startDate
	  SET @endDate = DATEADD(DAY, 14, GETUTCDATE())

	/* Final shortUrl would be shortUrl_randomString, so randomString length should be one less to accommodate underscore separator. */
	SET @randomStringLength = 20 - LEN(@shortUrl) - 1
	EXEC p_generate_unique_random_string @randomStringLength, @randomString OUTPUT
    INSERT t_url (userId, teamId, shortUrl, targetUrl, startDate, endDate, redirectionTypeId, preventViewingOnMobile, randomString)
	  VALUES (@userId, @teamId, @shortUrl, @targetUrl, @startDate, @endDate, @redirectionTypeId, @preventViewingOnMobile, @randomString)

	SELECT @returnStatus = 1, /* CAST(SCOPE_IDENTITY() AS int) as urlId, */
	       @message = CONCAT('<div>Your notebook URL has been setup successfully.</div><div>Please note that <strong>short URL is case sensitive</strong>.<br/>Your submission URL:</div>',
				  '<div style="font-family: monospace;">https://vexteams.org/nb/',
				  @shortUrl,
				  '_',
				  @randomString,
				  '</div>')
  END TRY
  BEGIN CATCH
    SELECT @returnStatus = 0, @message = ERROR_MESSAGE()
  END CATCH
END
GO
grant exec on p_add_url to vexteams24_user
go
