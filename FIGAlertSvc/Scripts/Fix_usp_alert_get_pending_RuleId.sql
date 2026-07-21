-- Run this script against the FIGAlert database.
-- PendingAlertRS expects RuleId as the eighth result column.

CREATE OR ALTER PROCEDURE [dbo].[usp_alert_get_pending]
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
		a.[MatchedRuleId]    AS RuleId
	FROM  [dbo].[Alert]                       a
	INNER JOIN [dbo].[RecipientSubscription] rs ON rs.[AlertRuleId] = a.[MatchedRuleId]
	INNER JOIN [dbo].[Recipient]              r  ON r.[Id]           = rs.[RecipientId]
	WHERE a.[SentAt]          IS NULL
	  AND a.[MatchedRuleId]   IS NOT NULL
	  AND a.[FriendlyMessage] IS NOT NULL
	  AND (
			(rs.[AlertMethods] & 1 = 1 AND r.[Email] <> '')
		 OR (rs.[AlertMethods] & 2 = 2 AND r.[PushoverKey] IS NOT NULL AND r.[PushoverKey] <> '')
		  )
	ORDER BY a.[Id];
END
GO
