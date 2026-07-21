using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models
{
    public partial class StrategyConfigRS : IDbEntity<StrategyConfigRS>
    {
        public string Strategy { get; set; } = "";
        public int StudyColId { get; set; } = -1;

        public StudyColRS? StudyCol { get; set; }

        public StrategyConfigRS()
        {
            this.Strategy = "";
            this.StudyColId = -1;
            this.StudyCol = null;
        }

        public StrategyConfigRS(StrategyConfigRS rec)
        {
            Assign(rec);
        }

        public void Assign(StrategyConfigRS inst)
        {
            this.Strategy = inst.Strategy;
            this.StudyColId = inst.StudyColId;
            if (inst.StudyCol != null)
            {
                this.StudyCol = new StudyColRS(inst.StudyCol);
            }
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Strategy", Strategy);
            parameters.AddWithValue("@StudyColId", StudyColId);
        }

        public StrategyConfigRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new StrategyConfigRS()
            {
                Strategy = SqlReaderUtil.GetString(reader, nSeq++),
                StudyColId = SqlReaderUtil.GetInt32(reader, nSeq++)
            };
            return rec;
        }

    }
}

