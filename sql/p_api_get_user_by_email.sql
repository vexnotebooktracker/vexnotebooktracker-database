SET ANSI_NULLS ON
GO
CREATE OR ALTER PROCEDURE [dbo].[p_api_get_user_by_email]
    @email VARCHAR(254),
    @isValid BIT OUTPUT,
    @userId INT OUTPUT,
    @teamNoWithLetter VARCHAR(10) OUTPUT,
    @firstName VARCHAR(128) OUTPUT,
    @lastName VARCHAR(128) OUTPUT,
	@phone varchar(17) OUTPUT,
	@roleId tinyint OUTPUT,
    @message VARCHAR(255) OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SELECT @isValid = 0, @message = 'User not found or inactive'
  SELECT
        @userId = u.userId,
        @teamNoWithLetter = CONCAT(t.teamNo, t.teamLetter),
        @firstName = u.firstName,
        @lastName = u.lastName,
		@phone = u.phone,
		@roleId = u.roleId
    FROM t_user u
	  JOIN t_team t ON u.teamId = t.teamId
    WHERE u.email = @email

  IF @@ROWCOUNT = 1
    SELECT @isValid = 1, @message = 'User found'
END
GO
grant exec on p_api_get_user_by_email to vexteams24_user
go
