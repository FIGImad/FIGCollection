SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetRoleClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RoleId] [nvarchar](450) NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetRoles](
	[Id] [nvarchar](450) NOT NULL,
	[Name] [nvarchar](256) NULL,
	[NormalizedName] [nvarchar](256) NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [nvarchar](450) NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserLogins](
	[LoginProvider] [nvarchar](450) NOT NULL,
	[ProviderKey] [nvarchar](450) NOT NULL,
	[ProviderDisplayName] [nvarchar](max) NULL,
	[UserId] [nvarchar](450) NOT NULL,
 CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY CLUSTERED 
(
	[LoginProvider] ASC,
	[ProviderKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserRoles](
	[UserId] [nvarchar](450) NOT NULL,
	[RoleId] [nvarchar](450) NOT NULL,
 CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUsers](
	[Id] [nvarchar](450) NOT NULL,
	[UserName] [nvarchar](256) NULL,
	[NormalizedUserName] [nvarchar](256) NULL,
	[Email] [nvarchar](256) NULL,
	[NormalizedEmail] [nvarchar](256) NULL,
	[EmailConfirmed] [bit] NOT NULL,
	[PasswordHash] [nvarchar](max) NULL,
	[SecurityStamp] [nvarchar](max) NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
	[PhoneNumber] [nvarchar](max) NULL,
	[PhoneNumberConfirmed] [bit] NOT NULL,
	[TwoFactorEnabled] [bit] NOT NULL,
	[LockoutEnd] [datetimeoffset](7) NULL,
	[LockoutEnabled] [bit] NOT NULL,
	[AccessFailedCount] [int] NOT NULL,
	[FirstName] [nvarchar](128) NULL,
	[LastName] [nvarchar](128) NULL,
 CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserTokens](
	[UserId] [nvarchar](450) NOT NULL,
	[LoginProvider] [nvarchar](450) NOT NULL,
	[Name] [nvarchar](450) NOT NULL,
	[Value] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[LoginProvider] ASC,
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ValidationToken](
	[ValidationTokenId] [bigint] NOT NULL,
	[Token] [varchar](4000) NOT NULL,
	[Timestamp] [datetime] NOT NULL,
	[UserId] [nvarchar](450) NOT NULL,
 CONSTRAINT [PK_ValidationToken] PRIMARY KEY CLUSTERED 
(
	[ValidationTokenId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
INSERT [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'343d18ad-e436-49ff-a0e2-8622e111842d', N'Bot', N'BOT', N'0822a48f-9fb5-4bf9-8dd9-9bf1e7ea6e7b')
GO
INSERT [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'46fb813d-1c6c-4599-bd11-29ae7c17c9c0', N'Operator', N'OPERATOR', N'18eace55-58c9-4ed7-bc39-409d9a48dd83')
GO
INSERT [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'48c5c239-001d-4ea2-ab54-14abf16d2671', N'Broker', N'BROKER', N'89df552b-4cda-4871-ac3c-5b12f1493ce4')
GO
INSERT [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'88226b5e-11e7-4211-98d6-b9c91512040c', N'SVC', N'SVC', N'3fadde80-c1ba-4ac2-8b1d-9c4428531243')
GO
INSERT [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'8c9e8a47-9a57-4707-8442-49427b89ebfb', N'LinkAPI', N'LINKAPI', N'd54085ed-192b-4512-b17b-f2661004307c')
GO
INSERT [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'e9951ab7-68f2-49af-b2ae-2f833f55f0a6', N'Administrator', N'ADMINISTRATOR', N'205bd782-57e0-4ad5-a765-90dbf2ab1438')
GO
INSERT [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'f6e7127a-d5d5-47bd-a218-1717376059b0', N'ATSAPI', N'ATSAPI', N'e29cf047-0a9f-4733-b786-810935599451')
GO
INSERT [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'fa0e9ce2-1210-4183-8444-ae0a31a5fa4d', N'SuperAdmin', N'SUPERADMIN', N'f8dad996-59fb-42ef-ba94-24059a713778')
GO
INSERT [dbo].[AspNetUserRoles] ([UserId], [RoleId]) VALUES (N'6673f5c2-1594-4898-8c6a-961d4e13a7c4', N'48c5c239-001d-4ea2-ab54-14abf16d2671')
GO
INSERT [dbo].[AspNetUserRoles] ([UserId], [RoleId]) VALUES (N'6673f5c2-1594-4898-8c6a-961d4e13a7c4', N'88226b5e-11e7-4211-98d6-b9c91512040c')
GO
INSERT [dbo].[AspNetUserRoles] ([UserId], [RoleId]) VALUES (N'e7af38ee-098a-419f-a661-608790807f36', N'88226b5e-11e7-4211-98d6-b9c91512040c')
GO
INSERT [dbo].[AspNetUsers] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [FirstName], [LastName]) VALUES (N'6673f5c2-1594-4898-8c6a-961d4e13a7c4', N'ATS', N'ATS', N'imad.ansari@gmail.com', N'IMAD.ANSARI@GMAIL.COM', 1, N'AQAAAAIAAYagAAAAEIi9kDdUawGWS+o+QKIF70lo5TZlaGsoxe3sqfV9ga3isOLH3pGAMF1aghUCs189+w==', N'MURQPZO6MFVGQKYQOG7DV6FWVP5UDVE7', N'1d10bee7-eabb-447a-908a-cf38b8604f6c', N'', 0, 0, CAST(N'2026-03-01T07:16:29.8180000+00:00' AS DateTimeOffset), 1, 0, N'', N'')
GO
INSERT [dbo].[AspNetUsers] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [FirstName], [LastName]) VALUES (N'e7af38ee-098a-419f-a661-608790807f36', N'SVC', N'SVC', N'imad.ansari@gmail.com', N'IMAD.ANSARI@GMAIL.COM', 1, N'AQAAAAIAAYagAAAAEL7jPT78HZRzb/UUj1YRTbYuW0c76ReAWka7US34rGV/wAvS82k3GD9oZCKni1724A==', N'4ONZWV6GCVAWRCRTIJJBW6FGNKROGD22', N'cc556c80-f95c-47bc-b8bb-b224f03c3b0e', N'', 0, 0, CAST(N'2025-04-28T23:33:36.5410000+00:00' AS DateTimeOffset), 1, 0, N'', N'')
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_role_select_all_json] 
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT 
	   [Id]
      ,[Name]
      ,[NormalizedName]
  FROM [AspNetRoles]
  FOR JSON AUTO
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_user_query] 
	@UserName nvarchar(256)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT 
		 Id
		,UserName
		,Email
		,EmailConfirmed
		,PhoneNumber
		,PhoneNumberConfirmed
		,LockoutEnd
		,LockoutEnabled
		,AccessFailedCount
		,FirstName
		,LastName
		,stuff((
			  SELECT ',' + R.Name 
				  FROM     AspNetUserRoles UR 
						INNER JOIN AspNetRoles R ON UR.RoleId = R.Id
				  WHERE  (UR.UserId = AspNetUsers.Id)
						  ORDER BY UR.UserId
				   for xml path('')
				   ),1,1,'') as Roles
	FROM AspNetUsers
	WHERE Upper([UserName]) = trim(Upper(@UserName))
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_user_select] 
	@Id nvarchar(450)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT 
		 Id
		,UserName
		,Email
		,EmailConfirmed
		,PhoneNumber
		,PhoneNumberConfirmed
		,LockoutEnd
		,LockoutEnabled
		,AccessFailedCount
		,FirstName
		,LastName
		,stuff((
			  SELECT ',' + R.Name 
				  FROM     AspNetUserRoles UR 
						INNER JOIN AspNetRoles R ON UR.RoleId = R.Id
				  WHERE  (UR.UserId = AspNetUsers.Id)
						  ORDER BY UR.UserId
				   for xml path('')
				   ),1,1,'') as Roles
	FROM AspNetUsers
	WHERE Id = @Id
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_user_select_all] 
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT 
		 Id
		,UserName
		,Email
		,EmailConfirmed
		,PhoneNumber
		,PhoneNumberConfirmed
		,LockoutEnd
		,LockoutEnabled
		,AccessFailedCount
		,FirstName
		,LastName
		,stuff((
			  SELECT ',' + R.Name 
				  FROM     AspNetUserRoles UR 
						INNER JOIN AspNetRoles R ON UR.RoleId = R.Id
				  WHERE  (UR.UserId = AspNetUsers.Id)
						  ORDER BY UR.UserId
				   for xml path('')
				   ),1,1,'') as Roles
	FROM AspNetUsers
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_user_select_all_json] 
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	Select usrs.Id
			,usrs.UserName
			,usrs.NormalizedUserName
			,usrs.Email
			,usrs.NormalizedEmail
			,usrs.EmailConfirmed
			,usrs.PasswordHash
			,usrs.SecurityStamp
			,usrs.ConcurrencyStamp
			,usrs.PhoneNumber
			,usrs.PhoneNumberConfirmed
			,usrs.LockoutEnd
			,usrs.LockoutEnabled
			,usrs.AccessFailedCount
			,usrs.FirstName
			,usrs.LastName, 
			Roles.Name
	FROM [dbo].[AspNetUsers] AS usrs
	inner join [dbo].[AspNetUserRoles] AS usrRoles on usrs.id = usrRoles.UserId
	inner join [dbo].[AspNetRoles] AS Roles on Roles.Id = usrRoles.RoleId
	FOR JSON AUTO
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_user_update]
	  @Id nvarchar(450) OUTPUT
	, @FirstName nvarchar(128)
	, @LastName nvarchar(128)
	, @PhoneNumber nvarchar(128)
	, @PhoneNumberConfirmed bit
	, @UserName nvarchar(256)
	, @Email nvarchar(256)
	, @EmailConfirmed bit
	, @PasswordHash nvarchar(max)
	, @SecurityStamp nvarchar(max)
	, @ConcurrencyStamp nvarchar(max)
	, @AccessFailedCount int
	, @LockoutEnabled bit
	, @LockoutEnd datetimeoffset(7)
AS
BEGIN
	DECLARE @NormalizedUserName nvarchar(256);
	DECLARE @NormalizedEmail nvarchar(256);
	SET @NormalizedUserName = upper(@UserName);
	SET @NormalizedEmail = upper(@Email);

	UPDATE [AspNetUsers]
	SET
		  Id = @Id
		, FirstName = @FirstName
		, LastName = @LastName
		, PhoneNumber = @PhoneNumber
		, PhoneNumberConfirmed = @PhoneNumberConfirmed
		, UserName = @UserName
		, NormalizedUserName = @NormalizedUserName
		, Email = @Email
		, NormalizedEmail = @NormalizedEmail
		, EmailConfirmed = @EmailConfirmed
		, PasswordHash = @PasswordHash
		, SecurityStamp = @SecurityStamp
		, ConcurrencyStamp = @ConcurrencyStamp
		, AccessFailedCount = @AccessFailedCount
		, LockoutEnabled = @LockoutEnabled
		, LockoutEnd = @LockoutEnd
	WHERE Id = @Id
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_ValidationToken_Delete]
(
	@ValidationTokenId bigint
)
AS
BEGIN
	DELETE FROM ValidationToken
	WHERE ValidationTokenId = @ValidationTokenId
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_ValidationToken_DeleteAll]
(
	@UserId nvarchar(128)
)
AS
BEGIN
	DELETE FROM ValidationToken
	WHERE UserId = @UserId
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_ValidationToken_Insert]	
(
	@ValidationTokenId bigint
	, @Token varchar(4000)
	, @UserId nvarchar(128)
)
AS
BEGIN
	-- EXEC uspPurgeTable 'ValidationToken', @SettingTenantId, 'ValidationToken', 75;

	INSERT INTO ValidationToken (
		ValidationTokenId
		, Token
		, UserId
		, [Timestamp]
	)
	VALUES (
		@ValidationTokenId
		, @Token
		, @UserId
		, getdate()
	)
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_ValidationToken_Purge]
(
	@Hours int
)
AS
BEGIN
	DELETE FROM ValidationToken
	WHERE DATEDIFF(HOUR, [Timestamp], getdate()) > @Hours
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_ValidationToken_Select]
(
	@ValidationTokenId bigint
)
AS
BEGIN
    -- SET NOCOUNT ON added to prevent extra result sets from
    -- interfering with SELECT statements.
    SET NOCOUNT ON

    SELECT 
		ValidationTokenId
		, Token
		, UserId
		, [Timestamp]
	FROM ValidationToken
	WHERE ValidationTokenId = @ValidationTokenId
END
GO
