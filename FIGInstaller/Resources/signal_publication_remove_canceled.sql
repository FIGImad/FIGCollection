-- Optional cleanup ONLY if the earlier publication migration added Signal.Canceled.
-- Stop both services. First reapply the revised source migration on a private source.
-- Run on each affected database using a client that stops on SQL errors.
-- Restores the signal procedure definitions from FIGAutoTrader_Schema.sql.
-- Historical outbox/receipt JSON is retained; the revised code ignores the removed field.
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF ISNULL(OBJECT_DEFINITION(OBJECT_ID('dbo.trigSignalPublication')),'') LIKE '%Canceled%'
    OR ISNULL(OBJECT_DEFINITION(OBJECT_ID('dbo.trigSignalOnChange')),'') LIKE '%Canceled%'
    THROW 51004, 'Apply the revised source migration before removing Signal.Canceled.', 1;
GO
BEGIN TRANSACTION;
GO
IF ISNULL(OBJECT_DEFINITION(OBJECT_ID('dbo.usp_signal_query_last')),'') LIKE '%Canceled%'
    EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[usp_signal_query_last]
(
    @StrategyName varchar(100) = ''''
)
AS
BEGIN
    SET NOCOUNT ON;

    SET @StrategyName = ISNULL(LTRIM(RTRIM(@StrategyName)), '''');

    ;WITH Q AS
    (
        SELECT
             S.[Id]
            ,S.[Strategy]
            ,S.[Tag]
            ,S.[Status]
            ,S.[Side]
            ,S.[StartTime]
            ,S.[StopTime]
            ,S.[StartPrice]
            ,S.[StopPrice]
            ,S.[LastUpdated]
            ,ROW_NUMBER() OVER
            (
                PARTITION BY S.[Strategy]
                ORDER BY S.[StartTime] DESC, S.[Id] DESC
            ) AS RN
        FROM dbo.[Signal] AS S
        WHERE @StrategyName = ''''
           OR S.[Strategy] = @StrategyName
    )
    SELECT
         [Id]
        ,[Strategy]
        ,[Tag]
        ,[Status]
        ,[Side]
        ,[StartTime]
        ,[StopTime]
        ,[StartPrice]
        ,[StopPrice]
        ,[LastUpdated]
    FROM Q
    WHERE RN = 1
    ORDER BY [Strategy], [StartTime] DESC;
END
');
GO

IF ISNULL(OBJECT_DEFINITION(OBJECT_ID('dbo.usp_signal_select')),'') LIKE '%Canceled%'
    EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[usp_signal_select]
(
	@Id int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    SELECT [Id]
          ,[Strategy]
          ,[Tag]
          ,[Status]
          ,[Side]
          ,[StartTime]
          ,[StopTime]
          ,[StartPrice]
          ,[StopPrice]
          ,[LastUpdated]
        FROM [Signal] 
        WHERE (@Id = -1 OR @Id = [Id])
END

');
GO

IF ISNULL(OBJECT_DEFINITION(OBJECT_ID('dbo.usp_signal_upsert')),'') LIKE '%Canceled%'
    EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[usp_signal_upsert]
(
    @IdNew int OUTPUT,
    @Id int,
    @Strategy varchar(100),
    @Tag varchar(50),
    @Status varchar(10),
    @Side decimal(18,4),
    @StartTime int,
    @StopTime int,
    @StartPrice decimal(18,4),
    @StopPrice decimal(18,4),
    @LastUpdated int
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @LastUpdated = -1
    BEGIN
        SET @LastUpdated =
            DATEDIFF(SECOND, ''19700101'', GETUTCDATE());
    END;


    DECLARE @StartTimeUTC datetime =
        [dbo].[fn_ConvertUTCRawTimeToLocalDateTime]
        (
            ISNULL(@StartTime, 0),
            ''UTC''
        );

    DECLARE @StartTimeEST datetime =
        [dbo].[fn_ConvertUTCRawTimeToLocalDateTime]
        (
            ISNULL(@StartTime, 0),
            ''Eastern Standard Time''
        );

    DECLARE @StartTimeDubai datetime =
        [dbo].[fn_ConvertUTCRawTimeToLocalDateTime]
        (
            ISNULL(@StartTime, 0),
            ''Arabian Standard Time''
        );

    DECLARE @StopTimeUTC datetime =
        [dbo].[fn_ConvertUTCRawTimeToLocalDateTime]
        (
            ISNULL(@StopTime, 0),
            ''UTC''
        );

    DECLARE @StopTimeEST datetime =
        [dbo].[fn_ConvertUTCRawTimeToLocalDateTime]
        (
            ISNULL(@StopTime, 0),
            ''Eastern Standard Time''
        );

    DECLARE @StopTimeDubai datetime =
        [dbo].[fn_ConvertUTCRawTimeToLocalDateTime]
        (
            ISNULL(@StopTime, 0),
            ''Arabian Standard Time''
        );

    DECLARE @AlertMessage nvarchar(max) = N'''';
    DECLARE @NL nvarchar(2) =
        NCHAR(13) + NCHAR(10);


    /************************************************************************
        New Signal
    ************************************************************************/

    IF @Id = -1
       OR NOT EXISTS
       (
           SELECT 1
           FROM [dbo].[Signal]
           WHERE [Id] = @Id
       )
    BEGIN
        INSERT INTO [dbo].[Signal]
        (
            [Strategy],
            [Tag],
            [Status],
            [Side],
            [StartTime],
            [StopTime],
            [StartPrice],
            [StopPrice],
            [LastUpdated]
        )
        VALUES
        (
            @Strategy,
            @Tag,
            @Status,
            @Side,
            @StartTime,
            @StopTime,
            @StartPrice,
            @StopPrice,
            @LastUpdated
        );

        SET @IdNew = SCOPE_IDENTITY();


        --------------------------------------------------------------------
        -- START alert
        --------------------------------------------------------------------

        IF @Status = ''START''
        BEGIN
            SET @AlertMessage =
                  N''<b><font color="#FF0000">START SIGNAL</font> — ''
                + CAST(@Strategy AS nvarchar(100))
                + N''</b>''
                + @NL

                + N''Side: <b>''
                + CAST(@Side AS nvarchar(20))
                + N''</b>''
                + @NL
                + @NL

                + N''<b>START</b>''
                + @NL

                + N''UTC:    ''
                + CAST(@StartTimeUTC AS nvarchar(40))
                + @NL

                + N''EST:    ''
                + CAST(@StartTimeEST AS nvarchar(40))
                + @NL

                + N''Dubai:  ''
                + CAST(@StartTimeDubai AS nvarchar(40))
                + @NL
                + @NL

                + N''<b>PRICE</b>''
                + @NL

                + N''Start: ''
                + CAST(ISNULL(@StartPrice, 0) AS nvarchar(20));


            EXEC [dbo].[usp_systemalert_add]
                @Source  = ''SIGNAL'',
                @Level   = ''INF'',
                @Message = @AlertMessage;
        END;
    END

    /************************************************************************
        Existing Signal
    ************************************************************************/

    ELSE
    BEGIN
        UPDATE [dbo].[Signal]
           SET [Strategy]    = @Strategy,
               [Tag]         = @Tag,
               [Status]      = @Status,
               [Side]        = @Side,
               [StartTime]   = @StartTime,
               [StopTime]    = @StopTime,
               [StartPrice]  = @StartPrice,
               [StopPrice]   = @StopPrice,
               [LastUpdated] = @LastUpdated
         WHERE [Id] = @Id;

        DECLARE @RowsUpdated int = @@ROWCOUNT;

        SET @IdNew = @Id;


        --------------------------------------------------------------------
        -- STOP alert
        --------------------------------------------------------------------

        IF @RowsUpdated > 0
           AND @Status = ''STOP''
        BEGIN
            SET @AlertMessage =
                  N''<b><font color="#FF0000">STOP SIGNAL</font> - ''
                + CAST(@Strategy AS nvarchar(100))
                + N''</b>''
                + @NL

                + N''Side: <b>''
                + CAST(@Side AS nvarchar(20))
                + N''</b>''
                + @NL
                + @NL

                + N''<b>START</b>''
                + @NL

                + N''UTC:   ''
                + CAST(@StartTimeUTC AS nvarchar(40))
                + @NL

                + N''EST:   ''
                + CAST(@StartTimeEST AS nvarchar(40))
                + @NL

                + N''Dubai: ''
                + CAST(@StartTimeDubai AS nvarchar(40))
                + @NL
                + @NL

                + N''<b>STOP</b>''
                + @NL

                + N''UTC:   ''
                + CAST(@StopTimeUTC AS nvarchar(40))
                + @NL

                + N''EST:   ''
                + CAST(@StopTimeEST AS nvarchar(40))
                + @NL

                + N''Dubai: ''
                + CAST(@StopTimeDubai AS nvarchar(40))
                + @NL
                + @NL

                + N''<b>PRICE</b>''
                + @NL

                + N''Start: ''
                + CAST(ISNULL(@StartPrice, 0) AS nvarchar(20))

                + N'' - Stop: ''
                + CAST(ISNULL(@StopPrice, 0) AS nvarchar(20))

                + N'' <b>(''
                + CAST
                  (
                      ISNULL(@StopPrice, 0)
                      - ISNULL(@StartPrice, 0)
                      AS nvarchar(20)
                  )
                + N'')</b>'';


            EXEC [dbo].[usp_systemalert_add]
                @Source  = ''SIGNAL'',
                @Level   = ''INF'',
                @Message = @AlertMessage;
        END;
    END;

    RETURN @IdNew;
END;
');
GO
IF COL_LENGTH('dbo.Signal','Canceled') IS NOT NULL
BEGIN
    DECLARE @Default sysname;
    SELECT @Default=dc.name FROM sys.default_constraints dc
    JOIN sys.columns c ON c.object_id=dc.parent_object_id AND c.column_id=dc.parent_column_id
    WHERE c.object_id=OBJECT_ID('dbo.Signal') AND c.name='Canceled';
    IF @Default IS NOT NULL
    BEGIN
        DECLARE @DropDefault nvarchar(400)=N'ALTER TABLE dbo.Signal DROP CONSTRAINT '+QUOTENAME(@Default);
        EXEC(@DropDefault);
    END;
    ALTER TABLE dbo.Signal DROP COLUMN Canceled;
END;
COMMIT;
GO
