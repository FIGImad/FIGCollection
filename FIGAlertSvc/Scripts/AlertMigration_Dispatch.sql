-- Legacy script: run AlertMigration_RecipientEnabled.sql last on the current schema.
-- Ensure the enabled field exists even when this older dispatch script is used first.
IF COL_LENGTH(N'dbo.Recipient', N'Enabled') IS NULL
    ALTER TABLE dbo.Recipient ADD Enabled bit NOT NULL CONSTRAINT DF_Recipient_Enabled DEFAULT (1) WITH VALUES;
GO
-- =============================================================================
-- FIG Alert Service - Dispatch Feature Migration
-- Run this script once against the FIGAlerts database before deploying the
-- updated service binaries.
-- =============================================================================

-- Step 1: Add dispatch-tracking columns to Alert table
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Alert') AND name = 'MatchedRuleId')
	ALTER TABLE [dbo].[Alert] ADD [MatchedRuleId] [int] NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Alert') AND name = 'FriendlyMessage')
	ALTER TABLE [dbo].[Alert] ADD [FriendlyMessage] [varchar](max) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Alert') AND name = 'SentAt')
	ALTER TABLE [dbo].[Alert] ADD [SentAt] [datetime2](7) NULL;
GO

-- Step 2: Update matched rule and resolved friendly message on an alert
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.usp_alert_update_match', 'P') IS NOT NULL
	DROP PROCEDURE [dbo].[usp_alert_update_match];
GO

CREATE PROCEDURE [dbo].[usp_alert_update_match]
(
	@Id              int,
	@MatchedRuleId   int,
	@FriendlyMessage varchar(max)
)
AS
BEGIN
	SET NOCOUNT ON;
	UPDATE [dbo].[Alert]
	SET    [MatchedRuleId]   = @MatchedRuleId,
		   [FriendlyMessage] = @FriendlyMessage
	WHERE  [Id] = @Id;
END
GO

-- Step 3: Get all unsent alerts with recipient information
--
--   Returns one row per (alert, recipient) combination.
--   Only includes recipients who:
--     - are subscribed to the matched alert rule
--     - have a valid delivery channel configured (email or pushover key)
--     - and whose subscription AlertMethods bitmask enables that channel
--       (bit 1 = EMAIL, bit 2 = PUSHOVER)
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.usp_alert_get_pending', 'P') IS NOT NULL
	DROP PROCEDURE [dbo].[usp_alert_get_pending];
GO

CREATE PROCEDURE [dbo].[usp_alert_get_pending]
AS
BEGIN
	SET NOCOUNT ON;

	SELECT
		a.[Id]               AS AlertId,
		a.[FriendlyMessage],
		rs.[AlertMethods],
		r.[Id]               AS RecipientId,
		r.[Name]             AS RecipientName,
		r.[Email],
		r.[PushoverKey],
		a.[MatchedRuleId]    AS RuleId,
		r.[Enabled] AS RecipientEnabled
	FROM  [dbo].[Alert]                  a
	INNER JOIN [dbo].[RecipientSubscription] rs ON rs.[AlertRuleId] = a.[MatchedRuleId]
	INNER JOIN [dbo].[Recipient]             r  ON r.[Id]           = rs.[RecipientId]
	WHERE r.[Enabled] = 1 AND a.[SentAt]          IS NULL
	  AND a.[MatchedRuleId]   IS NOT NULL
	  AND a.[FriendlyMessage] IS NOT NULL
	  AND (
			(rs.[AlertMethods] & 1 = 1 AND r.[Email]       <> '')
		 OR (rs.[AlertMethods] & 2 = 2 AND r.[PushoverKey] IS NOT NULL AND r.[PushoverKey] <> '')
		  )
	ORDER BY a.[Id];
END
GO

-- Step 4: Mark a single alert as sent
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.usp_alert_mark_sent', 'P') IS NOT NULL
	DROP PROCEDURE [dbo].[usp_alert_mark_sent];
GO

CREATE PROCEDURE [dbo].[usp_alert_mark_sent]
(
	@Id int
)
AS
BEGIN
	SET NOCOUNT ON;
	UPDATE [dbo].[Alert]
	SET    [SentAt] = SYSUTCDATETIME()
	WHERE  [Id] = @Id;
END
GO
