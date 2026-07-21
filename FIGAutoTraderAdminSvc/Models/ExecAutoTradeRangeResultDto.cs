using FIGCommon.Models;

namespace FIGAutoTraderAdminSvc.Models
{
    public class ExecAutoTradeRangeResultDto
    {
        public int AutoTradeId { get; set; } = -1;
        public int Status { get; set; } = (int) EAutoTradeStatus.NotAvailable;

        public ExecAutoTradeRangeResultDto()
        {
            AutoTradeId = -1;
            Status = (int)EAutoTradeStatus.NotAvailable;
        }

        public ExecAutoTradeRangeResultDto(ExecAutoTradeRangeResultDto other)
        {
            AutoTradeId = other.AutoTradeId;
            Status = other.Status;
        }
    }
}
