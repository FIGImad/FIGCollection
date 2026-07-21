# Native parity checklist

- [x] Native x64 project with no third-party runtime or plugin loader
- [x] Core records and deterministic OHLCV aggregation
- [x] Separate live classes for MA, WMA, ROC, SEM, BB, Extended, and ADX
- [x] Shared bounded tail-revision/rollback contract for bar-by-bar studies
- [ ] Configuration variable expansion compatible with `global_vars.json`
- [x] SQL Server ODBC connection, query repository, and transaction primitive
- [ ] Study history/signal write operations and transaction parity
- [ ] Event monitor and price-processing coordinator
- [ ] Study history update/bulk-insert behavior
- [ ] Signal state machine and atomic signal/history writes
- [ ] Native Classic collection
- [x] Separate live classes for ADXTrend, SlopeTrend, and LBaseADXEx
- [x] Native bar-by-bar ADXTrend collection
- [ ] Native L2 collection
- [ ] HTTP.sys ping/secured-ping API and JWT validation
- [ ] Controller registration/client heartbeat
- [ ] Windows Service lifecycle and installer integration
- [ ] Golden-output comparison against the .NET service
