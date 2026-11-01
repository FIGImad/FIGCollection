namespace FIGMarketLive;

// Compatibility facade; SQL consumers share the FIGS implementation in FIGCommon.
public static class StudyBinaryCodec
{
    public const int MaxPayloadBytes=FIGCommon.Utilities.StudyValueCodec.MaxPayloadBytes;
    public static byte[] Encode(IReadOnlyDictionary<string,object?> studies)=>FIGCommon.Utilities.StudyValueCodec.Encode(studies);
    public static Dictionary<string,object?> Decode(byte[] bytes)=>FIGCommon.Utilities.StudyValueCodec.Decode(bytes);
    public sealed class Encoder:IDisposable
    {
        private readonly FIGCommon.Utilities.StudyValueCodec.Encoder encoder=new();
        public byte[] Encode(IReadOnlyDictionary<string,object?> studies)=>encoder.Encode(studies);
        public void Dispose()=>encoder.Dispose();
    }
}
