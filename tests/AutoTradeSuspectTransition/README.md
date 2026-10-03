# AutoTrade SUSPECT transition regression

Run from the repository root with Windows authentication to a local test SQL Server:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tests/AutoTradeSuspectTransition/Test-SuspectTransition.ps1
```

Use `-Server <instance>` to select another test instance. The script runs the actual
deployment procedure with its table and procedure references redirected to temporary
objects in `tempdb`. It does not touch application tables or install a procedure.

The 15 checks cover first failure, repeated failure after manual reactivation,
final partial closes, transitions within the failure group, successful closes,
zero quantities, manual quantities, short positions, non-active bots, null prior
status, and a new failure after a retry returns to PROCESSING.

For an existing database matching the supplied procedure, execute
`FIGInstaller/Resources/fig_autotrader_suspect_transition_migration.sql` with the
AutoTrader database selected, then deploy the rebuilt FIGAutoTraderSvc. The script
alters only `usp_autotrade_signal_upsert`; it does not reset existing BOT statuses
or reconcile historical positions. The schema snapshot includes the same change.
The installer database script retains its existing canceled-close and insert
handling, with the equivalent transition guard added to updates.

FAILED and FILLED_PARTIALLY_FIN are treated as one failure episode. Saving either
again, or switching between them, does not undo a manual BOT reactivation. A retry
that leaves that group and then fails again does mark an ACTIVE BOT SUSPECT.

Service verification: build FIGAutoTraderSvc and inspect that unchanged pending
signals are not saved and that the BOT is reloaded after close processing, before
the new-signal eligibility check. A live broker integration test is not included.
