SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_api_check_email_availability]
@email VARCHAR(254),
@isAvailable BIT OUTPUT
AS
BEGIN
   SET NOCOUNT ON;
    SET @isAvailable = 1;
    IF EXISTS (SELECT 1
                 FROM t_user
                 WHERE email = @email)
      SET @isAvailable = 0;
END
GO
grant exec on p_api_check_email_availability to vexteams24_user
go
