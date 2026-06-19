SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_api_get_roles]
AS
BEGIN
  SET NOCOUNT ON;
  /* Exclude Administrator */
  SELECT r.roleId, r.roleName
    FROM t_role r
	WHERE r.roleId > 1
    ORDER BY 2
END
GO
grant exec on p_api_get_roles to vexteams24_user
go
