/*
All indexes are non-clustered by default, no need to specify it.
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE dbo.t_passwordHint(
	passwordHintId tinyint NOT NULL,
	passwordHint nvarchar(512) NOT NULL,
 CONSTRAINT pk_passwordHint_passwordHintId PRIMARY KEY CLUSTERED (passwordHintId)
)
GO
CREATE UNIQUE INDEX idx1_passwordHint_passwordHint ON dbo.t_passwordHint(passwordHint)
GO

CREATE TABLE dbo.t_redirectionType(
	redirectionTypeId tinyint NOT NULL,
	redirectionType varchar(128) NOT NULL,
 CONSTRAINT pk_redirectionType_redirectionTypeId PRIMARY KEY CLUSTERED (redirectionTypeId))
GO
CREATE UNIQUE INDEX xid1_redirectionType_redirectionType ON dbo.t_redirectionType (redirectionType)
GO

CREATE TABLE dbo.t_role(
	roleId tinyint NOT NULL,
	roleName varchar(32) NOT NULL,
 CONSTRAINT pk_role_roleId PRIMARY KEY CLUSTERED (roleId)
)
GO
CREATE UNIQUE INDEX idx1_role ON dbo.t_role (roleName)
GO

CREATE TABLE dbo.t_team(
	teamId int IDENTITY(1,1) NOT NULL,
	teamNo int NOT NULL,
	teamLetter char(1) NOT NULL,
 CONSTRAINT pk_team_teamId PRIMARY KEY CLUSTERED (teamId)
)
GO
CREATE UNIQUE INDEX idx1_team_teamNo_teamLetter ON dbo.t_team (teamNo, teamLetter)
GO

CREATE TABLE dbo.t_user(
	userId int IDENTITY(1,1) NOT NULL,
	firstName nvarchar(128) NOT NULL,
	lastName nvarchar(128) NOT NULL,
	email varchar(254) NOT NULL,
	phone varchar(17) NULL,
	roleId tinyint NOT NULL,
	teamId int NOT NULL,
	password varbinary(100) NOT NULL,
	isEmailConfirmed bit NOT NULL,
	temporaryPasswordKey uniqueidentifier NULL,
	temporaryPasswordExpirationDate smalldatetime NULL,
	lastPasswordChangedDate smalldatetime NULL,
	lastLoginDate smalldatetime NULL,
	lastLoginIp varchar(15) NULL,
	deviceId varchar(50) NULL,
	appInstanceId uniqueidentifier NULL,
	iPAddress varchar(39) NULL,
	deviceInfo nvarchar(2048) NULL,
 CONSTRAINT pk_user PRIMARY KEY CLUSTERED (userId)
)
GO
CREATE UNIQUE INDEX idx1_user_email ON dbo.t_user (email)
GO
ALTER TABLE dbo.t_user  WITH CHECK ADD CONSTRAINT fk_user_role FOREIGN KEY(roleId) REFERENCES dbo.t_role (roleId)
ALTER TABLE dbo.t_user CHECK CONSTRAINT fk_user_role
GO

ALTER TABLE dbo.t_user  WITH CHECK ADD  CONSTRAINT fk_user_team FOREIGN KEY(teamId) REFERENCES dbo.t_team (teamId)
ALTER TABLE dbo.t_user CHECK CONSTRAINT fk_user_team
GO

CREATE TABLE dbo.t_userPasswordHint(
	userId int NOT NULL,
	passwordHintId tinyint NOT NULL,
	passwordHintAnswer nvarchar(256) NOT NULL,
 CONSTRAINT pk_userPasswordHint_userId_passwordHintId PRIMARY KEY CLUSTERED (userId, passwordHintId)
)
GO
CREATE UNIQUE INDEX idx1_userPasswordHint ON dbo.t_userPasswordHint (userId, passwordHintAnswer)
GO
CREATE UNIQUE INDEX idx2_userPasswordHint ON dbo.t_userPasswordHint (userId, passwordHintId)
GO
ALTER TABLE dbo.t_userPasswordHint  WITH CHECK ADD  CONSTRAINT fk_userPasswordHint_passwordHint FOREIGN KEY(passwordHintId) REFERENCES dbo.t_passwordHint (passwordHintId)
ALTER TABLE dbo.t_userPasswordHint CHECK CONSTRAINT fk_userPasswordHint_passwordHint
GO
ALTER TABLE dbo.t_userPasswordHint  WITH CHECK ADD  CONSTRAINT fk_userPasswordHint_user FOREIGN KEY(userId) REFERENCES dbo.t_user (userId)
ALTER TABLE dbo.t_userPasswordHint CHECK CONSTRAINT fk_userPasswordHint_user
GO

CREATE TABLE dbo.t_url(
	urlId int IDENTITY(1,1) NOT NULL,
	userId int NOT NULL,
	teamId int NOT NULL,
	shortUrl varchar(16) NOT NULL,
	targetUrl nvarchar(2048) NOT NULL,
	startDate smalldatetime NOT NULL,
	endDate smalldatetime NOT NULL,
	redirectionTypeId tinyint NOT NULL,
	preventViewingOnMobile bit NOT NULL,
	randomString varchar(20) NOT NULL,
	shortUrl_randomString  AS (concat(shortUrl,'_',randomString)),
 CONSTRAINT pk_url_urlId PRIMARY KEY CLUSTERED (urlId)
)
GO
CREATE UNIQUE INDEX idx1_url_shortUrl ON dbo.t_url (shortUrl)
GO
CREATE UNIQUE INDEX idx2_url_randomString ON dbo.t_url (randomString)
GO
CREATE UNIQUE INDEX idx3_url_shortUrl_randomString ON dbo.t_url (shortUrl_randomString)
GO
ALTER TABLE dbo.t_url WITH CHECK ADD CONSTRAINT fk_url_redirectionType FOREIGN KEY(redirectionTypeId) REFERENCES dbo.t_redirectionType (redirectionTypeId)
ALTER TABLE dbo.t_url CHECK CONSTRAINT fk_url_redirectionType
GO
ALTER TABLE dbo.t_url WITH CHECK ADD CONSTRAINT fk_url_user FOREIGN KEY(userId) REFERENCES dbo.t_user (userId)
ALTER TABLE dbo.t_url CHECK CONSTRAINT fk_url_user
GO

CREATE TABLE dbo.t_session(
	sessionId uniqueidentifier NOT NULL,
	userId int NOT NULL,
	lastAccessTime smalldatetime NOT NULL,
	ipAddress varchar(15) NOT NULL,
	deviceFingerprint varchar(255) NULL,
 CONSTRAINT pk_session_sessionId PRIMARY KEY CLUSTERED (sessionId)
)
GO
CREATE UNIQUE INDEX xid1_session ON dbo.t_session (userId)
GO
ALTER TABLE dbo.t_session  WITH CHECK ADD CONSTRAINT fk_session_user FOREIGN KEY(userId) REFERENCES dbo.t_user (userId)
ALTER TABLE dbo.t_session CHECK CONSTRAINT fk_session_user
GO

CREATE TABLE dbo.t_mobile_session(
	sessionId uniqueidentifier NOT NULL,
	userId int NOT NULL,
	deviceId varchar(50) NOT NULL,
	appInstanceId uniqueidentifier NOT NULL,
	deviceFingerprint varchar(100) NULL,
	iPAddress varchar(39) NULL,
	deviceInfo nvarchar(2048) NULL,
	refreshToken varchar(200) NULL,
	createdAt datetime2(7) NULL,
	lastActivity datetime2(7) NULL,
	expiresAt datetime2(7) NULL,
	isActive bit NULL,
 CONSTRAINT pk_mobile_session PRIMARY KEY CLUSTERED (sessionId)
)
GO
ALTER TABLE dbo.t_mobile_session ADD CONSTRAINT df_mobile_session_sessionId DEFAULT (newid()) FOR sessionId
GO
ALTER TABLE dbo.t_mobile_session ADD CONSTRAINT df_mobile_session_createdAt DEFAULT (getutcdate()) FOR createdAt
GO
ALTER TABLE dbo.t_mobile_session ADD CONSTRAINT df_mobile_session_lastActivity DEFAULT (getutcdate()) FOR lastActivity
GO
ALTER TABLE dbo.t_mobile_session ADD CONSTRAINT df_mobile_session_isActive DEFAULT ((1)) FOR isActive
GO
ALTER TABLE dbo.t_mobile_session  WITH CHECK ADD CONSTRAINT fk_mobile_session_userId FOREIGN KEY(userId) REFERENCES dbo.t_user (userId)
ALTER TABLE dbo.t_mobile_session CHECK CONSTRAINT fk_mobile_session_userId
GO

CREATE TABLE dbo.t_urlLog (
	logId int IDENTITY(1,1) NOT NULL,
	visitDateTime datetime NOT NULL,
	urlId int NOT NULL,
	referringUrl varchar(2048) NULL,
	ipAddress varchar(64) NULL,
	hostName varchar(128) NULL,
	deviceType varchar(16) NULL,
	browserName varchar(64) NULL,
	browserVersion varchar(64) NULL,
	operatingSystem varchar(32) NULL,
 CONSTRAINT pk_urlLog_logId_urlId PRIMARY KEY CLUSTERED (logId, urlId)
 )
go
ALTER TABLE dbo.t_urlLog  WITH CHECK ADD CONSTRAINT fk_urlLog_url FOREIGN KEY(urlId) REFERENCES dbo.t_url (urlId)
ALTER TABLE dbo.t_urlLog CHECK CONSTRAINT fk_urlLog_url
GO

CREATE TABLE dbo.t_user_delete_log(
	userId int NOT NULL,
	userEmail varchar(255) NOT NULL,
	deletedAt datetime NULL,
	deviceId varchar(50) NULL,
 CONSTRAINT pk_t_user_delete_log PRIMARY KEY CLUSTERED (userId)
)
GO
CREATE INDEX ix_user_delete_log_deletedAt ON dbo.t_user_delete_log (deletedAt)
GO

CREATE TABLE dbo.t_mobile_session_archive (
	sessionId uniqueidentifier NOT NULL,
	userId int NOT NULL,
	deviceId varchar(50) NOT NULL,
	appInstanceId uniqueidentifier NOT NULL,
	deviceFingerprint varchar(100) NULL,
	iPAddress varchar(39) NULL,
	deviceInfo nvarchar(2048) NULL,
	refreshToken varchar(200) NULL,
	createdAt datetime2(7) NULL,
	lastActivity datetime2(7) NULL,
	expiresAt datetime2(7) NULL,
	isActive bit NULL,
 CONSTRAINT pk_mobile_session_archive_sessionId PRIMARY KEY CLUSTERED (sessionId)
)
GO

CREATE TABLE dbo.t_device_certificate (
	certificateId int IDENTITY(1,1) NOT NULL,
	deviceId varchar(50) NOT NULL,
	appInstanceId uniqueidentifier NOT NULL,
	deviceFingerprint varchar(100) NULL,
	publicKey nvarchar(max) NULL,
	certificateHash varchar(100) NULL,
	issuedAt datetime2(7) NULL,
	expiresAt datetime2(7) NULL,
	isActive bit NULL,
 CONSTRAINT pk_device_certificate_certificateId PRIMARY KEY CLUSTERED (certificateId),
 CONSTRAINT uq_device_certificate_deviceId_appInstanceId UNIQUE (deviceId, appInstanceId)
)
GO
ALTER TABLE dbo.t_device_certificate ADD CONSTRAINT df_device_certificate_issuedAt DEFAULT (getutcdate()) FOR issuedAt
GO
ALTER TABLE dbo.t_device_certificate ADD CONSTRAINT df_device_certificate_isActive DEFAULT ((1)) FOR isActive
GO

INSERT INTO t_role (roleId, roleName) VALUES (1, 'Administrator'), (2, 'Team Captain'), (3, 'School/Coach/Advisor');
GO
INSERT INTO t_redirectionType (redirectionTypeId, redirectionType) values (1, 'Simple Redirect'), (2, 'Display using URL encrypted IFRAME');
go
