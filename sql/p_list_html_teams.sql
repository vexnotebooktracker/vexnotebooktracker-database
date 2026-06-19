SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_list_html_teams]
@sessionId uniqueidentifier = NULL,
@teamId int = NULL,
@teamsHtml varchar(4000) OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SET @teamsHtml = ''
  DECLARE @userId int = 0,
          @roleId tinyint = 1,
		  @userTeamId int = 0
  IF @sessionId IS NULL
    RETURN
  SELECT @userId = u.userId,
         @roleId = u.roleId,
		 @userTeamId = u.teamId
    FROM t_session s
	  JOIN t_user u ON s.userId = u.userId
	WHERE s.sessionId = @sessionId
  IF @userId = 0
    RETURN
  IF @roleId = 2 /* Team member/captain */
    SELECT @teamsHtml = CONCAT('<option value="', t.teamId, '">', t.teamNo, t.teamLetter, '</option>')
	  FROM t_team t
	  WHERE t.teamId = @userTeamId
  ELSE /* coach/advisor */
    SELECT @teamsHtml = STRING_AGG(CONCAT('<option value="', t2.teamId, '"', CASE WHEN ISNULL(@teamId,-1) = t2.teamId THEN ' selected' ELSE '' END , '>', t2.teamNo, t2.teamLetter, '</option>'),'') WITHIN GROUP (ORDER BY t2.teamNo, t2.teamLetter)
	  FROM t_team t1
	    JOIN t_team t2 ON t1.teamNo = t2.teamNo
	  WHERE t1.teamId = @userTeamId
	    AND t2.teamId != @userTeamId /* Exclude the team that has no letter. */
  SET @teamsHtml = ISNULL(@teamsHtml,'')
END
GO
grant exec on p_list_html_teams to vexteams24_user
go
