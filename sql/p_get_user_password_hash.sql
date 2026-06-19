SET ANSI_NULLS ON
GO
CREATE OR ALTER procedure [dbo].[p_get_user_password_hash]
@email varchar(254)
as
begin
  SET NOCOUNT ON
  SELECT u.password
    FROM t_user u
	WHERE u.email = @email
end
GO
grant exec on p_get_user_password_hash to vexteams24_user
go
