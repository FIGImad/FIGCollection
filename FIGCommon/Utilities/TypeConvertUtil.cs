namespace FIGCommon.Utilities
{
    public class TypeConvertUtil
    {
        public static long? GetIntegerValue(object? val)
        {
            if (val is null || val is DBNull) return null;

            // Fast-path for numeric types
            if (val is long l) return l;
            if (val is int i) return (long) i;
            if (val is short s) return (long) s;
            if (val is byte b) return (long) b;

            // Strings: TryParse (no exceptions)
            if (val is string str)
            {
                if (string.IsNullOrWhiteSpace(str)) return null;
                if (long.TryParse(str, out var parsed)) return parsed;
                return null;
            }

            // Other IConvertible (e.g., decimal, double)
            try
            {
                return Convert.ToInt64(val);
            }
            catch (InvalidCastException) { return null; }
            catch (FormatException) { return null; }
            catch (OverflowException) { return null; }
            catch { return null; }
        }

        public static decimal? GetDecimalValue(object? val)
        {
            if (val is null || val is DBNull) return null;

            // Fast-path for numeric types
            if (val is decimal d) return d;
            if (val is double db) return (decimal)db;
            if (val is float f) return (decimal)f;
            if (val is long l) return (decimal)l;
            if (val is int i) return (decimal)i;
            if (val is short s) return (decimal)s;
            if (val is byte b) return (decimal)b;

            // Strings: TryParse (no exceptions)
            if (val is string str)
            {
                if (string.IsNullOrWhiteSpace(str)) return null;
                if (decimal.TryParse(str, out var parsed)) return parsed;
                return null;
            }

            // Other IConvertible
            try
            {
                return Convert.ToDecimal(val);
            }
            catch (InvalidCastException) { return null; }
            catch (FormatException) { return null; }
            catch (OverflowException) { return null; }
            catch { return null; }
        }

        public static double? GetDoubleValue(object? val)
        {
            if (val is null || val is DBNull) return null;

            // Fast-path for numeric types
            if (val is double d) return d;
            if (val is float f) return (double)f;
            if (val is decimal dec) return (double)dec;
            if (val is long l) return (double)l;
            if (val is int i) return (double)i;
            if (val is short s) return (double)s;
            if (val is byte b) return (double)b;

            // Strings: TryParse (no exceptions)
            if (val is string str)
            {
                if (string.IsNullOrWhiteSpace(str)) return null;
                if (double.TryParse(str, out var parsed)) return parsed;
                return null;
            }

            // Other IConvertible
            try
            {
                return Convert.ToDouble(val);
            }
            catch (InvalidCastException) { return null; }
            catch (FormatException) { return null; }
            catch (OverflowException) { return null; }
            catch { return null; }
        }


        public static bool? GetBooleanValue(object? val)
        {
            if (val is null || val is DBNull) return null;

            // Fast-path for real bool
            if (val is bool b) return b;

            // Numeric types (common from DB / legacy systems)
            if (val is byte by) return by != 0;
            if (val is short s) return s != 0;
            if (val is int i) return i != 0;
            if (val is long l) return l != 0;

            // Strings
            if (val is string str)
            {
                if (string.IsNullOrWhiteSpace(str)) return null;

                str = str.Trim();

                if (bool.TryParse(str, out var parsedBool))
                    return parsedBool;

                // Common "truthy/falsey" strings
                switch (str.ToLowerInvariant())
                {
                    case "1":
                    case "yes":
                    case "y":
                    case "on":
                    case "true":
                        return true;

                    case "0":
                    case "no":
                    case "n":
                    case "off":
                    case "false":
                        return false;
                }

                return null;
            }

            // Other IConvertible fallback
            try
            {
                return Convert.ToBoolean(val);
            }
            catch (InvalidCastException) { return null; }
            catch (FormatException) { return null; }
            catch (OverflowException) { return null; }
            catch { return null; }
        }


        public static DateTime? GetDateTimeValue(object? val)
        {
            if (val is null || val is DBNull) return null;

            // Fast path
            if (val is DateTime dt) return dt;

            // String parsing
            if (val is string str)
            {
                if (string.IsNullOrWhiteSpace(str)) return null;

                str = str.Trim();

                if (DateTime.TryParse(str, out var parsed))
                    return parsed;

                return null;
            }

            // Handle ticks (common if you store DateTime as bigint ticks)
            if (val is long ticks)
            {
                try
                {
                    return new DateTime(ticks);
                }
                catch
                {
                    return null;
                }
            }

            // Other conversions (SQL types etc.)
            try
            {
                return Convert.ToDateTime(val);
            }
            catch (InvalidCastException) { return null; }
            catch (FormatException) { return null; }
            catch (OverflowException) { return null; }
            catch { return null; }
        }


        public static string? GetStringValue(object? val)
        {
            if (val is null || val is DBNull) return null;

            if (val is string s)
                return s;

            try
            {
                return Convert.ToString(val);
            }
            catch
            {
                return null;
            }
        }

    }
}
