namespace FIGCommon.Models.FIGProviderAPI
{
    public class ContractInfo
    {
        /**
        * @brief The unique IB contract identifier
        */
        public int ConId { get; set; } = -1;

        /**
         * @brief The underlying's asset symbol
         */
        public string Symbol { get; set; } = "";

        /**
         * @brief asset's long name
         */
        public string Name { get; set; } = "";

        /**
         * @brief The security's type:
         *      STK - stock (or ETF)
         *      OPT - option
         *      FUT - future
         *      IND - index
         *      FOP - futures option
         *      CASH - forex pair
         *      BAG - combo
         *      WAR - warrant
         *      BOND- bond
         *      CMDTY- commodity
         *      NEWS- news
         *		FUND- mutual fund
		 */
        public string SecType { get; set; } = "";

        /**
        * @brief The contract's last trading day or contract month (for Options and Futures). Strings with format YYYYMM will be interpreted as the Contract Month whereas YYYYMMDD will be interpreted as Last Trading Day.
        */
        public string LastTradeDateOrContractMonth { get; set; } = "";

        /**
         * @brief The option's strike price
         */
        public double Strike { get; set; } = 0.0;

        /**
         * @brief Either Put or Call (i.e. Options). Valid values are P, PUT, C, CALL. 
         */
        public string Right { get; set; } = "";

        /**
         * @brief The instrument's multiplier (i.e. options, futures).
         */
        public string Multiplier { get; set; } = "";

        /**
         * @brief The instrument's minimum tick (i.e. options, futures).
         */
        public decimal MinTick { get; set; } = 1.0M;

        /**
         * @brief The destination exchange.
         */
        public string Exchange { get; set; } = "";

        /**
         * @brief The underlying's currency
         */
        public string Currency { get; set; } = "";

        /**
         * @brief The contract's symbol within its primary exchange
		 * For options, this will be the OCC symbol
         */
        public string LocalSymbol { get; set; } = "";

        /**
         * @brief The contract's primary exchange.
		 * For smart routed contracts, used to define contract in case of ambiguity. 
		 * Should be defined as native exchange of contract
		 * For exchanges which contain a period in name, will only be part of exchange name prior to period, i.e. ENEXT for ENEXT.BE
         */
        public string PrimaryExch { get; set; } = "";

        /**
         * @brief The trading class name for this contract.
         * Available in TWS contract description window as well. For example, GBL Dec '13 future's trading class is "FGBL"
         */
        public string TradingClass { get; set; } = "";

        /**
        * @brief If set to true, contract details requests and historical data queries can be performed pertaining to expired futures contracts.
        * Expired options or other instrument types are not available.
        */
        public bool IncludeExpired { get; set; } 

        /**
         * @brief Security's identifier when querying contract's details or placing orders
         *      ISIN - Example: Apple: US0378331005
         *      CUSIP - Example: Apple: 037833100
         */
        public string SecIdType { get; set; } = "";

        /**
        * @brief Identifier of the security type
        * @sa secIdType
        */
        public string SecId { get; set; } = "";

        /**
        * @brief Description of the contract
        */
        public string Description { get; set; } = "";

        /**
        * @brief IssuerId of the contract
        */
        public string IssuerId { get; set; } = "";

        /**
         * @brief Description of the combo legs.
         */
        public string ComboLegsDescription { get; set; } = "";

        public override string ToString()
        {
            return SecType + " " + Symbol + " " + Currency + " " + Exchange;
        }
    }
}
