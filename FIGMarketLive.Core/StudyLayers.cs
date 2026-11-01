using FIG.Studies;
using FIGCommon.Models;

namespace FIGMarketLive;

public static class StudyLayers
{
    public static bool Applies(string colType)=>colType.Equals("L2",StringComparison.OrdinalIgnoreCase);
    public static StudyColBase Configure(StudyColBase collection,Func<PriceDataRS,Dictionary<string,object?>>? stored=null)
    {
        if(!Applies(collection.Context.Configuration.ColType))
        {if(stored is not null)throw new InvalidDataException("Stored-input regeneration currently supports L2 only.");return collection;}
        var selected=collection.ConfiguredStudies.SingleOrDefault(s=>s.GetType().Name=="Study_L2")
            ??throw new InvalidDataException("L2 plugin does not contain Study_L2.");
        if(selected is not IStudyKeyContract contract)throw new InvalidDataException("Study_L2 must declare its written fields (IStudyKeyContract). Update the plugin before using layered storage.");
        collection.ConfigureStudyLayers(selected,contract.WrittenStudyKeys,stored);return collection;
    }
    public sealed class Encoder:IDisposable
    {
        private readonly StudyBinaryCodec.Encoder baseline=new(),overrides=new();
        public EncodedStudyBar Encode(BarStudy bar)=>new(bar.Price,baseline.Encode(bar.BaseStudies??bar.Studies),
            bar.OverrideStudies is null?null:overrides.Encode(bar.OverrideStudies));
        public void Dispose(){baseline.Dispose();overrides.Dispose();}
    }
    public static Dictionary<string,object?> Merge(byte[] studies,byte[]? overrides)=>FIGCommon.Utilities.StudyValueCodec.Merge(studies,overrides);
}
