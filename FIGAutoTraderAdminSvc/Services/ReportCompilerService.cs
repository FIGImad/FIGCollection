namespace FIGAutoTraderAdminSvc.Services
{
    public class ReportCompilerService
    {
        protected readonly IConfiguration _config;
        protected readonly ILogger<ReportCompilerService> _logger;
        private object _lockObj = new object();

        public ReportCompilerService(ILogger<ReportCompilerService> logger,
                                     IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        //public List<MonthlyRecDto> NetReport(int autoTradeId)
        //{
        //    List<MonthlyRecDto> strategyData = new();
        //    try
        //    {
        //        lock (_lockObj)
        //        {
        //            // 1- Retrieve AutoTrade Record
        //            var autoTrade = MainRepo.GetAutoTrade(autoTradeId);
        //            if (autoTrade != null)
        //            {
        //                // 2- Retrieve all TradeData for the AutoTrade
        //                var lst = MainRepo.SelectTrackData(autoTradeId, 0);

        //                // 3- find all sides of all strategies in the trackData and create a list of strategySides
        //                List<Tuple<string, decimal>> strategySides = new();
        //                lst.ForEach(trackData =>
        //                {
        //                    var strategyDataMap = JsonConvert.DeserializeObject<Dictionary<string, StrategyTrackData>>(trackData.StrategyData);
        //                    // collect from strategy data the available strategy Sides
        //                    if (strategyDataMap != null)
        //                    {
        //                        foreach (var key in strategyDataMap.Keys)
        //                        {
        //                            var strategyData = strategyDataMap[key];
        //                            if (strategyData == null)
        //                            {
        //                                continue;
        //                            }
        //                            strategyData.dataList.ForEach(strategyRec =>
        //                            {
        //                                if (!strategySides.Any(s => s.Item1 == key && s.Item2 == strategyRec.Side))
        //                                {
        //                                    strategySides.Add(new Tuple<string, decimal>(key, strategyRec.Side));
        //                                }
        //                                if (!strategySides.Any(s => s.Item1 == key && s.Item2 == 0))  // all for the strategy
        //                                {
        //                                    strategySides.Add(new Tuple<string, decimal>(key, 0));
        //                                }
        //                            });
        //                        }
        //                    }
        //                });

        //                // 4- Compile data for each sides of each strategies for each month
        //                MonthlyRecDto cMonthlyRec = new MonthlyRecDto();
        //                MonthlyRecDto pMonthlyRec = new MonthlyRecDto();
        //                lst.ForEach(trackData =>
        //                {
        //                    // from trackData.RawTime (epoch time) create UTC YYYYMM integer 
        //                    var yearMonth = DateTimeUtil.FormatDate(DateTimeUtil.ConvertUnixTimeToDateTime(trackData.RawTime), "yyyyMM");
        //                    var strategyDataMap = JsonConvert.DeserializeObject<Dictionary<string, StrategyTrackData>>(trackData.StrategyData);
        //                    if (cMonthlyRec.YearMonth != yearMonth)
        //                    {
        //                        pMonthlyRec = cMonthlyRec;

        //                        // create MonthlyRecDto for each yearMonth
        //                        cMonthlyRec = new MonthlyRecDto
        //                        {
        //                            YearMonth = yearMonth,
        //                            strategyData = new List<MonthlyDataDto>(),
        //                            netData = new MonthlyDataDto()
        //                            {
        //                                Strategy = "ALL",
        //                                YearMonth = yearMonth,
        //                                Side = 0
        //                            },
        //                        };
        //                        // create List<MonthlyDataDto> for each strategySides
        //                        strategySides.ForEach(s =>
        //                        {
        //                            cMonthlyRec.strategyData.Add(new MonthlyDataDto
        //                            {
        //                                Strategy = s.Item1,
        //                                YearMonth = yearMonth,
        //                                Side = s.Item2
        //                            });
        //                        });
        //                        strategyData.Add(cMonthlyRec);
        //                    }

        //                    // create List<MonthlyDataDto> for each strategySides
        //                    if (strategyDataMap != null)
        //                    {
        //                        // now that we gave strategyDataMap, iterate through strategyDataMap to compile monthly aggregate data
        //                        foreach (var key in strategyDataMap.Keys)
        //                        {
        //                            var strategyData = strategyDataMap[key];
        //                            if (strategyData == null)
        //                            {
        //                                continue;
        //                            }
        //                            strategyData.dataList.ForEach(strategyRec =>
        //                            {
        //                                if (cMonthlyRec.strategyData.Any(s => s.Strategy == key && s.Side == strategyRec.Side))
        //                                {
        //                                    // get the MonthlyDataDto from cMonthlyRec.strategyData that matches key and side
        //                                    var cMonthlyData = cMonthlyRec.strategyData.Find(s => s.Strategy == key && s.Side == strategyRec.Side);
        //                                    var pMonthlyData = pMonthlyRec.strategyData?.Find(s => s.Strategy == key && s.Side == strategyRec.Side);

        //                                    // update the MonthlyDataDto with the strategy data
        //                                    if (cMonthlyData != null)
        //                                    {
        //                                        int pNumBars = pMonthlyData?.NumBars ?? 0;
        //                                        int pFreq = pMonthlyData?.NumTrades ?? 0;
        //                                        decimal pClosePL = pMonthlyData?.ProfitLoss ?? 0;
        //                                        cMonthlyData.NumBars = strategyRec.BarCount - pNumBars;
        //                                        cMonthlyData.NumTrades = strategyRec.Freq - pFreq;
        //                                        cMonthlyData.ProfitLoss = strategyRec.ClosePL - pClosePL;
        //                                    }
        //                                }
        //                            });
        //                        }

        //                    }
        //                });

        //                // 5- Compile data for all sides of each strategies for each month
        //                strategyData.ForEach(monthlyRec =>
        //                {
        //                    monthlyRec.netData.NumBars = monthlyRec.strategyData.Sum(s => s.NumBars);
        //                    monthlyRec.netData.NumTrades = monthlyRec.strategyData.Sum(s => s.NumTrades);
        //                    monthlyRec.netData.ProfitLoss = monthlyRec.strategyData.Sum(s => s.ProfitLoss);

        //                    // create List<MonthlyDataDto> for each strategySides
        //                    var strategyAllData = monthlyRec.strategyData.Where(s => s.Side == 0).ToList();
        //                    if (strategyAllData.Count > 0)
        //                    {
        //                        strategyAllData.ForEach(s =>
        //                        {
        //                            List<MonthlyDataDto> dList = monthlyRec.strategyData.Where(s2 => s2.Strategy == s.Strategy && s2.Side != 0).ToList();
        //                            s.NumBars = dList.Sum(s2 => s2.NumBars);
        //                            s.NumTrades = dList.Sum(s2 => s2.NumTrades);
        //                            s.ProfitLoss = dList.Sum(s2 => s2.ProfitLoss);
        //                        });
        //                    }
        //                });
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger?.LogDebug("MainProcess: exception occurred - {0}", ex.Message);
        //    }
        //    return strategyData;
        //}
    }
}


