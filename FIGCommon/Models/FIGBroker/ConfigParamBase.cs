using Google.Protobuf.WellKnownTypes;
namespace FIGCommon.Models.FIGBroker
{
    public class ConfigParamBase
    {
        #region Accessors
        public virtual int BrokerClientId
        { 
            get
            {
                return -1;
            } 
        }

        public virtual int BrokerTrackingClientId
        {
            get
            {
                return -1;
            }
        }

        public virtual int OrderExecTimeoutSeconds
        {
            get
            {
                return 120;  // seconds
            }
        }

        public virtual int ExecTimeoutSeconds
        {
            get
            {
                return 20;  // seconds
            }
        }

        #endregion Accessors
    }
}
