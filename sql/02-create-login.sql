USE [master]
GO
/*
All database objects are owned by this account.
Runtime user should get appropriate stored procs permissions.
*/
CREATE LOGIN [vexteams24_dbo] WITH
    PASSWORD = N'INSERT_STRONG_PASSWORD_HERE',
    DEFAULT_DATABASE = [vexteams24],
    DEFAULT_LANGUAGE = [us_english],
    CHECK_EXPIRATION = OFF,
    CHECK_POLICY = OFF
GO
USE [vexteams24]
GO
CREATE USER [vexteams24_dbo] FOR LOGIN [vexteams24_dbo] WITH DEFAULT_SCHEMA = [dbo]
GO
GRANT CONNECT TO [vexteams24_dbo]
GO
ALTER ROLE [db_owner] ADD MEMBER [vexteams24_dbo]
GO

/*
Application runtime account used exclusively by the API and web application to execute
 stored procedures. MUST NOT HAVE ANY DDL privileges.
*/
USE [master]
GO
CREATE LOGIN [vexteams24_user] WITH
    PASSWORD = N'INSERT_STRONG_PASSWORD_HERE_DIFFERENT_FROM_DBO_PASSWORD',
    DEFAULT_DATABASE = [vexteams24],
    DEFAULT_LANGUAGE = [us_english],
    CHECK_EXPIRATION = OFF,
    CHECK_POLICY = OFF
GO
USE [vexteams24]
GO
CREATE USER [vexteams24_user]
    FOR LOGIN [vexteams24_user]
    WITH DEFAULT_SCHEMA = [dbo]
GO
GRANT CONNECT TO [vexteams24_user]
GO
