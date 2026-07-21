USE [UniPrint]
GO

/***** BEGIN - DROP Procedures *****/
DECLARE @schema VARCHAR(128), @name VARCHAR(128), @sqlCommand NVARCHAR(1000), @Rows INT = 0, @i INT = 1;
DECLARE @t TABLE(RowID INT IDENTITY(1,1), SchemaName VARCHAR(128), ObjectName VARCHAR(128));
 
INSERT INTO @t(SchemaName, ObjectName)
SELECT r.ROUTINE_SCHEMA, r.SPECIFIC_NAME
	FROM INFORMATION_SCHEMA.ROUTINES r
	WHERE r.ROUTINE_TYPE = 'PROCEDURE' AND r.SPECIFIC_NAME in (
		'uspDeleteExpiredLicenseSessions',
		'uspDeleteLicense',
		'uspDeleteLicenseSession',
		'uspInsertLicense',
		'uspInsertLicenseSession',
		'uspSelectCountLicenseSessions',
		'uspSelectLicenseInfos',
		'uspSelectLicenseSessionInfos',
		'uspSelectTopLicenseSession',
		'uspSelectValidLicenses',
		'uspSelectValidLicensesByType',
		'uspUpdateLicenseSession',
		'uspUpdateLicenseSessionExpiry'	
	)
	ORDER BY r.ROUTINE_SCHEMA, r.ROUTINE_NAME
 
SELECT @Rows = (SELECT COUNT(RowID) FROM @t), @i = 1;
 
WHILE (@i <= @Rows) 
BEGIN
    SELECT 	@sqlCommand = 'DROP PROC [' + t.SchemaName + '].[' + t.ObjectName + '];', 
			@schema = t.SchemaName, @name = t.ObjectName 
		FROM @t t WHERE RowID = @i;
		
    EXEC sp_executesql @sqlCommand;        
    PRINT 'Dropped PROC: [' + @schema + '].[' + @name + ']';    
    SET @i = @i + 1;
END
GO
/***** END - DROP Procedures *****/

/***** BEGIN - DROP TABLES *****/
DECLARE @sqlCommand NVARCHAR(1000);
DECLARE @Rows INT = 0, @i INT = 1,  @schema VARCHAR(128), @name VARCHAR(128);
DECLARE @FKRows INT = 0, @j INT = 1, @FKScheme VARCHAR(128), @FKName VARCHAR(128);
DECLARE @tablesToDelete TABLE(RowID INT IDENTITY(1,1), SchemaName NVARCHAR(10), TableName NVARCHAR(256));
DECLARE @FKToDelete TABLE(RowID INT IDENTITY(1,1), FKName NVARCHAR(256), FKSchema NVARCHAR(256), DeleteCmd NVARCHAR(1024));


INSERT INTO @tablesToDelete(SchemaName, TableName) 
SELECT t.TABLE_SCHEMA, t.TABLE_NAME 
	FROM INFORMATION_SCHEMA.TABLES t 
	WHERE t.TABLE_TYPE = 'BASE TABLE' AND t.TABLE_NAME IN (
		'__MigrationHistory',
		'AspNetRoles',
		'AspNetUserClaims',
		'AspNetUserLogins',
		'AspNetUserRoles',
		'AspNetUsers',
		'License',
		'LicenseSession'
	)

SELECT @Rows = (SELECT COUNT(RowID) FROM @tablesToDelete), @i = 1;

WHILE (@i <= @Rows) 
BEGIN
	-- drop Forign Keys
	DELETE FROM @FKToDelete
	INSERT INTO @FKToDelete(FKSchema, FKName, DeleteCmd)
		SELECT OBJECT_SCHEMA_NAME(k.parent_object_id) AS FKSchema, 
			   k.name AS FKName, 
			   ('ALTER TABLE [' +  OBJECT_SCHEMA_NAME(k.parent_object_id) + '].[' + OBJECT_NAME(k.parent_object_id) + '] DROP CONSTRAINT [' + k.name + ']') AS DeleteCmd
			FROM sys.foreign_keys k
			WHERE referenced_object_id in (SELECT object_id(t.TableName) FROM @tablesToDelete AS t)

	SELECT @FKRows = (SELECT COUNT(RowID) FROM @FKToDelete), @j = 1;
	WHILE (@j <= @FKRows) 
	BEGIN
		-- drop Forign Keys
		SELECT 	@sqlCommand = fk.DeleteCmd, @FKName = fk.FKName, @FKScheme = fk.FKSchema
			FROM @FKToDelete fk WHERE RowID = @j;

		PRINT 'DROPPING: ' + @sqlCommand;
		EXEC sp_executesql @sqlCommand;        
		PRINT 'Dropped Forign Key: [' + @FKScheme + '].[' + @FKName + ']';    
		SET @j = @j + 1;
	END

	-- dropping table
	SELECT 	@sqlCommand = 'DROP TABLE [' + t.SchemaName + '].[' + t.TableName + '];', 
		@schema = t.SchemaName, @name = t.TableName 
	FROM @tablesToDelete t WHERE RowID = @i;
		
	PRINT 'DROPPING: ' + @sqlCommand;

    EXEC sp_executesql @sqlCommand;        
    PRINT 'Dropped Table: [' + @schema + '].[' + @name + ']';    
    SET @i = @i + 1;
END
GO
/***** END - DROP TABLES *****/

/**** BEGIN - CREATE TABLES ***/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__MigrationHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ContextKey] [nvarchar](300) NOT NULL,
	[Model] [varbinary](max) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK_dbo.__MigrationHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC,
	[ContextKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetRoles]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetRoles](
	[Id] [nvarchar](128) NOT NULL,
	[Name] [nvarchar](256) NOT NULL,
	[Kind] [int] NULL,
	[Discriminator] [nvarchar](128) NOT NULL,
 CONSTRAINT [PK_dbo.AspNetRoles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetUserClaims]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [nvarchar](128) NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_dbo.AspNetUserClaims] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetUserLogins]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserLogins](
	[LoginProvider] [nvarchar](128) NOT NULL,
	[ProviderKey] [nvarchar](128) NOT NULL,
	[UserId] [nvarchar](128) NOT NULL,
 CONSTRAINT [PK_dbo.AspNetUserLogins] PRIMARY KEY CLUSTERED 
(
	[LoginProvider] ASC,
	[ProviderKey] ASC,
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetUserRoles]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserRoles](
	[UserId] [nvarchar](128) NOT NULL,
	[RoleId] [nvarchar](128) NOT NULL,
 CONSTRAINT [PK_dbo.AspNetUserRoles] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetUsers]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUsers](
	[Id] [nvarchar](128) NOT NULL,
	[Email] [nvarchar](256) NULL,
	[EmailConfirmed] [bit] NOT NULL,
	[PasswordHash] [nvarchar](max) NULL,
	[SecurityStamp] [nvarchar](max) NULL,
	[PhoneNumber] [nvarchar](max) NULL,
	[PhoneNumberConfirmed] [bit] NOT NULL,
	[TwoFactorEnabled] [bit] NOT NULL,
	[LockoutEndDateUtc] [datetime] NULL,
	[LockoutEnabled] [bit] NOT NULL,
	[AccessFailedCount] [int] NOT NULL,
	[UserName] [nvarchar](256) NOT NULL,
 CONSTRAINT [PK_dbo.AspNetUsers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[License]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[License](
	[LicenseId] [int] IDENTITY(1,1) NOT NULL,
	[LicenseType] [int] NOT NULL,
	[SerialNumber] [varchar](20) NOT NULL,
	[MachineName] [nvarchar](64) NOT NULL,
	[ServerIp] [varchar](16) NOT NULL,
	[VerMajor] [smallint] NOT NULL,
	[VerMinor] [smallint] NOT NULL,
	[SessionLimit] [int] NOT NULL,
	[Flags] [bigint] NOT NULL,
	[Expiry] [smalldatetime] NOT NULL,
	[Hash] [char](32) NOT NULL,
 CONSTRAINT [PK_License] PRIMARY KEY CLUSTERED 
(
	[LicenseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[LicenseSession]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LicenseSession](
	[LicenseSessionId] [bigint] IDENTITY(1,1) NOT NULL,
	[LicenseId] [int] NOT NULL,
	[DomainUserHash] [int] NOT NULL,
	[DomainName] [nvarchar](256) NOT NULL,
	[UserName] [nvarchar](256) NOT NULL,
	[MachineName] [nvarchar](64) NOT NULL,
	[NodeId] [char](36) NOT NULL,
	[SessionId] [int] NOT NULL,
	[Expiry] [smalldatetime] NOT NULL,
 CONSTRAINT [PK_LicenseSession] PRIMARY KEY CLUSTERED 
(
	[LicenseSessionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [RoleNameIndex]    Script Date: 25/02/2020 8:36:55 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [RoleNameIndex] ON [dbo].[AspNetRoles]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_UserId]    Script Date: 25/02/2020 8:36:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_UserId] ON [dbo].[AspNetUserClaims]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_UserId]    Script Date: 25/02/2020 8:36:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_UserId] ON [dbo].[AspNetUserLogins]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_RoleId]    Script Date: 25/02/2020 8:36:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_RoleId] ON [dbo].[AspNetUserRoles]
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_UserId]    Script Date: 25/02/2020 8:36:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_UserId] ON [dbo].[AspNetUserRoles]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UserNameIndex]    Script Date: 25/02/2020 8:36:55 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex] ON [dbo].[AspNetUsers]
(
	[UserName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_License]    Script Date: 25/02/2020 8:36:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_License] ON [dbo].[License]
(
	[LicenseType] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_UserLicenseSession_DUHash]    Script Date: 25/02/2020 8:36:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_UserLicenseSession_DUHash] ON [dbo].[LicenseSession]
(
	[LicenseId] ASC,
	[DomainUserHash] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_UserLicenseSession_Expiry]    Script Date: 25/02/2020 8:36:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_UserLicenseSession_Expiry] ON [dbo].[LicenseSession]
(
	[LicenseId] ASC,
	[Expiry] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
ALTER TABLE [dbo].[License] ADD  CONSTRAINT [DF_License_Hash]  DEFAULT (' ') FOR [Hash]
GO
ALTER TABLE [dbo].[AspNetUserClaims]  WITH CHECK ADD  CONSTRAINT [FK_dbo.AspNetUserClaims_dbo.AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserClaims] CHECK CONSTRAINT [FK_dbo.AspNetUserClaims_dbo.AspNetUsers_UserId]
GO
ALTER TABLE [dbo].[AspNetUserLogins]  WITH CHECK ADD  CONSTRAINT [FK_dbo.AspNetUserLogins_dbo.AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserLogins] CHECK CONSTRAINT [FK_dbo.AspNetUserLogins_dbo.AspNetUsers_UserId]
GO
ALTER TABLE [dbo].[AspNetUserRoles]  WITH CHECK ADD  CONSTRAINT [FK_dbo.AspNetUserRoles_dbo.AspNetRoles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[AspNetRoles] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserRoles] CHECK CONSTRAINT [FK_dbo.AspNetUserRoles_dbo.AspNetRoles_RoleId]
GO
ALTER TABLE [dbo].[AspNetUserRoles]  WITH CHECK ADD  CONSTRAINT [FK_dbo.AspNetUserRoles_dbo.AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserRoles] CHECK CONSTRAINT [FK_dbo.AspNetUserRoles_dbo.AspNetUsers_UserId]
GO
ALTER TABLE [dbo].[LicenseSession]  WITH CHECK ADD  CONSTRAINT [FK_LicenseSession_License] FOREIGN KEY([LicenseId])
REFERENCES [dbo].[License] ([LicenseId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[LicenseSession] CHECK CONSTRAINT [FK_LicenseSession_License]
GO
/**** END - CREATE TABLES ***/

/**** BEGIN - CREATE VIEWS ***/
/**** END - CREATE VIEWS ***/

/**** BEGIN - CREATE PROCEDURES ***/
/****** Object:  StoredProcedure [dbo].[uspDeleteExpiredLicenseSessions]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[uspDeleteExpiredLicenseSessions]
	@Expiry smalldatetime
AS
BEGIN
	DELETE FROM LicenseSession
	WHERE Expiry < @Expiry;
END
GO
/****** Object:  StoredProcedure [dbo].[uspDeleteLicense]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[uspDeleteLicense]
	@LicenseId int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DELETE FROM License
	WHERE LicenseId = @LicenseId;
END
GO
/****** Object:  StoredProcedure [dbo].[uspDeleteLicenseSession]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[uspDeleteLicenseSession]
	@LicenseSessionId bigint
AS
BEGIN
	DELETE FROM LicenseSession
	WHERE LicenseSessionId = @LicenseSessionId;
END
GO
/****** Object:  StoredProcedure [dbo].[uspInsertLicense]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[uspInsertLicense] 
	@LicenseId int OUT 
	, @LicenseType int
	, @SerialNumber varchar(20)
	, @MachineName nvarchar(64)
	, @ServerIp varchar(16)
	, @VerMajor smallint
	, @VerMinor smallint
	, @SessionLimit int
	, @Flags bigint
	, @Expiry smalldatetime	
	, @Hash char(32)
AS
BEGIN	
	IF (@LicenseId IS NULL)
	BEGIN
		INSERT INTO License (
			LicenseType
			, SerialNumber 
			, MachineName
			, ServerIp
			, VerMajor
			, VerMinor
			, SessionLimit
			, Flags
			, Expiry
			, [Hash]
		)
		VALUES (
			@LicenseType
			, @SerialNumber 
			, @MachineName
			, @ServerIp
			, @VerMajor
			, @VerMinor
			, @SessionLimit
			, @Flags
			, @Expiry
			, @Hash
		)
		SET @LicenseId = SCOPE_IDENTITY();	
	END
END
GO
/****** Object:  StoredProcedure [dbo].[uspInsertLicenseSession]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[uspInsertLicenseSession] 
	@LicenseSessionId bigint OUT
	, @LicenseId int
	, @DomainUserHash int
	, @DomainName nvarchar(256)
	, @UserName nvarchar(256)
	, @MachineName nvarchar(64)
	, @NodeId char(36)
	, @SessionId int
	, @Expiry smalldatetime
AS
BEGIN
	INSERT INTO LicenseSession (
		LicenseId
		, DomainUserHash
		, DomainName
		, UserName
		, MachineName
		, NodeId
		, SessionId
		, Expiry
	)
	VALUES (
		@LicenseId
		, @DomainUserHash
		, @DomainName
		, @UserName
		, @MachineName
		, @NodeId
		, @SessionId
		, @Expiry
	);
	SET @LicenseSessionId = SCOPE_IDENTITY();	
END
GO
/****** Object:  StoredProcedure [dbo].[uspSelectCountLicenseSessions]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[uspSelectCountLicenseSessions] 
	@LicenseId int
	, @Expiry smalldatetime
AS
;
BEGIN
    SELECT COUNT(*) FROM LicenseSession WHERE LicenseId = @LicenseId AND Expiry >= @Expiry;
END
GO
/****** Object:  StoredProcedure [dbo].[uspSelectLicenseInfos]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[uspSelectLicenseInfos]
	@Expiry smalldatetime
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DELETE FROM LicenseSession WHERE Expiry < @Expiry;

	SELECT 
		l.LicenseId
		, l.LicenseType
		, COUNT(ls.LicenseSessionId)
	FROM License l 
	LEFT JOIN LicenseSession ls ON ls.LicenseId = l.LicenseId 
	WHERE l.Expiry >= @Expiry
	GROUP BY l.licenseId, l.LicenseType 
END
GO
/****** Object:  StoredProcedure [dbo].[uspSelectLicenseSessionInfos]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[uspSelectLicenseSessionInfos]
	@Expiry smalldatetime
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DELETE FROM LicenseSession WHERE Expiry < @Expiry;

	SELECT
		l.LicenseId
		, l.LicenseType
		, l.Flags
		, ls.DomainName
		, ls.UserName
		, ls.MachineName
		, ls.NodeId
		, ls.SessionId
		, ls.Expiry
	FROM LicenseSession ls
	INNER JOIN License l ON l.LicenseId = ls.LicenseId;
END
GO
/****** Object:  StoredProcedure [dbo].[uspSelectTopLicenseSession]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[uspSelectTopLicenseSession] 
	@LicenseId int
	, @DomainUserHash int
	, @Expiry smalldatetime
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT TOP(1)
	    LicenseSessionId
		, LicenseId
		, DomainUserHash
		, DomainName
		, UserName
		, MachineName
		, NodeId
		, SessionId
		, Expiry
	FROM LicenseSession
	WHERE LicenseId = @LicenseId AND DomainUserHash = @DomainUserHash AND Expiry >= @Expiry;
END
GO
/****** Object:  StoredProcedure [dbo].[uspSelectValidLicenses]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[uspSelectValidLicenses] 
	@Expiry smalldatetime
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    SELECT
		LicenseId
		, LicenseType
		, SerialNumber
		, MachineName
		, ServerIp
		, VerMajor
		, VerMinor
		, SessionLimit
		, Flags
		, Expiry
		, [Hash]
	FROM License
	WHERE Expiry >= @Expiry;
END
GO
/****** Object:  StoredProcedure [dbo].[uspSelectValidLicensesByType]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[uspSelectValidLicensesByType] 
	@LicenseType int 
	, @Expiry smalldatetime
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    SELECT
		LicenseId
		, LicenseType
		, SerialNumber
		, MachineName
		, ServerIp
		, VerMajor
		, VerMinor
		, SessionLimit
		, Flags
		, Expiry
		, [Hash]
	FROM License
	WHERE LicenseType = @LicenseType AND Expiry >= @Expiry;
END
GO
/****** Object:  StoredProcedure [dbo].[uspUpdateLicenseSession]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[uspUpdateLicenseSession] 
	@LicenseSessionId bigint
AS
BEGIN
	DELETE FROM LicenseSession
	WHERE LicenseSessionId = @LicenseSessionId;
END
GO
/****** Object:  StoredProcedure [dbo].[uspUpdateLicenseSessionExpiry]    Script Date: 25/02/2020 8:36:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[uspUpdateLicenseSessionExpiry] 
	@LicenseSessionId bigint
	, @Expiry smalldatetime
AS
BEGIN
	UPDATE LicenseSession
	SET Expiry = @Expiry
	WHERE  LicenseSessionId = @LicenseSessionId;
END
GO
/**** END - CREATE PROCEDURES ***/
