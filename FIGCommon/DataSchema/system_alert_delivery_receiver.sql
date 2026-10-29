-- Run against the Alert database before deploying the updated services.
IF COL_LENGTH('dbo.Alert', 'SystemAlertId') IS NULL
    ALTER TABLE dbo.Alert ADD SystemAlertId int NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Alert') AND name = 'UX_Alert_SystemAlertReceipt')
    CREATE UNIQUE INDEX UX_Alert_SystemAlertReceipt ON dbo.Alert(ServiceId, SystemAlertId) WHERE SystemAlertId IS NOT NULL;
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_alert_upsert]
(
    @IdNew    int OUTPUT,
    @Id       int,
    @RawTime  int,
    @MSec     int,
    @ServiceId      varchar(50),
    @ServiceName    varchar(256),
    @ServiceRole    int,
    @ServiceAddress varchar(256),
    @LogName        varchar(256),
    @Source         varchar(50),
    @Level          varchar(20),
    @Message        varchar(max),
    @ExceptionText  varchar(max) = NULL,
    @IsSystemAlert bit = 0,
    @SystemAlertId int = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @IdNew = NULL;
    BEGIN TRANSACTION;
    IF @SystemAlertId IS NOT NULL
    BEGIN
        SELECT @IdNew = Id FROM dbo.Alert WITH (UPDLOCK, HOLDLOCK)
        WHERE ServiceId = @ServiceId AND SystemAlertId = @SystemAlertId;
        IF @IdNew > 0
        BEGIN
            COMMIT;
            RETURN @IdNew;
        END;
    END;

    SELECT @IdNew = [Id] FROM [dbo].[Alert] WHERE [Id] = @Id;
    IF (@@ROWCOUNT = 0)
    BEGIN
        -- Does not exist, insert new
        INSERT INTO [dbo].[Alert] (
             [RawTime]
            ,[MSec]
            ,[ServiceId]
            ,[ServiceName]
            ,[ServiceRole]
            ,[ServiceAddress]
            ,[LogName]
            ,[Source]
            ,[Level]
            ,[Message]
            ,[ExceptionText]
            ,[SystemAlertId]
            ,[IsSystemAlert]
        )
        VALUES (
             @RawTime
            ,@MSec
            ,@ServiceId
            ,@ServiceName
            ,@ServiceRole
            ,@ServiceAddress
            ,@LogName
            ,@Source
            ,@Level
            ,@Message
            ,@ExceptionText
            ,@SystemAlertId
            ,@IsSystemAlert
        )
        SET @IdNew = SCOPE_IDENTITY();
    END ELSE BEGIN
        -- Exists, update values
        UPDATE [dbo].[Alert] SET
             [RawTime]       = @RawTime
            ,[MSec]          = @MSec
            ,[ServiceId]     = @ServiceId
            ,[ServiceName]   = @ServiceName
            ,[ServiceRole]   = @ServiceRole
            ,[ServiceAddress]= @ServiceAddress
            ,[LogName]       = @LogName
            ,[Source]        = @Source
            ,[Level]         = @Level
            ,[Message]       = @Message
            ,[IsSystemAlert] = @IsSystemAlert
            ,[ExceptionText] = @ExceptionText
        WHERE [Id] = @IdNew;
    END
    COMMIT;
    RETURN @IdNew;
END

GO

