const fpArray =[];
function preMain(){

    setPriceStudy(true);
    setStudyTitle("ADX_Base Test");

    setColorPriceBars(true);

    let x=-1; // set it to -1 so that it becomes 0 in the first iteration
	fpArray[++x] = new FunctionParameter("AdxLen", FunctionParameter.NUMBER);
	fpArray[x].setName("ADX Length");
	fpArray[x].setLowerLimit(1);
	fpArray[x].setDefault(50);

	fpArray[++x] = new FunctionParameter("FastMALen", FunctionParameter.NUMBER);
	fpArray[x].setName("Price MA Len");
	fpArray[x].setLowerLimit(1);
	fpArray[x].setDefault(3);

	fpArray[++x] = new FunctionParameter("GuideMALen", FunctionParameter.NUMBER);
	fpArray[x].setName("Guide MA Len");
	fpArray[x].setLowerLimit(1);
	fpArray[x].setDefault(28);

	fpArray[++x] = new FunctionParameter("GuideStdDev", FunctionParameter.NUMBER);
	fpArray[x].setName("Guide StdDev");
	fpArray[x].setLowerLimit(0.01);
	fpArray[x].setDefault(0.5);

	fpArray[++x] = new FunctionParameter("StartBias", FunctionParameter.NUMBER);
	fpArray[x].setName("Start Bias");
	fpArray[x].setLowerLimit(-10);
	fpArray[x].setDefault(2.0);

	fpArray[++x] = new FunctionParameter("StopBias", FunctionParameter.NUMBER);
	fpArray[x].setName("Stop Bias");
	fpArray[x].setLowerLimit(-100);
	fpArray[x].setDefault(-1.5);

	fpArray[++x] = new FunctionParameter("ReEntryCooldownBars", FunctionParameter.NUMBER);
	fpArray[x].setName("Re-entry Cooldown Bars");
	fpArray[x].setLowerLimit(0);
	fpArray[x].setDefault(2);

	fpArray[++x] = new FunctionParameter("TrendReEntryShockMax", FunctionParameter.NUMBER);
	fpArray[x].setName("Trend Re-entry Shock Max");
	fpArray[x].setLowerLimit(0);
	fpArray[x].setDefault(4);

	fpArray[++x] = new FunctionParameter("ShockATRLen", FunctionParameter.NUMBER);
	fpArray[x].setName("Shock ATR Len");
	fpArray[x].setLowerLimit(1);
	fpArray[x].setDefault(7);

	fpArray[++x] = new FunctionParameter("ShockDropATR", FunctionParameter.NUMBER);
	fpArray[x].setName("Shock Drop ATR");
	fpArray[x].setLowerLimit(0);
	fpArray[x].setDefault(4.5);

	fpArray[++x] = new FunctionParameter("ShockArmedDropATR", FunctionParameter.NUMBER);
	fpArray[x].setName("Shock Armed Drop ATR");
	fpArray[x].setLowerLimit(0);
	fpArray[x].setDefault(2.5);

	fpArray[++x] = new FunctionParameter("ShockArmUpATR", FunctionParameter.NUMBER);
	fpArray[x].setName("Shock Arm Up ATR");
	fpArray[x].setLowerLimit(0);
	fpArray[x].setDefault(3.0);

	fpArray[++x] = new FunctionParameter("ShockRangeATR", FunctionParameter.NUMBER);
	fpArray[x].setName("Shock Range ATR");
	fpArray[x].setLowerLimit(0);
	fpArray[x].setDefault(2.5);

	fpArray[++x] = new FunctionParameter("ChopMinGuideWidthATR", FunctionParameter.NUMBER);
	fpArray[x].setName("Chop Min Guide Width ATR");
	fpArray[x].setLowerLimit(0);
	fpArray[x].setDefault(0.6);

	fpArray[++x] = new FunctionParameter("ChopMaxGuideSlopeATR", FunctionParameter.NUMBER);
	fpArray[x].setName("Chop Max Guide Slope ATR");
	fpArray[x].setLowerLimit(0);
	fpArray[x].setDefault(0.1);

	fpArray[++x] = new FunctionParameter("ContinuationMaxShockATR", FunctionParameter.NUMBER);
	fpArray[x].setName("Continuation Max Shock ATR");
	fpArray[x].setLowerLimit(0);
	fpArray[x].setDefault(1.0);

	askForInput();

}

let studyCol = null;
let engine = null;
let minDateTime = 0;
let maxDateTime = 0;
let cClose = undefined;
let cHigh = undefined;
let cLow = undefined;
let pClose = undefined;
let pHigh = undefined;
let pLow = undefined;
let ppClose = undefined;
let ppHigh = undefined;
let ppLow = undefined;

function main(AdxLen, FastMALen, GuideMALen, GuideStdDev, StartBias, StopBias,
			  ReEntryCooldownBars, TrendReEntryShockMax, ShockATRLen, ShockDropATR, ShockArmedDropATR, ShockArmUpATR,
			  ShockRangeATR, ChopMinGuideWidthATR, ChopMaxGuideSlopeATR, ContinuationMaxShockATR){
	// Initialize the TradeEngine if it is not initialized
	// and initialize the study collection and add strategies
	if (engine == null)
	{
		// default values in case if the configuration file is not provided or not defined in the config file
		const compactMode = true;
		const dataFileName = ''; // 'LC5dot1.csv' // L2dot1S100dot3.csv // LC1dot20.csv // 3Combined.csv
		const aum = 1200000;
		const multiplier = 20;
		const commission = 1.6;
		const canReinvestAUM =  false;
        // const startDateTime = new Date(2015, 0, 1, 0, 0, 0, 0);  // month is 0-based (6 is july)
		const startDateTime = new Date(2000, 0, 1, 0, 0, 0, 0);  // month is 0-based (6 is july)
		const EndDateTime = new Date(2022, 0, 1, 0, 0, 0, 0);  // month is 0-based
		// let startDateTime = new Date(2017, 12, 1, 00, 0, 0, 0);  // month is 0-based (6 is july)
		// let startDateTime = new Date(2016, 12, 1, 00, 0, 0, 0);  // month is 0-based (6 is july)

		// let startDateTime = new Date(1999, 12, 1, 00, 0, 0, 0);  // month is 0-based (6 is july)
		// let startDateTime = new Date(2022, 12, 1, 00, 0, 0, 0);  // month is 0-based (6 is july)
		minDateTime = startDateTime.getTime() / 1000;
		maxDateTime = EndDateTime.getTime() / 1000;
		maxDateTime = 0;

		debugPrint(`Parameters:    aum: ${aum}, multiplier: ${multiplier}, commission: ${commission}, canReinvestAUM: ${canReinvestAUM}\n`);
		debugPrint(`Parameters:    dataFileName: ${dataFileName.length > 0 ? dataFileName : '(empty)'}\n`);


		// initialize StudyCollection
      
        const studyColParams = {
			AdxLen: AdxLen,
			FastMALen: FastMALen,
			GuideMALen: GuideMALen,
			GuideStdDev: GuideStdDev,
			StartBias: StartBias,
			StopBias: StopBias,
			ReEntryCooldownBars: ReEntryCooldownBars,
			TrendReEntryShockMax: TrendReEntryShockMax,
			ShockATRLen: ShockATRLen,
			ShockDropATR: ShockDropATR,
			ShockArmedDropATR: ShockArmedDropATR,
			ShockArmUpATR: ShockArmUpATR,
			ShockRangeATR: ShockRangeATR,
			ChopMinGuideWidthATR: ChopMinGuideWidthATR,
			ChopMaxGuideSlopeATR: ChopMaxGuideSlopeATR,
			ContinuationMaxShockATR: ContinuationMaxShockATR
        };

		debugPrint(`studyColParams:  ${JSON.stringify(studyColParams)}\n`);


		studyCol = new StudyCollection(studyColParams);

		// initialize TradeEngine
		engine = new TradeEngine(aum, multiplier, commission, dataFileName, compactMode, canReinvestAUM);

		// Add strategies to TradeEngine
		// Use default strategies in case the configuration file is not provided
		// if qty is 0 will take PercQty to calculate qty from percentage
		const defStrategies = [

	//********************Approved************************// 
			// {name: "Base_L1", qty: 0, qtyPct: 1.0, enable: true, arg1: 'Mid'}, 
			// {name: "Dynamic_S1", qty: 0, qtyPct: 1.0, enable: true, arg1: 'Mid'}, 
			// {name: "Base_L1", qty: 0, qtyPct: 1.0, enable: true, arg1: 'C21'},
			// {name: "C7_L1", qty: 0, qtyPct: 1.0, enable: true, arg1: 'Mid'}, 
			// {name: "SCont_MAC_MAH3", qty: 0, qtyPct: 1.0, enable: true, arg1: 'Mid'},
			{name: "LADX", qty: 0, qtyPct: 1.0, enable: true, arg1: 'STrend'}
			//{name: "SBaseADX", qty: 0, qtyPct: 1.0, enable: true, arg1: 'STrend'}
		];
        defStrategies.forEach( strategyParam => {
			engine.AddStrategy(strategyParam, studyCol);
		});
	}
	
	// update values of studies
	cClose = close(0);
	cHigh = high(0);
	cLow = low(0);
	pClose = close(-1);
	pHigh = high(-1);
	pLow = low(-1);
	ppClose = close(-2);
	ppHigh = high(-2);
	ppLow = low(-2);

	const cTime = rawtime(0);
	if (cTime < maxDateTime || maxDateTime === 0) {
		studyCol.Calc();
	}
	if (cTime >= minDateTime && (maxDateTime === 0 || cTime < maxDateTime))
	{
		engine.ProcessOrder();   // process trading
		DrawBars();
		DrawPriceBars();
		PaintBackground();
		return CompileDataArray();
	}
}

// show data on legend dynamically, collect all strategy-based studies/data, strategy stats, 
// financial data, etc. to show on the legend of the chart
let initializedLegend = false;
let studyDataList = [];
const studyColor = Color.RGB(0xFF,0x94,0xFF);

function CompileDataArray() {
	const studyDataArr = [];

	if (studyDataList.length === 0) {

		studyDataList = [
			// {	 varName: "C5MAs_UBLB_Cont_BBExit"			// variable name to be displayed
			// 	,color: studyColor
			// 	,alias: 'C5MAs_UBLB_Cont_BBExit'				// default (varName e.g. "SEMC34")... Can change the name on the legend.
			// 	,plotType: PLOTTYPE_LINE	// default(PLOTTYPE_LINE),.. Plot types: PLOTTYPE_LINE, PLOTTYPE_FLATLINES, PLOTTYPE_DOT, PLOTTYPE_SQUAREWAVE, PLOTTYPE_HISTOGRAM
			// 	,lineThickness: 2			// default(1)
			// 	,plotStyle: PS_SOLID		// default (PS_SOLID)... Plot styles: PS_SOLID, PS_DOT, PS_DASH, PS_DASHDOT, PS_DASHDOTDOT
			// 	,enable: false				// default (true)
			// }
			///todo either add here or in config file
		  // {varName: "C1.MA", color: C1_color, level: "C1.MA", lineThickness: 2}
		];
	}


	// prepare labels
	if (engine.IsStarted)
	{
		if (!initializedLegend) {
			initializedLegend = true;
			let labelNdx = 0;

			// add Non-Study Data
			const outputData = engine.GetAllOutputData(engine.IsCompactMode);
			for (let key in outputData) {
				if (outputData.hasOwnProperty(key)) {
					const enable = outputData[key].enable != null ? outputData[key].enable: true;
					if (enable) {
						const plotType = outputData[key].plotType ?? PLOTTYPE_LINE;
						const plotStyle = outputData[key].plotStyle ?? PS_SOLID;
						const lineThickness =  outputData[key].lineThickness ?? 1;
						setCursorLabelName(key, labelNdx);
						const colorData = outputData[key] ? (outputData[key].color ? outputData[key].color : Color.white) : Color.white;
						setDefaultBarFgColor(colorData, labelNdx);
						setPlotType(plotType, labelNdx);
						setDefaultBarStyle(plotStyle, labelNdx);
						setDefaultBarThickness(lineThickness, labelNdx++);
					}
				}
			}

			// Monitor specific studies
			studyDataList.forEach(study => {
				const varName = study.alias ? study.alias : study.varName;
				const color = study.color ? study.color : studyColor;
				const plotType = study.plotType ? study.plotType : PLOTTYPE_LINE;
				const plotStyle = study.plotStyle ? study.plotStyle : PS_SOLID;
				const plotThickness = study.lineThickness ? study.lineThickness : 1;
				const enable = study.enable != null ? study.enable: true;
				if (enable) {
					setCursorLabelName(varName, labelNdx);
					setDefaultBarFgColor(color, labelNdx);
					setPlotType(plotType, labelNdx);
					setDefaultBarStyle(plotStyle, labelNdx);
					setDefaultBarThickness(plotThickness, labelNdx++);
				}
			});
		}

		// collect values (every bar)
		// -----------------------------
		// add Non-Study Data
		const outputData = engine.GetAllOutputData(engine.IsCompactMode);
		for (let key in outputData) {
			if (outputData.hasOwnProperty(key)) {
				const enable = outputData[key].enable != null ? outputData[key].enable: true;
                const isText = outputData[key].isText === true;
				if (enable) {
					studyDataArr.push(isText ? String(outputData[key].value) : outputData[key].value);
				}
			}
		}
		studyDataList.forEach(study => {
			study['value'] = studyCol.studies[0][study.varName];
			const enable = study.enable != null ? study.enable: true;
            const isText = study.isText === true;
			if (enable) {
                studyDataArr.push(isText ? String(study.value) : study.value);
			}
		});
	}
	return studyDataArr;
}

function PaintBackground() {
	// paint background
	const cStudy = studyCol.studies[0];
	if (cStudy) {
		let slope = cStudy.SLMN_MASlope;
		let ma = cStudy.SLMN_MA;
		const bearishonthly = cClose < ma && slope < 0;
		slope = cStudy.SLWK_MASlope;
		ma = cStudy.SLWK_MA;
		const bullishWeekly = cClose > ma && slope > 0;
		const bearishWeakly = cClose < ma && slope < 0;
		slope = cStudy.SLDAY_MASlope;
		ma = cStudy.SLDAY_MA;
		const bullishDaily = cClose > ma && slope > 0;
		const bearishDaily = cClose < ma && slope < 0;
		const bearish = bearishDaily || bearishWeakly || bearishonthly;
		const bullish = bullishDaily || bullishWeekly || bullishDaily;
		setBarBgColor( bullish ? Color.RGB(0, 20, 10) : bearish ? Color.RGB(20, 0, 10) : Color.black);
	}
}

// draw bars to show when a trade is initiated and completed
function DrawBars() {
	if (engine.strategies.length <=1)
	{
		return;
	}
	let dPt = 50;
	let strategiesDir = 0;
	let ctr = 0;
	engine.strategies.forEach(strategy => {
		dPt +=25;

		if (strategy.cSide !== 0) {
			ctr++;
			const strategyDir = strategy.cSide > 0 ? 1 : strategy.cSide < 0 ? -1 : 0;
			strategiesDir += strategyDir;
			const barStats = strategy.sideDefsMap[`${strategy.cSide}`];
			if (barStats && barStats.periods.length > 0) {
				const bar = barStats.periods[barStats.periods.length-1];
				const y = strategy.cSide > 0 ? bar.low - dPt : bar.high + dPt;
				drawLineRelative(
					bar.startBar - getCurrentBarIndex(), y,
					bar.endBar- getCurrentBarIndex(), y,
					PS_SOLID, 3, barStats.color, `${strategy.label}_${strategy.cSide}_${barStats.periods.length}` );

				//const midBar = (bar.startBar + bar.endBar) / 2;
				//const startX = bar.endBar - getCurrentBarIndex() + 5;
				//const allignText = Text.LEFT;
				const startX = bar.startBar - getCurrentBarIndex() - 5;
				const alignText = Text.RIGHT;
				drawTextRelative( startX, y, `${strategy.label} ${strategy.cSide}`, barStats.color, null,
					alignText, null, null, `${strategy.label}_${strategy.cSide}_${barStats.periods.length}_text`);
			}
		}
	});
	
	// price bar color based on overall side
	strategiesDir = ctr === 0 ? 0 : strategiesDir / ctr;
	const color = strategiesDir > 0 && strategiesDir < 1 ? Color.yellow
			: strategiesDir < 0 && strategiesDir > -1 ? Color.orange
			: strategiesDir >= 1 ? Color.green
			: strategiesDir <= -1 ? Color.red
			: Color.white;
     setPriceBarColor(color);
}

// draw bars to show when a trade is initiated and completed
function DrawPriceBars() {
	if (engine.strategies.length > 1)
	{
		return;
	}
	const strategy = engine.strategies[0];
	const barStats = strategy.sideDefsMap[`${strategy.cSide}`];
	if (barStats) {
		setPriceBarColor(barStats.color);
	}
	else {
		setPriceBarColor(Color.white);
	}
}

function debugObj(obj, spaces = 2) {
	const seen = typeof WeakSet === "function" ? new WeakSet() : [];
	const has = (x) => seen instanceof WeakSet ? seen.has(x) : seen.indexOf(x) !== -1;
	const add = (x) => seen instanceof WeakSet ? seen.add(x) : seen.push(x);

	const replacer = (k, v) => {
		if (typeof v === "function") return `[Function ${v.name || "anonymous"}]`;
		if (v && typeof v === "object") {
			if (has(v)) return "[Circular]";
			add(v);
		}
		return v;
	};

	debugPrint(JSON.stringify(obj, replacer, spaces) + "\n");
}

/*************************************************************************************************/
/*****************  cStudyCollection - Collection of studies used in Strategies ******************/
/*************************************************************************************************/
class StudyCollection {
	constructor(params = null) {
		// map of all studies in this collection (actual Study_[name] objects)
		this.studyList = [];

		// study maps (each map contains values for all study tags i.e. {'MAC1': 32444, 'SEMC1: 23444, etc.})
		this.studies = [{}, {}, {}];

		const fastMALen = params.FastMALen ?? 4;
		//const slowMALen = params.SlowMALen ?? 4;
		const guideMALen = params.GuideMALen ?? 8;
		const guideStdDev = params.GuideStdDev ?? 0.6;
		//const adxLenConf = params.AdxLenConf ?? 6;
		//const adxTriggerConf = params.AdxTriggerConf ?? 15;

		const adxLen = params.AdxLen ?? 50;
		this.studyList.push(new Study_SMA(fastMALen, "ohlc4", "Price"));
		//this.studyList.push(new Study_SMA(slowMALen, "ohlc4", "Stop"));
		this.studyList.push(new Study_BB(guideMALen, guideStdDev, "ohlc4", "Guide", "1"));
		this.studyList.push(new Study_ADX(adxLen, adxLen,"Active"));
		//this.studyList.push(new Study_ADX(adxLenConf, adxLenConf,"Conf"));
		//this.studyList.push(new Study_ADXROC('Active', 10, 'Active_BIAS'));
		this.studyList.push(new Study_PriceShock(params.ShockATRLen ?? 14, 'Shock'));
		// for 5 min bar
		// 1 futures day      = 276 bars
		// 2 futures days     = 552 bars
		// 1 futures week     = 276 × 5 = 1380 bars
		// 4 futures week     = 276 × 5 x 4= 5520 bars
		//
		// using one futures day back:276 for weekly
		// using one week  back:1380 for monthly
		//this.studyList.push(new Study_SMASlope(fastMALen+6, fastMALen, 0.1, 'SLConf'));
		this.studyList.push(new Study_SMASlope(guideMALen, 5, 0.1, 'SLGuide'));

		//this.studyList.push(new Study_SMASlope(6, 2, 0.1, 'SLHHR'));
		//this.studyList.push(new Study_SMASlope(12, 5, 0.1, 'SLHR'));
		// this.studyList.push(new Study_SMASlope(24, 10, 0.1, 'SL2HR'));
		// this.studyList.push(new Study_SMASlope(276, 23, 0.1, 'SLDAY'));
		// this.studyList.push(new Study_SMASlope(1380, 276, 0.1, 'SLWK'));
		// this.studyList.push(new Study_SMASlope(5520, 1380, 0.1, 'SLMN'));
		//this.studyList.push(new Study_VolatilityRatio(5, 48, "VOL"));

		this.studyList.push(new Study_LADX('STrend', params));
	}
	
	//this function is called every bar (or current bar) to calculate/refresh current values
	Calc() {
		const barState = getBarState();
		if (barState === BARSTATE_NEWBAR)
		{
			// populate new barStudies
			this.studies.pop();
			this.studies.unshift({});
		} 
		this.studyList.forEach(study =>
		{
			if (barState === BARSTATE_NEWBAR) {
				if (study.studyArr) {
					study.studyArr.pop();
					study.studyArr.unshift({});
				}
			}
			if (study.studyArr) {
				study.studyArr[0] = JSON.parse(JSON.stringify(study.studyArr[1]));	// deep copy
			}
			study.Calc(this.studies)
		});
	}
}

/*************************************************************************************************/
/*****************************************  SEM  ALGO  *******************************************/
/*************************************************************************************************/
class Study_SEM {
	// can instantiate this class in the following ways
	// 		var study = new Study_SEM(13);
	//  or
	// 		var study = new Study_SEM(13, 'ohlc4');
	//  or
	// 		var study = new Study_SEM(13, 'ohlc4', 'L8');

	constructor(length, source = null, label = null) {
		this.label = label;
		this.period = length;
		this.sumX = 0;
		this.sumY = 0;
		this.sumXY = 0;
		this.sumXX = 0;
		this.initFlag = false;
		this.val = 0;
        this.pVal = 0;
        this.ppVal = 0;
		this.source = !source ? "ohlc4" : source;
		this.closeSeries = eval(`${this.source}()`);
		this.lastPrice = this.closeSeries.getValue(0);
    }

    Calc(studies = null) {
        this.ppVal = this.pVal;
        this.pVal = this.val;

		if (this.closeSeries.getValue(-(this.period-1)) != null) 
		{
			if (!this.initFlag)
			{
				this.CalcInitial();
			}
			else 
			{
				this.Update();
			}
		}
		// studies is not used in this function except to update the values in case label is passed
		if (this.label && studies) 
		{
			studies[0][this.Tag()] = this.val;
		}
	}
		
	CalcInitial() {
		this.sumX = 0;
		this.sumY = 0;
		this.sumXY = 0;
		this.sumXX = 0;
		for (let i = 0; i < this.period; i++) {
			const y = this.closeSeries.getValue(-i);
			const x = i + 1;
			
			this.sumX += x;
			this.sumY += y;
			this.sumXY += x * y;
			this.sumXX += x * x;
		}
		this.initFlag = true;

		const meanX = this.sumX / this.period;
		const meanY = this.sumY / this.period;

		const slope = (this.sumXY - this.sumX * meanY) / (this.sumXX - this.sumX * meanX);
		this.val = meanY - slope * meanX;  //intercept
		this.lastPrice = this.closeSeries.getValue(0);
	}
	
	Update() {
		const barState = getBarState();
	    if (barState === BARSTATE_NEWBAR)
		{
			const newPrice = this.closeSeries.getValue(0);
			const oldPrice = this.closeSeries.getValue(-this.period);

			this.sumXY = this.sumXY + this.sumY + (newPrice) - (oldPrice * (this.period + 1));
			this.sumY = this.sumY - oldPrice + newPrice;
		}
		else if (barState === BARSTATE_CURRENTBAR)
		{
			const newPrice = this.closeSeries.getValue(0);
			const oldPrice = this.lastPrice;

			this.sumXY = this.sumXY - oldPrice + newPrice;
			this.sumY = this.sumY - oldPrice + newPrice;
		}

		const meanX = this.sumX / this.period;
		const meanY = this.sumY / this.period;

		const slope = (this.sumXY - this.sumX * meanY) / (this.sumXX - this.sumX * meanX);
		this.val = meanY - slope * meanX;  //intercept
		this.lastPrice = this.closeSeries.getValue(0);
	}

	Tag() {
		return Study_SEM.Label(this.label);
	}
	
	static Label(label) {
		return `SEM${label}`;
	}
}

/*************************************************************************************************/
/****************************************** SMA Algo *********************************************/
/*************************************************************************************************/
class Study_SMA {
	// can instantiate this class in the following ways
	// 		var study = new Study_SMA(13);
	//  or
	// 		var study = new Study_SMA(13, 'ohlc4');
	//  or
	// 		var study = new Study_SMA(13, 'ohlc4', 'L8');
    constructor(length, source = null, label = null) {
		this.label = label;
		this.period = length;
		this.runningSum = 0;
		this.initFlag = false;
		this.val = 0;
        this.pVal = 0;
        this.ppVal = 0;
		this.source = !source ? "ohlc4" : source;
		this.closeSeries = eval(`${this.source}()`);
		this.lastPrice = this.closeSeries.getValue(0)
    }

    Calc(studies = null) {
        this.ppVal = this.pVal;
        this.pVal = this.val;

		if (this.closeSeries.getValue(-(this.period-1)) != null) 
		{
			if (!this.initFlag)
			{
				this.CalcInitial();
			}
			else 
			{
				this.Update();
			}
		}
		
		// studies is not used in this function except to update the values in case label is passed
		if (this.label && studies) 
		{
			studies[0][`${this.Tag()}`] = this.val;
		}
    }

	CalcInitial() {
		let sum = 0;
		for (let i = 0; i < this.period; i++) {
			sum += this.closeSeries.getValue(-i);
		}
		this.runningSum = sum;
		this.initFlag = true;
		this.val =  this.runningSum / this.period;
		this.lastPrice = this.closeSeries.getValue(0);
	}
	
	Update() {
		const barState = getBarState();
		if (barState === BARSTATE_NEWBAR)
		{
			const newPrice = this.closeSeries.getValue(0);
			const oldPrice = this.closeSeries.getValue(-this.period);
			this.runningSum = this.runningSum - oldPrice + newPrice;
			this.lastPrice = newPrice;
		} 
		else if (barState === BARSTATE_CURRENTBAR)
		{
			const newPrice = this.closeSeries.getValue(0);
			this.runningSum = this.runningSum - this.lastPrice + newPrice;
			this.lastPrice = newPrice;
		}
		this.val = this.runningSum / this.period;
	}
		
	Tag() {
		return Study_SMA.Label(this.label);
	}
	
	static Label(label) {
		return `MA${label}`;
	}
}

/*************************************************************************************************/
/****************************************** BB Algo  *********************************************/
/*************************************************************************************************/
class Study_BB {
	// can instantiate this class in the following ways
	// 		var study = new Study_BB(13, 1.0);
	//  or
	// 		var study = new Study_BB(13, 1.0, 'ohlc4');
	//  or
	// 		var study = new Study_BB(13, 1.0, 'ohlc4', 'L8', '1');
	constructor(length, stdDev, source = null, label = null, stdDevPrefix = null) {
		this.label = label;
		this.stdDevPrefix = stdDevPrefix;
		this.period = length;
		this.stdDev = stdDev;
		this.runningSum = 0;
		this.runningSumSquares = 0;
		this.initFlag = false;
		this.val = 0;
		this.ub = 0;
		this.lb = 0;
        this.prev_val = 0;
        this.prev_ub = 0;
        this.prev_lb = 0;
        this.pprev_val = 0;
        this.pprev_ub = 0;
        this.pprev_lb = 0;
		this.source = !source ? "ohlc4" : source;
		this.closeSeries = eval(`${this.source}()`);
		this.lastPrice = this.closeSeries.getValue(0);
    }

    Calc(studies = null) {
        this.pprev_val = this.prev_val;
        this.pprev_ub = this.prev_ub;
        this.pprev_lb = this.prev_lb;
        this.prev_val = this.val;
        this.prev_ub = this.ub;
        this.prev_lb = this.lb;

		if (this.closeSeries.getValue(-(this.period-1)) != null) 
		{
			if (!this.initFlag)
			{
				this.CalcInitial();
			}
			else
			{
				this.Update();
			}
		}
		
		// studies is not used in this function except to update the values in case label is passed
		if (this.label && studies) 
		{
			studies[0][this.Tag('UB')] = this.ub;
			studies[0][this.Tag('LB')] = this.lb;
			studies[0][this.Tag('MID')] = this.val;
		}
    }

	CalcInitial() {
		this.runningSum = 0;
		for (let i = 0; i < this.period; i++) {
			const cPrice = this.closeSeries.getValue(-i);
			this.runningSum += cPrice;
			this.runningSumSquares += cPrice * cPrice;
		}
		this.initFlag = true;
		const sma = this.runningSum / this.period;
		const variance = (this.runningSumSquares / this.period) - (sma * sma);
		const stdDevVal = Math.sqrt(variance);
		this.val = sma;
		this.ub = sma + (this.stdDev * stdDevVal);
		this.lb = sma - (this.stdDev * stdDevVal);
		this.lastPrice = this.closeSeries.getValue(0);
	}
	
	Update() {
		const barState = getBarState();
	    if (barState === BARSTATE_NEWBAR)
		{
			const newPrice = this.closeSeries.getValue(0);
			const oldPrice = this.closeSeries.getValue(-this.period);
			this.runningSum = this.runningSum - oldPrice + newPrice;
			this.runningSumSquares = this.runningSumSquares - (oldPrice * oldPrice) + (newPrice * newPrice);
			this.lastPrice = newPrice;
		}
		else if (barState === BARSTATE_CURRENTBAR)
		{
			const newPrice = this.closeSeries.getValue(0);
			this.runningSum = this.runningSum - this.lastPrice + newPrice;
			this.runningSumSquares = this.runningSumSquares - (this.lastPrice * this.lastPrice) + (newPrice * newPrice);
			this.lastPrice = newPrice;
		}
		const sma = this.runningSum / this.period;
		const variance = (this.runningSumSquares / this.period) - (sma * sma);
		const stdDevVal = Math.sqrt(variance);

		this.val = sma;
		this.ub = sma + (this.stdDev * stdDevVal);
		this.lb = sma - (this.stdDev * stdDevVal);
	}
	
	Tag(varName) {
		return Study_BB.Label(this.label, this.stdDevPrefix, varName);
	}
	
	static Label(label, stdDevPrefix, varName) {
		varName = varName.toUpperCase();
		const prefix = stdDevPrefix === '1' ? "" : stdDevPrefix;
		return `${varName}${prefix}${label}`;
	}
	// UB3C1
}

/*************************************************************************************************/
/***************************************** ADX Algo  *********************************************/
/*************************************************************************************************/
class Study_ADX {
	constructor(length, smoothing, label = null) {
		this.label = label;
		this.length = length;
		this.smoothing = smoothing;

		this.highSeries = high();
		this.lowSeries = low();
		this.closeSeries = close();

		// this.xATR = atr(8);

		this.initDMFlag = false;
		this.initADXFlag = false;

		this.trSum = 0;
		this.pDMSum = 0;
		this.nDMSum = 0;

		this.baseTrSum = 0;
		this.basePDMSum = 0;
		this.baseNDMSum = 0;

		this.dxQueue = [];
		this.lastADX = undefined;
		this.baseADX = undefined;

		this.liveTr = 0;
		this.livePDM = 0;
		this.liveNDM = 0;

		this.ADX = undefined;
		this.PDI = undefined;
		this.NDI = undefined;
		this.ATR = undefined;

		this.lastBarIndex = -1;
		this.lastRawTime = undefined;
		this.lastADXBarId = undefined;

		if (length <= 0 || smoothing <= 0) {
			throw "Invalid param";
		}
	}

	Calc(studies = null) {
		if (this.closeSeries.getValue(-1) == null) {
			this.ADX = undefined;
			this.PDI = undefined;
			this.NDI = undefined;
			this.WriteToStudy(studies);
			return;
		}

		if (!this.initDMFlag) {
			if (this.closeSeries.getValue(-this.length) == null) {
				this.ADX = undefined;
				this.PDI = undefined;
				this.NDI = undefined;
				this.ATR = undefined;
				this.WriteToStudy(studies);
				return;
			}

			this.CalcInitialDM();
			this.CalcADXFromCurrentState();
			// this.ATR = this.xATR.getValue(0);
			this.WriteToStudy(studies);
			return;
		}

		this.UpdateOptimized();
		this.CalcADXFromCurrentState();
		// this.ATR = this.xATR.getValue(0);
		this.WriteToStudy(studies);
	}

	CalcInitialDM() {
		this.trSum = 0;
		this.pDMSum = 0;
		this.nDMSum = 0;

		for (let i = 0; i < this.length; i++) {
			const vals = this.CalcBarOffsetValues(i);
			this.trSum += vals.tr;
			this.pDMSum += vals.pDM;
			this.nDMSum += vals.nDM;
		}

		this.baseTrSum = this.trSum;
		this.basePDMSum = this.pDMSum;
		this.baseNDMSum = this.nDMSum;

		this.initDMFlag = true;
		this.lastBarIndex = getCurrentBarIndex();
		this.lastRawTime = this.GetCurrentBarId();
	}

	UpdateOptimized() {
		const currentBarIndex = getCurrentBarIndex();
		const currentBarId = this.GetCurrentBarId();
		const isNewBar = currentBarId !== this.lastRawTime;

		if (isNewBar) {
			// Confirm prior bar into base state
			this.baseTrSum = this.trSum;
			this.basePDMSum = this.pDMSum;
			this.baseNDMSum = this.nDMSum;

			if (this.initADXFlag) {
				this.baseADX = this.lastADX;
			}

			this.lastBarIndex = currentBarIndex;
			this.lastRawTime = currentBarId;
		}

		const vals = this.CalcBarOffsetValues(0);
		this.liveTr = vals.tr;
		this.livePDM = vals.pDM;
		this.liveNDM = vals.nDM;

		this.trSum = this.baseTrSum - (this.baseTrSum / this.length) + this.liveTr;
		this.pDMSum = this.basePDMSum - (this.basePDMSum / this.length) + this.livePDM;
		this.nDMSum = this.baseNDMSum - (this.baseNDMSum / this.length) + this.liveNDM;
	}

	CalcADXFromCurrentState() {
		if (this.trSum <= 0) {
			this.ADX = undefined;
			this.PDI = undefined;
			this.NDI = undefined;
			return;
		}

		const pdi = 100 * (this.pDMSum / this.trSum);
		const ndi = 100 * (this.nDMSum / this.trSum);
		const diSum = pdi + ndi;

		let dx = 0;
		if (diSum > 0) {
			dx = 100 * Math.abs(pdi - ndi) / diSum;
		}

		this.PDI = pdi;
		this.NDI = ndi;

		if (!this.initADXFlag) {
			const currentBarId = this.GetCurrentBarId();
			if (this.dxQueue.length === 0 || currentBarId !== this.lastADXBarId) {
				this.dxQueue.unshift(dx);
				if (this.dxQueue.length > this.smoothing) {
					this.dxQueue.pop();
				}
				this.lastADXBarId = currentBarId;
			}
			else {
				this.dxQueue[0] = dx;
			}

			if (this.dxQueue.length < this.smoothing) {
				this.ADX = undefined;
				return;
			}

			let sum = 0;
			for (let i = 0; i < this.dxQueue.length; i++) {
				sum += this.dxQueue[i];
			}

			this.lastADX = sum / this.smoothing;
			this.baseADX = this.lastADX;
			this.ADX = this.lastADX;
			this.initADXFlag = true;
			return;
		}

		const seedADX = (this.baseADX == null) ? this.lastADX : this.baseADX;
		this.lastADX = ((seedADX * (this.smoothing - 1)) + dx) / this.smoothing;
		this.ADX = this.lastADX;
	}

	GetCurrentBarId() {
		const currentRawTime = rawtime(0);
		return currentRawTime == null ? getCurrentBarIndex() : currentRawTime;
	}

	CalcBarOffsetValues(i) {
		const currHigh = this.highSeries.getValue(-i);
		const currLow = this.lowSeries.getValue(-i);
		const currClose = this.closeSeries.getValue(-i);

		const prevHigh = this.highSeries.getValue(-(i + 1));
		const prevLow = this.lowSeries.getValue(-(i + 1));
		const prevClose = this.closeSeries.getValue(-(i + 1));

		return this.CalcDMTRValues(currHigh, currLow, currClose, prevHigh, prevLow, prevClose);
	}

	CalcDMTRValues(currHigh, currLow, currClose, prevHigh, prevLow, prevClose) {
		const upMove = currHigh - prevHigh;
		const downMove = prevLow - currLow;

		const pDM = (upMove > downMove && upMove > 0) ? upMove : 0;
		const nDM = (downMove > upMove && downMove > 0) ? downMove : 0;

		const range1 = currHigh - currLow;
		const range2 = Math.abs(currHigh - prevClose);
		const range3 = Math.abs(currLow - prevClose);
		const tr = Math.max(range1, Math.max(range2, range3));

		return { tr, pDM, nDM };
	}

	WriteToStudy(studies) {
		if (!this.label || !studies) {
			return;
		}

		studies[0][this.Tag("ADX")] = this.ADX;
		studies[0][this.Tag("PDI")] = this.PDI;
		studies[0][this.Tag("NDI")] = this.NDI;
		studies[0][this.Tag("BIAS")] = this.PDI - this.NDI;
	}

	Tag(varName) {
		return Study_ADX.Label(this.label, varName);
	}

	static Label(label, varName) {
		return `${label}_${varName}`;
	}
}

/*************************************************************************************************/
/******************************** ADX Rate of Change Strategy Study   ****************************/
/*************************************************************************************************/
class Study_ADXROC {
	constructor(label, numBars, studyLabel) {
		this.tag = label;
		this.studyLabel = studyLabel;

		this.slopeLength = numBars;   // number of bars to measure slope over
		this.adxHistory = [];         // newest ADX value at index 0

		this.ADX = undefined;
		this.ADX_ROC = undefined;
		this.ADX_ROC_PERCENT = undefined;
		this.ADX_SLOPE = undefined;

		this.lastSlopeBarIndex = undefined;
	}

	Calc(studies) {
		const cStudy = studies[0];
		if (cStudy == null) {
			return;
		}

		this.ADX = cStudy[this.studyLabel];

		if (this.ADX == null) {
			this.ADX_ROC = undefined;
			this.ADX_ROC_PERCENT = undefined;
			this.ADX_SLOPE = undefined;
			this.writeStudies(studies);
			return;
		}

		this.CalcADXROC();

		this.writeStudies(studies);
	}

	CalcADXROC() {
		const currentRawTime = rawtime(0);
		const currentBarId = currentRawTime == null ? getCurrentBarIndex() : currentRawTime;

		/*
			We only want one history slot per bar.

			If this is a new bar, add a new ADX value at the front.
			If this is the same live bar, update index 0 instead of adding again.
		*/
		if (this.lastSlopeBarIndex !== currentBarId) {
			this.adxHistory.unshift(this.ADX);

			if (this.adxHistory.length > this.slopeLength + 1) {
				this.adxHistory.pop();
			}

			this.lastSlopeBarIndex = currentBarId;
		} else {
			this.adxHistory[0] = this.ADX;
		}

		// Need at least current ADX and previous ADX
		if (this.adxHistory.length >= 2) {
			const prevADX = this.adxHistory[1];

			this.ADX_ROC = this.ADX - prevADX;

			if (prevADX !== 0) {
				this.ADX_ROC_PERCENT = 100 * (this.ADX - prevADX) / prevADX;
			} else {
				this.ADX_ROC_PERCENT = undefined;
			}
		} else {
			this.ADX_ROC = undefined;
			this.ADX_ROC_PERCENT = undefined;
		}

		// Need current ADX and ADX from N bars ago
		if (this.adxHistory.length > this.slopeLength) {
			const oldADX = this.adxHistory[this.slopeLength];

			this.ADX_SLOPE = (this.ADX - oldADX) / this.slopeLength;
		} else {
			this.ADX_SLOPE = undefined;
		}
	}

	writeStudies(studies) {
		const cStudy = studies[0];
		if (cStudy == null) return;

		cStudy[this.Tag('ADX_ROC')] = this.ADX_ROC;
		cStudy[this.Tag('ADX_ROC_PERCENT')] = this.ADX_ROC_PERCENT;
		cStudy[this.Tag('ADX_SLOPE')] = this.ADX_SLOPE;
	}

	Tag(varName) {
		return Study_ADXROC.Label(this.tag, varName);
	}

	static Label(tag, varName) {
		return `${tag}_${varName}`;
	}
}

/*************************************************************************************************/
/******************************** Price Shock / Velocity Study   *********************************/
/*************************************************************************************************/
class Study_PriceShock {
	constructor(atrLen = 14, label = null) {
		this.tag = label;
		this.xATR = atr(atrLen);

		this.ATR = undefined;
		this.DownPoints = 0;
		this.UpPoints = 0;
		this.RangePoints = 0;
		this.PullbackPoints = 0;
		this.DownATR = 0;
		this.UpATR = 0;
		this.RangeATR = 0;
		this.PullbackATR = 0;
		this.ShockScore = 0;
	}

	Calc(studies) {
		this.ATR = this.xATR.getValue(0);

		const hasPrices = cClose != null && pClose != null && cHigh != null && cLow != null;
		if (!hasPrices || this.ATR == null || this.ATR <= 0) {
			this.DownPoints = 0;
			this.UpPoints = 0;
			this.RangePoints = 0;
			this.PullbackPoints = 0;
			this.DownATR = 0;
			this.UpATR = 0;
			this.RangeATR = 0;
			this.PullbackATR = 0;
			this.ShockScore = 0;
			this.WriteToStudy(studies);
			return;
		}

		const closeDelta = cClose - pClose;
		this.DownPoints = Math.max(0, -closeDelta);
		this.UpPoints = Math.max(0, closeDelta);
		this.RangePoints = Math.max(0, cHigh - cLow);
		this.PullbackPoints = Math.max(0, cHigh - cClose);

		this.DownATR = this.DownPoints / this.ATR;
		this.UpATR = this.UpPoints / this.ATR;
		this.RangeATR = this.RangePoints / this.ATR;
		this.PullbackATR = this.PullbackPoints / this.ATR;
		this.ShockScore = Math.max(this.DownATR, this.PullbackATR);

		this.WriteToStudy(studies);
	}

	WriteToStudy(studies) {
		if (!this.tag || !studies || !studies[0]) return;

		studies[0][this.Tag('ATR')] = this.ATR;
		studies[0][this.Tag('DownPoints')] = this.DownPoints;
		studies[0][this.Tag('UpPoints')] = this.UpPoints;
		studies[0][this.Tag('RangePoints')] = this.RangePoints;
		studies[0][this.Tag('PullbackPoints')] = this.PullbackPoints;
		studies[0][this.Tag('DownATR')] = this.DownATR;
		studies[0][this.Tag('UpATR')] = this.UpATR;
		studies[0][this.Tag('RangeATR')] = this.RangeATR;
		studies[0][this.Tag('PullbackATR')] = this.PullbackATR;
		studies[0][this.Tag('Score')] = this.ShockScore;
	}

	Tag(varName) {
		return Study_PriceShock.Label(this.tag, varName);
	}

	static Label(tag, varName) {
		return `${tag}_${varName}`;
	}
}

/*************************************************************************************************/
/****************************************** ADXTrend Strategy Study   ****************************/
/*************************************************************************************************/
class Study_LADX {
	constructor(label, params) {
		params = params || {};

		this.tag = label;
		this.adxTag = 'Active';
		this.studyArr = [{side: 0}, {}, {}, {}];

		this.startBias = params.StartBias == null ? 1.0 : params.StartBias;
		this.stopBias = params.StopBias == null ? -0.75 : params.StopBias;
		this.reEntryCooldownBars = params.ReEntryCooldownBars == null ? 1 : Math.floor(params.ReEntryCooldownBars);
		this.trendReEntryShockMax = params.TrendReEntryShockMax == null ? 0.65 : params.TrendReEntryShockMax;
		this.shockDropATR = params.ShockDropATR == null ? 1.15 : params.ShockDropATR;
		this.shockArmedDropATR = params.ShockArmedDropATR == null ? 0.85 : params.ShockArmedDropATR;
		this.shockArmUpATR = params.ShockArmUpATR == null ? 0.75 : params.ShockArmUpATR;
		this.shockRangeATR = params.ShockRangeATR == null ? 1.8 : params.ShockRangeATR;

		this.chopMinGuideWidthATR = params.ChopMinGuideWidthATR == null ? 0.80 : params.ChopMinGuideWidthATR;
		this.chopMaxGuideSlopeATR = params.ChopMaxGuideSlopeATR == null ? 0.06 : params.ChopMaxGuideSlopeATR;

		this.adxVal = undefined;
		this.adxBias = undefined;
		this.adxSlope = undefined;
		this.priceMA = undefined;
		this.guideMA = undefined;
		this.guideLB = undefined;
		this.guideUB = undefined;
		this.shockScore = 0;
		this.shockDownATR = 0;
		this.shockUpATR = 0;
		this.shockRangeATRValue = 0;
		this.shockPullbackATR = 0;
		this.shockATR = undefined;
		this.guideWidthATR = 0;
		this.isChop = false;

		this.maxUpATR = 0;
		this.barsSinceExit = 1000000;
		this.lastSeenBarId = undefined;
		this.lastExitReason = "";
		this.stopReason = "";
	}

	get Side() {
		return this.studyArr[0].side == null ? 0 : this.studyArr[0].side;
	}
	set Side(val) {
		this.studyArr[0].side = val == null ? 0 : val;
	}
	get pSide() {
		return this.studyArr[1].side == null ? 0 : this.studyArr[1].side;
	}

	Calc(studies) {
		this.ResetSwitches(studies);

		if (this.adxVal == null || this.adxBias == null || this.priceMA == null
			|| this.guideMA == null || this.guideLB == null || this.guideUB == null) {
			this.writeStudies(studies);
			return;
		}

		if (this.Side === 0) {
			this.Side = this.TestStart(studies);
		}
		else {
			if (this.TestStop(studies)) {
				this.Side = 0;
			}
		}
		this.writeStudies(studies);
	}

	writeStudies(studies) {
		const cStudy = studies[0];
		if (cStudy == null) return;

		cStudy[this.Tag('Side')] = this.Side;
		cStudy[this.Tag('PriceMA')] = this.priceMA;
		cStudy[this.Tag('GuideMA')] = this.guideMA;
		cStudy[this.Tag('GuideUB')] = this.guideUB;
		cStudy[this.Tag('GuideLB')] = this.guideLB;
		cStudy[this.Tag('ADX')] = this.adxVal;
		cStudy[this.Tag('ADXBias')] = this.adxBias;
		cStudy[this.Tag('ADXSlope')] = this.adxSlope;
		cStudy[this.Tag('ADXTrigger')] = this.startBias;
		cStudy[this.Tag('StopBias')] = this.stopBias;
		cStudy[this.Tag('MaxUpATR')] = this.maxUpATR;
		cStudy[this.Tag('BarsSinceExit')] = this.barsSinceExit;
		cStudy[this.Tag('LastExitReason')] = this.lastExitReason;
		cStudy[this.Tag('ShockScore')] = this.shockScore;
		cStudy[this.Tag('ShockDownATR')] = this.shockDownATR;
		cStudy[this.Tag('ShockUpATR')] = this.shockUpATR;
		cStudy[this.Tag('ShockPullbackATR')] = this.shockPullbackATR;
		cStudy[this.Tag('ShockRangeATR')] = this.shockRangeATRValue;
		cStudy[this.Tag('GuideWidthATR')] = this.guideWidthATR;
		cStudy[this.Tag('Chop')] = this.isChop ? 1 : 0;
		cStudy[this.Tag('StopReason')] = this.stopReason;
	}

	ResetSwitches(studies) {
		const cStudy = studies[0];
		const pStudy = studies[1];
		if (cStudy == null || pStudy == null) return;

		if (this.pSide !== 0 && this.Side === 0) {
			this.maxUpATR = 0;
			this.barsSinceExit = 0;
			this.lastExitReason = this.stopReason;
		}

		this.adxVal = cStudy[Study_ADX.Label(this.adxTag, 'ADX')];
		const pdiVal = cStudy[Study_ADX.Label(this.adxTag, 'PDI')];
		const ndiVal = cStudy[Study_ADX.Label(this.adxTag, 'NDI')];
		this.adxBias = pdiVal == null || ndiVal == null ? undefined : pdiVal - ndiVal;

		this.priceMA = cStudy.MAPrice;
		this.guideMA = cStudy.MIDGuide;
		this.guideLB = cStudy.LBGuide;
		this.guideUB = cStudy.UBGuide;
		this.adxSlope = cStudy[`${this.adxTag}_ADX_SLOPE`];

		this.shockScore = cStudy[Study_PriceShock.Label('Shock', 'Score')] ?? 0;
		this.shockDownATR = cStudy[Study_PriceShock.Label('Shock', 'DownATR')] ?? 0;
		this.shockUpATR = cStudy[Study_PriceShock.Label('Shock', 'UpATR')] ?? 0;
		this.shockRangeATRValue = cStudy[Study_PriceShock.Label('Shock', 'RangeATR')] ?? 0;
		this.shockPullbackATR = cStudy[Study_PriceShock.Label('Shock', 'PullbackATR')] ?? 0;
		this.shockATR = cStudy[Study_PriceShock.Label('Shock', 'ATR')];

		const guideSlopePerBar = cStudy[Study_SMASlope.Label('SLGuide', 'MASlopeValue')];
		const guideWidth = this.guideUB != null && this.guideLB != null ? Math.max(0, this.guideUB - this.guideLB) : 0;
		const hasChopInputs = this.shockATR != null && this.shockATR > 0 && guideWidth > 0 && guideSlopePerBar != null;

		// Chop means the guide band is both narrow and nearly flat
		const guideWidthATR = hasChopInputs ? guideWidth / this.shockATR : 0;
		const guideSlopeATR = hasChopInputs ? Math.abs(guideSlopePerBar) / this.shockATR : 0;
		this.isChop =  this.chopMaxGuideSlopeATR > 0 && guideSlopeATR <= this.chopMaxGuideSlopeATR
					&& this.chopMinGuideWidthATR > 0 && guideWidthATR <= this.chopMinGuideWidthATR;

		if (this.Side !== 0) {
			this.stopReason = "";
			if (this.shockUpATR > this.maxUpATR) {
				this.maxUpATR = this.shockUpATR;
			}
		}

		// Count bars since last exit, but only if the bar has changed
		// This is used to enforce a re-entry cooldown period after an exit
		const cRawTime = rawtime(0);
		const currentBarId =  cRawTime == null ? getCurrentBarIndex() : cRawTime;
		if (this.lastSeenBarId === undefined) {
			this.lastSeenBarId = currentBarId;
		}
		else if (this.lastSeenBarId !== currentBarId) {
			if (this.barsSinceExit < 1000000) {
				this.barsSinceExit++;
			}
			this.lastSeenBarId = currentBarId;
		}

	}

	TestStart(studies) {
		const cStudy = studies[0];
		const pStudy = studies[1];
		const isReEntryCoolDownStillActive =
			this.reEntryCooldownBars > 0 && this.barsSinceExit < this.reEntryCooldownBars;

		if (isReEntryCoolDownStillActive || cStudy == null || pStudy == null) {
			return 0.0;
		}

		// Check if BIAS value is not deteriorating, get previous BIAS Value first
		const pdiVal = pStudy[Study_ADX.Label(this.adxTag, 'PDI')];
		const ndiVal = pStudy[Study_ADX.Label(this.adxTag, 'NDI')];
		const pAdxBias = pdiVal == null || ndiVal == null ? undefined : pdiVal - ndiVal;
		const biasNotDeteriorating = pAdxBias == null || this.adxBias >= pAdxBias;

		const continuationAboveGuide = this.priceMA >= this.guideMA && cClose >= this.guideMA;
		const shockCleared = this.shockScore <= this.trendReEntryShockMax;
		const pullbackEntry = this.priceMA < this.guideMA;
		const trendReEntry = continuationAboveGuide && shockCleared;

		// start only when
		// - the guide band is NEITHER  narrow NOR  flat  (!this.isChop)
		// - and the price is recovering (cStudy.MAPrice >= pStudy.MAPrice)
		// - and the ADX bias is above the start threshold
		// - and one of the entry conditions is met (pullback, trend re-entry, or continuation)
		const shouldStart = !this.isChop
			&& biasNotDeteriorating
			&& this.adxBias > this.startBias
			&& cStudy.MAPrice >= pStudy.MAPrice
			&& (
				pullbackEntry
				|| trendReEntry
			)
		if (shouldStart) {
			return trendReEntry ? 1.1 : pullbackEntry ? 1.2 : 0.0;
		}
		return 0.0;
	}

	TestStop(studies) {
		// maxUpATR: maximum observed upward move from the previous bar’s close while the trade is open.
		//			 this value stays "armed" until the trade is closed, and is used to determine if a
		//			 shock drop should trigger a stop. it is compared to the shockArmUpATR threshold.
		const shockArmed = this.maxUpATR >= this.shockArmUpATR;

		// rapidDrop: the shock score is above the shockDropATR threshold, which indicates a rapid drop in price.
		// 	 	      shockScore measures the current downward move or pullback, relative to ATR.
		// 	 	      It takes the larger of these two values:
		// 					DownATR     = Math.max(0, pClose - cClose) / ATR;
		// 					PullbackATR = Math.max(0, cHigh - cClose) / ATR;
		//				    shockScore = Math.max(DownATR, PullbackATR);
		//			  ATR is the average true range, which is a measure of volatility. A higher ATR indicates
		//			  more volatility, while a lower ATR indicates less volatility.
		const rapidDrop = this.shockScore >= this.shockDropATR;

		// armedDrop: the shock score is above the shockArmedDropATR threshold, which indicates a rapid drop
		// in price after the trade has been armed.
		const armedDrop = shockArmed && this.shockScore >= this.shockArmedDropATR;

		// shockRangeDrop: the shock range ATR value is above the shockRangeATR threshold, which indicates
		// a rapid drop in price within a range.
		const shockRangeDrop = this.shockRangeATRValue >= this.shockRangeATR && this.shockDownATR > 0;

		if (this.adxBias < this.stopBias) {
			this.stopReason = "ADX_BIAS";
			return true;
		}
		if (rapidDrop) {
			this.stopReason = "SHOCK_DROP";
			return true;
		}
		if (armedDrop) {
			this.stopReason = "ARMED_DROP";
			return true;
		}
		if (shockRangeDrop) {
			this.stopReason = "SHOCK_RANGE";
			return true;
		}
		return false;
	}

	Tag(varName) {
		return Study_LADX.Label(this.tag, varName);
	}
	static Label(tag, varName) {
		return `${tag}_LADX_${varName}`;
	}
}

/*************************************************************************************************/
/***************************************** SMA SLope Strategy Study   ****************************/
/*************************************************************************************************/
class Study_SMASlope {
	constructor(maLength, lookbackBars, flatPoints, label = null) {
		this.tag = label;
		this.xMA = sma(maLength);

		this.lookbackBars = lookbackBars;
		this.flatPoints = flatPoints;

		this.studyArr = [{
			MASlope: 0
		}, {}];
	}

	Calc(studies) {
		const barState = getBarState();

		if (barState === BARSTATE_NEWBAR) {
			this.studyArr[1].MASlope = this.studyArr[0].MASlope;
		}

		const cStudy = studies[0];

		const maNow = this.xMA.getValue(0);
		const maPast = this.xMA.getValue(-this.lookbackBars);

		if (maNow == null || maPast == null) {
			return;
		}

		const slope = maNow - maPast;
		const slopePerBar = slope / this.lookbackBars;

		let slopeFlag = 0;

		if (Math.abs(slopePerBar) <= this.flatPoints) {
			slopeFlag = 0;
		}
		else if (slopePerBar > 0) {
			slopeFlag = 1;
		}
		else {
			slopeFlag = -1;
		}

		this.studyArr[0].MASlope = slopeFlag;

		cStudy[`${this.Tag('MASlope')}`] = slopeFlag;
		cStudy[`${this.Tag('MA')}`] = maNow;
		cStudy[`${this.Tag('MASlopeValue')}`] = slopePerBar;
	}

	Tag(varName) {
		return Study_SMASlope.Label(this.tag, varName);
	}

	static Label(tag, varName) {
		return `${tag}_${varName}`;
	}
}

/***********************************************************************************************************
 ************************************  ORDER PROCESSING ENGINE AND STRATEGIES ******************************
 ***********************************************************************************************************/
// BASE Strategy Class
class Strategy {
	constructor(studyCollection, qty, qtyPct, aum, multiplier) {
		this.label = "";
		this.qty = qty;
		this.qtyPct = qtyPct;
		this.aum = aum;
		this.multiplier = multiplier;

		this.studyCollection = studyCollection;
        this.periods = this.studyCollection ? this.studyCollection.periods : null;

		// main Side for the strategy. 0 means no signal, != 0 means either long or short depending on the sign
        this.strategyVars = [{side: 0, stopPrice: 0}, {side: 0, stopPrice: 0}];

		// counter of number of bars this strategy was activeStrategies
		this.sideDefsList = [];
		this.sideDefsMap = {};

		this.barState = getBarState();
		this.vars = [];
		this.lastTrade = null;

		this.canReinvestAUM = false;
	}

	get cSide() {
        return this.strategyVars[0].side == null ? 0 : this.strategyVars[0].side;
	}

	set cSide(val) {
		this.strategyVars[0].side = val;
	}

	get pSide() {
        return this.strategyVars[1].side == null ? 0 : this.strategyVars[1].side;
	}

	set pSide(val) {
		this.strategyVars[1].side = val;
	}

	get cStopPrice() {
        return this.strategyVars[0].stopPrice == null ? 0 : this.strategyVars[0].stopPrice;
	}

	set cStopPrice(val) {
		this.strategyVars[0].stopPrice = val;
	}

	get pStopPrice() {
        return this.strategyVars[1].stopPrice == null ? 0 : this.strategyVars[1].stopPrice;
	}

	set pStopPrice(val) {
		this.strategyVars[1].stopPrice = val;
	}

	InitSideDefs(sideDefs) {
		// add initializations of stats and create a Map for easy access of the side
		this.sideDefsMap = {};
		this.sideDefsList = [];
		sideDefs.forEach(sideDef => {
			// add stats
			sideDef['barCounter'] = 0;
			sideDef['barFreq'] = 0;
			sideDef['periods'] = [];
			// add financial info
			sideDef['openPL'] = 0;
			sideDef['closePL'] = 0;
			sideDef['qty'] = 0;
			sideDef['tradeVal'] = 0;
			//
			this.sideDefsMap[`${sideDef.side}`] = sideDef;
			this.sideDefsList.push(sideDef);
		});
	}
	PreOperations(trackData) {
		this.lastTrade = null;
	}

	Calc(trade, trackData) {
		this.barState = getBarState();
		if (this.barState === BARSTATE_NEWBAR) {
			this.strategyVars.pop();
			this.strategyVars.unshift({side: 0, stopPrice: 0});
            this.vars.pop();
            this.vars.unshift({});
		}
        this.vars[0] = JSON.parse(JSON.stringify(this.vars[1]));	// deep copy
		this.strategyVars[0].side = this.strategyVars[1].side;
		this.strategyVars[0].stopPrice = this.strategyVars[1].stopPrice;
        this.ResetSwitches(trackData);
		if (this.cSide === 0) {
			this.CheckStartSignal(trackData);
		} else {
			this.CheckEndSignal(trade, trackData);
		}
		this.PostOp(trackData);
	}




	FinCalc(finData, isNewBar) {
		// 1- update all stats in all sides
		if (finData == null) {
			finData = {side: undefined, qtyDeal: 0, valueDeal: 0, fillPrice: 0, orderTag: ""}
		}
		this.sideDefsList.forEach(barStatObj => {
			if (barStatObj.side !== 'ALL') {
				this.UpdateSideCumulativeValues(barStatObj, finData.side, finData.qtyDeal, finData.valueDeal, finData.orderTag, isNewBar);
			}
		});
		// calc ALL
		const sideDef = {barCounter: 0, barFreq: 0, qty: 0, openPL: 0, closePL: 0};
		let sideDefAll = null;
		this.sideDefsList.forEach(barStatObj => {
			if (barStatObj.side !== 'ALL') {
				sideDef.barCounter += barStatObj.barCounter;
				sideDef.barFreq += barStatObj.barFreq;
				sideDef.openPL += barStatObj.openPL;
				sideDef.closePL += barStatObj.closePL;
				sideDef.qty += barStatObj.qty;
			} else {
				sideDefAll = barStatObj;
			}
		});
		if (sideDefAll) {
			sideDefAll.barCounter = sideDef.barCounter;
			sideDefAll.barFreq = sideDef.barFreq;
			sideDefAll.openPL = sideDef.openPL;
			sideDefAll.closePL = sideDef.closePL;
			sideDefAll.qty = sideDef.qty;
		}

		// this needs to be revisited for DrawBars logic
		if (isNewBar && this.cSide !== 0) {
			const barStatObj = this.sideDefsMap[`${this.cSide}`];
			if (barStatObj) {
				const openPrice = (finData.orderTag && finData.fillPrice && finData.orderTag === 'OPEN') ? finData.fillPrice : 0;
				const closePrice = (finData.orderTag && finData.fillPrice && finData.orderTag === 'CLOSE') ? finData.fillPrice : 0;
				barStatObj.barCounter++;
				if (this.pSide === 0) {
					barStatObj.barFreq++;
					barStatObj.periods.push({
						startBar: getCurrentBarIndex(),
						endBar: getCurrentBarIndex(),
						openPrice: openPrice,
						closePrice: closePrice,
						high: high(0),
						low: low(0)
					});
				} else {
					barStatObj.periods[barStatObj.periods.length - 1].endBar = getCurrentBarIndex();
					if (openPrice > 0) {
						barStatObj.periods[barStatObj.periods.length - 1].openPrice = openPrice;
					}
					if (closePrice > 0) {
						barStatObj.periods[barStatObj.periods.length - 1].closePrice = closePrice;
					}
				}
			}
		}
	}

	UpdateSideCumulativeValues(barStatObj, side, iQty, iTradeVal, orderTag, isNewBar) {
		const isClose = orderTag === 'CLOSE';
		const isThisSide = side && `${side}` === barStatObj.side;
		const qty = iQty != null && isThisSide ? iQty : 0;
		const tradeVal = iTradeVal != null && isThisSide ? iTradeVal : 0;
		
		// if (!isThisSide) {
			// return;
		// }
		const cClose = isNewBar ? close(-1) : close(0);
		if (isNewBar) {
			barStatObj.qty += qty;
			barStatObj.tradeVal += tradeVal;
			//barStatObj.closePL = barStatObj.qty === 0 && isSideChanged ? barStatObj.tradeVal : barStatObj.closePL;
			barStatObj.openPL = barStatObj.qty === 0 ? 0 : (cClose * this.multiplier * barStatObj.qty) + (barStatObj.tradeVal);
			barStatObj.closePL += isClose ? barStatObj.tradeVal : 0;
			barStatObj.tradeVal = isClose ? 0 : barStatObj.tradeVal;
		}
	}

	CheckStartSignal(trackData) {
		if (this.cSide === 0) {
			this.cSide = this.CanOpen(trackData);
			this.cSide = this.cSide == null ? 0 : this.cSide;
			if (this.cSide !== 0) {
				this.cStopPrice = 0;
			}
		}
		return this.cSide;
	}

	CheckEndSignal(trade, trackData) {
		this.cStopPrice = 0;
		if (this.cSide !== 0) {
			this.cStopPrice = this.CanClose(trade, trackData);
			this.cStopPrice = this.cStopPrice == null ? 0 : this.cStopPrice;
			if (this.cStopPrice > 0) {
				this.cSide = 0;
			}
		}
		return this.cStopPrice;
	}

	OpenTrade(trackData) {
		if (this.cSide !== 0) {
            // we are looking at the prices from previous bar since the signal (Side != 0) occurred in the previous bar
			const cHigh = high(-1);
			const cLow = low(-1);
			const cClose = close(-1);
			const cHLC = (cHigh + cLow + cClose) / 3.0;

            const cTrackData = trackData[1];

			let dealQty = this.qty > 0 ? this.qty : 0;
			//const maxQty = Math.ceil(this.aum / this.multiplier / cHLC);
			const cNnet = cTrackData.cumulativeData.dealClosePL + cTrackData.cumulativeData.dealOpenPL + cTrackData.cumulativeData.dealCommission;
			const aum = this.canReinvestAUM ? this.aum + cNnet : this.aum;
			
			if (dealQty === 0 && this.qtyPct > 0 && this.qtyPct <= 1.0) {
				//dealQty = Math.ceil(((this.aum /*+ cNnet*/) * this.qtyPct) / this.multiplier / cHLC);
				dealQty = Math.ceil((aum * this.qtyPct) / this.multiplier / cHLC);
			}

			if (dealQty !== 0) {
				const posSign = this.cSide > 0 ? 1 : -1;
				return {
					id: -1,
					rawTime: cTrackData.rawTime,
					strategy: this.label,
					side: this.cSide,
					orderTag: "OPEN",
					orderType: "MARKET",
					qty: posSign * dealQty,
					price: 0,
					tradeStatus: "NEW",
					fillQty: 0,
					fillPrice: 0,
					lastRawTime: cTrackData.rawTime,
				};
			}
		}

		return null;
	}

    LimitPrice(trade, _trackData) {
		// if Close price already determined, just update price
        if (trade && trade.price && trade.price > 0) {
			return close(-1);
		}
		return this.cStopPrice;
	}

	OnFilled(trade, trackData) {
		this.lastTrade = JSON.parse(JSON.stringify(trade));
		if (trade.orderTag === "OPEN") {
			return this.OnFilledOpen(trade, trackData);
		} else if (trade.orderTag === "CLOSE") {
			return this.OnFilledClose(trade, trackData);
		}
		return null;
	}

	OnFilledOpen(trade, trackData) {
		const newTrade = {...trade};
		newTrade.id = -1;
		newTrade.rawTime = trackData[0].rawTime;
		newTrade.orderTag = "CLOSE";
		newTrade.orderType = "LIMIT";
		newTrade.qty = -trade.qty;
		newTrade.price = 0;
		newTrade.tradeStatus = "NEW";
		newTrade.fillQty = 0;
		newTrade.fillPrice = 0;
		newTrade.lastRawTime = trackData[0].rawTime;
		return newTrade;
	}

	OnFilledClose(trade, trackData) {
		this.cSide = 0;
		return null;
	}

    // Override GetMonitorVar
    // ---------
	GetMonitorVar() {
		return {};
	}

    // OVERRIDE ResetSwitches
    // --------
    ResetSwitches(_trackData) {
        // to be implemented by the derived class
    }

    // OVERRIDE CanOpen
    // --------
    CanOpen(_trackData) {
        return 0;
    }

    // OVERRIDE CanClose
    // --------
    CanClose(_trade, _trackData) {
        return 0;
    }

    // OVERRIDE PostOp
    // --------
    PostOp(_trackData) {
        // to be implemented by the derived class
    }
}

// BASE Trade Engine Class
class TradeEngine {
    constructor(aum, multiplier, commission, dataFilename = "", compactMode = true, canReinvestAUM = false) {
		this.canReinvestAUM = canReinvestAUM;  // future use... this will allow AUM to be reinvested in the strategy
		this.compactMode = compactMode;
        this.activeTrades = [];
        this.completedTrades = [];
		this.strategies = [];
		this.dataFilename = dataFilename;
		this.cumulativeData = {
			dealQty : 0,
			dealVal : 0,
			dealCommission : 0,
			dealClosePL : 0,
			dealOpenPL : 0,
			prevDealQty : 0			
		};

		this.awayAdj = 0;
		this.aum = aum;
		this.multiplier = multiplier;
		this.commission = commission;
		this.trackData = [{}, {}];
		this.tradeId = 1;
		this.NAV = 100;
		this.NAVMax = 100;
		// this.NAVDrawDown = (this.NAV / this.NAVMax - 1) * 100 ;
		this.NAVDrawDown = (this.NAV / this.NAVMax) ;
		this.MaxDrawDown = 0;
		this.canResetNAV_Trade = false;
	
		this.NAV_Trade = this.NAV;
		this.NAV_TradeMax = 100;
		this.NAV_TradeDrawDown = 0;
		this.NAV_TradeMaxDrawDown = 0;

		this.net = 0;
		this.totalBars = 0;
		
		this.fileHandle = null
		this.WriteHeader();

		this.started = false;
		
		// data output params
		this.openTrade = null;
		this.lastCloseNet = null;
    }
	
	AddStrategy(strategyParam, studyCol) {
		if (strategyParam == null) return;

		// strategyParam example:  {name: "S100dot2", qty: 3, qtyPct: 0, enable: false}
		const enabled = strategyParam.enable == null ? true : strategyParam.enable;
		const qty = strategyParam.qty;
		const qtyPct = strategyParam.qtyPct;
		const arg1 = typeof strategyParam.arg1 === 'string' ? `'${strategyParam.arg1}'` 
						: typeof strategyParam.arg1 === 'number' ? `${strategyParam.arg1}` 
						: strategyParam.arg1;
		const arg2 = typeof strategyParam.arg2 === 'string' ? `'${strategyParam.arg2}'` 
						: typeof strategyParam.arg2 === 'number' ? `${strategyParam.arg2}` 
						: strategyParam.arg2;
						
		if (enabled) {
			// Create a new instance of the strategy that is Strategy[name] dynamically
			const execStr = `new Strategy${strategyParam.name}(studyCol, ${qty}, ${qtyPct}, ${this.aum}, ${this.multiplier}, ${arg1}, ${arg2})`;
			debugPrint(execStr + '\n');
			const strategy = eval(execStr);
			if (strategy) {
				strategy.canReinvestAUM = this.canReinvestAUM;
				debugPrint(`Strategy ${strategyParam.name} added\n`);
				this.strategies.push(strategy);
			}
		}
	}

	get IsStarted() {
		return this.started;
	}

	get IsCompactMode() {
		return this.compactMode;
	}

	ProcessOrder() {
		this.started = true;
		const barState = getBarState();

		this.strategies.forEach(strategy => {
			// PreOps
			strategy.PreOperations(this.trackData);
		});

		if (barState === BARSTATE_NEWBAR) {
			this.totalBars++;
			this.trackData.pop();
			this.trackData.unshift({
				  rawTime: rawtime(0)
				, qtyDeal: 0
				, valueDeal: 0
				, commissionDeal: 0
				, strategyFin: {}
				, cumulativeData: this.cumulativeData
			});

            if (!this.trackData[0].cumulativeData || !this.trackData[1].cumulativeData) {
                return;   // require two bars at least
            }
			// Handle any outstanding operation from previous bar before start
			// of new bar
			this.OpenTrades();
			this.HandleActiveTrades();
			this.UpdateActiveTrades();
			this.UpdateCumulativeValues(this.trackData[0].qtyDeal, this.trackData[0].valueDeal, this.trackData[0].commissionDeal);
			this.net = this.cumulativeData.dealClosePL + this.cumulativeData.dealOpenPL + this.cumulativeData.dealCommission;
			this.NAV = (this.aum + this.net) * 100.0 / this.aum;
			this.NAVMax = this.NAV > this.NAVMax ? this.NAV : this.NAVMax;
			// this.NAVDrawDown = (this.NAV / this.NAVMax - 1) * 100 ;
			// this.MaxDrawDown = this.NAVDrawDown < this.MaxDrawDown ? this.NAVDrawDown : this.MaxDrawDown;
			this.NAVDrawDown = this.canReinvestAUM ? (this.NAV / this.NAVMax - 1) * 100 : (this.NAV - this.NAVMax);
			this.MaxDrawDown = this.NAVDrawDown < this.MaxDrawDown ? this.NAVDrawDown : this.MaxDrawDown;
			if (this.canResetNAV_Trade) {
				this.NAV_Trade = this.NAV;
				this.NAV_TradeMax = this.NAV_Trade > this.NAV_TradeMax ? this.NAV_Trade : this.NAV_TradeMax;
				this.NAV_TradeDrawDown = this.canReinvestAUM ? (this.NAV_Trade / this.NAV_TradeMax - 1) * 100 : (this.NAV_Trade - this.NAV_TradeMax);
				this.NAV_TradeMaxDrawDown = this.NAV_TradeDrawDown < this.NAV_TradeMaxDrawDown ? this.NAV_TradeDrawDown : this.NAV_TradeMaxDrawDown;
				this.canResetNAV_Trade = false;
			}

			this.strategies.forEach(strategy => strategy.FinCalc(this.trackData[0].strategyFin[strategy.label], true));  // touch all strategies

			this.DumpData();
		}
		else {
			this.UpdateCumulativeValuesLive();
		}
		this.strategies.forEach(strategy => {
			// find the trade for this strategy from this.activeTrades just return the first trade
            const trade = this.activeTrades.find(t => t.strategy === strategy.label && t.orderTag === "CLOSE");
			strategy.Calc(trade, this.trackData);
		});  // touch all strategies
	}

	OpenTrades() {
		const activeStrategies = this.CompileActiveStrategies();
		this.strategies.forEach(strategy =>
		{
			const hasOrder = activeStrategies.find(s => s === strategy.label);
			if (!hasOrder)
			{
				const newOrder = strategy.OpenTrade(this.trackData);
				if (newOrder != null)
				{
					newOrder.id = this.tradeId++;
					this.activeTrades.push(newOrder);
					activeStrategies.push(newOrder.strategy);
				}
			}
		});
	}
	
	HandleActiveTrades() {
		const cClose = close(-1);

		for (let ndx = 0; ndx < this.activeTrades.length; ndx++)
		{
			const trade = this.activeTrades[ndx];
			const strategy = this.FindStrategy(trade.strategy);

			if (!strategy) {
				continue;
			}

			// update order price
			// -----------------------
			if (trade.tradeStatus === "NEW")
			{
				if (trade.orderType === "LIMIT" || trade.orderType === "STOP")
				{
					const oldPrice = trade.price;
					let price = strategy.LimitPrice(trade, this.trackData);
					price = price === null || price === 0 ? null : price;
					if (price != null && oldPrice !== price)
					{
						trade.price = price;
					}
				}
				else if (trade.orderType === "MARKET")
				{
					const oldPrice = trade.price;
					let price = trade.price === 0 ? null : trade.price;
					// follow market - adjust prices
					if (trade.qty < 0)  // short
					{
						const lowerPrice = cClose + this.awayAdj;
						price = price == null || price > lowerPrice ? lowerPrice : price;
					}
					else  // long
					{
						const upperPrice = cClose - this.awayAdj;
						price = price == null || price < upperPrice ? upperPrice : price;
					}
					if (price != null && oldPrice !== price)
					{
						trade.price = price;
					}
				}
			}
		}
	}
	
	UpdateActiveTrades() {
		const cLow = low(-1); // -1 means execuation immediatly on close(-1), 0 means execuationthe low(0) must be Below Close(-1) for long 
		const cHigh = high(-1);

		// Go through the completedTrades and update the TradeOrderRS records
		const tmpCompletedTrades = [];
		this.activeTrades.forEach(trade =>
		{
			if (trade.price != null && trade.price > 0)
			{
				// check if trade is filled
				if ((trade.orderType === "STOP" && trade.qty < 0 && trade.price >= cLow)
					|| (trade.orderType !== "STOP" && trade.qty < 0 && trade.price <= cHigh)
					|| (trade.orderType !== "STOP" && trade.qty > 0 && trade.price >= cLow)
					|| (trade.orderType === "STOP" && trade.qty > 0 && trade.price <= cHigh))
				{
					trade.fillPrice = trade.price;
					trade.fillQty = trade.qty;
					trade.tradeStatus = "FILLED";
					trade.lastRawTime = this.trackData[0].rawTime;

					const tradeVal = -1.0 * trade.fillPrice * trade.qty * this.multiplier;
					this.trackData[0].qtyDeal += trade.qty;
					this.trackData[0].valueDeal += tradeVal;
					this.trackData[0].commissionDeal += -Math.abs(trade.qty * this.commission);
					this.trackData[0].strategyFin[trade.strategy] = {
						qtyDeal: trade.qty, 
						valueDeal: tradeVal, 
						fillPrice: trade.fillPrice, 
						orderTag: trade.orderTag,
						side: trade.side
					}
					if (trade.orderTag === "CLOSE") {
						this.canResetNAV_Trade = true;
					}
					tmpCompletedTrades.push(trade);
				}
			}
		});
		tmpCompletedTrades.forEach(trade =>
		{
			const strategy = this.FindStrategy(trade.strategy);
			if (strategy != null)
			{
				const newOrder = strategy.OnFilled(trade, this.trackData);
				if (newOrder != null)
				{
					newOrder.id = this.tradeId++;
					this.activeTrades.push(newOrder);
				}
			}
			this.activeTrades = this.activeTrades.filter(t => t.id !== trade.id);
		});
		if (this.dataFilename !== "" && // there is no point of keeping completed trades if there is no need to create a data dump
			tmpCompletedTrades.length > 0) {
			this.completedTrades.push(...tmpCompletedTrades);
		}
	}

	CompileActiveStrategies() {
		const activeStrategies = [];
		this.activeTrades.forEach(trade =>
		{
			if (!activeStrategies.find(s => s === trade.strategy))
			{
				activeStrategies.push(trade.strategy);
			}
		});
		return activeStrategies;
	}

	FindStrategy(label) {
		return this.strategies.find(strategy => strategy.label === label);
	}
	
	UpdateCumulativeValues(qty, tradeVal, commission) {
		this.cumulativeData.dealQty += qty;
		this.cumulativeData.dealVal += tradeVal;
		this.cumulativeData.dealCommission += commission;
		this.cumulativeData.dealClosePL = this.cumulativeData.dealQty === 0 && this.cumulativeData.prevDealQty !== 0 ? this.cumulativeData.dealVal : this.cumulativeData.dealClosePL;
		this.cumulativeData.dealOpenPL = this.cumulativeData.dealQty === 0 ? 0 : (close(-1) * this.multiplier * this.cumulativeData.dealQty) + (this.cumulativeData.dealVal - this.cumulativeData.dealClosePL);
		this.cumulativeData.prevDealQty = this.cumulativeData.dealQty;
	}
	UpdateCumulativeValuesLive() {
		this.cumulativeData.dealOpenPL = this.cumulativeData.dealQty === 0 ? 0 : (close(0) * this.multiplier * this.cumulativeData.dealQty) + (this.cumulativeData.dealVal - this.cumulativeData.dealClosePL);
		this.net = this.cumulativeData.dealClosePL + this.cumulativeData.dealOpenPL + this.cumulativeData.dealCommission;
		this.NAV = (this.aum + this.net) * 100.0 / this.aum;
		this.NAVMax = this.NAV > this.NAVMax ? this.NAV : this.NAVMax;
		// this.NAVDrawDown = (this.NAV / this.NAVMax - 1) * 100 ;
		// this.MaxDrawDown = this.NAVDrawDown < this.MaxDrawDown ? this.NAVDrawDown : this.MaxDrawDown;
		this.NAVDrawDown = this.canReinvestAUM ? (this.NAV / this.NAVMax - 1) * 100 : (this.NAV - this.NAVMax);
		this.MaxDrawDown = this.NAVDrawDown < this.MaxDrawDown ? this.NAVDrawDown : this.MaxDrawDown;

	}

	GetAllStrategyParams() {
		let paramMap = {};
		this.strategies.forEach(strategy => paramMap = Object.assign({}, paramMap, strategy.GetMonitorVar()));
		return paramMap;
	}
	
	GetAllStrategySides() {
		let sideMap = {};
		this.strategies.forEach(strategy => {
			let statColor = strategy.sideDefsList[`${strategy.cSide}`] ? strategy.sideDefsList[`${strategy.cSide}`].color : Color.white;
			statColor = statColor ? statColor : Color.white;
            sideMap[`${strategy.label}_Side`] = {value: strategy.cSide, color: statColor, isText: true};
		});
		return sideMap;
	}

	GetAllStrategyStats() {
        const statMap = {TotalBars: {value: this.totalBars, isText: true}};
		const totalBars = this.totalBars === 0 ? 1 : this.totalBars;
		this.strategies.forEach(strategy => {
			strategy.sideDefsList.forEach(sideDef => {
				const enabled = sideDef.enable != null ? sideDef.enable : true;
				if (enabled) {
					let sideTag = `_${sideDef.side}`;
					if (sideDef.side === 'ALL') {
						sideTag = '';
					}
					if (this.IsCompactMode) {
                        statMap[`${strategy.label}${sideTag}_bar_Frq`] = {value: `${sideDef.barCounter}_${sideDef.barFreq}`, color: sideDef.color, isText: true};
                        statMap[`${strategy.label}${sideTag}_ClosePL`] = {value: sideDef.closePL.toFixed(2), color: sideDef.color, isText: true};
					}
					else {
                        statMap[`${strategy.label}${sideTag}_Bars`] = {value: sideDef.barCounter, color: sideDef.color, isText: true};
                        statMap[`${strategy.label}${sideTag}_Perc`] = {value: sideDef.barCounter / totalBars, color: sideDef.color, isText: true};
                        statMap[`${strategy.label}${sideTag}_Freq`] = {value: sideDef.barFreq, color: sideDef.color, isText: true};
                        statMap[`${strategy.label}${sideTag}_Qty`] = {value: sideDef.qty, color: sideDef.color, isText: true};
                        statMap[`${strategy.label}${sideTag}_OpenPL`] = {value: sideDef.openPL.toFixed(2), color: sideDef.color, isText: true};
                        statMap[`${strategy.label}${sideTag}_ClosePL`] = {value: sideDef.closePL.toFixed(2), color: sideDef.color, isText: true};
					}
				}
				else if (!enabled && sideDef.side === 'ALL') {
                    statMap[`${strategy.label}_Bars`] = {value: sideDef.barCounter, color: sideDef.color, isText: true};
				}
			});
		});
		return statMap;
	}

	GetAllStrategyTradeData() {
		// find the trade for this strategy from this.activeTrades just return the first trade
		const tradeMap = {};
		this.strategies.forEach(strategy => {
			if (strategy.lastTrade) {
                tradeMap[`${strategy.label}_TradePrice`] = {value: strategy.lastTrade.price.toFixed(2), color: Color.white, isText: true};
			}
			else {
                tradeMap[`${strategy.label}_TradePrice`] = {value: 0, color: Color.white, isText: true};
			}
		});
		return tradeMap;
	}

	GetCumulativeData() {
		const finColor = Color.RGB(255, 133, 133);
		if (this.IsCompactMode) {
			return {
                NetPOS: {value: this.cumulativeData.dealQty, color: finColor, isText: true},
                NetClosePL: {value: this.cumulativeData.dealClosePL.toFixed(2), color: finColor, isText: true},
                NetOpenPL: {value: this.cumulativeData.dealOpenPL.toFixed(2), color: finColor, isText: true},
                Net: {value: this.net.toFixed(2), color: finColor, isText: true},
                NAV: {value: this.NAV.toFixed(2), color: finColor, isText: true},
                NAVMax: {value: this.NAVMax.toFixed(2), color: finColor, isText: true},
                NAVDrawDownPts: {value: this.NAVDrawDown.toFixed(2), color: finColor, isText: true},
                MaxDrawDownPts: {value: this.MaxDrawDown.toFixed(2), color: finColor, isText: true},
				NAV_Trade: {value: this.NAV_Trade.toFixed(2), color: finColor, isText: true},
				NAV_TradeMax: {value: this.NAV_TradeMax.toFixed(2), color: finColor, isText: true},
				NAV_TradeDrawDownPts: {value: this.NAV_TradeDrawDown.toFixed(2), color: finColor, isText: true},
				NAV_TradeMaxDrawDownPts: {value: this.NAV_TradeMaxDrawDown.toFixed(2), color: finColor, isText: true}
			};
		}
		return {
            NetPOS: {value: this.cumulativeData.dealQty, color: finColor, isText: true},
            NetValue: {value: this.cumulativeData.dealVal.toFixed(2), color: finColor, isText: true},
            NetCommission: {value: this.cumulativeData.dealCommission.toFixed(2), color: finColor, isText: true},
            NetClosePL: {value: this.cumulativeData.dealClosePL.toFixed(2), color: finColor, isText: true},
            NetOpenPL: {value: this.cumulativeData.dealOpenPL.toFixed(2), color: finColor, isText: true},
            Net: {value: this.net.toFixed(2), color: finColor, isText: true},
            NAV: {value: this.NAV.toFixed(2), color: finColor, isText: true},
			NAVMax: {value: this.NAVMax.toFixed(2), color: finColor, isText: true},
			NAVDrawDownPts: {value: this.NAVDrawDown.toFixed(2), color: finColor, isText: true},
			MaxDrawDownPts: {value: this.MaxDrawDown.toFixed(2), color: finColor, isText: true},
			NAV_Trade: {value: this.NAV_Trade.toFixed(2), color: finColor, isText: true},
			NAV_TradeMax: {value: this.NAV_TradeMax.toFixed(2), color: finColor, isText: true},
			NAV_TradeDrawDownPts: {value: this.NAV_TradeDrawDown.toFixed(2), color: finColor, isText: true},
			NAV_TradeMaxDrawDownPts: {value: this.NAV_TradeMaxDrawDown.toFixed(2), color: finColor, isText: true}
		};
	}
	
	GetAllOutputData() {
		const acumData = this.GetCumulativeData();
		const sideData = this.GetAllStrategySides();
		const statsData = this.GetAllStrategyStats();
		const tradeData = this.GetAllStrategyTradeData();
		const paramsData = this.GetAllStrategyParams();
		return Object.assign({}, acumData, sideData, statsData, tradeData, paramsData);
	}
	
	WriteHeader() {
		if (this.dataFilename !== "") {
			this.fileHandle = new File(`${this.dataFilename}`);
			if (this.fileHandle) {
				this.fileHandle.open( "wt" );
				this.fileHandle.writeln("OpenDate,OpenTime,OpenPrice,CloseDate,CloseTime,ClosePrice,Side,Qty,Time in Deal, Trade P&L, Total P&L, NAV, NAVMax, NAVDrawDownPts, MaxDrawDownPts, NAV_Trade, NAV_TradeMax, NAV_TradeDrawDownPts, NAV_TradeMaxDrawDownPts ");
				this.fileHandle.flush();
			}
		}
	}

	// this.fileHandle.writeln("OpenDate,OpenTime,Strategy,Side,OrderTag,OrderType,Qty,Price,Total P&L, NAV");
		// orderTrade.id
		// id: -1,
		// rawTime: cTrackData.rawTime,
		// strategy: this.label,
		// side: this.cSide,
		// orderTag: "OPEN",
		// orderType: "MARKET",
		// qty: posSign * dealQty,
		// price: 0,
		// tradeStatus: "NEW",
		// fillQty: 0,
		// fillPrice: 0,
		// lastRawTime: cTrackData.rawTime,
	DumpData() {
		if (this.completedTrades.length > 0 && this.dataFilename !== "" && this.fileHandle) {
			// compile CSV line of trades


			for (let ndx = 0; ndx < this.completedTrades.length; ndx++) {
				const trade = this.completedTrades[ndx];
				if (trade.orderTag === 'OPEN') {
					this.openTrade = trade;
				}
				else {
					const closeTrade = trade;
					// I already have openTrade, and trade (closeTrade)
					const openTime = TradeEngine.ConvertEpochToLocal(this.openTrade.lastRawTime);
					const closeTime = TradeEngine.ConvertEpochToLocal(closeTrade.lastRawTime);
					const dTime = (closeTrade.lastRawTime - this.openTrade.lastRawTime)/60;
					const tradePL = this.net - (this.lastCloseNet == null ? 0 : this.lastCloseNet);
					const line = `${openTime},${this.openTrade.fillPrice},${closeTime},${closeTrade.fillPrice},${this.openTrade.side},${this.openTrade.fillQty},${dTime},${tradePL},${this.net},${this.NAV},${this.NAVMax},${this.NAVDrawDown},${this.MaxDrawDown},${this.NAV_Trade},${this.NAV_TradeMax},${this.NAV_TradeDrawDown},${this.NAV_TradeMaxDrawDown}`;
					this.fileHandle.writeln(line);
					this.fileHandle.flush();
					this.lastCloseNet = this.net;
					// this.lastCloseWinning = this.net;
					// this.lastCloseLosing = this.net;
				}	
			}
			// this.completedTrades.forEach(trade => {
				// if (trade.orderTag === 'OPEN') {
					// openTrade = trade;
				// }
				// else {
					// // I already have openTrade, and trade (closeTrade)
					// const openTime = TradeEngine.ConvertEpochToLocal(openTrade.lastRawTime);
					// const closeTime = TradeEngine.ConvertEpochToLocal(trade.lastRawTime);
					// let line = `${openTime},${openTrade.fillPrice},${trade.side},${trade.orderTag},${trade.orderType},${trade.fillQty},${trade.fillPrice}`;
					// line += `,${this.net},${this.NAV}`;
					// this.fileHandle.writeln(line);
					// this.fileHandle.flush();
				// }	
			// });
			this.completedTrades = [];
		}
		
	}
	
	CloseDataFile() {
		if (this.dataFilename !== "" && this.fileHandle) {
			this.fileHandle.close();
		}
	}

	static ConvertEpochToLocal(epochTime) {
		const date = new Date(epochTime * 1000); // Convert to milliseconds
		return date.toLocaleString();  // Returns local time (adjusted for DST)
	}	
	
}


/***********************************************************************************************************
 ****************************************************  STRATEGIES ******************************************
 ***********************************************************************************************************/

class StrategyLADX extends Strategy {
	constructor(studyCollection, qty, qtyPct, aum, multiplier, tag) {
		super(studyCollection, qty, qtyPct, aum, multiplier);
		this.label = StrategyLADX.Label();
		this.tag = tag;

		this.colors = {
			lightPink: Color.RGB(255, 182, 193),
			hotPink: Color.RGB(255, 105, 180),
			forestGreen: Color.RGB(34, 139, 34),
			darkYellow: Color.RGB(190, 190, 0),
			lavender: Color.RGB(238, 130, 238),
			lavender2: Color.RGB(230, 100, 250),
			lightGreen: Color.RGB(144, 238, 144),
			neutralGray: Color.RGB(155,155,155),
			midnightBlue: Color.RGB(25, 25, 65),
			GR1: Color.RGB(128, 128, 128),
			GR2: Color.RGB(96, 160, 96),
			GR3: Color.RGB(64, 192, 64),
			GR4: Color.RGB(32, 224, 32),
			GR5: Color.RGB(0, 255, 0)
		}

		// Initialize the sides in this strategy
		const sideDefs = [
			{ side: "ALL", color: Color.RGB(255, 100, 0), enable: true}  // cumulative stats
			//,{ side: "1", color: this.colors.hotPink, enable: true}
			,{ side: "1.1", color: this.colors.forestGreen, enable: true}
			,{ side: "1.2", color: this.colors.darkYellow, enable: true}
			,{ side: "1.3", color: this.colors.lightGreen, enable: true}
		];
		this.InitSideDefs(sideDefs);

		this.vars = [{}, {}];

	}

	// Override the method canOpen from Strategy. This is called every tick to check if a trade can be opened
	// This method returns the Side (this.cSide will be updated). Side != 0 indicates that there is signal
	CanOpen(trackData) {
		const cStudy = this.studyCollection.studies[0];
		if (!cStudy) {
			return 0;
		}

		if (this.pSide !== 0) return 0;

		return cStudy[`${this.tag}_LADX_Side`] ?? 0;
	}

	// Override the method canClose from Strategy
	CanClose(trade, trackData) {
		// if Close price already determined, just update price
		if( trade && trade.price &&  trade.price > 0)
		{
			return cClose;
		}

		const cStudy = this.studyCollection.studies[0];
		const side = cStudy[`${this.tag}_LADX_Side`] ?? 0;

		if (side !== 0 && this.cSide !== side) return cClose;
		return side === 0 ? cClose : 0;
	}

	static Label() {
		return "LADX";
	}


	GetMonitorVar() {
		const cStudy = this.studyCollection.studies[0];
		const levels = this.studyCollection.levelsGrid;
		const sTag = `${this.tag}_LADX`;
		const studiesToMonitor = [
			{label: `${sTag}_Side`, color: this.colors.darkYellow, isText: true},
			{label: `${sTag}_ADX`, color: this.colors.darkYellow, isText: true},
			{label: `${sTag}_ADXBias`, color: this.colors.darkYellow, isText: true},
			{label: `${sTag}_ADXConf`, color: this.colors.darkYellow, isText: true},
			{label: `${sTag}_ADXConfBias`, color: this.colors.darkYellow, isText: true},
			{label: `${sTag}_ADXSlope`, color: this.colors.darkYellow, isText: true},
			{label: `${sTag}_PriceMA`, color: this.colors.lightGreen, plotType: PLOTTYPE_LINE, isText: false},
			{label: `${sTag}_GuideMA`, color: this.colors.lavender, plotType: PLOTTYPE_LINE, isText: false},
			{label: `${sTag}_GuideUB`, color: this.colors.lavender, plotType: PLOTTYPE_LINE, plotStyle: PS_DOT, lineThickness: 2, isText: false},
			{label: `${sTag}_GuideLB`, color: this.colors.lavender, plotType: PLOTTYPE_LINE, plotStyle: PS_DOT, lineThickness: 2, isText: false},
			{label: `${sTag}_ADXTrigger`, color: this.colors.lavender, plotType: PLOTTYPE_LINE, plotStyle: PS_DOT, lineThickness: 2, isText: false},
			{label: `${sTag}_StopBias`, color: this.colors.hotPink, isText: true},
			{label: `${sTag}_WeakBars`, color: this.colors.darkYellow, isText: true},
			{label: `${sTag}_MaxBias`, color: this.colors.darkYellow, isText: true},
			{label: `${sTag}_MaxUpATR`, color: this.colors.darkYellow, isText: true},
			{label: `${sTag}_BarsSinceExit`, color: this.colors.darkYellow, isText: true},
			{label: `${sTag}_LastExitReason`, color: this.colors.hotPink, isText: true},
			{label: `${sTag}_ShockScore`, color: this.colors.hotPink, isText: true},
			{label: `${sTag}_ShockDownATR`, color: this.colors.hotPink, isText: true},
			{label: `${sTag}_ShockUpATR`, color: this.colors.lightGreen, isText: true},
			{label: `${sTag}_ShockPullbackATR`, color: this.colors.hotPink, isText: true},
			{label: `${sTag}_ShockRangeATR`, color: this.colors.hotPink, isText: true},
			{label: `${sTag}_GuideWidthATR`, color: this.colors.lavender, isText: true},
			{label: `${sTag}_Chop`, color: this.colors.hotPink, isText: true},
			{label: `${sTag}_StopReason`, color: this.colors.hotPink, isText: true}
		];
		// const gridToMonitor = [];
		// if (levels && levels.length > 0) {
		// 	levels.forEach((level, ndx) => {
		// 		gridToMonitor.push({label: `${level}`, color: this.colors.GR1, plotType: PLOTTYPE_LINE, plotStyle: PS_DOT, lineThickness: 2, isText: false})
		// 	});
		// }
		// concat gridToMonitor and studiesToMonitor into toMonitor array, gridToMonitor goes first (top of the array)
		const toMonitor = [
			// ...gridToMonitor,
			...studiesToMonitor
		];
		const varsToMonitor = {};
		varsToMonitor['.'] = {value: '', isText: true};  // <----- Empty line
		varsToMonitor['----ADXTREND vars -----'] = {value: '----------------', isText: true};
		const defColor = this.colors.darkYellow;
		for (let ndx = 0; ndx < toMonitor.length; ndx++) {
			const item = toMonitor[ndx];
			const isText = item.isText ?? false;
			const color = item.color ?? defColor;
			const plotType = item.plotType ?? PLOTTYPE_LINE;
			const plotStyle = item.plotStyle ?? PS_SOLID;
			const lineThickness = item.lineThickness ?? 1;
			const val = item.value ?? cStudy[item.label];
			varsToMonitor[item.label] = {value: val, isText: isText, color: color,
				plotType: plotType, plotStyle: plotStyle, lineThickness: lineThickness};
		}
		varsToMonitor['--------------------------------------'] = {value: '----------------', isText: true};
		return varsToMonitor;
	}
}
