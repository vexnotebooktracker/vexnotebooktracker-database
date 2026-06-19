SET ANSI_NULLS ON
GO
CREATE OR ALTER procedure [dbo].[p_api_validate_session]
@sessionId uniqueidentifier ,
@deviceId varchar(255) ,
@ipAddress varchar(45) ,
@userAgent varchar(255),
@isValid bit output,
@userId int output,
@email varchar(254) output,
@teamNoWithLetter varchar(10) output,
@firstName varchar(128) output,
@lastName varchar(128) output,
@roleId tinyint output,
@message varchar(255) output
as
begin
  set nocount on

  -- Initialize output parameters
  select @isValid = 0, @userId = 0, @email = '', @teamNoWithLetter = '', @roleId = 2, @firstName = '', @lastName = '', @message = 'Session validation failed'

  -- Declare local variables
  declare @sessionUserId int
  declare @sessionDeviceId varchar(255)
  declare @sessionExpiry datetime2
  declare @sessionActive bit

  -- Check if session exists and get session details
  select
    @sessionUserId = ms.UserId,
    @sessionDeviceId = ms.DeviceId,
    @sessionExpiry = ms.ExpiresAt,
    @sessionActive = ms.IsActive
  from t_mobile_session ms
  where ms.SessionId = @sessionId

  -- Validate session exists
  if @sessionUserId is null
  begin
    set @message = 'Session not found'
    return
  end

  -- Validate session is active
  if @sessionActive = 0
  begin
    set @message = 'Session is inactive'
    return
  end

  -- Validate session hasn't expired
  if @sessionExpiry < getutcdate()
  begin
    -- Mark session as inactive
    update t_mobile_session
    set IsActive = 0
    where SessionId = @sessionId

    set @message = 'Session has expired'
    return
  end

  -- Validate device ID matches (security check)
  if @sessionDeviceId != @deviceId
  begin
    set @message = 'Device ID mismatch'
    return
  end

  -- Update last activity and IP address
  update t_mobile_session
  set LastActivity = getutcdate(),
    IPAddress = @ipAddress
  where SessionId = @sessionId

  -- Get user details
  select @userId = u.userId,
    @email = u.email,
    @teamNoWithLetter = CONCAT(t.teamNo, t.teamLetter),
    @firstName = u.firstName,
    @lastName = u.lastName,
	@roleId = u.roleId
  from t_user u
    join t_team t on u.teamId = t.teamId
  where u.userId = @sessionUserId

  -- Validate user exists and is active
  if @userId is null
  begin
    set @message = 'User not found'
    return
  end

  declare @userActive bit = ISNULL((SELECT 1 from t_user where userId = @userId),0)

  if @userActive = 0
  begin
    update t_mobile_session
    set IsActive = 0
    where SessionId = @sessionId

    set @message = 'User account is inactive'
    return
  end

  select @isValid = 1, @message = 'Session validated successfully'
end
GO
grant exec on p_api_validate_session to vexteams24_user
go
