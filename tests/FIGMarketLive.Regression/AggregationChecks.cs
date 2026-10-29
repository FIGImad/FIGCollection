using FIGCommon.Models;
using FIGMarketLive;

internal static class AggregationChecks
{
    public static int Run()
    {
        int checks=0;
        void Check(bool value,string message){if(!value)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
        var five=new IntervalRS{Id="5",IntervalLen=300,Seq=5};var ten=new IntervalRS{Id="10",IntervalLen=600,Seq=10};
        var start=new DateTimeOffset(2026,10,29,12,0,0,TimeSpan.Zero).ToUnixTimeSeconds();
        var input=new List<PriceDataRS>
        {
            new(){Id=1,DataSetId=7,RawTime=start,Open=10,High=12,Low=9,Close=11,Volume=5},
            new(){Id=2,DataSetId=7,RawTime=start+300,Open=11,High=15,Low=10,Close=14,Volume=7},
            new(){Id=3,DataSetId=7,RawTime=start+600,Open=14,High=16,Low=13,Close=15,Volume=8}
        };
        var bars=PriceBarAggregation.Build(input,five,ten);
        Check(bars.Count==2&&bars[0].Open==10&&bars[0].High==15&&bars[0].Low==9&&bars[0].Close==14&&bars[0].Volume==12,"5-to-10 minute aggregation preserves first/max/min/last/summed OHLCV");
        Check(bars[1].RawTime==start+600&&bars[1].Volume==8,"Partial current aggregate bar remains available");
        input[0].High=20;var corrected=PriceBarAggregation.Build(input,five,ten);
        Check(corrected[0].High==20&&corrected[1].High==bars[1].High,"Historical source correction updates its aggregate without changing another bucket");
        var gapped=PriceBarAggregation.Build([input[0],new(input[2]){RawTime=start+1200}],five,ten);
        Check(gapped.Count==2&&gapped[1].RawTime==start+1200,"Aggregation does not manufacture bars across source gaps");
        var direct=PriceBarAggregation.Build(input,five,five);
        Check(direct.Count==input.Count&&direct[0].Id==input[0].Id&&direct[1].RawTime==input[1].RawTime,"Exact dataset input is copied without resampling");
        bool rejected=false;try{PriceBarAggregation.Build([new(input[0]){RawTime=start+60}],five,ten);}catch(InvalidDataException){rejected=true;}
        Check(rejected,"Misaligned source bars cannot be split to fit target UTC boundaries");
        bool overflow=false;try{PriceBarAggregation.Build([new(input[0]){Volume=int.MaxValue},new(input[1]){Volume=1}],five,ten);}catch(OverflowException){overflow=true;}
        Check(overflow,"Aggregate volume overflow fails explicitly instead of truncating");
        var daily=new IntervalRS{Id="D",IntervalLen=86400};var monthly=new IntervalRS{Id="M",IntervalLen=2592000};
        PriceDataRS Day(int month,int day,int volume)=>new(){RawTime=new DateTimeOffset(2024,month,day,0,0,0,TimeSpan.Zero).ToUnixTimeSeconds(),Open=10,High=12,Low=9,Close=11,Volume=volume};
        var months=PriceBarAggregation.Build([Day(2,28,2),Day(2,29,3),Day(3,1,4)],daily,monthly);
        Check(months.Count==2&&months[0].Volume==5&&months[0].PriceDate==new DateTime(2024,2,1,0,0,0,DateTimeKind.Utc)&&months[1].PriceDate.Month==3,"Calendar months handle leap day and real month boundaries");
        var catalog=new StudyConfigurationCatalog([new(12,"NQZ6")],[five,ten],[new(7,12,"5")],[new("L2","L2.dll","")],
            [new(101,12,"10","L2","{}",true),new(102,12,"5","L2","{}",false)]);
        var plan=SqlMonitorLoader.Build(catalog,[new(){Id=12,Symbol="NQZ6"}]);
        Check(plan.Monitors.Count==1&&plan.Monitors[0].StudyCol.Id==101&&plan.Monitors[0].DataSetId==7&&plan.Monitors[0].SourceInterval.Id=="5","SQL monitor plan excludes disabled rows and chooses an aggregate source");
        catalog.DataSets.Add(new(8,12,"10"));var exact=SqlMonitorLoader.Build(catalog,[new(){Id=12}]);
        Check(exact.Monitors.Single().DataSetId==8&&exact.Monitors.Single().SourceInterval.Id=="10","New exact dataset replaces aggregate source automatically");
        string identity=SqlMonitorLoader.Identity(exact.Monitors[0]);exact.Monitors[0].StudyCol.ColParams="{\"period\":6}";
        Check(identity!=SqlMonitorLoader.Identity(exact.Monitors[0]),"Parameter changes invalidate the cached study generation");
        catalog.Studies.Add(new(103,12,"1","L2","{}",true));var mixed=SqlMonitorLoader.Build(catalog,[new(){Id=12}]);
        Check(mixed.Monitors.Count==1&&mixed.Issues.Single().StudyColId==103,"An incompatible enabled study is reported while valid monitors remain runnable");
        return checks;
    }
}
