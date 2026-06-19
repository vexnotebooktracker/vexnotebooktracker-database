SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_list_html_redirectionTypes]
@redirectionTypeId tinyint = 2,
@redirectionTypesHtml varchar(1000) OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SET @redirectionTypesHtml = ''
  SELECT @redirectionTypesHtml = STRING_AGG(CONCAT('<option value="', rt.redirectionTypeId, '"', CASE WHEN rt.redirectionTypeId = @redirectionTypeId THEN ' selected' ELSE '' END, '>', rt.redirectionType, '</option>'),'')
         WITHIN GROUP (ORDER BY rt.redirectionType)
    FROM t_redirectionType rt
  SET @redirectionTypesHtml = ISNULL(@redirectionTypesHtml,'')
END
GO
grant exec on p_list_html_redirectionTypes to vexteams24_user
go
