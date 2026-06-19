SET ANSI_NULLS ON
GO
CREATE OR ALTER procedure [dbo].[p_api_authenticate_mobile_user]
@sessionId uniqueidentifier,
@email varchar(254),
@passwordHash varchar(255),
@isValid bit output,
@userId int output,
@teamNoWithLetter varchar(10) output,
@firstName varchar(128) output,
@lastName varchar(128) output,
@message varchar(255) output
as
begin
  set nocount on
  select @isValid = 1, @userId = 100, @teamNoWithLetter = '999Z', @firstName = 'First', @lastName = 'Last', @message = 'All good.'
end
GO
grant exec on p_api_authenticate_mobile_user to vexteams24_user
go
