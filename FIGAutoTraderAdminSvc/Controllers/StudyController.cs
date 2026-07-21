using FIGCommon.Controllers;
using FIGCommon.DataAccess;
using FIGCommon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FIGAutoTraderAdminSvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudyController : FIGBaseController
    {
        public StudyController(
                   IWebHostEnvironment hostEnvironment
                   , IHttpContextAccessor httpContextAccessor
                   , ILogger<StudyController> logger
                   , IServiceProvider serviceProvider
                   , IConfiguration config
                   ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
        }

        #region PriceSource
        // GET api/study/pricesrc
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("pricesrc")]
        [HttpGet]
        public async Task<IActionResult> GetPriceSource()
        {
            try
            {
                var priceSrcs = MainRepo.GetPriceSrcs();
                return Ok(priceSrcs.ToArray());
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        #endregion PriceSource

        //#region Study

        //// GET api/study/cross_ma/{dataSetId}/{cycle}/{brk}/{priceSrc}/{rawTime}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("cross_ma/{dataSetId}/{cycle}/{brk}/{priceSrc}/{rawTime}")]
        //[HttpGet]
        //public async Task<IActionResult> MACrossStudy(int dataSetId, int cycle, int brk, string priceSrc, long rawTime)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }

        //        var priceInput15M = SourceData.GetDataByInterval(priceData, "15");
        //        var priceInput = SourceData.GetSourceData(priceInput15M, priceSrc);
        //        decimal[] cycleMA = new decimal[priceInput.Count];
        //        decimal[] breakMA = new decimal[priceInput.Count];
        //        TI_MA.GetData(priceInput, cycle, out cycleMA);
        //        TI_MA.GetData(priceInput, brk, out breakMA);
        //        StudyCrossParams[] crossParams;
        //        List<StudyCrossParams> crossParamsReturn = new();
        //        TI_StudyCross.GetData(priceInput15M, cycleMA, breakMA, out crossParams);
        //        if (rawTime <= 0)
        //        {
        //            return Ok(crossParams);
        //        }
        //        for (int i = crossParams.Length - 1; i >= 0; i--)
        //        {
        //            if (crossParams[i].RawTime >= rawTime)
        //            {
        //                crossParamsReturn.Insert(0, crossParams[i]);
        //            }

        //        }
        //        return Ok(crossParamsReturn.ToArray());
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}


        //// GET api/study/trend_ma/{dataSetId}/{period}/{priceSrc}/{rawTime}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("trend_ma/{dataSetId}/{period}/{priceSrc}/{rawTime}")]
        //[HttpGet]
        //public async Task<IActionResult> TrendStudyMA(int dataSetId, int period, string priceSrc, long rawTime)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }

        //        var priceInput = SourceData.GetSourceData(priceData, priceSrc);
        //        decimal[] ma = new decimal[priceInput.Count];
        //        TI_MA.GetData(priceInput, period, out ma);
        //        StudyTrendParm[] trendParams;
        //        List<StudyTrendParm> trendParamsReturn = new();
        //        TI_StudyTrend.GetData(priceData, ma, out trendParams);
        //        if (rawTime <= 0)
        //        {
        //            return Ok(trendParams);
        //        }
        //        for (int i = trendParams.Length - 1; i >= 0; i--)
        //        {
        //            if (trendParams[i].RawTime >= rawTime)
        //            {
        //                trendParamsReturn.Insert(0, trendParams[i]);
        //            }

        //        }
        //        return Ok(trendParamsReturn.ToArray());
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}

        //// GET api/study/trend/{option}/{dataSetId}/{period}/{stddev}/{priceSrc}/{rawTime}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("trend/{option}/{dataSetId}/{period}/{stddev}/{priceSrc}/{rawTime}")]
        //[HttpGet]
        //public async Task<IActionResult> TrendStudy(int dataSetId, string option, int period, double stddev, string priceSrc, long rawTime)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }

        //        var priceInput = SourceData.GetSourceData(priceData, priceSrc);
        //        decimal[] ma = new decimal[priceInput.Count];
        //        decimal[] ub = new decimal[priceInput.Count];
        //        decimal[] lb = new decimal[priceInput.Count];
        //        TI_BB.GetData(priceInput, period, stddev, out ub, out ma, out lb);
        //        StudyTrendParm[] trendParams;
        //        List<StudyTrendParm> trendParamsReturn = new();
        //        TI_StudyTrend.GetData(priceData, (option == "MA" ? ma : option == "UB"? ub : lb), out trendParams);
        //        if (rawTime <= 0)
        //        {
        //            return Ok(trendParams);
        //        }
        //        for (int i = trendParams.Length - 1; i >= 0; i--)
        //        {
        //            if (trendParams[i].RawTime >= rawTime)
        //            {
        //                trendParamsReturn.Insert(0, trendParams[i]);
        //            }

        //        }
        //        return Ok(trendParamsReturn.ToArray());
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}


        //// GET api/study/sem/{dataSetId}/{len}/{priceSrc}/{rawTime}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("sem/{dataSetId}/{len}/{priceSrc}/{rawTime}")]
        //[HttpGet]
        //public async Task<IActionResult> SEMStudy(int dataSetId, int len, string priceSrc, long rawTime)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }

        //        StudySEMParams[] semParams;
        //        TI_SEM.GetData(priceData, len, priceSrc, out semParams);

        //        return Ok(semParams);
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}

        //// GET api/study/cycledir/{dataSetId}/{period}/{stdDev}/{priceSrc}/{rawTime}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("cycledir/{dataSetId}/{period}/{stdDev}/{priceSrc}/{rawTime}")]
        //[HttpGet]
        //public async Task<IActionResult> CycleDirStudy(int dataSetId, int period, decimal stdDev, string priceSrc, long rawTime)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }

        //        var priceInput = SourceData.GetSourceData(priceData, priceSrc);
        //        decimal[] mb = new decimal[priceInput.Count];
        //        decimal[] ub = new decimal[priceInput.Count];
        //        decimal[] lb = new decimal[priceInput.Count];
        //        TI_BB.GetData(priceInput, period, (double)stdDev, out ub, out mb, out lb);
        //        StudyCycleDirParm[] dataOut;
        //        TI_StudyCycleDir.GetData(priceData, ub, lb, mb, out dataOut);
        //        if (rawTime <= 0)
        //        {
        //            return Ok(dataOut);
        //        }
        //        List<StudyCycleDirParm> dataOutTemp = new();
        //        for (int i = dataOut.Length - 1; i >= 0; i--)
        //        {
        //            if (dataOut[i].RawTime >= rawTime)
        //            {
        //                dataOutTemp.Insert(0, dataOut[i]);
        //            }
        //        }
        //        return Ok(dataOutTemp.ToArray());
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}

        //// GET api/study/pivotlevel/{dataSetId}/{period}/{stdDev}/{priceSrc}/{rawTime}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("pivotlevel/{dataSetId}/{period}/{stdDev}/{priceSrc}/{rawTime}")]
        //[HttpGet]
        //public async Task<IActionResult> PivotLevelStudy(int dataSetId, int period, decimal stdDev, string priceSrc, long rawTime)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }

        //        var priceInput = SourceData.GetSourceData(priceData, priceSrc);
        //        decimal[] mb = new decimal[priceInput.Count];
        //        decimal[] ub = new decimal[priceInput.Count];
        //        decimal[] lb = new decimal[priceInput.Count];
        //        StudyCycleDirParm[] cycleDir;
        //        PivotLevelParm[] pivotLevelOut;
        //        TI_BB.GetData(priceInput, period, (double)stdDev, out ub, out mb, out lb);
        //        TI_StudyCycleDir.GetData(priceData, ub, lb, mb, out cycleDir);
        //        TI_PivotLevel.GetData(priceData, cycleDir, out pivotLevelOut);

        //        if (rawTime <= 0)
        //        {
        //            return Ok(pivotLevelOut);
        //        }
        //        List<PivotLevelParm> dataOutTemp = new();
        //        for (int i = pivotLevelOut.Length - 1; i >= 0; i--)
        //        {
        //            if (pivotLevelOut[i].RawTime >= rawTime)
        //            {
        //                dataOutTemp.Insert(0, pivotLevelOut[i]);
        //            }
        //        }
        //        return Ok(dataOutTemp.ToArray());
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}

        //private class SwitchFnParm
        //{
        //    public PriceDataRS Price = new();
        //    public long RawTime { get; set; } = 0;
        //    public decimal BreakSwitch { get; set; } = 0.0M;
        //    public decimal PreBreakSwitch { get; set; } = 0.0M;

        //    public SwitchFnParm() { }
        //    public SwitchFnParm(BreakSwitchParm breakSwitchParam, BreakSwitchParm preBreakSwitchParam)
        //    {
        //        this.BreakSwitch = breakSwitchParam.BreakSwitch;
        //        this.PreBreakSwitch = preBreakSwitchParam.BreakSwitch;
        //        this.Price = new PriceDataRS(breakSwitchParam.Price);
        //        this.RawTime = this.Price.RawTime;
        //    }
        //}
        //// GET api/study/breakswitch/{dataSetId}/{period}/{periodbreak}/{periodprebreak}/{stdDev}/{priceSrc}/{rawTime}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("breakswitch/{dataSetId}/{period}/{periodbreak}/{periodprebreak}/{stdDev}/{priceSrc}/{rawTime}")]
        //[HttpGet]
        //public async Task<IActionResult> BreakSwitchStudy(int dataSetId, int period, int periodBreak, int periodPreBreak, decimal stdDev, string priceSrc, long rawTime)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }

        //        var priceInput = SourceData.GetSourceData(priceData, priceSrc);
        //        decimal[] mbCycle = new decimal[priceInput.Count];
        //        decimal[] ubCycle = new decimal[priceInput.Count];
        //        decimal[] lbCycle = new decimal[priceInput.Count];
        //        decimal[] mbBreak = new decimal[priceInput.Count];
        //        decimal[] ubBreak = new decimal[priceInput.Count];
        //        decimal[] lbBreak = new decimal[priceInput.Count];
        //        decimal[] mbPreBreak = new decimal[priceInput.Count];
        //        decimal[] ubPreBreak = new decimal[priceInput.Count];
        //        decimal[] lbPreBreak = new decimal[priceInput.Count];
        //        StudyTrendParm[] trendCycle;
        //        StudyTrendParm[] trendBreak;
        //        StudyTrendParm[] trendPreBreak;
        //        StudyCycleDirParm[] crossSeries;
        //        BreakSwitchParm[] breakSwitch;
        //        BreakSwitchParm[] preBreakSwitch;
        //        TI_BB.GetData(priceInput, period, (double)stdDev, out ubCycle, out mbCycle, out lbCycle);
        //        TI_BB.GetData(priceInput, periodBreak, (double)stdDev, out ubBreak, out mbBreak, out lbBreak);
        //        TI_BB.GetData(priceInput, periodPreBreak, (double)stdDev, out ubPreBreak, out mbPreBreak, out lbPreBreak);
        //        TI_StudyTrend.GetData(priceData, mbCycle, out trendCycle);
        //        TI_StudyTrend.GetData(priceData, mbBreak, out trendBreak);
        //        TI_StudyTrend.GetData(priceData, mbPreBreak, out trendPreBreak);
        //        TI_StudyCycleDir.GetData(priceData, ubCycle, lbCycle, mbCycle, out crossSeries);
        //        TI_BreakSwitch.GetData(priceData, mbCycle, mbBreak, trendCycle, trendBreak, ubBreak, lbBreak, crossSeries, out breakSwitch);
        //        TI_BreakSwitch.GetData(priceData, mbBreak, mbBreak, trendBreak, trendPreBreak, ubPreBreak, lbPreBreak, crossSeries, out preBreakSwitch);

        //        SwitchFnParm[] outParam = new SwitchFnParm[priceInput.Count];


        //        for (int i = 0; i < priceData.Count; i++)
        //        {
        //            outParam[i] = new SwitchFnParm(breakSwitch[i], preBreakSwitch[i]);
        //        }
        //        if (rawTime <= 0)
        //        {
        //            return Ok(outParam);
        //        }
        //        List<SwitchFnParm> dataOutTemp = new();
        //        for (int i = outParam.Length - 1; i >= 0; i--)
        //        {
        //            if (outParam[i].RawTime >= rawTime)
        //            {
        //                dataOutTemp.Insert(0, outParam[i]);
        //            }
        //        }
        //        return Ok(dataOutTemp.ToArray());
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}

        //// GET api/study/breakcontrarian/{dataSetId}/{period}/{periodbreak}/{stdDev}/{priceSrc}/{rawTime}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("breakcontrarian/{dataSetId}/{period}/{periodbreak}/{stdDev}/{priceSrc}/{rawTime}")]
        //[HttpGet]
        //public async Task<IActionResult> BreakContrarianStudy(int dataSetId, int period, int periodBreak, decimal stdDev, string priceSrc, long rawTime)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }

        //        var priceInput = SourceData.GetSourceData(priceData, priceSrc);
        //        decimal[] mbCycle = new decimal[priceInput.Count];
        //        decimal[] ubCycle = new decimal[priceInput.Count];
        //        decimal[] lbCycle = new decimal[priceInput.Count];
        //        decimal[] mbBreak = new decimal[priceInput.Count];
        //        decimal[] ubBreak = new decimal[priceInput.Count];
        //        decimal[] lbBreak = new decimal[priceInput.Count];
        //        StudyTrendParm[] trendCycle;
        //        StudyTrendParm[] trendBreak;
        //        StudyCycleDirParm[] crossSeries;
        //        BreakSwitchParm[] breakSwitch;
        //        ContrarianBreakParam[] contrarianBreak;
        //        TI_BB.GetData(priceInput, period, (double)stdDev, out ubCycle, out mbCycle, out lbCycle);
        //        TI_BB.GetData(priceInput, periodBreak, (double)stdDev, out ubBreak, out mbBreak, out lbBreak);
        //        TI_StudyTrend.GetData(priceData, mbCycle, out trendCycle);
        //        TI_StudyTrend.GetData(priceData, mbBreak, out trendBreak);
        //        TI_StudyCycleDir.GetData(priceData, ubCycle, lbCycle, mbCycle, out crossSeries);
        //        TI_BreakSwitch.GetData(priceData, mbCycle, mbBreak, trendCycle, trendBreak, ubBreak, lbBreak, crossSeries, out breakSwitch);
        //        TI_ContrarianBreak.GetData(priceData, mbBreak, ubBreak, lbBreak, crossSeries, breakSwitch, out contrarianBreak);

        //        if (rawTime <= 0)
        //        {
        //            return Ok(contrarianBreak);
        //        }
        //        List<ContrarianBreakParam> dataOutTemp = new();
        //        for (int i = contrarianBreak.Length - 1; i >= 0; i--)
        //        {
        //            if (contrarianBreak[i].RawTime >= rawTime)
        //            {
        //                dataOutTemp.Insert(0, contrarianBreak[i]);
        //            }
        //        }
        //        return Ok(dataOutTemp.ToArray());
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}

        //// GET api/study/reversion/{dataSetId}/{period}/{stdDev}/{priceSrc}/{rawTime}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("reversion/{dataSetId}/{period}/{stdDev}/{priceSrc}/{rawTime}")]
        //[HttpGet]
        //public async Task<IActionResult> ReversionStudy(int dataSetId, int period, decimal stdDev, string priceSrc, long rawTime)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }

        //        var priceInput = SourceData.GetSourceData(priceData, priceSrc);
        //        decimal[] mbCycle = new decimal[priceInput.Count];
        //        decimal[] ubCycle = new decimal[priceInput.Count];
        //        decimal[] lbCycle = new decimal[priceInput.Count];
        //        StudyTrendParm[] trendCycle;
        //        StudyTrendParm[] trendUB;
        //        StudyTrendParm[] trendLB;
        //        StudyCycleDirParm[] crossSeries;
        //        TI_BB.GetData(priceInput, period, (double)stdDev, out ubCycle, out mbCycle, out lbCycle);
        //        TI_StudyTrend.GetData(priceData, mbCycle, out trendCycle);
        //        TI_StudyTrend.GetData(priceData, ubCycle, out trendUB);
        //        TI_StudyTrend.GetData(priceData, lbCycle, out trendLB);
        //        TI_StudyCycleDir.GetData(priceData, ubCycle, lbCycle, mbCycle, out crossSeries);
        //        ReversionParam[] reversion;
        //        TI_Reversion.GetData(priceData, mbCycle, ubCycle, lbCycle, trendCycle, trendUB, trendLB, crossSeries, out reversion);

        //        if (rawTime <= 0)
        //        {
        //            return Ok(reversion);
        //        }
        //        List<ReversionParam> dataOutTemp = new();
        //        for (int i = reversion.Length - 1; i >= 0; i--)
        //        {
        //            if (reversion[i].RawTime >= rawTime)
        //            {
        //                dataOutTemp.Insert(0, reversion[i]);
        //            }
        //        }
        //        return Ok(dataOutTemp.ToArray());
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}

        //// GET api/study/signal/{dataSetId}/{period}/{periodbreak}/{periodprebreak}/{stdDev}/{rawTime}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("signal/{dataSetId}/{period}/{periodbreak}/{periodprebreak}/{stdDev}/{rawTime}")]
        //[HttpGet]
        //public async Task<IActionResult> SignalStudy(int dataSetId, int period, int periodBreak, int periodPreBreak, decimal stdDev, long rawTime)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }
        //        TradeSignalParams[] signal;

        //        TI_TradeSignal.GetData(priceData, period, periodBreak, periodPreBreak, (double)stdDev, out signal);

        //        if (rawTime <= 0)
        //        {
        //            return Ok(signal);
        //        }
        //        List<TradeSignalParams> dataOutTemp = new();
        //        for (int i = signal.Length - 1; i >= 0; i--)
        //        {
        //            if (signal[i].RawTime >= rawTime)
        //            {
        //                dataOutTemp.Insert(0, signal[i]);
        //            }
        //        }
        //        return Ok(dataOutTemp.ToArray());
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}

        //// GET api/study/signal2/{dataSetId}/{period}/{stdDev}/{rawTime}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("signal2/{dataSetId}/{period}/{stdDev}/{rawTime}")]
        //[HttpGet]
        //public async Task<IActionResult> Signal2Study(int dataSetId, int period, decimal stdDev, long rawTime)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }
        //        TradeSignal2Params[] signal;

        //        TI_TradeSignal2.GetData(priceData, period, (double)stdDev, out signal);

        //        if (rawTime <= 0)
        //        {
        //            return Ok(signal);
        //        }
        //        List<TradeSignal2Params> dataOutTemp = new();
        //        for (int i = signal.Length - 1; i >= 0; i--)
        //        {
        //            if (signal[i].RawTime >= rawTime)
        //            {
        //                dataOutTemp.Insert(0, signal[i]);
        //            }
        //        }
        //        return Ok(dataOutTemp.ToArray());
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}


        //// GET api/study/test
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("test")]
        //[HttpGet]
        //public async Task<IActionResult> TestStudy()
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(7);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }

        //        //StudySEMParams[] study1;
        //        //TI2_SEM_Params study2 = new TI2_SEM_Params(200, "ohlc4");
        //        //TI_SEM.GetData(priceData, 200, "ohlc4", out study1);
        //        //TI2_SEM.GetData(priceData, ref study2);

        //        //decimal [] study1;
        //        //TI2_MA_Params study2 = new TI2_MA_Params(200, "ohlc4");
        //        //var priceInput = SourceData.GetSourceData(priceData, "ohlc4");
        //        //TI_MA.GetData(priceInput, 200, out study1);
        //        //TI2_MA.GetData(priceData, ref study2);

        //        //decimal[] ub;
        //        //decimal[] mid;
        //        //decimal[] lb;
        //        //TI2_BB_Params study2 = new TI2_BB_Params(200, 2.0D, "ohlc4");
        //        //var priceInput = SourceData.GetSourceData(priceData, "ohlc4");
        //        //TI_BB.GetData(priceInput, 200, 2.0D, out ub, out mid, out lb);
        //        //TI2_BB.GetData(priceData, ref study2);


        //        //TI2_BB_Params bbParams = new TI2_BB_Params(200, 2.0D, "ohlc4");
        //        //TI2_BB.GetData(priceData, ref bbParams);
        //        //decimal[] ub = new decimal[priceData.Count];
        //        //decimal[] mid = new decimal[priceData.Count];
        //        //decimal[] lb = new decimal[priceData.Count];
        //        //StudyCycleDirParm[] crossSeries;
        //        //TI2_CycleDir_Params cycleDirParams = new TI2_CycleDir_Params();
        //        //for (int ndx = 0; ndx < bbParams.study.Count; ndx++)
        //        //{
        //        //    ub[ndx] = bbParams.study[ndx].UB ?? 0;
        //        //    lb[ndx] = bbParams.study[ndx].LB ?? 0;
        //        //    mid[ndx] = bbParams.study[ndx].MID ?? 0;
        //        //}
        //        //TI_StudyCycleDir.GetData(priceData, ub, lb, mid, out crossSeries);
        //        //TI2_CycleDir.GetData(priceData, bbParams, ref cycleDirParams);


        //        TI2_BB_Params bbParams = new TI2_BB_Params(200, 2.0D, "ohlc4");
        //        TI2_BB.GetData(priceData, ref bbParams);
        //        TI2_UBLBMA_Reversion_Params ublbmaParams = new TI2_UBLBMA_Reversion_Params();
        //        TI2_UBLBMA_Reversion.GetData(priceData, bbParams, ref ublbmaParams);
        //        for (int i = 1; i < priceData.Count; i++)
        //        {
        //            var val1 = ((TI2_UBLBMA_Reversion_Study) ublbmaParams.study[i-1]).Rev;
        //            var val2 = ((TI2_UBLBMA_Reversion_Study)ublbmaParams.study[i]).Rev;
        //            if ((val1 - val2) != 0)
        //            {
        //                Console.WriteLine($"i= {i}, difference: {val1 - val2}");
        //            }
        //        }

        //        return Ok();
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}


        //// GET api/study/sem2/{dataSetId}/{len}/{priceSrc}/{rawTime}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("sem2/{dataSetId}/{len}/{priceSrc}/{rawTime}")]
        //[HttpGet]
        //public async Task<IActionResult> SEMS2tudy(int dataSetId, int len, string priceSrc, long rawTime)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }

        //        string tag = $"SEM2@{dataSetId}@{len}@{priceSrc}";
        //        TI2_SEM_Params sem2Params;
        //        TI2Parameter? tempParam;
        //        if (MainRepo.mapStudyParams.TryGetValue(tag, out tempParam))
        //        {
        //            sem2Params = (TI2_SEM_Params) tempParam;
        //        }
        //        else
        //        {
        //            sem2Params = new TI2_SEM_Params(len, priceSrc);
        //            MainRepo.mapStudyParams.Add(tag, sem2Params);
        //        }

        //        TI2_SEM.GetData(priceData, ref sem2Params);


        //        List<TI2_SEM_Study> retStudy = new List<TI2_SEM_Study>();
        //        sem2Params.study.ForEach(study => retStudy.Add((TI2_SEM_Study)study.Clone()));

        //        return Ok(retStudy);

        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}

        //// GET api/study/ma2/{dataSetId}/{len}/{priceSrc}/{rawTime}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("ma2/{dataSetId}/{len}/{priceSrc}/{rawTime}")]
        //[HttpGet]
        //public async Task<IActionResult> MA2tudy(int dataSetId, int len, string priceSrc, long rawTime)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }

        //        string tag = $"MA2@{dataSetId}@{len}@{priceSrc}";
        //        TI2_MA_Params studyParams;
        //        TI2Parameter? tempParam;
        //        if (MainRepo.mapStudyParams.TryGetValue(tag, out tempParam))
        //        {
        //            studyParams = (TI2_MA_Params)tempParam;
        //        }
        //        else
        //        {
        //            studyParams = new TI2_MA_Params(len, priceSrc);
        //            MainRepo.mapStudyParams.Add(tag, studyParams);
        //        }

        //        TI2_MA.GetData(priceData, ref studyParams);
        //        return Ok(studyParams.study);

        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}


        //// GET api/study/bb2/{dataSetId}/{len}/{stddev}/{priceSrc}/{rawTime}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("bb2/{dataSetId}/{len}/{stddev}/{priceSrc}/{rawTime}")]
        //[HttpGet]
        //public async Task<IActionResult> BB2Study(int dataSetId, int len, double stdDev, string priceSrc, long rawTime)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }

        //        string tag = $"BB2@{dataSetId}@{len}@{stdDev}@{priceSrc}";
        //        TI2_BB_Params studyParams;
        //        TI2Parameter? tempParam;
        //        if (MainRepo.mapStudyParams.TryGetValue(tag, out tempParam))
        //        {
        //            studyParams = (TI2_BB_Params)tempParam;
        //        }
        //        else
        //        {
        //            studyParams = new TI2_BB_Params(len, stdDev, priceSrc);
        //            MainRepo.mapStudyParams.Add(tag, studyParams);
        //        }
        //        TI2_BB.GetData(priceData, ref studyParams);

        //        List<TI2_BB_Study> retStudy = new List<TI2_BB_Study>();
        //        studyParams.study.ForEach(study => retStudy.Add((TI2_BB_Study)study));

        //        return Ok(retStudy);

        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}


        //// GET api/study/cycledir2/{dataSetId}/{len}/{stddev}/{priceSrc}/{rawTime}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("cycledir2/{dataSetId}/{len}/{stddev}/{priceSrc}/{rawTime}")]
        //[HttpGet]
        //public async Task<IActionResult> CycleDir2Study(int dataSetId, int len, double stdDev, string priceSrc, long rawTime)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }

        //        string tag = $"BBCycleDir2@{dataSetId}@{len}@{stdDev}@{priceSrc}";
        //        TI2_BB_Params bbParams;
        //        TI2Parameter? tempParam;
        //        if (MainRepo.mapStudyParams.TryGetValue(tag, out tempParam))
        //        {
        //            bbParams = (TI2_BB_Params)tempParam;
        //        }
        //        else
        //        {
        //            bbParams = new TI2_BB_Params(len, stdDev, priceSrc);
        //            MainRepo.mapStudyParams.Add(tag, bbParams);
        //        }

        //        TI2_BB.GetData(priceData, ref bbParams);

        //        tag = $"CycleDir2@{dataSetId}@{len}@{stdDev}@{priceSrc}";
        //        TI2_CycleDir_Params? studyParams;
        //        if (MainRepo.mapStudyParams.TryGetValue(tag, out tempParam))
        //        {
        //            studyParams = (TI2_CycleDir_Params) tempParam;
        //        }
        //        else
        //        {
        //            studyParams = new TI2_CycleDir_Params();
        //            MainRepo.mapStudyParams.Add(tag, studyParams);
        //        }
        //        TI2_CycleDir.GetData(priceData, bbParams, ref studyParams);

        //        return Ok(studyParams.study);
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}


        //// GET api/study/ulmrev2/{dataSetId}/{len}/{stddev}/{priceSrc}/{rawTime}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("ulmrev2/{dataSetId}/{len}/{stddev}/{priceSrc}/{rawTime}")]
        //[HttpGet]
        //public async Task<IActionResult> UBLBMAReversion2Study(int dataSetId, int len, double stdDev, string priceSrc, long rawTime)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }

        //        string tag = $"BBREV2@{dataSetId}@{len}@{stdDev}@{priceSrc}";
        //        TI2_BB_Params bbParams;
        //        TI2Parameter? tempParam;
        //        if (MainRepo.mapStudyParams.TryGetValue(tag, out tempParam))
        //        {
        //            bbParams = (TI2_BB_Params)tempParam;
        //        }
        //        else
        //        {
        //            bbParams = new TI2_BB_Params(len, stdDev, priceSrc);
        //            MainRepo.mapStudyParams.Add(tag, bbParams);
        //        }

        //        TI2_BB.GetData(priceData, ref bbParams);

        //        tag = $"ulmrev2@{dataSetId}@{len}@{stdDev}@{priceSrc}";
        //        TI2_UBLBMA_Reversion_Params studyParams;
        //        if (MainRepo.mapStudyParams.TryGetValue(tag, out tempParam))
        //        {
        //            studyParams = (TI2_UBLBMA_Reversion_Params)tempParam;
        //        }
        //        else
        //        {
        //            studyParams = new TI2_UBLBMA_Reversion_Params();
        //            MainRepo.mapStudyParams.Add(tag, studyParams);
        //        }
        //        TI2_UBLBMA_Reversion.GetData(priceData, bbParams, ref studyParams);

        //        return Ok(studyParams.study);
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}

        //// GET api/study/masuppres2/{dataSetId}/{len1}/{len2}/{priceSrc}/{rawTime}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("masuppres2/{dataSetId}/{len1}/{len2}/{priceSrc}/{rawTime}")]
        //[HttpGet]
        //public async Task<IActionResult> MASupportResistance2(int dataSetId, int len1, int len2, string priceSrc, long rawTime)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }

        //        TI2Parameter? tempParam;

        //        TI2_MA_Params ma1Params;
        //        string tag = $"MASUPRES2@MA1@{dataSetId}@{len1}@{priceSrc}";
        //        if (MainRepo.mapStudyParams.TryGetValue(tag, out tempParam))
        //        {
        //            ma1Params = (TI2_MA_Params)tempParam;
        //        }
        //        else
        //        {
        //            ma1Params = new TI2_MA_Params(len1, priceSrc);
        //            MainRepo.mapStudyParams.Add(tag, ma1Params);
        //        }
        //        TI2_MA.GetData(priceData, ref ma1Params);

        //        TI2_MA_Params? ma2Params;
        //        tag = $"MASUPRES2@MA2@{dataSetId}@{len2}@{priceSrc}";
        //        if (MainRepo.mapStudyParams.TryGetValue(tag, out tempParam))
        //        {
        //            ma2Params = (TI2_MA_Params)tempParam;
        //        }
        //        else
        //        {
        //            ma2Params = new TI2_MA_Params(len2, priceSrc);
        //            MainRepo.mapStudyParams.Add(tag, ma2Params);
        //        }
        //        TI2_MA.GetData(priceData, ref ma2Params);

        //        tag = $"MASUPRES2@{dataSetId}@{len1}@{len2}@{priceSrc}";
        //        TI2_MASupportResistance_Params? studyParams;
        //        if (MainRepo.mapStudyParams.TryGetValue(tag, out tempParam))
        //        {
        //            studyParams = (TI2_MASupportResistance_Params)tempParam;
        //        }
        //        else
        //        {
        //            studyParams = new TI2_MASupportResistance_Params();
        //            MainRepo.mapStudyParams.Add(tag, studyParams);
        //        }
        //        TI2_MASupportResistance.GetData(priceData, ma1Params, ma2Params, ref studyParams);

        //        return Ok(studyParams.study);
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}

        //// GET api/study/macontpivot2/{dataSetId}/{lenm}/{len1}/{len2}/{priceSrc}/{rawTime}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("macontpivot2/{dataSetId}/{lenm}/{len1}/{len2}/{priceSrc}/{rawTime}")]
        //[HttpGet]
        //public async Task<IActionResult> MAContrarianPivot2(int dataSetId, int lenm, int len1, int len2, string priceSrc, long rawTime)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }

        //        TI2Parameter? tempParam;

        //        TI2_MA_Params? ma1Params;
        //        string tag = $"MASUPRES2@MA1@{dataSetId}@{len1}@{priceSrc}";
        //        if (MainRepo.mapStudyParams.TryGetValue(tag, out tempParam))
        //        {
        //            ma1Params = (TI2_MA_Params)tempParam;
        //        }
        //        else
        //        {
        //            ma1Params = new TI2_MA_Params(len1, priceSrc);
        //            MainRepo.mapStudyParams.Add(tag, ma1Params);
        //        }
        //        TI2_MA.GetData(priceData, ref ma1Params);

        //        TI2_MA_Params? ma2Params;
        //        tag = $"MASUPRES2@MA2@{dataSetId}@{len2}@{priceSrc}";
        //        if (MainRepo.mapStudyParams.TryGetValue(tag, out tempParam))
        //        {
        //            ma2Params = (TI2_MA_Params)tempParam;
        //        }
        //        else
        //        {
        //            ma2Params = new TI2_MA_Params(len2, priceSrc);
        //            MainRepo.mapStudyParams.Add(tag, ma2Params);
        //        }
        //        TI2_MA.GetData(priceData, ref ma2Params);

        //        TI2_MA_Params? maMParams;
        //        tag = $"MASUPRES2@MAM@{dataSetId}@{lenm}@{priceSrc}";
        //        if (MainRepo.mapStudyParams.TryGetValue(tag, out tempParam))
        //        {
        //            maMParams = (TI2_MA_Params)tempParam;
        //        }
        //        else
        //        {
        //            maMParams = new TI2_MA_Params(lenm, priceSrc);
        //            MainRepo.mapStudyParams.Add(tag, maMParams);
        //        }
        //        TI2_MA.GetData(priceData, ref maMParams);

        //        TI2_MASupportResistance_Params? supMParams;
        //        tag = $"MASUPRES2@{dataSetId}@{len1}@{len2}@{priceSrc}";
        //        if (MainRepo.mapStudyParams.TryGetValue(tag, out tempParam))
        //        {
        //            supMParams = (TI2_MASupportResistance_Params)tempParam;
        //        }
        //        else
        //        {
        //            supMParams = new TI2_MASupportResistance_Params();
        //            MainRepo.mapStudyParams.Add(tag, supMParams);
        //        }
        //        TI2_MASupportResistance.GetData(priceData, ma1Params, ma2Params, ref supMParams);

        //        TI2_MAContrarianPivot_Params? studyParams;
        //        tag = $"MACONTPIVOT@{dataSetId}@{lenm}@{len1}@{priceSrc}";
        //        if (MainRepo.mapStudyParams.TryGetValue(tag, out tempParam))
        //        {
        //            studyParams = (TI2_MAContrarianPivot_Params)tempParam;
        //        }
        //        else
        //        {
        //            studyParams = new TI2_MAContrarianPivot_Params();
        //            MainRepo.mapStudyParams.Add(tag, studyParams);
        //        }
        //        TI2_MAContrarianPivot.GetData(priceData, maMParams, ma1Params, supMParams, ref studyParams);

        //        return Ok(studyParams.study);
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}


        //// GET api/study/col2/{dataSetId}/{c21}/{stddev1}/{stddev2}/{priceSrc}/{rawTime}/{maxNumRec}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("col2/{dataSetId}/{c21}/{stddev1}/{stddev2}/{priceSrc}/{rawTime}/{maxNumRec}")]
        //[HttpGet]
        //public async Task<IActionResult> MAContrarianPivot2(int dataSetId, int c21, double stddev1, double stddev2, string priceSrc, long rawTime, int maxNumRec)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }

        //        TI2Parameter? tempParam;

        //        TI2_StudyCol_Params? studyParams;
        //        string tag = $"COL2@{dataSetId}@{c21}@{stddev1}@{stddev2}@{priceSrc}";
        //        if (MainRepo.mapStudyParams.TryGetValue(tag, out tempParam))
        //        {
        //            studyParams = (TI2_StudyCol_Params)tempParam;
        //        }
        //        else
        //        {
        //            studyParams = new TI2_StudyCol_Params(c21, stddev1, stddev2);
        //            MainRepo.mapStudyParams.Add(tag, studyParams);
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, rawTime, maxNumRec);
        //        if (priceData.Count == 0)
        //        {
        //            List<TI2_StudyCol_Study> tmpStudy = new List<TI2_StudyCol_Study>();
        //            return Ok(tmpStudy);
        //        }

        //        if (studyParams.priceData.Count > 0)
        //        {
        //            long priceDataMinRange = studyParams.priceData[0].RawTime;
        //            long priceDataMaxRange = studyParams.priceData[studyParams.priceData.Count - 1].RawTime;
        //            if (priceData[0].RawTime < priceDataMinRange)
        //            {
        //                studyParams.Reset();
        //            }
        //            else
        //            {
        //                for (int i = priceData.Count - 1; i >= 0; i--)
        //                {
        //                    if (priceData[i].RawTime < priceDataMaxRange)
        //                    {
        //                        priceData.RemoveAt(0);
        //                    }
        //                }
        //            }
        //        }
        //        if (priceData.Count > 0)
        //        {
        //            TI2_StudyCol.GetData(priceData, ref studyParams);
        //        }

        //        List<TI2_StudyCol_Study> retStudy = new List<TI2_StudyCol_Study>();
        //        int numRec = 0;
        //        studyParams?.study.ForEach(study => {
        //            if (study.RawTime >= rawTime && numRec < maxNumRec)
        //            {
        //                retStudy.Add((TI2_StudyCol_Study)study.Clone());
        //                numRec++;
        //            }
        //        });

        //        return Ok(retStudy);

        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}


        //// GET api/study/studydiff/{dataSetId}/{len}/varName/varNameNew
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("studydiff/{dataSetId}/{len}/{varName}/{varNameNew}")]
        //[HttpGet]
        //public async Task<IActionResult> StudyDiff(int dataSetId, int len, string varName, string varNameNew)
        //{
        //    try
        //    {
        //        var dataSet = MainRepo.GetDataSet(dataSetId);
        //        if (dataSet == null)
        //        {
        //            throw new Exception("DataSet not found");
        //        }
        //        List<PriceDataRS> priceData = MainRepo.GetPriceData(dataSet.Id, -1);
        //        if (priceData == null || priceData.Count == 0)
        //        {
        //            throw new Exception($"No price data found for ticker {{dataSet.TickerId}} and interval {{dataSet.IntervalId}}");
        //        }

        //        Periods periods = new Periods(len);

        //        TI2_StudyCol_Params? studyParams = new TI2_StudyCol_Params(len, 2.0D, 1.6D, 3.0D);
        //        TI2_StudyCol.GetData(priceData, ref studyParams, 0);

        //        StudyCol col = new StudyCol(len, 2.0D, 1.6D, 3.0D);
        //        List<BarStudy> results = new List<BarStudy>();
        //        col.Calc(priceData, 0, ref results);

        //        // define a Json array to store the results
        //        var jsonArray = new List<Dictionary<string, object>>();

        //        for (int i = 0; i < priceData.Count; i++)
        //        {
        //            if (studyParams?.study.Count > i && results[i].ContainsKey(varNameNew))
        //            {
        //                TI2_StudyCol_Study cStudy = (TI2_StudyCol_Study)studyParams.study[i];

        //                long rawTime = results[i].Price.RawTime;
        //                object? valTI2 = cStudy.GetVar(varName);
        //                object? valStudy = results[i][varNameNew];

        //                decimal dValTI2 = TypeConvertUtils.GetDecimalValue(valTI2, 0.0M) ?? 0.0M;
        //                decimal dValStudy = TypeConvertUtils.GetDecimalValue(valStudy, 0.0M) ?? 0.0M;
        //                // compare the values of the two variables
        //                bool isDiff = (dValTI2 - dValStudy) != 0.0M;
        //                if (isDiff && jsonArray.Count < 1000)
        //                {
        //                    // create a json object that contains OldVal, NewVal, and diff
        //                    var jsonObject = new Dictionary<string, object>
        //                    {
        //                        { "i", i},
        //                        { "RawTime",  rawTime },
        //                        { "valTI2", valTI2??0 },
        //                        { "valStudy", valStudy??0 }

        //                    };
        //                    jsonArray.Add(jsonObject);
        //                }
        //            }
        //            else
        //            {
        //                throw new Exception($"Value not found at index {i}");
        //            }
        //        }

        //        return Ok(jsonArray);

        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}

        //#endregion Study

    }
}