
namespace FIGCommon.Utilities.FIGProviderAPI
{
    public class ProviderErrorCodes
    {
        public const int NOT_ASSIGNED = -1;          // NOT Assigned
        public const int NO_ERROR = 0;               // Success
        public const int WARNING = 2;                // Warning
        public const int INFO = 3;                   // Info
        public const int FAIL = 100;                 // Failed
        public const int ACCT_ERR = 101;             // Account Error
        public const int VALIDATION_ERR = 103;       // Validation Error
        public const int PARSE_ERR = 104;            // Parse Error
        public const int NOT_FOUND = 105;            // Not Found
        public const int INVALID_PARAM = 106;        // Invalid Parameter
        public const int NOT_ALLOWED = 107;          // Not Allowed
        public const int INCOMPLETE = 108;           // Incomplete
        public const int SERVICE_ERR = 111;          // Service Error
        public const int EXPIRY_ERR = 112;           // Expired
        public const int NOT_SUPPORTED = 113;        // Not Supported
        public const int COMM_ERR = 114;             // Communication Error
        public const int VERSION_ERR = 115;          // Version out of date
        public const int NOT_AVAILABLE = 116;        // Not Available
        public const int NOT_PERMITTED = 117;        // Not Permitted
        public const int NO_DATA = 118;              // No Data
        public const int TICKER_IN_USE = 120;        // Ticker is already in use
        public const int DUPLICATE_ORDER = 130;      // Duplicate order ID
        public const int ORDER_ALREADY_FILLED = 131; // Order is already filled
        public const int ORDER_NOT_MATCHING = 132;   // Order does not match
        public const int INVALID_BLOCK_SIZE = 133;   // Invalid Block Order Size
        public const int ORDER_CANCELLED = 134;      // Order Cancelled
        public const int INVALID_ORDER_ID = 135;      // Could not determine order ID
        public const int MSG_RATE_ERR = 140;         // Exceeded max message rate

    }

    public class WaitForResponseErrorCodes
    {
        public const int NOT_ASSIGNED = 1;          // response event not Assigned
        public const int NOT_INITIALIZED = 2;       // response event not Initialized
        public const int TIMEOUT = 3;               // response event Timeout
        public const int COMM = 4;                  // Connection eror
        public const int WAIT_EXCEPTION = 5;        // general wait exception

    }
}
