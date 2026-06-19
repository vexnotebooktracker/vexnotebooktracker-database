SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_api_get_teams]
@sessionId uniqueidentifier
AS
BEGIN
  SET NOCOUNT ON;
  DECLARE @roleId tinyint = 2
  DECLARE @teamId int = 0
  DECLARE @teamNo int = 0
  SELECT @roleId = u.roleId, @teamId = u.teamId
    FROM t_user u
	  JOIN t_mobile_session s ON u.userId = s.userId
	WHERE s.sessionId = @sessionId
  IF @roleId = 2
    SELECT t.teamId, CONCAT(t.teamNo, t.teamLetter) teamName
      FROM t_team t
	    JOIN t_user u ON t.teamId = u.teamId
	    JOIN t_mobile_session s ON u.userId = s.userId
	  WHERE s.sessionId = @sessionId
      ORDER BY 2
  ELSE IF @roleId = 3
    BEGIN
	  SELECT @teamNo = t.teamNo
	    FROM t_team t
		WHERE t.teamId = @teamId
      SELECT t.teamId, CONCAT(t.teamNo, t.teamLetter) teamName
        FROM t_team t
	    WHERE t.teamNo = @teamNo
        ORDER BY 2
	END
END
GO
grant exec on p_api_get_teams to vexteams24_user
go
