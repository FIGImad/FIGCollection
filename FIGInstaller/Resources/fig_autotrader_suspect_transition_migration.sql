-- Run against the existing AutoTrader database. Does not reset any BOT status.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[usp_autotrade_signal_upsert]
(
    @IdNew int OUTPUT,
    @Id int,
    @AutoTradeId int,
    @SignalId int,
    @Tag varchar(50),
    @BotId int,
    @TickerId int,
    @OpenStatus varchar(20),
    @OpenStatusCode int,
    @OpenAvgPrice decimal(18,4),
    @CloseStatus varchar(20),
    @CloseStatusCode int,
    @CloseAvgPrice decimal(18,4),
    @OrigQty int,
    @FilledQty int,
    @ManualQty int,
    @LastUpdated int
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @LastUpdated = -1
    BEGIN
        SET @LastUpdated =
            DATEDIFF(SECOND, '19700101', GETUTCDATE());
    END;

    ------------------------------------------------------------------------
    -- Get Bot details
    ------------------------------------------------------------------------

    DECLARE @BotAccountId varchar(50);
    DECLARE @BotServiceId varchar(50);

    SELECT
        @BotAccountId = [AccountId],
        @BotServiceId = [BrokerServiceId]
    FROM [dbo].[Bot]
    WHERE [Id] = @BotId;


    ------------------------------------------------------------------------
    -- Determine whether AutoTradeSignal exists
    ------------------------------------------------------------------------

    SELECT @IdNew = [Id]
    FROM [dbo].[AutoTradeSignal]
    WHERE [Id] = @Id;

    IF @@ROWCOUNT = 0
    BEGIN
        --------------------------------------------------------------------
        -- Does not exist - insert
        --------------------------------------------------------------------

        INSERT INTO [dbo].[AutoTradeSignal]
        (
            [AutoTradeId],
            [SignalId],
            [Tag],
            [BotId],
            [TickerId],
            [OpenStatus],
            [OpenStatusCode],
            [OpenAvgPrice],
            [CloseStatus],
            [CloseStatusCode],
            [CloseAvgPrice],
            [OrigQty],
            [FilledQty],
            [ManualQty],
            [LastUpdated]
        )
        VALUES
        (
            @AutoTradeId,
            @SignalId,
            @Tag,
            @BotId,
            @TickerId,
            @OpenStatus,
            @OpenStatusCode,
            @OpenAvgPrice,
            @CloseStatus,
            @CloseStatusCode,
            @CloseAvgPrice,
            @OrigQty,
            @FilledQty,
            @ManualQty,
            @LastUpdated
        );

        SET @IdNew = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        --------------------------------------------------------------------
        -- Existing AutoTradeSignal
        --------------------------------------------------------------------

        -- Capture the actual previous row atomically with the update.
        DECLARE @PreviousSignal TABLE (CloseStatus varchar(20));

        UPDATE [dbo].[AutoTradeSignal] WITH (ROWLOCK)
           SET [AutoTradeId]     = @AutoTradeId,
               [SignalId]        = @SignalId,
               [Tag]             = @Tag,
               [BotId]           = @BotId,
               [TickerId]        = @TickerId,
               [OpenStatus]      = @OpenStatus,
               [OpenStatusCode]  = @OpenStatusCode,
               [OpenAvgPrice]    = @OpenAvgPrice,
               [CloseStatus]     = @CloseStatus,
               [CloseStatusCode] = @CloseStatusCode,
               [CloseAvgPrice]   = @CloseAvgPrice,
               [OrigQty]         = @OrigQty,
               [FilledQty]       = @FilledQty,
               [ManualQty]       = @ManualQty,
               [LastUpdated]     = @LastUpdated
        OUTPUT deleted.[CloseStatus] INTO @PreviousSignal (CloseStatus)
         WHERE [Id] = @IdNew;


        --------------------------------------------------------------------
        -- Determine actual position quantity
        --------------------------------------------------------------------

        DECLARE @PositionQty int =
            CASE
                WHEN @OpenStatus = 'FILLED_MANUALLY'
                    THEN @ManualQty
                ELSE @FilledQty
            END;


        --------------------------------------------------------------------
        -- Only a new failure episode should invalidate a manually reactivated bot
        --------------------------------------------------------------------

        IF EXISTS (SELECT 1 FROM @PreviousSignal
                   WHERE ISNULL(CloseStatus, '') NOT IN ('FAILED', 'FILLED_PARTIALLY_FIN'))
           AND @CloseStatus IN ('FAILED', 'FILLED_PARTIALLY_FIN')
           AND ISNULL(@PositionQty, 0) <> 0
        BEGIN
            UPDATE [dbo].[Bot] WITH (ROWLOCK)
               SET [Status] = 'SUSPECT',
                   [LastUpdated] =
                       DATEDIFF(SECOND, '19700101', GETUTCDATE())
             WHERE [Id] = @BotId
               AND [Status] = 'ACTIVE';


            ----------------------------------------------------------------
            -- Alert only if ACTIVE -> SUSPECT actually occurred
            ----------------------------------------------------------------

            IF @@ROWCOUNT > 0
            BEGIN
                DECLARE @AlertMessage nvarchar(max);

                SET @AlertMessage =
                      N'Bot "'
                    + ISNULL(CAST(@BotAccountId AS nvarchar(50)), N'UNKNOWN')
                    + N'" Id('
                    + CAST(@BotId AS nvarchar(20))
                    + N') changed to SUSPECT. Manual intervention required. '
                    + N'AutoTradeId='
                    + CAST(@AutoTradeId AS nvarchar(20))
                    + N', SignalId='
                    + CAST(@SignalId AS nvarchar(20))
                    + N', CloseStatus='
                    + ISNULL(CAST(@CloseStatus AS nvarchar(20)), N'NULL')
                    + N', PositionQty='
                    + CAST(ISNULL(@PositionQty, 0) AS nvarchar(20));

                EXEC [dbo].[usp_systemalert_add]
                    @Source  = 'AUTOTRADE_SIGNAL',
                    @Level   = 'CRITICAL',
                    @Message = @AlertMessage;
            END;
        END;
    END;

    RETURN @IdNew;
END;
GO
