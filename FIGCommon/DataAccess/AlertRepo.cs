using FIGCommon.Exceptions;
using FIGCommon.Models;
using FIGCommon.Models.Alert;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Cms;
using System.Data;

namespace FIGCommon.DataAccess
{
    public class AlertRepo
    {
        private static ILogger<AlertRepo>? _logger;
        private static readonly Object lockAccess = new();
        private static readonly RepositoryBase RBASE = new RepositoryBase();
        private static readonly RepositoryBase RBASEADMIN = new RepositoryBase();

        public static void Initialize(IServiceProvider serviceProvider, string connectionTag = "AlertConnection")
        {
            string connectionString = "";
            string adminConnectionString = "";
            lock (lockAccess)
            {
                _logger = serviceProvider.GetRequiredService<ILogger<AlertRepo>>();

                // get connection string from appsettings.json
                var config = serviceProvider.GetRequiredService<IConfiguration>();
                if (config != null)
                {
                    connectionString = config.GetConnectionString(connectionTag) ?? "";
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        connectionString = config.GetConnectionString("DefaultConnection") ?? "";
                    }
                    adminConnectionString = config.GetConnectionString("AdminConnection") ?? "";
                }
                if (config == null || connectionString == "")
                {
                    _logger?.LogError($"Database {connectionTag} Configuration is invalid");
                    throw new SqlConfigException();
                }
                // Access RBASE directly as it is a static field
                RBASE.Initialize(connectionString, _logger);
                if (adminConnectionString != "")
                {
                    RBASEADMIN.Initialize(adminConnectionString, _logger);
                }
            }
        }

        #region Properties
        public static string ConnectionString
        {
            get
            {
                lock (lockAccess)
                {
                    return RBASE.ConnectionString;
                }
            }
        }

        public static string DatabaseName
        {
            get
            {
                lock (lockAccess)
                {
                    return RBASE.DatabaseName;
                }
            }
        }
        #endregion Properties

        #region Transactions

        public static SqlTransaction? OpenTransaction()
        {
            SqlConnection? connection = null;

            try
            {
                connection = new SqlConnection(RBASE.ConnectionString);
                connection.Open();

                var transaction = connection.BeginTransaction();
                return transaction;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to open transaction - {ex.Message}");

                // Ensure we don't leak the connection
                connection?.Dispose();
                return null;
            }
        }
        public static void CommitTransaction(SqlTransaction? transaction)
        {
            if (transaction == null) return;

            var conn = transaction.Connection;
            try
            {
                transaction.Commit();
            }
            finally
            {
                transaction.Dispose();
                conn?.Dispose(); // IMPORTANT: closes connection
            }
        }

        public static void RollbackTransaction(SqlTransaction? transaction)
        {
            if (transaction == null) return;

            var conn = transaction.Connection;
            try
            {
                transaction.Rollback();
            }
            finally
            {
                transaction.Dispose();
                conn?.Dispose(); // IMPORTANT: closes connection
            }
        }


        #endregion Transactions

        #region Event
        public static EventRS? GetEvent(int Id)
        {
            return RBASE.Select<EventRS, int>(new EventRS(), "usp_event_select", "@Id", Id);
        }

        public static EventRS? GetEvent(string eventType, string eventId)
        {
            return RBASE.Select<EventRS, string, string>(new EventRS(), "usp_event_query", "@EventType", eventType, "@EventId", eventId);
        }

        public static List<EventRS> GetEvents()
        {
            return RBASE.SelectMulti<EventRS, int>(new EventRS(), "usp_event_select", "@Id", -1);
        }

        public static List<EventRS> GetActiveEvents(int duration)
        {
            return RBASE.SelectMulti<EventRS, int>(new EventRS(), "usp_event_select_active", "@Seconds", duration);
        }

        public static void TouchPriceDataEvent(int datasetId)
        {
            RBASE.ExecuteScalar<string, string>("usp_event_touch", "@EventType", "PRICEDATA", "@EventId", $"{datasetId}");
        }

        public static void TouchEvent(string eventType, string eventId)
        {
            RBASE.ExecuteScalar<string, string>("usp_event_touch", "@EventType", eventType, "@EventId", eventId);
        }

        public static void CleanUpEvents(string eventType, int duration)
        {
            RBASE.ExecuteScalar<string, int>("usp_event_cleanup", "@EventType", eventType, "@DurationInMin", duration);
        }
        #endregion Event

        #region Alert

        public static AlertRS? GetAlert(int id)
        {
            return RBASE.Select<AlertRS, int>(new AlertRS(), "usp_alert_select", "@Id", id);
        }

        public static List<AlertRS> GetAlerts()
        {
            return RBASE.SelectMulti<AlertRS, int>(new AlertRS(), "usp_alert_select", "@Id", -1);
        }

        public static List<AlertRS> GetAlertsSince(int duration)
        {
            return RBASE.SelectMulti<AlertRS, int>(new AlertRS(), "usp_alert_select_since", "@Duration", duration);
        }

        public static AlertRS UpsertAlert(AlertRS rec)
        {
            long newId = RBASE.Upsert<AlertRS>(rec, "usp_alert_upsert", "@IdNew");
            rec.Id = (int)newId;
            return rec;
        }

        public static void UpdateAlertMatch(int alertId, int matchedRuleId, string friendlyMessage)
        {
            RBASE.ExecuteScalar<int, int, string>("usp_alert_update_match", "@Id", alertId, "@MatchedRuleId", matchedRuleId, "@FriendlyMessage", friendlyMessage);
        }

        public static List<PendingAlertRS> GetPendingAlerts(int lookbackMinutes = 30)
        {
            return RBASE.SelectMulti<PendingAlertRS, int>(new PendingAlertRS(), "usp_alert_get_pending", "@LookbackMinutes", lookbackMinutes);
        }

        public static List<PendingAlertRS> GetPendingImmediateAlerts(int maxRec = 100, int lookbackMinutes = 30)
        {
            return RBASE.SelectMulti<PendingAlertRS, int, int>(new PendingAlertRS(),
                "usp_alert_get_pending_immediate", "@MaxRec", maxRec, "@LookbackMinutes", lookbackMinutes);
        }

        public static void MarkAlertSent(int alertId)
        {
            RBASE.ExecuteScalar<int>("usp_alert_mark_sent", "@Id", alertId);
        }

        public static void DeleteAlert(int id)
        {
            RBASE.Delete<int>("usp_alert_delete", "@Id", id);
        }

        #endregion Alert

        #region AlertRuleOperator

        public static AlertRuleOperatorRS? GetAlertRuleOperator(string operatorCode)
        {
            return RBASE.Select<AlertRuleOperatorRS, string>(new AlertRuleOperatorRS(), "usp_alertruleoperator_select", "@OperatorCode", operatorCode);
        }

        public static List<AlertRuleOperatorRS> GetAlertRuleOperators()
        {
            return RBASE.SelectMulti<AlertRuleOperatorRS, string>(new AlertRuleOperatorRS(), "usp_alertruleoperator_select", "@OperatorCode", "");
        }

        #endregion AlertRuleOperator

        #region AlertRule

        public static AlertRuleRS? GetAlertRule(int id)
        {
            return RBASE.Select<AlertRuleRS, int>(new AlertRuleRS(), "usp_alertrule_select", "@Id", id);
        }

        public static List<AlertRuleRS> GetAlertRules()
        {
            return RBASE.SelectMulti<AlertRuleRS, int>(new AlertRuleRS(), "usp_alertrule_select", "@Id", -1);
        }

        public static AlertRuleRS UpsertAlertRule(AlertRuleRS rec)
        {
            long newId = RBASE.Upsert<AlertRuleRS>(rec, "usp_alertrule_upsert", "@IdNew");
            rec.Id = (int)newId;
            return rec;
        }

        public static void DeleteAlertRule(int id)
        {
            RBASE.Delete<int>("usp_alertrule_delete", "@Id", id);
        }

        #endregion AlertRule

        #region AlertMethod

        public static List<AlertMethodRS> GetAlertMethods()
        {
            return RBASE.SelectMulti<AlertMethodRS>(new AlertMethodRS(), "usp_alertmethod_select");
        }

        #endregion AlertMethod

        #region Recipient

        public static AlertRecipientRS? GetRecipient(int id)
        {
            var rec = RBASE.Select<AlertRecipientRS, int>(new AlertRecipientRS(), "usp_recipient_select", "@Id", id);
            rec?.Subscriptions = GetRecipientSubscriptions(rec.Id);
            return rec;
        }

        public static List<AlertRecipientRS> GetRecipients()
        {
            var recs = RBASE.SelectMulti<AlertRecipientRS, int>(new AlertRecipientRS(), "usp_recipient_select", "@Id", -1);
            foreach (var rec in recs)
            {
                rec.Subscriptions = GetRecipientSubscriptions(rec.Id);
            }
            return recs;
        }


        public static AlertRecipientRS UpsertRecipient(AlertRecipientRS rec)
        {
            long newId = RBASE.Upsert<AlertRecipientRS>(rec, "usp_recipient_upsert", "@IdNew");
            rec.Id = (int)newId;
            return rec;
        }

        public static void DeleteRecipient(int id)
        {
            RBASE.Delete<int>("usp_recipient_delete", "@Id", id);
        }
        #endregion Recipient

        #region RecipientSubscription

        public static List<AlertRecipientSubscriptionRS> GetRecipientSubscriptions(int recipientId)
        {
            return RBASE.SelectMulti<AlertRecipientSubscriptionRS, int>(new AlertRecipientSubscriptionRS(), "usp_recipientsubscription_query", "@RecipientId", recipientId);
        }

        public static AlertRecipientSubscriptionRS UpsertRecipientSubscription(AlertRecipientSubscriptionRS rec)
        {
            long newId = RBASE.Upsert<AlertRecipientSubscriptionRS>(rec, "usp_recipientsubscription_upsert", "@IdNew");
            return rec;
        }

        public static void DeleteRecipientSubscription(int recipientId, int alertRuleId)
        {
            RBASE.Delete<int, int>("usp_recipientsubscription_delete", "@RecipientId", recipientId, "@AlertRuleId", alertRuleId);
        }
        #endregion RecipientSubscription

        #region AlertType

        public static List<AlertTypeRS> GetAlertTypes()
        {
            return RBASE.SelectMulti<AlertTypeRS>(new AlertTypeRS(), "usp_alerttype_select");
        }

        #endregion AlertType

    }
}

