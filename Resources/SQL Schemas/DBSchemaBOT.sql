SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[fn_ConvertLocalDateTimeToRawUTC] (@Datetime DATETIME)
RETURNS BIGINT
AS
BEGIN
    DECLARE @LocalTimeOffset BIGINT;
    DECLARE @DateTimeUTC DATETIME;
	SET @DateTimeUTC =  DATEADD(hour,DATEDIFF (hour, GETDATE(), GETUTCDATE()), @Datetime);
	RETURN DATEDIFF(SECOND, '1970-01-01 00:00:00', @DateTimeUTC);
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[fn_ConvertToLocalDateTime] (@Datetime BIGINT)
RETURNS DATETIME
AS
BEGIN
    DECLARE @LocalTimeOffset BIGINT
           ,@AdjustedLocalDatetime BIGINT;
    SET @LocalTimeOffset = DATEDIFF(second,GETDATE(),GETUTCDATE())
    SET @AdjustedLocalDatetime = @Datetime - @LocalTimeOffset
    RETURN (SELECT DATEADD(second,@AdjustedLocalDatetime, CAST('1970-01-01 00:00:00' AS datetime)))
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BOTInstruction](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[BotId] [varchar](50) NOT NULL,
	[RefId] [int] NOT NULL,
	[RawTime] [int] NOT NULL,
	[Ticker] [varchar](25) NOT NULL,
	[Action] [varchar](50) NOT NULL,
	[UpperPrice] [decimal](18, 4) NOT NULL,
	[LowerPrice] [decimal](18, 4) NOT NULL,
	[AwayAdj] [decimal](18, 4) NOT NULL,
	[Qty] [int] NOT NULL,
	[QtyLimit] [int] NOT NULL,
	[Spread] [decimal](18, 4) NOT NULL,
	[TakeProfit] [decimal](18, 4) NOT NULL,
	[TimeStamp] [datetime] NOT NULL,
 CONSTRAINT [PK_BOTInstruction] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO






CREATE VIEW [dbo].[ViewBotInstructions]
AS
	SELECT 
		 [Id]
		,[BotId]
		,[RawTime]
		,dateadd(hour, +4, dateadd(S, [RawTime], '1970-01-01')) AS [time]
		,[Ticker]
		,LEFT([Ticker], CASE WHEN charindex(' ', [Ticker]) <= 0 THEN len([Ticker]) ELSE charindex(' ', [Ticker]) - 1 END) AS TickerRaw
		,[Action]
		,(CASE WHEN [UpperPrice] = 0 THEN 1 ELSE [UpperPrice] END) AS [UpperPrice]  
		,(CASE WHEN [LowerPrice] = 0 THEN 1 ELSE [LowerPrice] END) AS [LowerPrice]  
		,(CASE WHEN [AwayAdj] = 0 THEN 1 ELSE [AwayAdj] END) AS [AwayAdj]  
		,(CASE WHEN [Qty] = 0 THEN 1 ELSE [QTY] END) AS [QTY]
		,(CASE WHEN [QtyLimit] = 0 THEN 1 ELSE [QtyLimit] END) AS [QtyLimit]
		,(CASE WHEN [Spread] = 0 THEN 1 ELSE [Spread] END) AS [Spread]
		,(CASE WHEN [TakeProfit] = 0 THEN 1 ELSE [TakeProfit] END) AS [TakeProfit]
		,[TimeStamp]
	FROM [BOTInstruction]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[account_desc](
	[account_desc_id] [int] NOT NULL,
	[account_desc_ib_id] [nvarchar](50) NULL,
	[account_desc_nickname] [nvarchar](50) NULL,
 CONSTRAINT [PK_account_desc] PRIMARY KEY CLUSTERED 
(
	[account_desc_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ArchiveBotOrderInstruction](
	[Id] [int] NOT NULL,
	[BotId] [varchar](50) NOT NULL,
	[RefId] [varchar](36) NOT NULL,
	[RawTime] [int] NOT NULL,
	[Ticker] [varchar](25) NOT NULL,
	[Action] [varchar](50) NOT NULL,
	[OrderTag] [varchar](50) NOT NULL,
	[OrderType] [varchar](50) NOT NULL,
	[Qty] [int] NOT NULL,
	[Price] [decimal](18, 4) NOT NULL,
	[TimeStamp] [datetime] NOT NULL,
	[OrderStatus] [int] NOT NULL,
	[QtyFilled] [int] NOT NULL,
	[FillPrice] [decimal](18, 4) NULL,
	[IBKR_Order_id] [int] NULL,
	[LastRawTime] [int] NOT NULL,
 CONSTRAINT [PK_ArchiveBotOrderInstruction] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AutomatedBot_trade_capture](
	[id_bot_trades] [int] NOT NULL,
	[bot_acc_summary_id] [nvarchar](50) NULL,
	[bot_trades_symbol] [nvarchar](50) NULL,
	[bot_trades_localsymbol] [nvarchar](50) NULL,
	[bot_trades_sectype] [nvarchar](50) NULL,
	[bot_trades_exchange] [nvarchar](50) NULL,
	[bot_trades_price] [float] NULL,
	[bot_trades_multiplier] [float] NULL,
	[bot_trades_shares] [float] NULL,
	[bot_trades_cumQty] [float] NULL,
	[bot_trades_side] [nvarchar](10) NULL,
	[bot_trades_execid] [nvarchar](max) NULL,
	[bot_trades_commission] [float] NULL,
	[bot_trades_commission_execId] [nvarchar](max) NULL,
	[bot_trades_date] [date] NULL,
	[bot_trades_time] [nvarchar](50) NULL,
	[BotOrderInstruction_id] [int] NULL,
	[IBKR_Order_id] [int] NULL,
 CONSTRAINT [PK_AutomatedBot_trade_capture] PRIMARY KEY CLUSTERED 
(
	[id_bot_trades] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[bot_acc_summary](
	[id_bot_acc_summary] [int] NOT NULL,
	[bot_acc_summary_id] [nvarchar](50) NULL,
	[bot_acc_AccountType] [nvarchar](50) NULL,
	[bot_acc_Cushion] [float] NULL,
	[bot_acc_DayTradesRemaining] [float] NULL,
	[bot_acc_LookAheadNextChange] [float] NULL,
	[bot_acc_AccruedCash] [float] NULL,
	[bot_acc_AvailableFunds] [float] NULL,
	[bot_acc_BuyingPower] [float] NULL,
	[bot_acc_EquityWithLoanValue] [float] NULL,
	[bot_acc_ExcessLiquidity] [float] NULL,
	[bot_acc_FullAvailableFunds] [float] NULL,
	[bot_acc_FullExcessLiquidity] [float] NULL,
	[bot_acc_FullInitMarginReq] [float] NULL,
	[bot_acc_FullMaintMarginReq] [float] NULL,
	[bot_acc_GrossPositionValue] [float] NULL,
	[bot_acc_InitMarginReq] [float] NULL,
	[bot_acc_LookAheadAvailableFunds] [float] NULL,
	[bot_acc_LookAheadExcessLiquidity] [float] NULL,
	[bot_acc_LookAheadInitMarginReq] [float] NULL,
	[bot_acc_LookAheadMaintMarginReq] [float] NULL,
	[bot_acc_MaintMarginReq] [float] NULL,
	[bot_acc_NetLiquidation] [float] NULL,
	[bot_acc_PreviousDayEquityWithLoanValue] [float] NULL,
	[bot_acc_TotalCashValue] [float] NULL,
 CONSTRAINT [PK_bot_acc_sumary] PRIMARY KEY CLUSTERED 
(
	[id_bot_acc_summary] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BotOrderInstruction](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[BotId] [varchar](50) NOT NULL,
	[RefId] [varchar](36) NOT NULL,
	[RawTime] [int] NOT NULL,
	[Ticker] [varchar](25) NOT NULL,
	[Action] [varchar](50) NOT NULL,
	[OrderTag] [varchar](50) NOT NULL,
	[OrderType] [varchar](50) NOT NULL,
	[Qty] [int] NOT NULL,
	[Price] [decimal](18, 4) NOT NULL,
	[TimeStamp] [datetime] NOT NULL,
	[OrderStatus] [int] NOT NULL,
	[QtyFilled] [int] NOT NULL,
	[FillPrice] [decimal](18, 4) NULL,
	[IBKR_Order_id] [int] NULL,
	[LastRawTime] [int] NOT NULL,
 CONSTRAINT [PK_BotOrderInstruction] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[employee_tbl](
	[employee_id] [int] NOT NULL,
	[employee_full_name] [nvarchar](max) NULL,
	[employee_user_name] [nvarchar](max) NULL,
	[employee_password] [nvarchar](max) NULL,
	[employee_type] [nvarchar](max) NULL,
	[employee_status] [int] NULL,
 CONSTRAINT [PK_employee_tbl] PRIMARY KEY CLUSTERED 
(
	[employee_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Event](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EventType] [varchar](50) NOT NULL,
	[EventId] [varchar](100) NOT NULL,
	[Message] [varchar](256) NOT NULL,
	[TimeStamp] [datetime] NOT NULL,
 CONSTRAINT [PK_Event] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [IX_Event_Name] UNIQUE NONCLUSTERED 
(
	[EventType] ASC,
	[EventId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ib_bot_trade_capture](
	[id_bot_trades] [int] NOT NULL,
	[bot_acc_summary_id] [nvarchar](50) NULL,
	[bot_trades_symbol] [nvarchar](50) NULL,
	[bot_trades_localsymbol] [nvarchar](50) NULL,
	[bot_trades_sectype] [nvarchar](50) NULL,
	[bot_trades_exchange] [nvarchar](50) NULL,
	[bot_trades_price] [float] NULL,
	[bot_trades_multiplier] [float] NULL,
	[bot_trades_shares] [float] NULL,
	[bot_trades_cumQty] [float] NULL,
	[bot_trades_side] [nvarchar](10) NULL,
	[bot_trades_execid] [nvarchar](max) NULL,
	[bot_trades_commission] [float] NULL,
	[bot_trades_commission_execId] [nvarchar](max) NULL,
	[bot_trades_date] [date] NULL,
	[bot_trades_time] [nvarchar](50) NULL,
 CONSTRAINT [PK_ib_bot_trade_capture] PRIMARY KEY CLUSTERED 
(
	[id_bot_trades] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[IB_Bot_Trade_Capture_New](
	[id_bot_trades] [int] NOT NULL,
	[bot_acc_summary_id] [nvarchar](50) NULL,
	[bot_trades_symbol] [nvarchar](50) NULL,
	[bot_trades_localsymbol] [nvarchar](50) NULL,
	[bot_trades_sectype] [nvarchar](50) NULL,
	[bot_trades_exchange] [nvarchar](50) NULL,
	[bot_trades_price] [float] NULL,
	[bot_trades_multiplier] [float] NULL,
	[bot_trades_shares] [float] NULL,
	[bot_trades_cumQty] [float] NULL,
	[bot_trades_side] [nvarchar](10) NULL,
	[bot_trades_execid] [nvarchar](max) NULL,
	[bot_trades_commission] [float] NULL,
	[bot_trades_commission_execId] [nvarchar](max) NULL,
	[bot_trades_date] [date] NULL,
	[bot_trades_time] [nvarchar](50) NULL,
	[IBKR_Order_id] [int] NULL,
	[IBKR_Excution_Time] [nvarchar](50) NULL,
 CONSTRAINT [PK_IB_Bot_Trade_Capture_New] PRIMARY KEY CLUSTERED 
(
	[id_bot_trades] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ib_con_type_table](
	[ID] [int] NOT NULL,
	[connection_type_desc] [nvarchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ib_con_way_table](
	[ID] [int] NOT NULL,
	[connection_way_desc] [nvarchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ib_pnl_summary](
	[bot_pnl_id] [int] NOT NULL,
	[bot_pnl_acc_id] [nvarchar](50) NULL,
	[bot_pnl_dailyPnL] [float] NULL,
	[bot_pnl_realizedPnL] [float] NULL,
	[bot_pnl_unrealizedPnL] [float] NULL,
	[bot_pnl_date] [date] NULL,
	[bot_pnl_time] [nvarchar](50) NULL,
 CONSTRAINT [PK_ib_pnl_summary] PRIMARY KEY CLUSTERED 
(
	[bot_pnl_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ib_settings](
	[ID] [int] NULL,
	[ib_connection_way] [nvarchar](max) NULL,
	[ib_connection_type] [nvarchar](max) NULL,
	[ib_host_ip] [nvarchar](max) NULL,
	[ib_host_port] [nvarchar](max) NULL,
	[ib_client_id] [int] NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Instruction_Log](
	[Instruction_id] [int] NOT NULL,
	[BotId] [nvarchar](max) NULL,
	[Instruction_timeStamp] [nvarchar](max) NULL,
 CONSTRAINT [PK_Instruction_Log] PRIMARY KEY CLUSTERED 
(
	[Instruction_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[system_iceberg_tbl](
	[system_iceberg_id] [int] NOT NULL,
	[system_iceberg_Bot_id] [nvarchar](max) NULL,
	[system_iceberg_ticker_symbol] [nvarchar](max) NULL,
	[system_iceberg_ticker_value] [int] NULL,
 CONSTRAINT [PK_system_iceberg_tbl] PRIMARY KEY CLUSTERED 
(
	[system_iceberg_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[system_limit](
	[system_limit_id] [int] NOT NULL,
	[system_limit_Bot_id] [nvarchar](max) NULL,
	[system_limit_ticker_symbol] [nvarchar](max) NULL,
	[system_limit_ticker_limit] [int] NULL,
	[system_limit_ticker_Short_limit] [int] NULL,
 CONSTRAINT [PK_system_limit] PRIMARY KEY CLUSTERED 
(
	[system_limit_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ticker_description](
	[ticker_id] [int] NOT NULL,
	[ticker_Bot_id] [nvarchar](max) NULL,
	[ticker_desc] [nvarchar](max) NULL,
	[ticker_symbol] [nvarchar](max) NULL,
	[ticker_symbol_exch] [nvarchar](max) NULL,
	[ticker_symbol_primary_exch] [nvarchar](max) NULL,
	[ticker_symbol_SecType] [nvarchar](max) NULL,
	[ticker_symbol_Currency] [nvarchar](max) NULL,
	[ticker_symbol_exp_date] [nvarchar](max) NULL,
	[ticker_symbol_local_Name] [nvarchar](max) NULL,
	[ticker_symbol_MinTick] [float] NULL,
	[ticker_conid] [int] NULL,
 CONSTRAINT [PK_ticker_description] PRIMARY KEY CLUSTERED 
(
	[ticker_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TWAP_StAlgo_Settings](
	[stalgo_id] [int] NOT NULL,
	[stalgo_bot_id] [nvarchar](max) NULL,
	[stalgo_ticker_symbol] [nvarchar](max) NULL,
	[stalgo_strategy_name] [nvarchar](max) NULL,
	[stalgo_starting_time] [int] NULL,
	[stalgo_ending_time] [int] NULL,
	[stalgo_past_end_time] [int] NULL,
 CONSTRAINT [PK_TWAP_StAlgo_Settings] PRIMARY KEY CLUSTERED 
(
	[stalgo_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[ArchiveBotOrderInstruction] ADD  CONSTRAINT [DF_ArchiveBotOrderInstruction_OrderStatus]  DEFAULT ((0)) FOR [OrderStatus]
GO
ALTER TABLE [dbo].[ArchiveBotOrderInstruction] ADD  CONSTRAINT [DF_ArchiveBotOrderInstruction_QtyFilled]  DEFAULT ((0)) FOR [QtyFilled]
GO
ALTER TABLE [dbo].[ArchiveBotOrderInstruction] ADD  CONSTRAINT [DF_ArchiveBotOrderInstruction_LastRawTime]  DEFAULT ((0)) FOR [LastRawTime]
GO
ALTER TABLE [dbo].[BotOrderInstruction] ADD  CONSTRAINT [DF_BotOrderInstruction_OrderStatus]  DEFAULT ((0)) FOR [OrderStatus]
GO
ALTER TABLE [dbo].[BotOrderInstruction] ADD  CONSTRAINT [DF_BotOrderInstruction_QtyFilled_1]  DEFAULT ((0)) FOR [QtyFilled]
GO
ALTER TABLE [dbo].[BotOrderInstruction] ADD  CONSTRAINT [DF_BotOrderInstruction_LastRawTime]  DEFAULT ((0)) FOR [LastRawTime]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[CHECK_ORDER_STATUS_AND_UPDATE_IT]
	@BotId nvarchar(50),
	@Ticker nvarchar(25),
	@Status_Description nvarchar(max),
	@IBKR_Order_id int
AS
BEGIN
	
	Declare @max_order_id int

	--if @Status_Description = 'Canceled'
	--	Begin

	--		Select @max_order_id = max(Id) from BotOrderInstruction 
	--		Where BotId = @BotId and Ticker = @Ticker and OrderStatus <> 0 and OrderStatus = 1

	--		Update BotOrderInstruction set OrderStatus = -1
	--		Where Id = @max_order_id and OrderStatus = 1

	--		goto myend
	--	End

	if @Status_Description = 'Submitted' or @Status_Description = 'PreSubmitted' or @Status_Description = 'PendingSubmit'
		Begin

			Select @max_order_id = max(Id) from BotOrderInstruction 
			Where BotId = @BotId and Ticker = @Ticker and OrderStatus = 1 and IBKR_Order_id is null

			Update BotOrderInstruction set 
			OrderStatus = 1 ,
			IBKR_Order_id = @IBKR_Order_id
			Where Id = @max_order_id

			goto myend
		End

	if @Status_Description = 'Filled'
		Begin

			Select @max_order_id = max(Id) from BotOrderInstruction 
			Where BotId = @BotId and Ticker = @Ticker and OrderStatus = 1 and IBKR_Order_id = @IBKR_Order_id

			Update BotOrderInstruction set OrderStatus = 3
			Where Id = @max_order_id and OrderStatus = 1

			goto myend
		End


	-- (-1)
myend:

	--Select * from BotOrderInstruction Where 
	--Id = @max_order_id
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[DELETE_TICKER_DESCRIPTION]
	@ticker_Bot_id nvarchar(max),
	@ticker_symbol nvarchar(max)
AS
BEGIN
	
	Delete from ticker_description where
	(ticker_Bot_id = @ticker_Bot_id and ticker_symbol = @ticker_symbol)

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[DELETE_TWAP_STALGO]
	@stalgo_bot_id nvarchar(max),
	@stalgo_ticker_symbol nvarchar(max)
AS
BEGIN
	SET NOCOUNT ON;

	Delete from TWAP_StAlgo_Settings where 
	stalgo_bot_id = @stalgo_bot_id and stalgo_ticker_symbol = @stalgo_ticker_symbol

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GET_All_CON_TYPE]
	
AS
BEGIN
	
	Select * from ib_con_type_table

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GET_All_CON_WAY]
	
AS
BEGIN
	
	Select * from ib_con_way_table
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GET_ALL_OPEN_ORDER_IN_DB]
	@botid varchar(50),
	@Ticker varchar(50),
	@OrderStatus int
AS
BEGIN

	Declare @Id int,
			@BotId_db varchar(50),
			@RefId varchar(36),
			@RawTime int,
			@Ticker_db varchar(25),
			@Action varchar(50),
			@OrderTag varchar(50),
			@OrderType varchar(50),
			@Qty int,
			@Price decimal(18,4),
			@TimeStamp datetime,
			@OrderStatus_db int,
			@QtyFilled int,
			@FillPrice decimal(18,4),
			@IBKR_Order_id int,
			@LastRawTime int
			--@isorder_done int
	
	--Set @isorder_done = -1;

	CREATE TABLE #temp_table (  Id int,
								BotId_db varchar(50),
								RefId varchar(36),
								RawTime int,
								Ticker_db varchar(25),
								[Action] varchar(50),
								OrderTag varchar(50),
								OrderType varchar(50),
								Qty int,
								Price decimal(18,4),
								[TimeStamp] datetime,
								OrderStatus_db int,
								QtyFilled int,
								FillPrice decimal(18,4),
								IBKR_Order_id int,
								LastRawTime int);

	Declare open_order_cursor CURSOR For
		Select * from BotOrderInstruction where 
		botid = @BotId and Ticker = @Ticker and (OrderStatus = 1 or OrderStatus = 2)
					
		Open open_order_cursor
		Fetch next from open_order_cursor
		into @Id ,@BotId_db ,@RefId ,@RawTime ,@Ticker_db ,@Action ,@OrderTag ,
			 @OrderType ,@Qty ,@Price ,@TimeStamp ,@OrderStatus_db ,@QtyFilled ,
			 @FillPrice ,@IBKR_Order_id ,@LastRawTime 
		while @@FETCH_STATUS = 0
			Begin
				
				--Set @isorder_done = -1;

				--Select @isorder_done = id from BotOrderInstruction where 
				--RefId = @RefId and OrderStatus = 3 and OrderTag = @OrderTag

				if NOT EXISTS (Select * from BotOrderInstruction where RefId = @RefId and OrderStatus = 3 and OrderTag = @OrderTag)
					Begin
						insert into #temp_table values(  @Id ,@BotId_db ,@RefId ,@RawTime ,@Ticker_db ,@Action ,@OrderTag ,
												 @OrderType ,@Qty ,@Price ,@TimeStamp ,@OrderStatus_db ,@QtyFilled ,
												 @FillPrice ,@IBKR_Order_id ,@LastRawTime)
					End
				
				Fetch next from open_order_cursor
				into     @Id ,@BotId_db ,@RefId ,@RawTime ,@Ticker_db ,@Action ,@OrderTag ,
						 @OrderType ,@Qty ,@Price ,@TimeStamp ,@OrderStatus_db ,@QtyFilled ,
						 @FillPrice ,@IBKR_Order_id ,@LastRawTime 
			End
		Close open_order_cursor
		Deallocate open_order_cursor

	Select * from #temp_table
	Drop table #temp_table
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GET_BOT_BULK_INSTRUCTION]
	@Ticker_Symbole nvarchar(25),
	@botid nvarchar(50)
AS
BEGIN

	Declare @max_bot_id int,
			@order_time datetime,
			@order_date date,
			@now_date date,
			@order_hour int,
			@now_hour int,
			@order_minute int,
			@now_minute int,
			@minute_deferanc int

	Select @max_bot_id = max(Id) from BotOrderInstruction where 
	botid = @botid and Ticker = @Ticker_Symbole and OrderStatus = 0

	Update BotOrderInstruction set OrderStatus = -1 
	Where botid = @botid and Ticker = @Ticker_Symbole and Id < @max_bot_id and OrderStatus = 0

	SELECT @order_time = dbo.fn_ConvertToLocalDateTime(RawTime)
	FROM [IBbot].[dbo].[BotOrderInstruction]
	Where botid = @botid and Id = @max_bot_id

	select @order_hour = datepart(HOUR, @order_time)

	select @order_minute = datepart(minute, @order_time)

	select @now_hour = datepart(HOUR, GETDATE())

	Set @now_hour = @now_hour

	select @now_minute = datepart(minute, GETDATE())

	SELECT @order_date = DATEADD(dd, 0, DATEDIFF(dd, 0, @order_time))

	SELECT @now_date = DATEADD(dd, 0, DATEDIFF(dd, 0, GETDATE()))

	Set @minute_deferanc = DATEDIFF(minute, @order_time, GETDATE())

	if @minute_deferanc <= 30
		Begin
			--Select 'Go'

			Declare @RefId varchar(36),
					@OrderTag varchar(50),
					@OrderStatus int,
					@is_open_order_filled int

			Select @RefId = [RefId] , @OrderTag = [OrderTag] FROM [IBbot].[dbo].[BotOrderInstruction]
			Where botid = @botid and Id = @max_bot_id

			--CLOSE
			if @OrderTag = 'CLOSE'
				Begin
					Select @is_open_order_filled = [Id] FROM [IBbot].[dbo].[BotOrderInstruction]
					Where botid = @botid and [RefId] = @RefId and OrderStatus = 3

					if @is_open_order_filled is not null
						Begin
							SELECT [Id]
								  ,[BotId]
								  ,[RefId]
								  ,[RawTime]
								  ,dbo.fn_ConvertToLocalDateTime(RawTime) as Order_Time
								  ,[Ticker]
								  ,[Action]
								  ,[OrderTag]
								  ,[OrderType]
								  ,[Qty]
								  ,[Price]
								  ,[TimeStamp]
								  ,[OrderStatus]
							FROM [IBbot].[dbo].[BotOrderInstruction]
							Where botid = @botid and Id = @max_bot_id

							update BotOrderInstruction Set OrderStatus = 1
							Where botid = @botid and Id = @max_bot_id

							goto myend
						End
					Else
						Begin
							goto myend
						End
				End

			SELECT [Id]
				  ,[BotId]
				  ,[RefId]
				  ,[RawTime]
				  ,dbo.fn_ConvertToLocalDateTime(RawTime) as Order_Time
				  ,[Ticker]
				  ,[Action]
				  ,[OrderTag]
				  ,[OrderType]
				  ,[Qty]
				  ,[Price]
				  ,[TimeStamp]
				  ,[OrderStatus]
			FROM [IBbot].[dbo].[BotOrderInstruction]
			Where botid = @botid and Id = @max_bot_id

			update BotOrderInstruction Set OrderStatus = 1
			Where botid = @botid and Id = @max_bot_id
		End
	Else
		Begin
			update BotOrderInstruction Set OrderStatus = -2
			Where botid = @botid and Id = @max_bot_id
		End

	myend:
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[GET_BOT_INSTRUCTION]
	@BotRefId nvarchar(max),
	@TickerSymbole nvarchar(max)
AS
BEGIN

	Declare @max_id int

	Select @max_id = max(id) from [ViewBotInstructions] where BotId = @BotRefId and TickerRaw = @TickerSymbole

	Select top(1) * from [ViewBotInstructions] where  BotId = @BotRefId and TickerRaw = @TickerSymbole and id = @max_id order by id desc
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[GET_BOT_NICK_NAME]
	@account_desc_ib_id nvarchar(50)
AS
BEGIN
	
	Select * from account_desc Where account_desc_ib_id = @account_desc_ib_id
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GET_BULK_INSTRUCTION]
	@Ticker_Symbole nvarchar(25),
	@botid nvarchar(50)
AS
BEGIN
	Declare @max_bot_id int,
			@order_time datetime,
			@order_date date,
			@now_date date,
			@order_hour int,
			@now_hour int,
			@order_minute int,
			@now_minute int,
			@minute_deferanc int,
			@myRefid nvarchar(36),
			@last_rawtime int,
			@IsNew bit = 0


	SET @last_rawtime = dbo.[fn_ConvertLocalDateTimeToRawUTC](getdate());

	Select @max_bot_id = max(Id) from BotOrderInstruction where 
	botid = @botid and Ticker = @Ticker_Symbole and OrderStatus = 0

	Select  @myRefid = RefId from BotOrderInstruction where 
	id = @max_bot_id

	Update BotOrderInstruction set OrderStatus = -3 , LastRawTime = @last_rawtime
	Where botid = @botid and Ticker = @Ticker_Symbole
		  and Id < @max_bot_id and OrderStatus = 0
		  and RefId = @myRefid

	SELECT @order_time = dbo.fn_ConvertToLocalDateTime(RawTime)
	FROM [IBbot].[dbo].[BotOrderInstruction]
	Where botid = @botid and Id = @max_bot_id

	select @order_hour = datepart(HOUR, @order_time) 

	select @order_minute = datepart(minute, @order_time)

	select @now_hour = datepart(HOUR, GETDATE())

	Set @now_hour = @now_hour

	select @now_minute = datepart(minute, GETDATE())

	SELECT @order_date = DATEADD(dd, 0, DATEDIFF(dd, 0, @order_time))

	SELECT @now_date = DATEADD(dd, 0, DATEDIFF(dd, 0, GETDATE()))

	Set @minute_deferanc = DATEDIFF(minute, @order_time, GETDATE())

	if @minute_deferanc <= 30
		Begin
			--Select 'Go'

			Declare @RefId varchar(36),
					@OrderTag varchar(50),
					@OrderAction varchar(50),
					@OrderStatus int,
					@is_open_order_filled int

			Select @RefId = [RefId] , @OrderTag = [OrderTag] , @OrderAction = [Action] FROM [IBbot].[dbo].[BotOrderInstruction]
			Where botid = @botid and Id = @max_bot_id

			--CLOSE
			if @OrderTag = 'CLOSE'
				Begin
					Select @is_open_order_filled = [Id] FROM [IBbot].[dbo].[BotOrderInstruction]
					Where botid = @botid and [RefId] = @RefId and OrderStatus = 3

					
					IF @OrderAction = 'UPDATE' AND NOT EXISTS (SELECT 1 FROM [BotOrderInstruction] WHERE [BotId] = @botid AND [RefId] = @RefId AND [OrderTag] = @OrderTag AND IBKR_order_id > 0)
					BEGIN
						SET @IsNew = 1;
					END

					if @is_open_order_filled is not null
						Begin
							SELECT [Id]
								  ,[BotId]
								  ,[RefId]
								  ,[RawTime]
								  ,dbo.fn_ConvertToLocalDateTime(RawTime) as Order_Time
								  ,[Ticker]
								  ,(CASE WHEN @IsNew = 1 THEN 'NEW' ELSE [Action] END) AS [Action]
								  ,[OrderTag]
								  ,[OrderType]
								  ,[Qty]
								  ,[Price]
								  ,[TimeStamp]
								  ,[OrderStatus]
							FROM [IBbot].[dbo].[BotOrderInstruction]
							Where botid = @botid and Id = @max_bot_id

							update BotOrderInstruction Set OrderStatus = 1 , LastRawTime = @last_rawtime
							Where botid = @botid and Id = @max_bot_id

							goto myend
						End
					Else
						Begin
							goto myend
						End
				End

			if @OrderAction = 'UPDATE'
				Begin

					Select @is_open_order_filled = [Id] FROM [IBbot].[dbo].[BotOrderInstruction]
					Where botid = @botid and [RefId] = @RefId and OrderStatus = 3

					Set @IsNew = 0;

					IF @OrderAction = 'UPDATE' AND NOT EXISTS (SELECT 1 FROM [BotOrderInstruction] WHERE [BotId] = @botid AND [RefId] = @RefId AND [OrderTag] = @OrderTag AND IBKR_order_id > 0)
					BEGIN
						SET @IsNew = 1;
					END

					if @is_open_order_filled is null
						Begin
							SELECT [Id]
								  ,[BotId]
								  ,[RefId]
								  ,[RawTime]
								  ,dbo.fn_ConvertToLocalDateTime(RawTime) as Order_Time
								  ,[Ticker]
								  ,(CASE WHEN @IsNew = 1 THEN 'NEW' ELSE [Action] END) AS [Action]
								  ,[OrderTag]
								  ,[OrderType]
								  ,[Qty]
								  ,[Price]
								  ,[TimeStamp]
								  ,[OrderStatus]
							FROM [IBbot].[dbo].[BotOrderInstruction]
							Where botid = @botid and Id = @max_bot_id

							update BotOrderInstruction Set OrderStatus = 1 , LastRawTime = @last_rawtime
							Where botid = @botid and Id = @max_bot_id

							goto myend
						End
				End
			Else
				Begin
					SELECT [Id]
						  ,[BotId]
						  ,[RefId]
						  ,[RawTime]
						  ,dbo.fn_ConvertToLocalDateTime(RawTime) as Order_Time
						  ,[Ticker]
						  ,[Action]
						  ,[OrderTag]
						  ,[OrderType]
						  ,[Qty]
						  ,[Price]
						  ,[TimeStamp]
						  ,[OrderStatus]
					FROM [IBbot].[dbo].[BotOrderInstruction]
					Where botid = @botid and Id = @max_bot_id

					update BotOrderInstruction Set OrderStatus = 1 , LastRawTime = @last_rawtime
					Where botid = @botid and Id = @max_bot_id
				End
			
		End
	Else
		Begin
			update BotOrderInstruction Set OrderStatus = -2 , LastRawTime = @last_rawtime
			Where botid = @botid and Id = @max_bot_id
		End

	myend:

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GET_BULK_INSTRUCTION_EX]
	@Ticker_Symbole nvarchar(25),
	@botid nvarchar(50)
AS
BEGIN
	Declare @max_bot_id int,
			@minute_deferanc int,
			@myRefid nvarchar(36),
			@RefId varchar(36),
			@OrderTag varchar(50),
			@OrderAction varchar(50),
			@rawTime int,
			@last_rawtime int;


	-- Get latest record that is new (OrderStatus = 0)
	SELECT TOP 1 @max_bot_id = [Id]
				,@myRefid = [RefId]
				,@rawTime = [RawTime]
				,@RefId = [RefId]
				,@OrderTag = [OrderTag]
				,@OrderAction = [Action] 
		FROM [BotOrderInstruction]
		WHERE [BotId] = @botid AND [Ticker] = @Ticker_Symbole AND [OrderStatus] = 0
		ORDER BY [Id] DESC;
	IF @@ROWCOUNT = 0  RETURN;

	SET @last_rawtime = dbo.[fn_ConvertLocalDateTimeToRawUTC](getdate());
	
	-- Mark all other new requests as ignored (OrderStatus = -3)
	UPDATE [BotOrderInstruction] set [OrderStatus] = -3, [LastRawTime] = @last_rawtime 
		WHERE [BotId] = @botid AND [Ticker] = @Ticker_Symbole
		  AND [Id] < @max_bot_id AND [OrderStatus] = 0
		  AND [RefId] = @myRefid

	-- Check if Instruction is expired
	SET @minute_deferanc = (@last_rawtime - @rawTime) / 60;
	IF (@minute_deferanc > 30) 
	BEGIN
		UPDATE [BotOrderInstruction] Set [OrderStatus] = -2, [LastRawTime] = @last_rawtime
			WHERE [Id] = @max_bot_id;
		RETURN;
	END

	--CLOSE
	IF @OrderTag = 'CLOSE' AND NOT EXISTS(SELECT 1 FROM [BotOrderInstruction] WHERE [BotId] = @botid AND [RefId] = @RefId AND [OrderTag] = 'OPEN' AND [OrderStatus] = 3)
	BEGIN
		UPDATE [BotOrderInstruction] Set [OrderStatus] = -3, [LastRawTime] = @last_rawtime WHERE Id = @max_bot_id;
		RETURN;
	END

	-- Check if there is an IBKR_order_id already exists for this order... if not.. treat it as new
	DECLARE @IsNew bit = 0;
	IF @OrderAction = 'UPDATE' AND NOT EXISTS (SELECT 1 FROM [BotOrderInstruction] WHERE [BotId] = @botid AND [RefId] = @RefId AND [OrderTag] = @OrderTag AND IBKR_order_id > 0)
	BEGIN
		SET @IsNew = 1;
	END

	UPDATE [BotOrderInstruction] Set [OrderStatus] = 1, [LastRawTime] = @last_rawtime WHERE Id = @max_bot_id;
	SELECT   [Id]
			,[BotId]
			,[RefId]
			,[RawTime]
			,dbo.fn_ConvertToLocalDateTime(RawTime) as Order_Time
			,[Ticker]
			,(CASE WHEN @IsNew = 1 THEN 'NEW' ELSE [Action] END) AS [Action]
			,[OrderTag]
			,[OrderType]
			,[Qty]
			,[Price]
			,[TimeStamp]
			,[OrderStatus]
		FROM [BotOrderInstruction]
		WHERE [Id] = @max_bot_id

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GET_EMPLOYEE_LOGIN_INFORMATION]
	@Employee_name nvarchar(max)
AS
BEGIN
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	Select * from employee_tbl Where employee_user_name = @Employee_name
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GET_IB_SETTINGS]
	
AS
BEGIN
	
	Select * from ib_settings
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GET_ORDER_ID_BY_ORDER_IBKR_ID]
	@IBKR_Order_id int,
	@BotId nvarchar(50)
AS
BEGIN
	
	Select id from BotOrderInstruction 
	Where 
	IBKR_Order_id = @IBKR_Order_id and 
	BotId = @BotId
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GET_SYSTEM_ICEBERG]
	@system_iceberg_Bot_id nvarchar(max)
AS
BEGIN
	SET NOCOUNT ON;

	Select * from system_iceberg_tbl Where 
	system_iceberg_Bot_id = @system_iceberg_Bot_id

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GET_SYSTEM_LIMIT]
	@system_limit_Bot_id nvarchar(max)
AS
BEGIN
	
	Select * from system_limit Where 
	system_limit_Bot_id = @system_limit_Bot_id

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[GET_SYSTEM_TWAP_STALGO]
	@stalgo_bot_id nvarchar(max)
AS
BEGIN
	
	SET NOCOUNT ON;

	Select * from TWAP_StAlgo_Settings Where 
	stalgo_bot_id = @stalgo_bot_id
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GET_TICKER_DESC_BY_BOT_ID]
	@ticker_Bot_id nvarchar(MAX)
AS
BEGIN
	Select * from ticker_description Where ticker_Bot_id = @ticker_Bot_id
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[INSERT_IB_AutomatedBot_trade_capture]
	@bot_acc_summary_id nvarchar(50),
	@bot_trades_symbol nvarchar(50),
	@bot_trades_localsymbol nvarchar(50),
	@bot_trades_sectype nvarchar(50),
	@bot_trades_exchange nvarchar(50),
	@bot_trades_price float,
	@bot_trades_multiplier float,
	@bot_trades_shares float,
	@bot_trades_cumQty float,
	@bot_trades_side nvarchar(10),
	@bot_trades_execid nvarchar(max),
	@bot_trades_commission float,
	@bot_trades_commission_execId nvarchar(max),
	@bot_trades_date date,
	@bot_trades_time nvarchar(50),
	@IBKR_Order_id int
AS
BEGIN
	
	Declare @id_bot_trades_counter int,
			@is_@bot_trades_execid_inserted nvarchar(max)

	Select @is_@bot_trades_execid_inserted = bot_trades_execid from AutomatedBot_trade_capture
	Where bot_trades_execid = @bot_trades_execid

	if @is_@bot_trades_execid_inserted is null
		Begin
			Select @id_bot_trades_counter = max(id_bot_trades) from AutomatedBot_trade_capture

			if @id_bot_trades_counter is null
				Begin
					Set @id_bot_trades_counter = 1;
				End
			Else
				Begin
					Set @id_bot_trades_counter = @id_bot_trades_counter + 1;
				End

			Insert into AutomatedBot_trade_capture (id_bot_trades,
													bot_acc_summary_id ,
													bot_trades_symbol ,
													bot_trades_localsymbol ,
													bot_trades_sectype ,
													bot_trades_exchange ,
													bot_trades_price ,
													bot_trades_multiplier ,
													bot_trades_shares ,
													bot_trades_cumQty ,
													bot_trades_side ,
													bot_trades_execid ,
													bot_trades_commission ,
													bot_trades_commission_execId,
													bot_trades_date,
													bot_trades_time,
													IBKR_Order_id)
													values(@id_bot_trades_counter,
													@bot_acc_summary_id ,
													@bot_trades_symbol ,
													@bot_trades_localsymbol ,
													@bot_trades_sectype ,
													@bot_trades_exchange ,
													@bot_trades_price ,
													@bot_trades_multiplier ,
													@bot_trades_shares ,
													@bot_trades_cumQty ,
													@bot_trades_side ,
													@bot_trades_execid ,
													@bot_trades_commission ,
													@bot_trades_commission_execId,
													@bot_trades_date,
													@bot_trades_time,
													@IBKR_Order_id)
		End
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[INSERT_IB_BOT_TRADE_CAPTURE]
	@bot_acc_summary_id nvarchar(50),
	@bot_trades_symbol nvarchar(50),
	@bot_trades_localsymbol nvarchar(50),
	@bot_trades_sectype nvarchar(50),
	@bot_trades_exchange nvarchar(50),
	@bot_trades_price float,
	@bot_trades_multiplier float,
	@bot_trades_shares float,
	@bot_trades_cumQty float,
	@bot_trades_side nvarchar(10),
	@bot_trades_execid nvarchar(max),
	@bot_trades_commission float,
	@bot_trades_commission_execId nvarchar(max),
	@bot_trades_date date,
	@bot_trades_time nvarchar(50)
AS
BEGIN
	
	Declare @id_bot_trades_counter int,
			@is_@bot_trades_execid_inserted nvarchar(max)

	Select @is_@bot_trades_execid_inserted = bot_trades_execid from ib_bot_trade_capture
	Where bot_trades_execid = @bot_trades_execid

	if @is_@bot_trades_execid_inserted is null
		Begin
			Select @id_bot_trades_counter = max(id_bot_trades) from ib_bot_trade_capture

			if @id_bot_trades_counter is null
				Begin
					Set @id_bot_trades_counter = 1;
				End
			Else
				Begin
					Set @id_bot_trades_counter = @id_bot_trades_counter + 1;
				End

			Insert into ib_bot_trade_capture values(@id_bot_trades_counter,
													@bot_acc_summary_id ,
													@bot_trades_symbol ,
													@bot_trades_localsymbol ,
													@bot_trades_sectype ,
													@bot_trades_exchange ,
													@bot_trades_price ,
													@bot_trades_multiplier ,
													@bot_trades_shares ,
													@bot_trades_cumQty ,
													@bot_trades_side ,
													@bot_trades_execid ,
													@bot_trades_commission ,
													@bot_trades_commission_execId,
													@bot_trades_date,
													@bot_trades_time)
		End
	
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[INSERT_IB_BOT_TRADE_CAPTURE_NEW]
	@bot_acc_summary_id nvarchar(50),
	@bot_trades_symbol nvarchar(50),
	@bot_trades_localsymbol nvarchar(50),
	@bot_trades_sectype nvarchar(50),
	@bot_trades_exchange nvarchar(50),
	@bot_trades_price float,
	@bot_trades_multiplier float,
	@bot_trades_shares float,
	@bot_trades_cumQty float,
	@bot_trades_side nvarchar(10),
	@bot_trades_execid nvarchar(max),
	@bot_trades_commission float,
	@bot_trades_commission_execId nvarchar(max),
	@bot_trades_date date,
	@bot_trades_time nvarchar(50),
	@IBKR_Order_id int,
	@IBKR_Excution_Time nvarchar(50)
AS
BEGIN
	SET NOCOUNT ON;

    Declare @id_bot_trades_counter int,
			@is_@bot_trades_execid_inserted nvarchar(max)

	Select @is_@bot_trades_execid_inserted = bot_trades_execid from IB_Bot_Trade_Capture_New
	Where bot_trades_execid = @bot_trades_execid

	if @is_@bot_trades_execid_inserted is null
		Begin
			Select @id_bot_trades_counter = max(id_bot_trades) from IB_Bot_Trade_Capture_New

			if @id_bot_trades_counter is null
				Begin
					Set @id_bot_trades_counter = 1;
				End
			Else
				Begin
					Set @id_bot_trades_counter = @id_bot_trades_counter + 1;
				End

			Insert into IB_Bot_Trade_Capture_New values(@id_bot_trades_counter,
													@bot_acc_summary_id ,
													@bot_trades_symbol ,
													@bot_trades_localsymbol ,
													@bot_trades_sectype ,
													@bot_trades_exchange ,
													@bot_trades_price ,
													@bot_trades_multiplier ,
													@bot_trades_shares ,
													@bot_trades_cumQty ,
													@bot_trades_side ,
													@bot_trades_execid ,
													@bot_trades_commission ,
													@bot_trades_commission_execId,
													@bot_trades_date,
													@bot_trades_time,
													@IBKR_Order_id,
													@IBKR_Excution_Time)
		End
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[INSERT_UPDATE_BOT_ACC_SUMMARY]
	@bot_acc_summary_id	nvarchar(50),
	@bot_acc_AccountType	nvarchar(50),
	@bot_acc_Cushion	float,
	@bot_acc_DayTradesRemaining	float,
	@bot_acc_LookAheadNextChange	float,
	@bot_acc_AccruedCash	float,
	@bot_acc_AvailableFunds	float,
	@bot_acc_BuyingPower	float,
	@bot_acc_EquityWithLoanValue	float,
	@bot_acc_ExcessLiquidity	float,
	@bot_acc_FullAvailableFunds	float,
	@bot_acc_FullExcessLiquidity	float,
	@bot_acc_FullInitMarginReq	float,
	@bot_acc_FullMaintMarginReq	float,
	@bot_acc_GrossPositionValue	float,
	@bot_acc_InitMarginReq	float,
	@bot_acc_LookAheadAvailableFunds	float,
	@bot_acc_LookAheadExcessLiquidity	float,
	@bot_acc_LookAheadInitMarginReq	float,
	@bot_acc_LookAheadMaintMarginReq	float,
	@bot_acc_MaintMarginReq	float,
	@bot_acc_NetLiquidation	float,
	@bot_acc_PreviousDayEquityWithLoanValue	float,
	@bot_acc_TotalCashValue	float
AS
BEGIN
	
	Declare @id_bot_acc_summary_cunter int,
			@is_bot_acc_summary_id_inserted int


	Select @is_bot_acc_summary_id_inserted = id_bot_acc_summary from bot_acc_summary 
	where (bot_acc_summary_id = @bot_acc_summary_id)

	if @is_bot_acc_summary_id_inserted is null
		Begin
			Select @id_bot_acc_summary_cunter = max(id_bot_acc_summary) from bot_acc_summary
			
			if @id_bot_acc_summary_cunter is null
				Begin
					Set @id_bot_acc_summary_cunter = 1;
				End
			Else
				Begin
					Set @id_bot_acc_summary_cunter = @id_bot_acc_summary_cunter + 1;
				End

			INSERT Into bot_acc_summary Values( @id_bot_acc_summary_cunter ,
											    @bot_acc_summary_id,
											    @bot_acc_AccountType	,
												@bot_acc_Cushion	,
												@bot_acc_DayTradesRemaining	,
												@bot_acc_LookAheadNextChange	,
												@bot_acc_AccruedCash	,
												@bot_acc_AvailableFunds	,
												@bot_acc_BuyingPower	,
												@bot_acc_EquityWithLoanValue	,
												@bot_acc_ExcessLiquidity	,
												@bot_acc_FullAvailableFunds	,
												@bot_acc_FullExcessLiquidity	,
												@bot_acc_FullInitMarginReq	,
												@bot_acc_FullMaintMarginReq	,
												@bot_acc_GrossPositionValue	,
												@bot_acc_InitMarginReq	,
												@bot_acc_LookAheadAvailableFunds	,
												@bot_acc_LookAheadExcessLiquidity	,
												@bot_acc_LookAheadInitMarginReq	,
												@bot_acc_LookAheadMaintMarginReq	,
												@bot_acc_MaintMarginReq	,
												@bot_acc_NetLiquidation	,
												@bot_acc_PreviousDayEquityWithLoanValue	,
												@bot_acc_TotalCashValue)
		End
	Else
		Begin
			Update bot_acc_summary Set bot_acc_AccountType	= @bot_acc_AccountType,
												bot_acc_Cushion = @bot_acc_Cushion	,
												bot_acc_DayTradesRemaining	= @bot_acc_DayTradesRemaining ,
												bot_acc_LookAheadNextChange = @bot_acc_LookAheadNextChange ,
												bot_acc_AccruedCash = @bot_acc_AccruedCash,
												bot_acc_AvailableFunds = @bot_acc_AvailableFunds,
												bot_acc_BuyingPower = @bot_acc_BuyingPower,
												bot_acc_EquityWithLoanValue = @bot_acc_EquityWithLoanValue,
												bot_acc_ExcessLiquidity = @bot_acc_ExcessLiquidity,
												bot_acc_FullAvailableFunds = @bot_acc_FullAvailableFunds,
												bot_acc_FullExcessLiquidity = @bot_acc_FullExcessLiquidity,
												bot_acc_FullInitMarginReq = @bot_acc_FullInitMarginReq,
												bot_acc_FullMaintMarginReq = @bot_acc_FullMaintMarginReq,
												bot_acc_GrossPositionValue = @bot_acc_GrossPositionValue,
												bot_acc_InitMarginReq = @bot_acc_InitMarginReq,
												bot_acc_LookAheadAvailableFunds = @bot_acc_LookAheadAvailableFunds,
												bot_acc_LookAheadExcessLiquidity = @bot_acc_LookAheadExcessLiquidity,
												bot_acc_LookAheadInitMarginReq = @bot_acc_LookAheadInitMarginReq,
												bot_acc_LookAheadMaintMarginReq = @bot_acc_LookAheadMaintMarginReq,
												bot_acc_MaintMarginReq = @bot_acc_LookAheadMaintMarginReq,
												bot_acc_NetLiquidation = @bot_acc_NetLiquidation,
												bot_acc_PreviousDayEquityWithLoanValue = @bot_acc_PreviousDayEquityWithLoanValue,
												bot_acc_TotalCashValue = @bot_acc_TotalCashValue
			Where (id_bot_acc_summary = @is_bot_acc_summary_id_inserted)
		End

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[INSERT_UPDATE_BOT_PNL_SUMMARY]
	@bot_pnl_acc_id nvarchar(50),
	@bot_pnl_dailyPnL float,
	@bot_pnl_realizedPnL float,
	@bot_pnl_unrealizedPnL float,
	@bot_pnl_date date,
	@bot_pnl_time nvarchar(8)
AS
BEGIN
	
	Declare @bot_pnl_id_counter int

	Select @bot_pnl_id_counter = max(bot_pnl_id) from ib_pnl_summary

	if @bot_pnl_id_counter is null
		Begin
			Set @bot_pnl_id_counter = 1;
		End
	Else
		Begin
			Set @bot_pnl_id_counter = @bot_pnl_id_counter + 1;
		End

	insert into ib_pnl_summary values(  @bot_pnl_id_counter,
										@bot_pnl_acc_id ,
										@bot_pnl_dailyPnL,
										@bot_pnl_realizedPnL ,
										@bot_pnl_unrealizedPnL ,
										@bot_pnl_date ,
										@bot_pnl_time)
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[INSERT_UPDATE_SYSTEM_ICEBERG]
	@system_iceberg_Bot_id nvarchar(max),
	@system_iceberg_ticker_symbol nvarchar(max),
	@system_iceberg_ticker_value int
AS
BEGIN
	
	Declare @system_iceberg_id_counter int,
			@is_added_to_db int

	Select @is_added_to_db = system_iceberg_id from system_iceberg_tbl 
	Where system_iceberg_Bot_id = @system_iceberg_Bot_id and 
		  system_iceberg_ticker_symbol = @system_iceberg_ticker_symbol

	if @is_added_to_db is null
		Begin
			Select @system_iceberg_id_counter = max(system_iceberg_id) from system_iceberg_tbl

			if @system_iceberg_id_counter is null
				Begin
					Set @system_iceberg_id_counter = 1;
				End
			Else
				Begin
					Set @system_iceberg_id_counter = @system_iceberg_id_counter + 1;
				End

			insert into system_iceberg_tbl (system_iceberg_id , 
									 system_iceberg_Bot_id , 
									 system_iceberg_ticker_symbol,
									 system_iceberg_ticker_value)
									 values
									 (@system_iceberg_id_counter,
									  @system_iceberg_Bot_id,
									  @system_iceberg_ticker_symbol,
									  @system_iceberg_ticker_value)
		End
	Else
		Begin
			
			Update system_iceberg_tbl Set system_iceberg_ticker_value = @system_iceberg_ticker_value 
			Where system_iceberg_id = @is_added_to_db

		End
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[INSERT_UPDATE_SYSTEM_LIMIT]
	@system_limit_Bot_id nvarchar(max),
	@system_limit_ticker_symbol nvarchar(max),
	@system_limit_ticker_limit int
AS
BEGIN
	
	Declare @system_limit_id_counter int,
			@is_added_to_db int

	Select @is_added_to_db = system_limit_id from system_limit 
	Where system_limit_Bot_id = @system_limit_Bot_id and 
		  system_limit_ticker_symbol = @system_limit_ticker_symbol

	if @is_added_to_db is null
		Begin
			Select @system_limit_id_counter = max(system_limit_id) from system_limit

			if @system_limit_id_counter is null
				Begin
					Set @system_limit_id_counter = 1;
				End
			Else
				Begin
					Set @system_limit_id_counter = @system_limit_id_counter + 1;
				End

			insert into system_limit (system_limit_id , 
									 system_limit_Bot_id , 
									 system_limit_ticker_symbol,
									 system_limit_ticker_limit,
									 system_limit_ticker_Short_limit)
									 values
									 (@system_limit_id_counter,
									  @system_limit_Bot_id,
									  @system_limit_ticker_symbol,
									  @system_limit_ticker_limit,
									  0)
		End
	Else
		Begin
			
			Update system_limit Set system_limit_ticker_limit = @system_limit_ticker_limit 
			Where system_limit_id = @is_added_to_db

		End

	


END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[INSERT_UPDATE_SYSTEM_SHORT_LIMIT]
	@system_limit_Bot_id nvarchar(max),
	@system_limit_ticker_symbol nvarchar(max),
	@system_limit_ticker_SHORT_limit int
AS
BEGIN
	Declare @system_limit_id_counter int,
			@is_added_to_db int

	Select @is_added_to_db = system_limit_id from system_limit 
	Where system_limit_Bot_id = @system_limit_Bot_id and 
		  system_limit_ticker_symbol = @system_limit_ticker_symbol

	if @is_added_to_db is null
		Begin
			Select @system_limit_id_counter = max(system_limit_id) from system_limit

			if @system_limit_id_counter is null
				Begin
					Set @system_limit_id_counter = 1;
				End
			Else
				Begin
					Set @system_limit_id_counter = @system_limit_id_counter + 1;
				End

			insert into system_limit (system_limit_id , 
									 system_limit_Bot_id , 
									 system_limit_ticker_symbol,
									 system_limit_ticker_limit,
									 system_limit_ticker_Short_limit)
									 values
									 (@system_limit_id_counter,
									  @system_limit_Bot_id,
									  @system_limit_ticker_symbol,
									  0,
									  @system_limit_ticker_SHORT_limit)
		End
	Else
		Begin
			
			Update system_limit Set system_limit_ticker_Short_limit = @system_limit_ticker_SHORT_limit 
			Where system_limit_id = @is_added_to_db

		End
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[INSERT_UPDATE_TICKER_DESCRIPTION]
	@ticker_Bot_id nvarchar(max),
	@ticker_desc nvarchar(max),
	@ticker_symbol nvarchar(max),
	@ticker_symbol_exch nvarchar(max),
	@ticker_symbol_primary_exch nvarchar(max),
	@ticker_symbol_SecType nvarchar(max),
	@ticker_symbol_Currency nvarchar(max),
	@ticker_symbol_exp_date nvarchar(max),
	@ticker_symbol_local_Name nvarchar(max),
	@ticker_symbol_MinTick float,
	@ticker_conid int
AS
BEGIN
	Declare @isold nvarchar(max),
			@ticker_id_max int,
			@ticker_idold_id int,
			@isacc_existe nvarchar(max)

	Select @isacc_existe = account_desc_ib_id from account_desc
	Where account_desc_ib_id = @ticker_Bot_id

	if @isacc_existe is null
		Begin
			goto myend
		End
	Else
		Begin
			Select @ticker_idold_id = ticker_id , @isold = ticker_symbol from ticker_description where (ticker_Bot_id = @ticker_Bot_id and ticker_symbol = @ticker_symbol)

			if (@isold is null)
				Begin

					Select @ticker_id_max = max(ticker_id) from ticker_description;
					if (@ticker_id_max is null)
						Begin
							Set @ticker_id_max = 1
						End
					Else
						Begin
							Set @ticker_id_max = @ticker_id_max + 1
						End

					insert into ticker_description Values(@ticker_id_max , @ticker_Bot_id , @ticker_desc , @ticker_symbol , 
					@ticker_symbol_exch , @ticker_symbol_primary_exch , @ticker_symbol_SecType , @ticker_symbol_Currency ,
					@ticker_symbol_exp_date , @ticker_symbol_local_Name , @ticker_symbol_MinTick ,
					@ticker_conid)
				End
			Else
				Begin
					Update ticker_description Set ticker_desc = @ticker_desc ,
					ticker_symbol = @ticker_symbol ,
					ticker_symbol_exch = @ticker_symbol_exch ,
					ticker_symbol_primary_exch = @ticker_symbol_primary_exch,
					ticker_symbol_SecType = @ticker_symbol_SecType,
					ticker_symbol_Currency = @ticker_symbol_Currency,
					ticker_symbol_exp_date = @ticker_symbol_exp_date,
					ticker_symbol_local_Name = @ticker_symbol_local_Name,
					ticker_symbol_MinTick = @ticker_symbol_MinTick ,
					ticker_conid = @ticker_conid
					Where (ticker_Bot_id = @ticker_Bot_id and ticker_id = @ticker_idold_id)
				End
		End

	
myend:

	
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[INSERT_UPDATE_TWAP_STALGO]
	@stalgo_bot_id nvarchar(max),
	@stalgo_ticker_symbol nvarchar(max),
	@stalgo_strategy_name nvarchar(max),
	@stalgo_starting_time int,
	@stalgo_ending_time int,
	@stalgo_past_end_time int
AS
BEGIN
	SET NOCOUNT ON;

	Declare @stalgo_id_counter int,
			@is_added_to_db int

	Select @is_added_to_db = stalgo_id from TWAP_StAlgo_Settings
	Where stalgo_bot_id = @stalgo_bot_id and 
	stalgo_ticker_symbol = @stalgo_ticker_symbol

	if @is_added_to_db is null
		Begin
			Select @stalgo_id_counter = max(stalgo_id) from TWAP_StAlgo_Settings

			if @stalgo_id_counter is null
				Begin
					Set @stalgo_id_counter = 1;
				End
			Else
				Begin
					Set @stalgo_id_counter = @stalgo_id_counter + 1;
				End

			insert into TWAP_StAlgo_Settings (stalgo_id , 
									 stalgo_bot_id , 
									 stalgo_ticker_symbol,
									 stalgo_strategy_name,
									 stalgo_starting_time,
									 stalgo_ending_time,
									 stalgo_past_end_time)
									 values
									 (@stalgo_id_counter,
									  @stalgo_bot_id ,
									  @stalgo_ticker_symbol,
									  @stalgo_strategy_name ,
									  @stalgo_starting_time ,
									  @stalgo_ending_time ,
									  @stalgo_past_end_time)
		End
	Else
		Begin
			
			Update TWAP_StAlgo_Settings Set stalgo_strategy_name = @stalgo_strategy_name ,
											stalgo_starting_time = @stalgo_starting_time ,
											stalgo_ending_time = @stalgo_ending_time,
											stalgo_past_end_time = @stalgo_past_end_time
			Where stalgo_id = @is_added_to_db

		End
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[NEW_LIMIT_ORDER]
	@BotId nvarchar(50),
	@Ticker nvarchar(25),
	@Qty int,
	@Price decimal,
	@IBKR_Order_id int
AS
BEGIN
	
	Declare @RefId int,
			@Action nvarchar(25),
			@OrderTag nvarchar(50),
			@OrderType nvarchar(50),
			@TimeStamp datetime,
			@OrderStatus int,
			@nowdatetime datetime,
			@RowTime int,
			@convertedDatetime datetime,
			@id_counter int
			

	Set @RefId = -1
	Set @Action = 'NEW'
	Set @OrderTag = 'OPEN'
	Set @OrderType = 'LIMIT'
	Set @TimeStamp = GETDATE()
	Set @OrderStatus = 1

	Set @TimeStamp = GETDATE()

	Set @nowdatetime = GETDATE()
	EXEC  @RowTime = fn_ConvertLocalDateTimeToRawUTC @nowdatetime

	INSERT INTO [dbo].[BotOrderInstruction]
           ([BotId]
           ,[RefId]
           ,[RawTime]
           ,[Ticker]
           ,[Action]
           ,[OrderTag]
           ,[OrderType]
           ,[Qty]
           ,[Price]
           ,[TimeStamp]
           ,[OrderStatus]
		   ,[IBKR_Order_id])
     VALUES
           (@BotId,
            @RefId,
            @RowTime,
			@Ticker,
			@Action,
			@OrderTag,
			@OrderType,
			@Qty,
            @Price,
			@TimeStamp,
			@OrderStatus,
			@IBKR_Order_id)

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[ORDER_CANCELED] 
	@Id int
AS
BEGIN

	DECLARE @CurRawTime int = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE());

	Update BotOrderInstruction set OrderStatus = -1 ,
	[LastRawTime] = @CurRawTime
	Where Id = @Id and OrderStatus <> 3 and OrderStatus <> 2

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[ORDER_QTY_ERROR]
	@Id int
AS
BEGIN
	
	DECLARE @CurRawTime int = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE());

	Update BotOrderInstruction set OrderStatus = -4 ,
	[LastRawTime] = @CurRawTime
	Where Id = @Id
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[ORDER_UPDATE_ERROR]
	@Id int
AS
BEGIN
	DECLARE @CurRawTime int = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE());

	Update BotOrderInstruction set OrderStatus = -3 ,
	[LastRawTime] = @CurRawTime
	Where Id = @Id and OrderStatus <> 3 and OrderStatus <> 2
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PNL_REPORT]
	@date_from date,
	@date_to date,
	@bot_pnl_acc_id nvarchar(50)
AS
BEGIN
	
	Select * from ib_pnl_summary where bot_pnl_date between @date_from and @date_to
	and bot_pnl_time = '00:00:00' and bot_pnl_acc_id = @bot_pnl_acc_id

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SqlQueryNotificationStoredProcedure-354d49f8-e145-4f9c-9057-39e901899980] AS BEGIN BEGIN TRANSACTION; RECEIVE TOP(0) conversation_handle FROM [SqlQueryNotificationService-354d49f8-e145-4f9c-9057-39e901899980]; IF (SELECT COUNT(*) FROM [SqlQueryNotificationService-354d49f8-e145-4f9c-9057-39e901899980] WHERE message_type_name = 'http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer') > 0 BEGIN if ((SELECT COUNT(*) FROM sys.services WHERE name = 'SqlQueryNotificationService-354d49f8-e145-4f9c-9057-39e901899980') > 0)   DROP SERVICE [SqlQueryNotificationService-354d49f8-e145-4f9c-9057-39e901899980]; if (OBJECT_ID('SqlQueryNotificationService-354d49f8-e145-4f9c-9057-39e901899980', 'SQ') IS NOT NULL)   DROP QUEUE [SqlQueryNotificationService-354d49f8-e145-4f9c-9057-39e901899980]; DROP PROCEDURE [SqlQueryNotificationStoredProcedure-354d49f8-e145-4f9c-9057-39e901899980]; END COMMIT TRANSACTION; END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SqlQueryNotificationStoredProcedure-533211d3-af37-4921-a248-284ab39632a9] AS BEGIN BEGIN TRANSACTION; RECEIVE TOP(0) conversation_handle FROM [SqlQueryNotificationService-533211d3-af37-4921-a248-284ab39632a9]; IF (SELECT COUNT(*) FROM [SqlQueryNotificationService-533211d3-af37-4921-a248-284ab39632a9] WHERE message_type_name = 'http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer') > 0 BEGIN if ((SELECT COUNT(*) FROM sys.services WHERE name = 'SqlQueryNotificationService-533211d3-af37-4921-a248-284ab39632a9') > 0)   DROP SERVICE [SqlQueryNotificationService-533211d3-af37-4921-a248-284ab39632a9]; if (OBJECT_ID('SqlQueryNotificationService-533211d3-af37-4921-a248-284ab39632a9', 'SQ') IS NOT NULL)   DROP QUEUE [SqlQueryNotificationService-533211d3-af37-4921-a248-284ab39632a9]; DROP PROCEDURE [SqlQueryNotificationStoredProcedure-533211d3-af37-4921-a248-284ab39632a9]; END COMMIT TRANSACTION; END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SqlQueryNotificationStoredProcedure-7aa5674c-89e7-4ffe-9d3d-4e16fdd4977c] AS BEGIN BEGIN TRANSACTION; RECEIVE TOP(0) conversation_handle FROM [SqlQueryNotificationService-7aa5674c-89e7-4ffe-9d3d-4e16fdd4977c]; IF (SELECT COUNT(*) FROM [SqlQueryNotificationService-7aa5674c-89e7-4ffe-9d3d-4e16fdd4977c] WHERE message_type_name = 'http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer') > 0 BEGIN if ((SELECT COUNT(*) FROM sys.services WHERE name = 'SqlQueryNotificationService-7aa5674c-89e7-4ffe-9d3d-4e16fdd4977c') > 0)   DROP SERVICE [SqlQueryNotificationService-7aa5674c-89e7-4ffe-9d3d-4e16fdd4977c]; if (OBJECT_ID('SqlQueryNotificationService-7aa5674c-89e7-4ffe-9d3d-4e16fdd4977c', 'SQ') IS NOT NULL)   DROP QUEUE [SqlQueryNotificationService-7aa5674c-89e7-4ffe-9d3d-4e16fdd4977c]; DROP PROCEDURE [SqlQueryNotificationStoredProcedure-7aa5674c-89e7-4ffe-9d3d-4e16fdd4977c]; END COMMIT TRANSACTION; END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SqlQueryNotificationStoredProcedure-965450ba-c45c-4619-84a5-b44b99be6b16] AS BEGIN BEGIN TRANSACTION; RECEIVE TOP(0) conversation_handle FROM [SqlQueryNotificationService-965450ba-c45c-4619-84a5-b44b99be6b16]; IF (SELECT COUNT(*) FROM [SqlQueryNotificationService-965450ba-c45c-4619-84a5-b44b99be6b16] WHERE message_type_name = 'http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer') > 0 BEGIN if ((SELECT COUNT(*) FROM sys.services WHERE name = 'SqlQueryNotificationService-965450ba-c45c-4619-84a5-b44b99be6b16') > 0)   DROP SERVICE [SqlQueryNotificationService-965450ba-c45c-4619-84a5-b44b99be6b16]; if (OBJECT_ID('SqlQueryNotificationService-965450ba-c45c-4619-84a5-b44b99be6b16', 'SQ') IS NOT NULL)   DROP QUEUE [SqlQueryNotificationService-965450ba-c45c-4619-84a5-b44b99be6b16]; DROP PROCEDURE [SqlQueryNotificationStoredProcedure-965450ba-c45c-4619-84a5-b44b99be6b16]; END COMMIT TRANSACTION; END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SqlQueryNotificationStoredProcedure-c9f4da69-4daf-4964-9cbb-bbe52c3aaf61] AS BEGIN BEGIN TRANSACTION; RECEIVE TOP(0) conversation_handle FROM [SqlQueryNotificationService-c9f4da69-4daf-4964-9cbb-bbe52c3aaf61]; IF (SELECT COUNT(*) FROM [SqlQueryNotificationService-c9f4da69-4daf-4964-9cbb-bbe52c3aaf61] WHERE message_type_name = 'http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer') > 0 BEGIN if ((SELECT COUNT(*) FROM sys.services WHERE name = 'SqlQueryNotificationService-c9f4da69-4daf-4964-9cbb-bbe52c3aaf61') > 0)   DROP SERVICE [SqlQueryNotificationService-c9f4da69-4daf-4964-9cbb-bbe52c3aaf61]; if (OBJECT_ID('SqlQueryNotificationService-c9f4da69-4daf-4964-9cbb-bbe52c3aaf61', 'SQ') IS NOT NULL)   DROP QUEUE [SqlQueryNotificationService-c9f4da69-4daf-4964-9cbb-bbe52c3aaf61]; DROP PROCEDURE [SqlQueryNotificationStoredProcedure-c9f4da69-4daf-4964-9cbb-bbe52c3aaf61]; END COMMIT TRANSACTION; END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[TRADING_CAPTURE]
	@date_from date,
	@date_to date,
	@bot_acc_summary_id nvarchar(50)
AS
BEGIN
	
	Select * from ib_bot_trade_capture where bot_trades_date between @date_from and @date_to
	and bot_acc_summary_id = @bot_acc_summary_id
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[UPDATE_IB_SETTINGS]
	@ib_connection_way nvarchar(max),
	@ib_connection_type nvarchar(max),
	@ib_host_ip nvarchar(max),
	@ib_host_port nvarchar(max),
	@ib_client_id int
AS
BEGIN
	
	UPDATE ib_settings Set ib_connection_way = @ib_connection_way ,
            ib_connection_type = @ib_connection_type ,
            ib_host_ip = @ib_host_ip ,
            ib_host_port = @ib_host_port ,
            ib_client_id = @ib_client_id
            Where ID = 1
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[UPDATE_ORDER_STATUS]
	@BotId nvarchar(50),
	@Ticker nvarchar(25),
	@Status_Description nvarchar(max),
	@IBKR_Order_id int,
	@Id int
AS
BEGIN
	DECLARE @CurRawTime int = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE());

	if @Status_Description = 'Submitted' or @Status_Description = 'PreSubmitted' or @Status_Description = 'PendingSubmit'
		Begin

			Update BotOrderInstruction set 
			OrderStatus = 1 ,
			IBKR_Order_id = @IBKR_Order_id,
			[LastRawTime] = @CurRawTime
			Where Id = @Id

			goto myend
		End


	if @Status_Description = 'Filled'
		Begin
			
			Update BotOrderInstruction set OrderStatus = 3, [LastRawTime] = @CurRawTime
			Where Id = @Id
			--and (OrderStatus = 1 or OrderStatus = 2)

			goto myend
		End

myend:
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_event_query]
(
	@EventType varchar(50),
	@EventId varchar(100)
)
AS
BEGIN

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [Id]
		  ,[EventType]
		  ,[EventId]
		  ,[Message]
		  ,[TimeStamp]
		FROM [dbo].[Event]
		WHERE [EventType] = @EventType AND [EventId] = @EventId;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_event_select]
(
	@Id int
)
AS
BEGIN

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [Id]
		  ,[EventType]
		  ,[EventId]
		  ,[Message]
		  ,[TimeStamp]
		FROM [dbo].[Event]
		WHERE @Id = -1 OR @Id = [Id]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_event_select_active]
(
	@Seconds int
)
AS
BEGIN

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @TimeSince datetime = DATEADD(SECOND, -@Seconds, GETDATE());

	SELECT [Id]
		  ,[EventType]
		  ,[EventId]
		  ,[Message]
		  ,[TimeStamp]
		FROM [dbo].[Event]
		WHERE [TimeStamp] > @TimeSince;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_event_touch]
(
	@EventType varchar(50),
	@EventId varchar(100)
)

AS
BEGIN
	SET NOCOUNT ON;
	UPDATE [Event] SET [TimeStamp] = getdate() WHERE [EventType] = @EventType AND [EventId] = @EventId;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_order_set_status]
(
	@Id int,
	@OrderStatus int,
	@QtyFilled int,
	@FillPrice decimal(18,4),
	@BrokerId int
)

AS
BEGIN

	UPDATE [BotOrderInstruction]  SET
			  [OrderStatus] = @OrderStatus
			, [QtyFilled] = @QtyFilled
			, [FillPrice]= @FillPrice
			, [IBKR_Order_id] = @BrokerId
			, [LastRawTime] = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE())
		WHERE Id = @Id

	RETURN @@ROWCOUNT;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_orders_retrieve_last_partially_filled] 
	@RefId varchar(36),
	@OrderTag varchar(50)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT TOP 1
			 [Id]
			,[BotId]
			,[RefId]
			,[RawTime]
			,[Ticker]
			,[Action]
			,[OrderTag]
			,[OrderType]
			,[Qty]
			,[Price]
			,[TimeStamp]
			,[OrderStatus]
			,[QtyFilled]
			,[FillPrice]
			,[IBKR_Order_id]
			,[LastRawTime]
		FROM [BotOrderInstruction] 
		WHERE [RefId] = @RefId AND [OrderTag] = @OrderTag AND [OrderStatus] = 2 
		ORDER BY Id DESC;

END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_orders_retrieve_new] 
	@TickerSymbol nvarchar(25) = '',
	@TargetBotId varchar(50) = '',
	@ExipryInMinutes int = 9999999
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	DECLARE @BotOrderInstruction TABLE ([Id] [int] NOT NULL);

	WITH RankedRecords AS (
		SELECT [Id] ,ROW_NUMBER() OVER (PARTITION BY RefId, [OrderTag] ORDER BY [Id] DESC) AS RowNum
			FROM [dbo].[BotOrderInstruction] 
			WHERE [OrderStatus] in (0) AND (@TargetBotId = '' OR [BotId] = @TargetBotId) AND (@TickerSymbol = '' OR [Ticker] = @TickerSymbol)
	)
	INSERT INTO @BotOrderInstruction
	SELECT   [Id]
		FROM RankedRecords 
		WHERE RowNum = 1
		ORDER by Id ASC;


	DECLARE @Id int;
	DECLARE @RefId varchar(36);
	DECLARE @RawTime int;
	DECLARE @OrderTag varchar(50);
	DECLARE @Qty int;
	DECLARE @Price decimal(18, 4);
	DECLARE @OrderStatus int;
	DECLARE @QtyFilled int;
	DECLARE @FillPrice decimal(18, 4);
	DECLARE @IBKR_Order_id int;
	DECLARE @CurRawTime int = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE());

	DECLARE BotOrderCursor CURSOR FOR SELECT [Id] FROM @BotOrderInstruction

	BEGIN TRY
		-- do this set of operation in a transaction so that it does not interfer with other operations trying to update or add to the table 
		BEGIN TRANSACTION

		OPEN BotOrderCursor
		FETCH NEXT FROM BotOrderCursor INTO @Id;

		WHILE @@FETCH_STATUS = 0
		BEGIN
			SELECT   @RefId = [RefId]
					,@RawTime = [RawTime]
					,@OrderTag = [OrderTag]
					,@Qty = [Qty]
					,@Price = [Price]
					,@OrderStatus = [OrderStatus]
					,@QtyFilled = [QtyFilled]
					,@FillPrice = [FillPrice]
					,@IBKR_Order_id = [IBKR_Order_id]
				FROM [BotOrderInstruction]
				WHERE [Id] = @Id;

				IF (@OrderStatus != 0)
				BEGIN
					CONTINUE;
				END

			-- Modify all records that are still in new (OrderStatus = 0) to -3 (ignored) exccept this one that we want to process
			UPDATE [BotOrderInstruction] SET [OrderStatus] = -3, [LastRawTime] = @CurRawTime WHERE [RefId] = @RefId AND [OrderTag] = @OrderTag AND [OrderStatus]  = 0 AND [Id] != @Id;

			-- Mark records that are submitted as canceclled (OrderStatus -1)
			UPDATE [BotOrderInstruction] SET [OrderStatus] = -1, [LastRawTime] = @CurRawTime WHERE [RefId] = @RefId AND [OrderTag] = @OrderTag AND [OrderStatus]  = 1 AND [Id] != @Id;

			-- Check if the order is already COMPLETELY FILLED
			IF EXISTS(SELECT 1 FROM [BotOrderInstruction] WHERE [RefId] = @RefId AND [OrderTag] = @OrderTag AND [OrderStatus] = 3)
			BEGIN
				-- there exists a record that is FULLY FILLED
				-- MARK this record as ignored (update the status to -3)
				UPDATE [BotOrderInstruction] SET [OrderStatus] = -3, [LastRawTime] = @CurRawTime WHERE [Id] = @Id;
			END 
			-- Order Expiry Check
			ELSE IF ((@CurRawTime - @RawTime) / 60 > @ExipryInMinutes)
			BEGIN
				-- MARK this record as expired (update the status of it to -2) and do not include it in the return record set (CanDelete = 1)
				UPDATE [BotOrderInstruction] SET [OrderStatus] = -2, [LastRawTime] = @CurRawTime WHERE [Id] = @Id;
			END
			ELSE
			BEGIN
				-- Finally Modify this record as picked 
				UPDATE [BotOrderInstruction] SET [OrderStatus] = 1, [LastRawTime] = @CurRawTime WHERE [Id] = @Id;
			END

			---- PARTIAL FILLED Check -- if qty is > remaining set it to -3
			--DECLARE @TotalQtyFilled int;
			--DECLARE @AvgFillPrice decimal (18,4);

			---- Partial Fill logic
			--SELECT TOP 1 @AvgFillPrice = [FillPrice], @TotalQtyFilled = [QtyFilled] FROM [BotOrderInstruction] WHERE [RefId] = @RefId AND [OrderTag] = @OrderTag AND [OrderStatus] = 2 ORDER BY Id DESC;
			--IF (@@ROWCOUNT > 0)
			--BEGIN
			--	IF (@TotalQtyFilled >= @Qty ) 
			--	BEGIN
			--		-- should not happen ignore this record and mark it as ignored (OrderStatus = -3)
			--		UPDATE [BotOrderInstruction] SET [OrderStatus] = -3, [LastRawTime] = @CurRawTime WHERE [Id] = @Id;
			--	END ELSE BEGIN
			--		UPDATE [BotOrderInstruction] SET [QtyFilled] = @TotalQtyFilled, [FillPrice] = @AvgFillPrice, [LastRawTime] = @CurRawTime WHERE [Id] = @Id;
			--	END
			--END

			FETCH NEXT FROM BotOrderCursor INTO @Id
		END;
		CLOSE BotOrderCursor;
		DEALLOCATE BotOrderCursor;

		COMMIT TRANSACTION

		SELECT   [Id]
				,[BotId]
				,[RefId]
				,[RawTime]
				,[Ticker]
				,[Action]
				,[OrderTag]
				,[OrderType]
				,[Qty]
				,[Price]
				,[TimeStamp]
				,[OrderStatus]
				,[QtyFilled]
				,[FillPrice]
				,[IBKR_Order_id]
				,[LastRawTime]
			FROM [BotOrderInstruction]
			WHERE OrderStatus = 1   -- Return new orders to be submitted (1), old orders that need to be canceled (-9) and partially filled orders that need to be canceled (4)


	END TRY
	BEGIN CATCH
	    -- Rollback the transaction in case of an error
        IF @@TRANCOUNT > 0
		BEGIN
            ROLLBACK TRANSACTION;
		END

		-- Log the error or rethrow it
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

		-- Re-throw the error to propagate it
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH;



END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[PARTIAL_fill_STATUS]
   ON  [dbo].[AutomatedBot_trade_capture] 
   AFTER INSERT,UPDATE
AS 
BEGIN
	
	Declare @bot_acc_summary_id nvarchar(50),
			@bot_trades_symbol nvarchar(50),
			@Bot_Order_Instruction_id int,
			@IBKR_Order_id int,
			@bot_trades_cumQty float,
			@FillPrice decimal(18,4),
			@check_Order_Status int,
			@id_bot_trades int,
			@CurRawTime int = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE()),
			@BotOrderInstruction_id int

	SELECT @id_bot_trades = id_bot_trades , @bot_acc_summary_id = bot_acc_summary_id , @bot_trades_symbol = bot_trades_symbol , 
		   @IBKR_Order_id = IBKR_Order_id , @bot_trades_cumQty = bot_trades_cumQty ,
		   @FillPrice = bot_trades_price
	FROM INSERTED;

	Select @BotOrderInstruction_id = id , @check_Order_Status = OrderStatus from BotOrderInstruction
	Where IBKR_Order_id = @IBKR_Order_id

	if @check_Order_Status = 3
		Begin
			Update BotOrderInstruction Set QtyFilled = @bot_trades_cumQty , FillPrice = @FillPrice,
			[LastRawTime] = @CurRawTime
			where IBKR_Order_id = @IBKR_Order_id and id = @BotOrderInstruction_id
		End
	Else
		Begin
			Update BotOrderInstruction Set OrderStatus = 2 , QtyFilled = @bot_trades_cumQty , FillPrice = @FillPrice,
			[LastRawTime] = @CurRawTime
			where IBKR_Order_id = @IBKR_Order_id and OrderStatus <> 3 and id = @BotOrderInstruction_id
		End
	

	Select @Bot_Order_Instruction_id = id from BotOrderInstruction 
	Where BotId = @bot_acc_summary_id and Ticker = @bot_trades_symbol and IBKR_Order_id = @IBKR_Order_id

	Update AutomatedBot_trade_capture set BotOrderInstruction_id = @Bot_Order_Instruction_id where 
	id_bot_trades = @id_bot_trades;

END
GO
ALTER TABLE [dbo].[AutomatedBot_trade_capture] ENABLE TRIGGER [PARTIAL_fill_STATUS]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[Instruction_Log_Time_Stamp] 
	ON  [dbo].[BotOrderInstruction] 
   AFTER INSERT
AS 
BEGIN
	
	Declare @Id int,
			@BotId nvarchar(50),
			@timeStamp nvarchar(max)

	Set @timeStamp = CONCAT(CONVERT(Date, GETDATE()) , ' ' , datepart(HOUR, GETDATE()) , ':' , datepart(minute, GETDATE()), ':' , datepart(SECOND, GETDATE()), ':' , datepart(MILLISECOND, GETDATE()) )

	Select @Id = Id , @BotId = BotId from inserted

	insert into Instruction_Log (Instruction_id , BotId , Instruction_timeStamp) 
	Values (@Id , @BotId , @timeStamp)
END
GO
ALTER TABLE [dbo].[BotOrderInstruction] ENABLE TRIGGER [Instruction_Log_Time_Stamp]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[trigBotOrderInstructionOnChange]
   ON  [dbo].[BotOrderInstruction]
   AFTER INSERT, UPDATE
AS 
BEGIN
	SET NOCOUNT ON;

 -- Only proceed if rows were actually affected
    IF NOT EXISTS (SELECT 1 FROM inserted)
        RETURN;
		
	DECLARE @EventType varchar(50);
	DECLARE @EventId varchar(100);
	DECLARE @Message varchar(256);
    SELECT  TOP 1 
			@EventType = 'BOTORDERINSTRUCTION',
			@EventId = CAST(i.BotId AS VARCHAR(10)),
			@Message = 'Inserted Automatically from BotOrderInstruction Trigger' 
		FROM inserted i;

	IF @EventType = 'BOTORDERINSTRUCTION' 
	BEGIN
		UPDATE [Event]
			SET TimeStamp = GETDATE()
			WHERE [EventType] = @EventType AND [EventId] = @EventId;

		IF @@ROWCOUNT = 0
		BEGIN
			-- If no row was updated, perform an INSERT
			INSERT INTO [Event] ([EventType], [EventId], [Message], [TimeStamp])
			VALUES (@EventType, @EventId, @Message, GETDATE());
		END
	END
END


GO
ALTER TABLE [dbo].[BotOrderInstruction] ENABLE TRIGGER [trigBotOrderInstructionOnChange]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[trigBotOrderInstructionOnDelete]
   ON  [dbo].[BotOrderInstruction]
   AFTER DELETE
AS 
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Handle all deleted rows
    MERGE INTO [Event] AS target
    USING (
        SELECT 
            'BOTORDERINSTRUCTION_DEL' AS EventType,
            CAST(BotId AS VARCHAR(10)) AS EventId,
            'Inserted Automatically from BotOrderInstruction Trigger' AS Message,
            MAX(GETDATE()) AS TimeStamp
        FROM deleted
		GROUP BY BotId  -- Group by the ID to ensure one row per EventId
    ) AS source
    ON (target.[EventType] = source.[EventType] AND target.[EventId] = source.[EventId])
    WHEN MATCHED THEN
        UPDATE SET 
            TimeStamp = source.TimeStamp,
            Message = source.Message
    WHEN NOT MATCHED THEN
        INSERT ([EventType], [EventId], [Message], [TimeStamp])
        VALUES (source.EventType, source.EventId, source.Message, source.TimeStamp);

END
GO
ALTER TABLE [dbo].[BotOrderInstruction] ENABLE TRIGGER [trigBotOrderInstructionOnDelete]
GO
