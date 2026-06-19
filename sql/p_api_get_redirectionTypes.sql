SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_api_get_redirectionTypes]
AS
BEGIN
  SET NOCOUNT ON;
  SELECT rt.redirectionTypeId, rt.redirectionType
    FROM t_redirectionType rt
    ORDER BY 1
END
GO
grant exec on p_api_get_redirectionTypes to vexteams24_user
go
