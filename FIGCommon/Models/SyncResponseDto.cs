namespace FIGCommon.Models
{
    public class SyncResponseDto
    {
        public SyncResponseDto(bool ok)
        {
            OK = ok;
        }
        public bool OK { get; set; }
    }
}