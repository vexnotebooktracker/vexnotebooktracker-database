SET ANSI_NULLS ON
GO
CREATE OR ALTER procedure [dbo].[p_authenticate_user]
@email varchar(254),
@password varbinary(100),
@sessionId uniqueidentifier = null OUTPUT
as
begin
  SET NOCOUNT ON
  set @sessionId = newid()
  return
end
GO
grant exec on p_authenticate_user to vexteams24_user
go
