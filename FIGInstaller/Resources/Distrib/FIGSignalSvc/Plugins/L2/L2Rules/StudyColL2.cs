using Microsoft.Extensions.Logging;

namespace FIG.Studies;

public record StudyParamRec(int N, string Label);

public partial class StudyColL2 : StudyColBase
{
    private readonly L2Options options;
    private readonly int maxPeriod;

    protected override string? CheckpointSchema => "l2-v1";
    // Extended needs the outgoing price at -period; the extra ten prices cover
    // replay of the retained parameter/output history during recent-bar corrections.
    protected override int CheckpointPriceHistoryLimit => Math.Max(options.MaxPeriod, 24564) + maxNumStudies;
    // L2 consumes OHLCV/time only. The host validates and reloads this bounded
    // price window from StudyHistory instead of embedding it in every checkpoint.
    protected override bool CheckpointPricesFromHistory => true;

    public StudyColL2(StudyCollectionContext context) : base(context)
    {
        collectionTag = "L2";
        options = GetParameters<L2Options>();
        options.Validate(context.Configuration.Id);

        maxPeriod = new[]
        {
            options.MaxPeriod,
            24564 + 4000
        }.Max() + 1000;

        maxNumPrices = maxPeriod + 1000;
        priceList = new CPriceList(maxNumPrices);
        _logger.LogInformation(
            "Initialized {CollectionType} v{Version} for StudyCol {StudyColId}",
            collectionTag,
            "1.0.0",
            context.Configuration.Id);
    }

    protected override void InitializeStudies()
    {
        studyList = [];

        studyList = new();
        List<StudyParamRec> studyParamRecs = new();

        // prepare cycles
        for (int n = options.MinPeriod; n <= options.MaxPeriod; n += options.PeriodInc)
        {
            int cNum = n / options.PeriodInc;
            string lbl = $"C{cNum}";

            // want to save n, and lbl in an array or list to use when we create the studies later
            studyParamRecs.Add(new StudyParamRec(n, lbl));
        }
        studyParamRecs.Add(new StudyParamRec(24564, "C89"));
        studyParamRecs.Add(new StudyParamRec(15180, "C55"));
        studyParamRecs.Add(new StudyParamRec(9384, "C34"));
        studyParamRecs.Add(new StudyParamRec(723, "H2"));
        studyParamRecs.Add(new StudyParamRec(447, "H1"));
        studyParamRecs.Add(new StudyParamRec(170, "L1"));
        studyParamRecs.Add(new StudyParamRec(130, "L2"));
        studyParamRecs.Add(new StudyParamRec(65, "L3"));
        studyParamRecs.Add(new StudyParamRec(25, "L4"));
        studyParamRecs.Add(new StudyParamRec(80, "LL1"));
        studyParamRecs.Add(new StudyParamRec(50, "LL2"));
        studyParamRecs.Add(new StudyParamRec(31, "LL3"));

        studyParamRecs.ForEach(rec =>
        {
            studyList.Add(new StudyExtended(rec.N, "ohlc4", options.StdDev1, options.StdDev2, rec.Label));
            studyList.Add(new StudyCycleDir(rec.N, "ohlc4", rec.Label, "1"));
            studyList.Add(new StudyCycleDir(rec.N, "ohlc4", rec.Label, "2"));
            studyList.Add(new StudyCrossSEMMA(rec.Label));
        });

        //studyList.Add(new StudyMA(24564, "ohlc4", "C89"));
        //studyList.Add(new StudyMA(15180, "ohlc4", "C55"));
        //studyList.Add(new StudyMA(9384, "ohlc4", "C34"));

        studyList.Add(new StudyMajorTrends("C21"));

        //studyList.Add(new StudySEMActiveCycle(minPeriod, maxPeriod, periodInc));
        studyList.Add(new StudyLowerUpper("C6", "C20", options.MinPeriod, maxPeriod, options.PeriodInc));
        studyList.Add(new StudyLowerUpper("C6", "C21", options.MinPeriod, maxPeriod, options.PeriodInc));
        studyList.Add(new StudyLowerUpper("C4", "C12", options.MinPeriod, maxPeriod, options.PeriodInc));

        studyList.Add(new StudyFlag("C5", "C21"));
        studyList.Add(new StudyFlag("C3", "C13"));

        studyList.Add(new StudyLineTrendEx("C8"));
        studyList.Add(new StudyLineTrendEx("C7"));
        studyList.Add(new StudyLineTrendEx("C5"));
        studyList.Add(new StudyLineTrendEx("C3"));
        studyList.Add(new StudyLineTrendEx("C2"));
        studyList.Add(new StudyLineTrendEx("C1"));
        studyList.Add(new StudyLineTrendEx("L1"));
        studyList.Add(new StudyLineTrendEx("L2"));

        studyList.Add(new StudyTrend2SEM("C1", "C5"));
        studyList.Add(new StudyTrend2SEM("C2", "C5"));
        studyList.Add(new StudyTrend2SEM("C3", "C5"));


        studyList.Add(new StudyTrend3SEM("L3", "C1", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("L3", "C2", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("L3", "C3", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("L2", "C1", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("L2", "C2", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("L2", "C3", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("L2", "C5", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("L1", "C1", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("L1", "C2", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("L1", "C3", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("L1", "C5", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C1", "C2", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C1", "C3", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C1", "C5", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C1", "C7", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C1", "C8", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C2", "C3", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C2", "C4", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C2", "C5", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C2", "C8", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C2", "C11", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C3", "C5", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C3", "C6", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C3", "C8", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C3", "C11", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C3", "C13", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C3", "C18", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C3", "C21", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C4", "C7", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C4", "C11", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C4", "C13", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C5", "C8", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C5", "C13", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C5", "C21", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C7", "C8", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C7", "C9", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C8", "C13", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C8", "C21", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C9", "C13", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C2", "C21", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C3", "C21", L2RequiredTrend3SemOutputs.Keys));
        studyList.Add(new StudyTrend3SEM("C13", "C21", L2RequiredTrend3SemOutputs.Keys));

        studyList.Add(new StudyTrend3MA("C5", "C1", "C2", "C3", "C4"));
        studyList.Add(new StudyTrend3MA("C3", "L2", "L1", "C1", "C2"));
        studyList.Add(new StudyTrend3MA("C4", "L1", "C1", "C2", "C3"));

        studyList.Add(new StudyPivot2Cycles("C2", "L2"));
        studyList.Add(new StudyPivot2Cycles("C3", "L2"));
        studyList.Add(new StudyPivot2Cycles("C3", "L1"));
        studyList.Add(new StudyPivot2Cycles("C5", "C1"));
        studyList.Add(new StudyPivot2Cycles("C5", "C2"));
        studyList.Add(new StudyPivot2Cycles("C5", "C3"));

        studyList.Add(new StudyPivotStatus("L2"));
        studyList.Add(new StudyPivotStatus("L1"));
        studyList.Add(new StudyPivotStatus("C1"));
        studyList.Add(new StudyPivotStatus("C2"));
        studyList.Add(new StudyPivotStatus("C3"));
        studyList.Add(new StudyPivotStatus("C4"));
        studyList.Add(new StudyPivotStatus("C5"));
        studyList.Add(new StudyPivotStatus("C8"));
        studyList.Add(new StudyPivotStatus("C13"));
        studyList.Add(new StudyPivotStatus("C18"));
        studyList.Add(new StudyPivotStatus("C21"));


        studyList.Add(new StudyCycleTrendEx("C21"));
        studyList.Add(new StudyCycleTrendEx("C13"));
        studyList.Add(new StudyCycleTrendEx("C12"));
        studyList.Add(new StudyCycleTrendEx("C11"));
        studyList.Add(new StudyCycleTrendEx("C10"));
        studyList.Add(new StudyCycleTrendEx("C9"));
        studyList.Add(new StudyCycleTrendEx("C8"));
        studyList.Add(new StudyCycleTrendEx("C7"));
        studyList.Add(new StudyCycleTrendEx("C6"));
        studyList.Add(new StudyCycleTrendEx("C5"));
        studyList.Add(new StudyCycleTrendEx("C4"));
        studyList.Add(new StudyCycleTrendEx("C3"));
        studyList.Add(new StudyCycleTrendEx("C2"));
        studyList.Add(new StudyCycleTrendEx("C1"));
        studyList.Add(new StudyCycleTrendEx("L1"));
        studyList.Add(new StudyCycleTrendEx("L2"));

        //studyList.Add(new StudySEMMASupport(minPeriod, maxPeriod, periodInc)); 
        //studyList.Add(new StudySEMMAResistance(minPeriod, maxPeriod, periodInc));

        studyList.Add(new StudyADX(50, 150, "ADXFast"));

        // Strategies
        // Separate output prefix avoids overwriting Study_L2's postSide fields.
        // This publishes a diagnostic signal; Study_L2 does not consume it.
        studyList.Add(new StudyBaseSignal("L2", "L2"));
        studyList.Add(new Study_L2());
        SelectRequiredStudyOutputs();
    }

    public override int GetMaxLen() => maxPeriod;
}
