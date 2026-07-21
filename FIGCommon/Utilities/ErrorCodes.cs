namespace FIGCommon.Utilities
{
    public static class ErrorCodes
    {
        // define all ErrorCodeDefs enums here:
        public const int UnSpecified = -1;
        public const int NoError = 0;
        public const int ErrorTooManyRequests = 11;
        public const int AuthError_General = 100;                            // General Authentication error
        public const int AuthError_Login = 101;                              // General Login failure
        public const int AuthError_InvalidCred = 102;                        // Invalid username or password
        public const int AuthError_EmailConfirm = 103;                       // Email needs to be confirmed
        public const int AuthError_Registration = 104;                       // Registration error
        public const int AuthError_InvalidUser = 105;                        // Invalid User
        public const int AuthError_Unauthorized = 106;                       // User unauthorized
        public const int AuthError_Authentication = 107;                     // User not authenticated
        public const int DBError_General = 200;                              // General database failure
        public const int DBError_Duplicate = 201;                            // Inserting duplicate entry
        public const int DBError_ProcedureExec = 202;                        // Error executing procedure
        public const int DBError_Update = 203;                               // Error updating database
        public const int DBError_Insert = 204;                               // Error inserting to database
        public const int DBError_Upsert = 205;                               // Error upserting to database
        public const int DataError_Config = 300;                             // Data configuration error
        public const int DataError_Duplicate = 301;                          // Duplicate entry error
        public const int DataError_NoData = 302;                             // No data error
        public const int DataError_InvalidData = 303;                        // data is invalid error
        public const int DataError_NotReady = 304;                           // data is not yet ready
        public const int DataError_InvalidArgument = 305;                    // Invalid Argument
        public const int DataError_Parsing = 306;                            // Data could not be parsed
        public const int ClientError_NotFound = 404;                         // Client Error - Not Found
        public const int SysError_InternalServerError = 500;                 // System Error - General
        public const int SysError_NotImplemented = 501;                      // The server does not support the requested function
        public const int SysError_BadGateway = 502;                          // The server is temporarily unavailable
        public const int SysError_ServiceUnavailable = 503;                  // The server is temporarily unavailable
        public const int SysError_GatewayTimeout = 504;                      // The response from server is timed out
        public const int ApiService_General = 600;                           // The response from RootsApiService is not successful
        public const int ApiService_NotResponding = 601;                     // The response from RootsApiService has timed out

        //// AutoTradeSignal related error codes
        //public const int SignalNotFound = 701;                               // Signal not found
        //public const int OrderTimeout = 702;                                 // Order was not submitted (or completed) within the expected time frame
        //public const int OrderExpired = 703;                                 // Signal was expired before the order could be submitted
        public const int OrderRejected = 704;
        //public const int OrderFailed = 705;
        //public const int InvalidOrderId = 706;
        //public const int SignalCanceled = 711;                               // Signal is canceled
        //public const int SignalClosedWhileOpen = 712;                        // Signal has not finished the open process but is already closed (e.g., closed by user or system before the open process is completed)
        //public const int SignalClosedButOpenFailed = 713;                    // Signal closed but the open process is at a failed state
        ////public const int SignalClosedDueToNewlyOpenedOne = 714;              // Signal has not finished the open process but is already closed (e.g., closed by user or system before the open process is completed)
        //public const int NoOpenPosition = 715;                               // Signal has no open position, it may have been cancelled or modified manually


        public const int LastError = 999999;                                 // Dummy Error

        public static string ToText(int errorCode)
        {
            // Get all public static fields of the ErrorCodes class
            var fields = typeof(ErrorCodes).GetFields(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static
            );

            // Find the field with the matching value
            foreach (var field in fields)
            {
                if (field.GetValue(null) is int value && value == errorCode)
                {
                    return field.Name; // Return the field name (e.g., "NoError")
                }
            }

            return "UnSpecified"; // Default if not found
        }

        public static int ToErrorCode(string errorCodeStr)
        {
            // Get all public static fields of the ErrorCodes class
            var fields = typeof(ErrorCodes).GetFields(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static
            );

            // Find the field with the matching name (case-insensitive)
            foreach (var field in fields)
            {
                if (field.Name.Equals(errorCodeStr, StringComparison.OrdinalIgnoreCase))
                {
                    return (int?)field.GetValue(null) ?? UnSpecified; // Return the field's value (e.g., 0 for "NoError")
                }
            }

            return UnSpecified; // Default if not found
        }

    }
}