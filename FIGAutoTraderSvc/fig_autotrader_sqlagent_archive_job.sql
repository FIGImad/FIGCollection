/*
	fig_autotrader_sqlagent_archive_job.sql
	----------------------------------------
	Creates (or recreates) the SQL Server Agent job that calls
	usp_autotrade_signal_archive_process every 5 minutes.

	Prerequisites
	-------------
	- SQL Server Agent service must be running on the target instance.
	- The executing login must be a member of the sysadmin server role,
	  OR hold the SQLAgentOperatorRole / SQLAgentUserRole role in msdb.
	- The stored procedure usp_autotrade_signal_archive_process must
	  already exist in the FIGAutoTrader database.

	Idempotent
	----------
	Safe to run multiple times: the existing job and its schedule are
	dropped and recreated so the definition stays in sync with this script.
*/

USE [msdb];
GO

-- ----------------------------------------------------------------
-- Drop the job if it already exists (including its owned steps)
-- ----------------------------------------------------------------
IF EXISTS (
	SELECT 1
	FROM msdb.dbo.sysjobs
	WHERE [name] = N'FIGAutoTrader - Archive Completed Signals'
)
BEGIN
	EXEC msdb.dbo.sp_delete_job
		@job_name              = N'FIGAutoTrader - Archive Completed Signals',
		@delete_unused_schedule = 0; -- keep orphan schedules; cleaned up below
END;
GO

-- ----------------------------------------------------------------
-- Drop the schedule if it already exists
-- ----------------------------------------------------------------
IF EXISTS (
	SELECT 1
	FROM msdb.dbo.sysschedules
	WHERE [name] = N'FIGAutoTrader - Every 5 Minutes'
)
BEGIN
	EXEC msdb.dbo.sp_delete_schedule
		@schedule_name = N'FIGAutoTrader - Every 5 Minutes',
		@force_delete  = 1; -- detach from any other job before deleting
END;
GO

-- ----------------------------------------------------------------
-- Create the job
-- ----------------------------------------------------------------
EXEC msdb.dbo.sp_add_job
	@job_name              = N'FIGAutoTrader - Archive Completed Signals',
	@enabled               = 1,
	@description           = N'Archives completed AutoTradeSignal records into AutoTradeSignalArchive. Invokes usp_autotrade_signal_archive_process every 5 minutes.',
	@start_step_id         = 1,
	@notify_level_eventlog = 2; -- write to Windows Event Log on failure
GO

-- ----------------------------------------------------------------
-- Add the single job step
-- ----------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobstep
	@job_name          = N'FIGAutoTrader - Archive Completed Signals',
	@step_name         = N'Archive Completed Signals',
	@step_id           = 1,
	@subsystem         = N'TSQL',
	@command           = N'EXEC [dbo].[usp_autotrade_signal_archive_process];',
	@database_name     = N'FIGAutoTrader',
	@on_success_action = 1,  -- 1 = Quit the job reporting success
	@on_fail_action    = 2;  -- 2 = Quit the job reporting failure
GO

-- ----------------------------------------------------------------
-- Create the schedule: every 5 minutes, all day, every day
-- ----------------------------------------------------------------
EXEC msdb.dbo.sp_add_schedule
	@schedule_name        = N'FIGAutoTrader - Every 5 Minutes',
	@enabled              = 1,
	@freq_type            = 4,      -- 4  = Daily
	@freq_interval        = 1,      -- Every 1 day
	@freq_subday_type     = 4,      -- 4  = Minutes
	@freq_subday_interval = 5,      -- Every 5 minutes
	@active_start_time    = 0,      -- 00:00:00  (start of day)
	@active_end_time      = 235959; -- 23:59:59  (end of day)
GO

-- ----------------------------------------------------------------
-- Attach the schedule to the job
-- ----------------------------------------------------------------
EXEC msdb.dbo.sp_attach_schedule
	@job_name      = N'FIGAutoTrader - Archive Completed Signals',
	@schedule_name = N'FIGAutoTrader - Every 5 Minutes';
GO

-- ----------------------------------------------------------------
-- Register the job on the local server
-- ----------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobserver
	@job_name    = N'FIGAutoTrader - Archive Completed Signals',
	@server_name = N'(local)';
GO
