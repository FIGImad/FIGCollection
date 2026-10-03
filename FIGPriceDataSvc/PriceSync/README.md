# Controller-routed price synchronization

The standalone host is FIGPriceDataSvc. See [configuration and scheduling](../README.md) for its enabled defaults, dataset selection, market schedule, and pre-close/correction cadence.

PriceSyncApiService derives from ApiServiceBase. It calls POST /api/historicaldata through FIGControllerSvc using the PriceSync destination role and an optional explicit service ID. Requests use the existing authenticated SVC service account. Each local dataset supplies its ticker contract; database identity numbers are not copied from the remote server.

The provider API accepts HistoricalDataRequest. StartRawTime is its existing end-time field, in UTC Unix seconds. IntervalId is provider notation such as "1 min", Duration is seconds such as "600 S", and NumBars must be zero. Responses identify the exact requested range and carry RawTime/OHLC/Volume bars.

FIGPriceDataSvc requests one-minute FUT or STK prices. Continuous futures are not supported by this historical paging path because the current provider ignores their requested end time. Requests are bounded to 1,000 bars. A historical empty result can advance the catch-up scan; provider failures and database failures cannot. On restart the worker resumes from persisted PriceData with overlap, rechecking empty periods if needed.

At pre-close time the end timestamp remains the actual current second, allowing the forming bar to be fetched. The request duration remains a whole number of minutes. At the boundary correction, newly returned bars beyond the last locally stored bar are excluded. Recent overlapping bars can be corrected; older corrections outside the overlap require deliberate backfill.

RemotePriceStore commits each batch atomically. Normal catch-up updates changed bars and inserts missing bars, relying on the existing unique DataSetId/RawTime index. Boundary corrections only update existing rows; they never insert rows, including holes inside the overlap. Unchanged replay does not emit a spurious PRICEDATA event. Actual changes activate the existing database trigger, which FIGSignalSvc can observe from the same local database.

The historical API returns 400 for invalid requests, 429 when another API request is active, and 503 for provider failure or invalid results. Failures are logged and catch-up is retried after 75 seconds, with corrections blocked until it succeeds. FIGPriceSyncSvc retains its provider and account limits; this component does not bypass them.

Keep FIGPriceDataSvc and the remote FIGPriceSyncSvc clocks synchronized, and match TimeOffsetSec and MarketSchedule configuration. No local broker connection or local FIGPriceSyncSvc installation is required.
