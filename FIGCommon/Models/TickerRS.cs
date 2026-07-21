using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models
{
    public partial class TickerRS : IDbEntity<TickerRS>
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public string LocalSymbol { get; set; }
        public string Name { get; set; }
        public string SecurityType { get; set; }
        public string Currency { get; set; }
        public int ExpiryDate { get; set; }
        public decimal MinTick { get; set; }
        public int ContractSize { get; set; }
        public string Exchange { get; set; }
        public int PeriodMultiplier { get; set; }

        public TickerRS()
        {
            this.Id = -1;
            this.Symbol = "";
            this.LocalSymbol = "";
            this.Name = "";
            this.SecurityType = "";
            this.Currency = "";
            this.ExpiryDate = 19700101;
            this.MinTick = 0.01M;
            this.ContractSize = 1;
            this.Exchange = "";
            this.PeriodMultiplier = 92;
        }

        public TickerRS(TickerRS rec)
        {
            this.Id = rec.Id;
            this.Symbol = rec.Symbol;
            this.LocalSymbol = rec.LocalSymbol;
            this.Name = rec.Name;
            this.SecurityType = rec.SecurityType;
            this.Currency = rec.Currency;
            this.ExpiryDate = rec.ExpiryDate;
            this.MinTick = rec.MinTick;
            this.ContractSize = rec.ContractSize;
            this.Exchange = rec.Exchange;
            this.PeriodMultiplier = rec.PeriodMultiplier;
    
        }

        public bool SameAs(TickerRS rec)
        {
            return Symbol == rec.Symbol
                && SecurityType == rec.SecurityType
                && Currency == rec.Currency
                && ExpiryDate == rec.ExpiryDate
                && MinTick == rec.MinTick
                && ContractSize == rec.ContractSize
                && Exchange == rec.Exchange
                && PeriodMultiplier == rec.PeriodMultiplier
                && LocalSymbol == rec.LocalSymbol;
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@Symbol", Symbol);
            parameters.AddWithValue("@LocalSymbol", LocalSymbol);
            parameters.AddWithValue("@Name", Name);
            parameters.AddWithValue("@SecurityType", SecurityType);
            parameters.AddWithValue("@Currency", Currency);
            parameters.AddWithValue("@ExpiryDate", ExpiryDate);
            parameters.AddWithValue("@MinTick", MinTick);
            parameters.AddWithValue("@ContractSize", ContractSize);
            parameters.AddWithValue("@Exchange", Exchange);
            parameters.AddWithValue("@PeriodMultiplier", PeriodMultiplier);
        }

        public TickerRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new TickerRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                Symbol = SqlReaderUtil.GetString(reader, nSeq++),
                LocalSymbol = SqlReaderUtil.GetString(reader, nSeq++),
                Name = SqlReaderUtil.GetString(reader, nSeq++),
                SecurityType = SqlReaderUtil.GetString(reader, nSeq++),
                Currency = SqlReaderUtil.GetString(reader, nSeq++),
                ExpiryDate = SqlReaderUtil.GetInt32(reader, nSeq++),
                MinTick = SqlReaderUtil.GetDecimal(reader, nSeq++),
                ContractSize = SqlReaderUtil.GetInt32(reader, nSeq++),
                Exchange = SqlReaderUtil.GetString(reader, nSeq++),
                PeriodMultiplier = SqlReaderUtil.GetInt32(reader, nSeq++)
            };
            return rec;
        }

    }
}
