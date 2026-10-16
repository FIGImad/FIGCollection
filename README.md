# FIGAutoTradeServer
Auto-Trade Engine - Sends instructions to different Auto-Trade BOTs for execution based on set Trading Strategies

## Event monitor transport

`EventMonitor:Mode` defaults to `SqlDependency`, preserving existing services' behavior.
To opt a service into polling, merge these settings into its existing configuration
(retain its connection string and event table), then restart the service:

```json
"EventMonitor": {
  "Mode": "Polling",
  "PollIntervalMilliseconds": 1000
}
```

Polling uses ordinary, cancellable SQL reads of the configured event table. It does
not start SqlDependency, register query notifications, run the notification refresh
loop, or require Service Broker. Mode names are case-insensitive; unsupported modes
and nonpositive polling intervals fail configuration validation. Changing modes
requires a restart. FIGAlertSvc forwards these settings while retaining its Alert
database connection override.

The public Subscribe/Unsubscribe interface, event types, and callback payloads are
unchanged. The first successful read establishes a baseline without replaying old
events. Subsequent reads publish inserted rows, newer timestamps, or changed messages
at equal timestamps; older timestamps remain ignored, as in notification mode.
Deleted rows publish a null payload. Each poll reads the full table rather than a
wall-clock window, so UTC/local timestamp differences do not exclude rows by age.
This does not correct mixed timestamp conventions in writers or other readers.

There is one polling loop per monitor. Reads and synchronous callbacks finish before
the next interval starts. Thus detection latency includes the interval, query time,
and callback time; it is not a strict one-second delivery guarantee. Rapid changes
can coalesce into the latest stored state, and rows inserted and deleted between
polls are not observable. Identical timestamp/message rewrites are not observable
either. The Event table is a state notification mechanism, not a durable event queue.
Read failures retain the previous snapshot and retry after the configured interval;
disposal cancels pending reads and delays.

Deploy the rebuilt shared library/services and explicitly enable polling in every
relevant service against the affected database to remove their query-notification
subscriptions. Other clients can retain subscriptions, so changing one service alone
does not establish that notification deadlocks are eliminated. This change does not
alter database triggers or business logic. Polling adds one full-table read per monitor
per interval; assess load before choosing a shorter interval. Roll back by selecting
`SqlDependency` and restarting.

Regression checks (no database required):
`dotnet run --project tests/FIGEventMonitor.Regression/FIGEventMonitor.Regression.csproj`

## Signal transaction deadlocks

FIGSignalSvc retries SQL Server deadlock-victim error 1205 up to three times after
the initial attempt. Each attempt uses a fresh connection/transaction and reloads
durable signal progress and signal rows before rebuilding the batch. History,
signals, progress, and checkpoint writes are retried together, with the completed-bar
boundary fixed for the call. Transaction callbacks must therefore be replayable
database writes using the supplied transaction, without external side effects.
Failed attempts are disposed before randomized delays of 100–200, 200–300, and
400–500 milliseconds. Publication wakeup happens only after a successful attempt
and outside the retry boundary. Exhaustion is logged as an error and propagated.

Timeouts, lock timeouts, connection failures, and other errors are not automatically
retried by this policy. Upsert and commit call durations are logged, including when
calls throw; a call-ended message does not establish successful commit. Retries
handle victim recovery but do not prevent deadlocks or shorten waits when another
transaction is selected as the victim. Polling addresses a separate notification
contention path. Rebuild/deploy FIGSignalSvc to activate this change; no new setting
is required.

Signal replay and retry regressions:
`dotnet run --project tests/FIGSignalReplay.Regression/FIGSignalReplay.Regression.csproj`
