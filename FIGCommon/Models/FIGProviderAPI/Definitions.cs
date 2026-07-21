
using FIGCommon.Models;

namespace FIGCommon.Models.FIGProviderAPI
{

    //public static class OrderTags
    //{
    //    public const string OPEN = "OPEN";
    //    public const string CLOSE = "CLOSE";
    //}

    //public static class OrderType
    //{
    //    public const string MARKET = "MARKET";
    //    public const string LIMIT = "LIMIT";
    //}


    public static class OrderTagDef
    {
        public const string UNDEFINED = "";
        public const string OPEN = "OPEN";
        public const string CLOSE= "CLOSE";
        public const string CANCEL = "CANCEL";
    }

    public static class OrderTypes
    {
        public const string UNDEFINED = "";
        public const string LIMIT = "LIMIT";
        public const string MARKET = "MARKET";
        public const string STOP = "STOP";     // become a market order when the stop price is reached
        public const string STOP_LIMIT = "STOP_LIMIT";     // become a limit order when the stop price is reached
    }

    public static class TimeInForceTypes
    {
        public const string DAY = "DAY";     // Valid for the day only.
        public const string GTC = "GTC";     // Good until canceled. The order will continue to work within the system and in the marketplace until it executes or is canceled.
        public const string IOC = "IOC";     // Immediate or Cancel. Any portion that is not filled as soon as it becomes available in the market is canceled.
        public const string GTD = "GTD";     // Good until Date. It will remain working within the system and in the marketplace until it executes or until the close of the market on the date specified
    }

    public static class OrderStatusCodes
    {
        public const int NEW = 0;
        public const int UNDEFINED = -1;
        public const int FAIL_EXPIRED = -2;
        public const int FAIL_CANCELED_NOT_SUBMITTED = -3;
        public const int FAIL_CANCELED = -4;
        public const int FAIL_REJECTED = -5;
        public const int FAIL_TIMEOUT = -6;
        public const int FAIL_CONNECTION = -7;
        public const int FAIL_DUPLICATE_ID = -8;
        public const int FAIL_CLOSING_INCOMPLETE_ORDER = -9;
        public const int FAIL_CLOSED_BEFORE_SUBMISSION = -10;
        public const int FAIL_CLOSED_WHILE_SUBMISSION = -11;
        public const int FAIL_NO_OPEN_POS = -12;
        public const int FAIL_AUTHORIZATION = -13;
        public const int IGNORED_LONG_POS_LIMIT = -14;
        public const int IGNORED_SHORT_POS_LIMIT = -15;

        public const int FAIL_OTHER = -20;

        public const int IGNORED = -21;
        public const int IGNORED_ACCOUNT_NOT_CONFIG = -22;
        public const int IGNORED_TICKER_NOT_CONFIG = -23;
        public const int IGNORED_BROKER_NOT_CONFIG = -24;
        public const int IGNORED_ORDER_ID_ERROR = -25;
        public const int IGNORED_DATABASE_ERROR = -26;
        public const int IGNORED_TICKER_NOT_ALLOWED = -27;
        public const int IGNORED_COMM_ERR = -28;
        public const int IGNORED_DATA_INVALID = -29;
        public const int IGNORED_DUPLICATE_ORDER = -30;


        public const int INPROGRESS = 1;
        public const int INPROGRESS_TIMEOUT = 2;
        public const int INPROGRESS_SUBMITTED = 3;
        public const int INPROGRESS_INACTIVE = 4;
        public const int INPROGRESS_CANCEL = 5;
        public const int INPROGRESS_OTHER = 9;

        public const int SUCCESS = 10;
        public const int PARTIALLY_FILLED = 11;
        public const int PARTIALLY_FILLED_FINAL = 12;
        public const int FILLED = 13;

        public static bool IsInProgress(int statusCode)
        {
            return statusCode == OrderStatusCodes.INPROGRESS
                || statusCode == OrderStatusCodes.INPROGRESS_TIMEOUT
                || statusCode == OrderStatusCodes.INPROGRESS_SUBMITTED
                || statusCode == OrderStatusCodes.INPROGRESS_INACTIVE
                || statusCode == OrderStatusCodes.PARTIALLY_FILLED;
        }
        public static bool IsComplete(int statusCode)
        {
            return statusCode == OrderStatusCodes.FILLED
                || statusCode == OrderStatusCodes.PARTIALLY_FILLED_FINAL
                || (statusCode < 0 && statusCode >= OrderStatusCodes.FAIL_OTHER);
        }

        public static string GetOrderStatus(int statusCode)
        {
            // this is a simplified mapping, it should be expanded based on actual broker status codes
            string orderStatus = OrderStatus.NONE;
            if (statusCode == OrderStatusCodes.FAIL_CANCELED || statusCode == OrderStatusCodes.FAIL_CLOSING_INCOMPLETE_ORDER || statusCode == OrderStatusCodes.FAIL_CLOSED_BEFORE_SUBMISSION)
            {
                orderStatus = OrderStatus.CANCELED;
            }
            else if (statusCode < 0)
            {
                orderStatus = OrderStatus.FAILED;
            }
            else if (statusCode > 0 && statusCode < OrderStatusCodes.INPROGRESS_OTHER)
            {
                orderStatus = OrderStatus.PROCESSING;
            }
            else if (statusCode == OrderStatusCodes.FILLED)
            {
                orderStatus = OrderStatus.FILLED;
            }
            else if (statusCode == OrderStatusCodes.PARTIALLY_FILLED)
            {
                orderStatus = OrderStatus.FILLED_PARTIALLY;
            }
            else if (statusCode == OrderStatusCodes.PARTIALLY_FILLED_FINAL)
            {
                orderStatus = OrderStatus.FILLED_PARTIALLY_FIN;
            }
            return orderStatus;
        }

    }
}
