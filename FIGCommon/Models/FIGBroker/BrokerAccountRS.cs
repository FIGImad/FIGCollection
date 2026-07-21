using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;
using FIGCommon.Models.FIGProviderAPI;


namespace FIGCommon.Models.FIGBroker
{
    public partial class BrokerAccountRS : IDbEntity<BrokerAccountRS>
    {

        public int Id { get; set; } = -1;
        public string AccountId { get; set; } = string.Empty;
        public string AdapterType { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountServiceId { get; set; } = string.Empty;
        public string URL { get; set; } = string.Empty;
        public string BrokerAccountId { get; set; } = string.Empty;
        public string BrokerAccountParams { get; set; } = string.Empty;
        public bool Enable { get; set; } = false;

        public ConfigParamBase? acctParams { get; set; } = null;
        //public List<FIGBrokerSvc.Models.TickerRS> acctTickerPermissions = new();
        public List<BrokerAccountTickerRS> acctTickerPermissions { get; set; } = new();

        public BrokerAccountRS()
        {
            Id = -1;
            AccountId = string.Empty;
            AdapterType = string.Empty;
            AccountName = string.Empty;
            AccountServiceId = string.Empty;
            URL = string.Empty;
            BrokerAccountId = string.Empty;
            BrokerAccountParams = string.Empty;
            Enable = false;
            acctTickerPermissions = new();
            acctParams = null;
        }

        public BrokerAccountRS(BrokerAccountRS rec)
        {
            this.Id = rec.Id;
            this.AccountId = rec.AccountId;
            this.AdapterType = rec.AdapterType;
            this.AccountName = rec.AccountName;
            this.AccountServiceId = rec.AccountServiceId;
            this.URL = rec.URL;
            this.BrokerAccountId = rec.BrokerAccountId;
            this.BrokerAccountParams = rec.BrokerAccountParams;
            this.Enable = rec.Enable;
            this.acctTickerPermissions = new();
            foreach (var item in rec.acctTickerPermissions)
            {
                this.acctTickerPermissions.Add(new BrokerAccountTickerRS(item));
            }
            ParseAccountParams();
        }

        public void ParseAccountParams()
        {
            try
            {
                if (AdapterType == "IBKR")
                {
                    this.acctParams = this.BrokerAccountParams.ToConfig<ConfigParamIBKR>();
                }
            }
            catch
            {
                this.acctParams = null;
            }
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@AccountId", AccountId);
            parameters.AddWithValue("@AdapterType", AdapterType);
            parameters.AddWithValue("@AccountName", AccountName);
            parameters.AddWithValue("@AccountServiceId", AccountServiceId);
            parameters.AddWithValue("@URL", URL);
            parameters.AddWithValue("@BrokerAccountId", BrokerAccountId);
            parameters.AddWithValue("@BrokerAccountParams", BrokerAccountParams);
            parameters.AddWithValue("@Enable", Enable);
        }

        public BrokerAccountRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new BrokerAccountRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                AccountId = SqlReaderUtil.GetString(reader, nSeq++),
                AdapterType = SqlReaderUtil.GetString(reader, nSeq++),
                AccountName = SqlReaderUtil.GetString(reader, nSeq++),
                AccountServiceId = SqlReaderUtil.GetString(reader, nSeq++),
                URL = SqlReaderUtil.GetString(reader, nSeq++),
                BrokerAccountId = SqlReaderUtil.GetString(reader, nSeq++),
                BrokerAccountParams = SqlReaderUtil.GetString(reader, nSeq++),
                Enable = SqlReaderUtil.GetBoolean(reader, nSeq++)
            };
            rec.ParseAccountParams();

            return rec;
        }

        public ProviderOptions ToProviderOptions()
        {
            var hostParams = StringUtil.ParseHostPort(URL);
            ProviderOptions options = new ProviderOptions()
            {
                Id = this.AccountId,
                Type = this.AdapterType,
                Host = hostParams.Host,
                Port = hostParams.Port,
                ClientId = acctParams?.BrokerClientId??-1,
                TrackingClientId = acctParams?.BrokerTrackingClientId ?? -1,
                EnableFrozenData = false
            };
            return options;
        }

        public bool IsTickerAllowed(string localSymbol)
        {
            if (string.IsNullOrEmpty(localSymbol))
                return false;
            // find if localSymbol is in acctTickerPermissions, check for TickerRS.LocalSymbol and do case-insensitive compare
            return acctTickerPermissions.Any(t =>
                (string.Equals(t.ticker?.LocalSymbol, localSymbol, StringComparison.OrdinalIgnoreCase)
                || string.Equals(t.ticker?.Symbol, localSymbol, StringComparison.OrdinalIgnoreCase)) && t.Allowed
                );
        }
        public int LongLimit(string localSymbol)
        {
            if (string.IsNullOrEmpty(localSymbol))
                return 0;

            // find if localSymbol is in acctTickerPermissions, check for TickerRS.LocalSymbol and do case-insensitive compare
            var perm = acctTickerPermissions.FirstOrDefault(t =>
                (string.Equals(t.ticker?.LocalSymbol, localSymbol, StringComparison.OrdinalIgnoreCase)
                || string.Equals(t.ticker?.Symbol, localSymbol, StringComparison.OrdinalIgnoreCase)) && t.Allowed
                );

            return perm != null ? perm.LongLimit : 0;
        }
        public int ShortLimit(string localSymbol)
        {
            if (string.IsNullOrEmpty(localSymbol))
                return 0;

            // find if localSymbol is in acctTickerPermissions, check for TickerRS.LocalSymbol and do case-insensitive compare
            var perm = acctTickerPermissions.FirstOrDefault(t =>
                (string.Equals(t.ticker?.LocalSymbol, localSymbol, StringComparison.OrdinalIgnoreCase)
                || string.Equals(t.ticker?.Symbol, localSymbol, StringComparison.OrdinalIgnoreCase)) && t.Allowed
                );

            return perm != null ? perm.ShortLimit : 0;
        }
    }
}
