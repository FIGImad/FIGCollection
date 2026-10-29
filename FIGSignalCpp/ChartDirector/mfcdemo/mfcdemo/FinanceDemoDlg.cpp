// CFinanceDemoDlg.cpp : implementation file
//

#include "stdafx.h"
#include "resource.h"
#include "FinanceDemoDlg.h"
#include "chartdir.h"
#include "FinanceChart.h"
#include "ChartViewer.h"
#include <math.h>
#include <sstream>
#include <vector>
#include <algorithm>

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

//
// Constructor
//
CFinanceDemoDlg::CFinanceDemoDlg(CWnd* pParent /*=0*/)
    : CDialog(IDD_FINANCEDEMO, pParent)
{
    m_avgPeriod1 = m_avgPeriod2 = 0;
}

//
// Destructor
//
CFinanceDemoDlg::~CFinanceDemoDlg()
{
	delete m_ChartViewer.getChart();
}

void CFinanceDemoDlg::DoDataExchange(CDataExchange* pDX)
{
    CDialog::DoDataExchange(pDX);
    DDX_Control(pDX, IDC_PointerPB, m_PointerPB);
	DDX_Control(pDX, IDC_TICKERSYMBOL, m_TickerSymbol);
	DDX_Control(pDX, IDC_COMPAREWITH, m_CompareWith);
    DDX_Control(pDX, IDC_TIMERANGE, m_TimeRange);
    DDX_Control(pDX, IDC_VOLUME, m_Volume);
	DDX_Control(pDX, IDC_PABABOLICSAR, m_ParabolicSAR);
    DDX_Control(pDX, IDC_LOGSCALE, m_LogScale);
	DDX_Control(pDX, IDC_PERCENTAGESCALE, m_PercentageScale);
    DDX_Control(pDX, IDC_CHARTTYPE, m_ChartType);
    DDX_Control(pDX, IDC_BAND, m_Band);
    DDX_Control(pDX, IDC_AVGTYPE1, m_AvgType1);
    DDX_Control(pDX, IDC_AVGTYPE2, m_AvgType2);
    DDX_Control(pDX, IDC_MOVAVG1, m_MovAvg1);
    DDX_Control(pDX, IDC_MOVAVG2, m_MovAvg2);
    DDX_Control(pDX, IDC_INDICATOR1, m_Indicator1);
    DDX_Control(pDX, IDC_INDICATOR2, m_Indicator2);
    DDX_Control(pDX, IDC_ChartViewer, m_ChartViewer);
}

BEGIN_MESSAGE_MAP(CFinanceDemoDlg, CDialog)
    ON_BN_CLICKED(IDC_PointerPB, OnPointerPB)
    ON_BN_CLICKED(IDC_ZoomInPB, OnZoomInPB)
    ON_BN_CLICKED(IDC_ZoomOutPB, OnZoomOutPB)
	ON_EN_KILLFOCUS(IDC_TICKERSYMBOL, OnTextChange)
	ON_EN_KILLFOCUS(IDC_COMPAREWITH, OnTextChange)
    ON_CBN_SELCHANGE(IDC_TIMERANGE, OnTimeRangeChange)
    ON_BN_CLICKED(IDC_VOLUME, OnSelectChange)
	ON_BN_CLICKED(IDC_PABABOLICSAR, OnSelectChange)
 	ON_BN_CLICKED(IDC_PERCENTAGESCALE, OnSelectChange)
    ON_BN_CLICKED(IDC_LOGSCALE, OnSelectChange)
    ON_CBN_SELCHANGE(IDC_CHARTTYPE, OnSelectChange)
    ON_CBN_SELCHANGE(IDC_BAND, OnSelectChange)
    ON_CBN_SELCHANGE(IDC_AVGTYPE1, OnSelectChange)
    ON_CBN_SELCHANGE(IDC_AVGTYPE2, OnSelectChange)
    ON_EN_KILLFOCUS(IDC_MOVAVG1, OnTextChange)
    ON_EN_KILLFOCUS(IDC_MOVAVG2, OnTextChange)
    ON_CBN_SELCHANGE(IDC_INDICATOR1, OnSelectChange)
    ON_CBN_SELCHANGE(IDC_INDICATOR2, OnSelectChange)
	ON_CONTROL(CVN_MouseMovePlotArea, IDC_ChartViewer, OnMouseMovePlotArea)
	ON_CONTROL(CVN_ViewPortChanged, IDC_ChartViewer, OnViewPortChanged)
END_MESSAGE_MAP()

//
// Contents of the CFinanceDemoDlg combo boxes
//

static const TCHAR* timeRanges[] =
{
    _T("30"), _T("1 month"),
    _T("60"), _T("2 months"),
    _T("90"), _T("3 months"),
    _T("180"), _T("6 months"),
    _T("360"), _T("1 year"),
    _T("720"), _T("2 years"),
    _T("1080"), _T("3 years"),
    _T("1440"), _T("4 years"),
    _T("1800"), _T("5 years"),
    _T("3600"), _T("10 years")
};
static int timeRangeCount = (int)(sizeof(timeRanges) / sizeof(*timeRanges));

static const TCHAR *chartTypes[] =
{
    _T("None"), _T("None"),
    _T("CandleStick"), _T("CandleStick"),
    _T("Close"), _T("Closing Price"),
    _T("Median"), _T("Median Price"),
    _T("OHLC"), _T("OHLC"),
    _T("TP"), _T("Typical Price"),
    _T("WC"), _T("Weighted Close")
};
static int chartTypeCount = (int)(sizeof(chartTypes) / sizeof(*chartTypes));

static const TCHAR *bandTypes[] =
{
    _T("None"), _T("None"),
    _T("BB"), _T("Bollinger Band"),
    _T("DC"), _T("Donchain Channel"),
    _T("Envelop"), _T("Envelop (SMA 20 +/- 10%)")
};
static int bandTypeCount = (int)(sizeof(bandTypes) / sizeof(*bandTypes));

static const TCHAR *avgTypes[] =
{
    _T("None"), _T("None"),
    _T("SMA"), _T("Simple"),
    _T("EMA"), _T("Exponential"),
    _T("TMA"), _T("Triangular"),
    _T("WMA"), _T("Weighted")
};
static int avgTypeCount = (int)(sizeof(avgTypes) / sizeof(*avgTypes));

static const TCHAR *indicatorTypes[] =
{
    _T("None"), _T("None"),
    _T("AccDist"), _T("Accumulation/Distribution"),
    _T("AroonOsc"), _T("Aroon Oscillator"),
    _T("Aroon"), _T("Aroon Up/Down"),
    _T("ADX"), _T("Avg Directional Index"),
    _T("ATR"), _T("Avg True Range"),
    _T("BBW"), _T("Bollinger Band Width"),
    _T("CMF"), _T("Chaikin Money Flow"),
    _T("COscillator"), _T("Chaikin Oscillator"),
    _T("CVolatility"), _T("Chaikin Volatility"),
    _T("CLV"), _T("Close Location Value"),
    _T("CCI"), _T("Commodity Channel Index"),
    _T("DPO"), _T("Detrended Price Osc"),
    _T("DCW"), _T("Donchian Channel Width"),
    _T("EMV"), _T("Ease of Movement"),
    _T("FStoch"), _T("Fast Stochastic"),
    _T("MACD"), _T("MACD"),
    _T("MDX"), _T("Mass Index"),
    _T("Momentum"), _T("Momentum"),
    _T("MFI"), _T("Money Flow Index"),
    _T("NVI"), _T("Neg Volume Index"),
    _T("OBV"), _T("On Balance Volume"),
    _T("Performance"), _T("Performance"),
    _T("PPO"), _T("% Price Oscillator"),
    _T("PVO"), _T("% Volume Oscillator"),
    _T("PVI"), _T("Pos Volume Index"),
    _T("PVT"), _T("Price Volume Trend"),
    _T("ROC"), _T("Rate of Change"),
    _T("RSI"), _T("RSI"),
    _T("SStoch"), _T("Slow Stochastic"),
    _T("StochRSI"), _T("StochRSI"),
    _T("TRIX"), _T("TRIX"),
    _T("UO"), _T("Ultimate Oscillator"),
    _T("Vol"), _T("Volume"),
    _T("WilliamR"), _T("William's %R")
};
static int indicatorTypeCount = (int)(sizeof(indicatorTypes) / sizeof(*indicatorTypes));

//
// Helper utility to initialize a combo box from an array of text
//
static void initComboBox(CComboBox &b, const TCHAR *list[], int count, const TCHAR *initial)
{
    b.Clear();
    for (int i = 0; i < count; i += 2)
        //The odd indexes are the display text, the even indexes are the keys
        b.SetItemDataPtr(b.AddString(list[i + 1]), (void *)(list[i]));
    b.SelectString(0, initial);
}

/////////////////////////////////////////////////////////////////////////////
// CFinanceDemoDlg event handlers

//
// Initialization
//
BOOL CFinanceDemoDlg::OnInitDialog()
{
    CDialog::OnInitDialog();
  
    // Load icons to mouse usage buttons
    loadButtonIcon(IDC_PointerPB, IDI_Scroll, 20, 20);
    loadButtonIcon(IDC_ZoomInPB, IDI_ZoomIn, 20, 20);
    loadButtonIcon(IDC_ZoomOutPB, IDI_ZoomOut, 20, 20);
    
    //
    // Intialialize the controls
    //
    initComboBox(m_TimeRange, timeRanges, timeRangeCount, _T("3 months"));
    initComboBox(m_ChartType, chartTypes, chartTypeCount, _T("CandleStick"));
    initComboBox(m_Band, bandTypes, bandTypeCount, _T("Bollinger Band"));
    initComboBox(m_AvgType1, avgTypes, avgTypeCount, _T("Simple"));
    initComboBox(m_AvgType2, avgTypes, avgTypeCount, _T("Simple"));
    initComboBox(m_Indicator1, indicatorTypes, indicatorTypeCount, _T("RSI"));
    initComboBox(m_Indicator2, indicatorTypes, indicatorTypeCount, _T("MACD"));

    m_Volume.SetCheck(1);
    m_MovAvg1.SetWindowText(_T("5"));
    m_MovAvg2.SetWindowText(_T("20"));

    // Enable mouse wheel zooming by setting the zoom ratio to 1.1 per wheel event
    m_ChartViewer.setMouseWheelZoomRatio(1.1);

    // Initially set the mouse to drag to scroll mode.
    m_PointerPB.SetCheck(1);
    m_ChartViewer.setMouseUsage(Chart::MouseUsageScroll);

    // Set the ticker symbol and emulate an OnTextChange event to draw the chart
    m_TickerSymbol.SetWindowText(_T("ASE"));
    OnTextChange();

    return TRUE;
}

//
// User clicks on the Pointer pushbutton
//
void CFinanceDemoDlg::OnPointerPB()
{
    m_ChartViewer.setMouseUsage(Chart::MouseUsageScroll);
}

//
// User clicks on the Zoom In pushbutton
//
void CFinanceDemoDlg::OnZoomInPB()
{
    m_ChartViewer.setMouseUsage(Chart::MouseUsageZoomIn);
}

//
// User clicks on the Zoom Out pushbutton
//
void CFinanceDemoDlg::OnZoomOutPB()
{
    m_ChartViewer.setMouseUsage(Chart::MouseUsageZoomOut);
}

//
// User selection has changed - update the chart
//
void CFinanceDemoDlg::OnSelectChange() 
{
    drawChart(&m_ChartViewer);  
}

//
// User has entered text for the moving average period or the ticker/compare key
//
void CFinanceDemoDlg::OnTextChange() 
{
    CString avgText;
    m_MovAvg1.GetWindowText(avgText);
    int new_avgPeriod1 = (int)_tcstol(avgText, 0, 0);
    
    m_MovAvg2.GetWindowText(avgText);
    int new_avgPeriod2 = (int)_tcstol(avgText, 0, 0);

    CString tickerKey;
    m_TickerSymbol.GetWindowText(tickerKey);
    if (tickerKey == _T(""))
    {
        // Default tickerKey
        tickerKey = _T("ASE");
        m_TickerSymbol.SetWindowText(tickerKey);
    }

	CString compareKey;
	m_CompareWith.GetWindowText(compareKey);

	// If the ticker key or compare key are changed, we need to reload the data
    bool needReloadData = (m_tickerKey != tickerKey) || (m_compareKey != compareKey);
	if (needReloadData)
		loadData(tickerKey, compareKey);

    // If anyything is changed, we need to redraw the chart.
    if (needReloadData || (new_avgPeriod1 != m_avgPeriod1) || (new_avgPeriod2 != m_avgPeriod2))
		OnSelectChange();
}

//
// User has change the Time Period
// - Update the viewport to display the chosen period 
//
void CFinanceDemoDlg::OnTimeRangeChange()
{
    int durationInDays = (int)_tcstol((const TCHAR*)m_TimeRange.GetItemDataPtr(
        m_TimeRange.GetCurSel()), 0, 0);
    m_ChartViewer.setViewPortWidth(durationInDays / (double)m_dailyPrice.timeStamps.size());
    m_ChartViewer.setViewPortLeft(1 - m_ChartViewer.getViewPortWidth());
    m_ChartViewer.updateViewPort(true, true);
}

//
// User hit ENTER key. Some text field may have changed.
//
void CFinanceDemoDlg::OnOK()
{
    OnTextChange();
}

/////////////////////////////////////////////////////////////////////////////
// Data source

void CFinanceDemoDlg::loadData(const CString& ticker, const CString& compare)
{
    // Reload main data if ticker key has changed
    if (m_tickerKey != ticker)
    {
        m_tickerKey = ticker;

        // Simulator to generate realistic random OHLC values
        FinanceSimulator db(TCHARtoUTF8(ticker), Chart::chartTime(2010, 1, 1),
            Chart::chartTime(2020, 12, 31), 86400);

        // Copy daily data to the data arrays
        m_dailyPrice.timeStamps = arrayToVector(db.getTimeStamps());
        m_dailyPrice.highData = arrayToVector(db.getHighData());
        m_dailyPrice.lowData = arrayToVector(db.getLowData());
        m_dailyPrice.openData = arrayToVector(db.getOpenData());
        m_dailyPrice.closeData = arrayToVector(db.getCloseData());
        m_dailyPrice.volData = arrayToVector(db.getVolData());

        // Set up the viewport to display the duration selected by the user
        m_ChartViewer.setFullRange("x", 0, (int)m_dailyPrice.timeStamps.size() - 1);
        int durationInDays = (int)_tcstol((const TCHAR*)m_TimeRange.GetItemDataPtr(
            m_TimeRange.GetCurSel()), 0, 0);
        m_ChartViewer.setViewPortWidth(durationInDays / (double)m_dailyPrice.timeStamps.size());
        m_ChartViewer.setViewPortLeft(1 - m_ChartViewer.getViewPortWidth());
    }
    
    // Reload compare data if compare key has changed
    if (m_compareKey != compare)
    {   
        m_compareKey = compare;
        if (m_compareKey == _T(""))
            m_dailyPrice.compareData.clear();
        else
        {
            // Simulator to generate realistic random OHLC values
            FinanceSimulator db2(TCHARtoUTF8(compare), Chart::chartTime(2010, 1, 1),
                Chart::chartTime(2020, 12, 31), 86400);
            m_dailyPrice.compareData = arrayToVector(db2.getCloseData());
        }
	}

    // In this example, we will compute the weekly and monthly prices on demand. We just need to
    // clear the old data here so that they will be re-computed if necessary.
    m_weeklyPrice.timeStamps.clear();
    m_monthlyPrice.timeStamps.clear();
}

//
// Aggregate the data into weekly or monthly data depending on the aggregator
//
void CFinanceDemoDlg::aggregateData(const ArrayMath& aggregator, PriceData& p)
{
    int size = aggregator.aggregate(vectorToArray(p.timeStamps), Chart::AggregateFirst).len;
    p.timeStamps.resize(size);
    size = aggregator.aggregate(vectorToArray(p.highData), Chart::AggregateMax).len;
    p.highData.resize(size);
    size = aggregator.aggregate(vectorToArray(p.lowData), Chart::AggregateMin).len;
    p.lowData.resize(size);
    size = aggregator.aggregate(vectorToArray(p.openData), Chart::AggregateFirst).len;
    p.openData.resize(size);
    size = aggregator.aggregate(vectorToArray(p.closeData), Chart::AggregateLast).len;
    p.closeData.resize(size);
    size = aggregator.aggregate(vectorToArray(p.volData), Chart::AggregateSum).len;
    p.volData.resize(size);
    size = aggregator.aggregate(vectorToArray(p.compareData), Chart::AggregateLast).len;
    p.compareData.resize(size);
}

/////////////////////////////////////////////////////////////////////////////
// Chart Creation

//
// Add a moving average line to the FinanceChart object.
//
static LineLayer* addMovingAvg(FinanceChart *m, CString avgType, int avgPeriod, int color)
{
    if (avgPeriod > 1)
    {
        if (avgType == _T("SMA"))
            return m->addSimpleMovingAvg(avgPeriod, color);
        else if (avgType == _T("EMA"))
            return m->addExpMovingAvg(avgPeriod, color);
        else if (avgType == _T("TMA"))
            return m->addTriMovingAvg(avgPeriod, color);
        else if (avgType == _T("WMA"))
            return m->addWeightedMovingAvg(avgPeriod, color);
    }

	return 0;
}

//
// Add an indicator chart to the FinanceChart object. In this demo example, the indicator
//
static XYChart* addIndicator(FinanceChart *m, CString indicator, int height)
{
    if (indicator == _T("RSI"))
        return m->addRSI(height, 14, 0x800080, 20, 0xff6666, 0x6666ff);
    else if (indicator == _T("StochRSI"))
        return m->addStochRSI(height, 14, 0x800080, 30, 0xff6666, 0x6666ff);
    else if (indicator == _T("MACD"))
        return m->addMACD(height, 26, 12, 9, 0xff, 0xff00ff, 0x8000);
    else if (indicator == _T("FStoch"))
        return m->addFastStochastic(height, 14, 3, 0x6060, 0x606000);
    else if (indicator == _T("SStoch"))
        return m->addSlowStochastic(height, 14, 3, 0x6060, 0x606000);
    else if (indicator == _T("ATR"))
        return m->addATR(height, 14, 0x808080, 0xff);
    else if (indicator == _T("ADX"))
        return m->addADX(height, 14, 0x8000, 0x800000, 0x80);
    else if (indicator == _T("DCW"))
        return m->addDonchianWidth(height, 20, 0xff);
    else if (indicator == _T("BBW"))
        return m->addBollingerWidth(height, 20, 2, 0xff);
    else if (indicator == _T("DPO"))
        return m->addDPO(height, 20, 0xff);
    else if (indicator == _T("PVT"))
        return m->addPVT(height, 0xff);
    else if (indicator == _T("Momentum"))
        return m->addMomentum(height, 12, 0xff);
    else if (indicator == _T("Performance"))
        return m->addPerformance(height, 0xff);
    else if (indicator == _T("ROC"))
        return m->addROC(height, 12, 0xff);
    else if (indicator == _T("OBV"))
        return m->addOBV(height, 0xff);
    else if (indicator == _T("AccDist"))
        return m->addAccDist(height, 0xff);
    else if (indicator == _T("CLV"))
        return m->addCLV(height, 0xff);
    else if (indicator == _T("WilliamR"))
        return m->addWilliamR(height, 14, 0x800080, 30, 0xff6666, 0x6666ff);
    else if (indicator == _T("Aroon"))
        return m->addAroon(height, 14, 0x339933, 0x333399);
    else if (indicator == _T("AroonOsc"))
        return m->addAroonOsc(height, 14, 0xff);
    else if (indicator == _T("CCI"))
        return m->addCCI(height, 20, 0x800080, 100, 0xff6666, 0x6666ff);
    else if (indicator == _T("EMV"))
        return m->addEaseOfMovement(height, 9, 0x6060, 0x606000);
    else if (indicator == _T("MDX"))
        return m->addMassIndex(height, 0x800080, 0xff6666, 0x6666ff);
    else if (indicator == _T("CVolatility"))
        return m->addChaikinVolatility(height, 10, 10, 0xff);
    else if (indicator == _T("COscillator"))
        return m->addChaikinOscillator(height, 0xff);
    else if (indicator == _T("CMF"))
        return m->addChaikinMoneyFlow(height, 21, 0x8000);
    else if (indicator == _T("NVI"))
        return m->addNVI(height, 255, 0xff, 0x883333);
    else if (indicator == _T("PVI"))
        return m->addPVI(height, 255, 0xff, 0x883333);
    else if (indicator == _T("MFI"))
        return m->addMFI(height, 14, 0x800080, 30, 0xff6666, 0x6666ff);
    else if (indicator == _T("PVO"))
        return m->addPVO(height, 26, 12, 9, 0xff, 0xff00ff, 0x8000);
    else if (indicator == _T("PPO"))
        return m->addPPO(height, 26, 12, 9, 0xff, 0xff00ff, 0x8000);
    else if (indicator == _T("UO"))
        return m->addUltimateOscillator(height, 7, 14, 28, 0x800080, 20, 0xff6666, 0x6666ff);
    else if (indicator == _T("Vol"))
        return m->addVolIndicator(height, 0x99ff99, 0xff9999, 0xc0c0c0);
    else if (indicator == _T("TRIX"))
        return m->addTRIX(height, 12, 0xff);
	else
		return 0;
}

//
// Creates a dummy chart to show an error message.
//
static void errMsg(CChartViewer* viewer, const char *msg)
{
     MultiChart m(400, 200);
     m.addTitle(Chart::Center, msg, "Arial", 10)->setMaxWidth(m.getWidth());
     viewer->setChart(&m);
}

//
// Draw the chart according to user selection
//
void CFinanceDemoDlg::drawChart(CChartViewer* viewer)
{
    // The first moving average period selected by the user, bound to [0, 300].
    CString avgText;
    m_MovAvg1.GetWindowText(avgText);
    m_avgPeriod1 = (std::max)(0, (std::min)(300, (int)_tcstol(avgText, 0, 0)));

    // The second moving average period selected by the user, bound to [0, 300].
    m_MovAvg2.GetWindowText(avgText);
    m_avgPeriod2 = (std::max)(0, (std::min)(300, (int)_tcstol(avgText, 0, 0)));

    // We need extra leading data points to compute moving averages and technical indicators.
    // It should be at least 25 to support common indicators.
    int extraPoints = (std::max)(25, (std::max)(m_avgPeriod1, m_avgPeriod2));

    // Get the start and end index visible on the viewport
    int startIndex = (int)floor(viewer->getValueAtViewPort("x", viewer->getViewPortLeft()));
    int endIndex = (int)ceil(viewer->getValueAtViewPort("x", viewer->getViewPortRight()));

    // Choose either daily, weekly or monthly prices depending on the date range
    PriceData* p = &m_dailyPrice;
    
    // If more than 400 trading days, use weekly or monthly prices
    if (endIndex - startIndex >= 400)
    {
        if (endIndex - startIndex < 2000)
        {
            // Between 400 and 2000 trading days, use weekly prices
            p = &m_weeklyPrice;
            if (p->timeStamps.empty())
            {
                // If no weekly prices, compute them from daily prices.
                ArrayMath a(vectorToArray(m_dailyPrice.timeStamps));
                aggregateData(a.selectStartOfWeek(), *p = m_dailyPrice);
            }
        }
        else
        {
            // More than 2000 trading days, use monthly prices
            p = &m_monthlyPrice;
            if (p->timeStamps.empty())
            {
                // If no monthly prices, compute them from daily prices.
                ArrayMath a(vectorToArray(m_dailyPrice.timeStamps));
                aggregateData(a.selectStartOfMonth(), *p = m_dailyPrice);
            }
        }

        // Re-compute the index based on the weekly/monthly prices
        DoubleArray ts = vectorToArray(p->timeStamps);
        startIndex = (int)floor(Chart::bSearch(ts, m_dailyPrice.timeStamps[startIndex]));
        endIndex = (int)ceil(Chart::bSearch(ts, m_dailyPrice.timeStamps[endIndex]));
    }

    //
    // At this stage, we have determined the price data to use. We need to obtain the part of the 
    // arrays that are within the visible x-axis range.
    //

    // Add extra points before the startIndex, as moving averages and may technical indicators 
    // requires data before the first visible point. 
    extraPoints = (std::min)(extraPoints, startIndex);
    startIndex -= extraPoints;
    int duration = endIndex - startIndex + 1;

    // Obtain the visible part of the data plus the extra points
    DoubleArray timeStamps = vectorToArray(p->timeStamps, startIndex, duration);
    DoubleArray highData = vectorToArray(p->highData, startIndex, duration);
    DoubleArray lowData = vectorToArray(p->lowData, startIndex, duration);
    DoubleArray openData = vectorToArray(p->openData, startIndex, duration);
    DoubleArray closeData = vectorToArray(p->closeData, startIndex, duration);
    DoubleArray volData = vectorToArray(p->volData, startIndex, duration);
    DoubleArray compareData = vectorToArray(p->compareData, startIndex, duration);

    //
    // Now, we have obtain the data. We can plot the chart.
    //
    int width = 780;
    int mainHeight = 260;
    int indicatorHeight = 80;

    // Create the chart object using the selected size
    FinanceChart *m = new FinanceChart(width);

	// Hide default legend box as we are using dynamic legend
	m->setLegendStyle("normal", 8, Chart::Transparent, Chart::Transparent);

    // Set the data into the chart object
    m->setData(timeStamps, highData, lowData, openData, closeData, volData, extraPoints);

    // Put the company name at the bottom left of the FinanceChart title region
    CString companyName;
    m_TickerSymbol.GetWindowText(companyName);
	m->addPlotAreaTitle(Chart::BottomLeft, TCHARtoUTF8(companyName));

    // A copyright message at the bottom right of the FinanceChart title region
	m->addPlotAreaTitle(Chart::BottomRight,
        "<*font=Arial,size=8*>(c) Advanced Software Engineering");

    // Add the main chart
	m->addMainChart(mainHeight);

    // Set log or linear scale according to user preference
	m->setLogScale(m_LogScale.GetCheck() != 0);

	// Set axis labels to show data values or percentage change to user preference
	if (m_PercentageScale.GetCheck())
		m->setPercentageAxis();

    // Draw the main chart depending on the chart type the user has selected
    CString selectedType = (const TCHAR *)m_ChartType.GetItemDataPtr(m_ChartType.GetCurSel());
    if (selectedType == _T("Close"))
		m->addCloseLine(0x000040);
    else if (selectedType == _T("TP"))
		m->addTypicalPrice(0x000040);
    else if (selectedType == _T("WC"))
		m->addWeightedClose(0x000040);
    else if (selectedType == _T("Median"))
		m->addMedianPrice(0x000040);

    // Add comparison line if there is data for comparison
	if (compareData.len > 0) 
        m->addComparison(compareData, 0x0000ff, TCHARtoUTF8(m_compareKey));
	
    // Add moving average lines.
    addMovingAvg(m, (const TCHAR *)m_AvgType1.GetItemDataPtr(m_AvgType1.GetCurSel()), 
        m_avgPeriod1, 0x663300);
    addMovingAvg(m, (const TCHAR *)m_AvgType2.GetItemDataPtr(m_AvgType2.GetCurSel()), 
        m_avgPeriod2, 0x9900ff);

    // Draw the main chart if the user has selected CandleStick or OHLC. We draw after the 
    // moving average lines to make sure it is behind the moving average lines.
    if (selectedType == _T("CandleStick"))
		m->addCandleStick(0x33ff33, 0xff3333);
    else if (selectedType == _T("OHLC"))
		m->addHLOC(0x8000, 0x800000);

	// Add parabolic SAR if necessary
	if (m_ParabolicSAR.GetCheck())
		m->addParabolicSAR(0.02, 0.02, 0.2, Chart::DiamondShape, 5, 0x008800, 0x000000);

    // Add price band/channel/envelop to the chart according to user selection
    CString selectedBand = (const TCHAR *)m_Band.GetItemDataPtr(m_Band.GetCurSel());
    if (selectedBand == _T("BB"))
		m->addBollingerBand(20, 2, 0x9999ff, 0xc06666ff);
    else if (selectedBand == _T("DC"))
		m->addDonchianChannel(20, 0x9999ff, 0xc06666ff);
    else if (selectedBand == _T("Envelop"))
		m->addEnvelop(20, 0.1, 0x9999ff, 0xc06666ff);

    // Add volume bars to the main chart if necessary
    if (m_Volume.GetCheck())
		m->addVolBars(indicatorHeight, 0x99ff99, 0xff9999, 0xc0c0c0);

    // Add indicators as according to user selection.
    addIndicator(m, (const TCHAR*)m_Indicator1.GetItemDataPtr(m_Indicator1.GetCurSel()),
        indicatorHeight);
    addIndicator(m, (const TCHAR *)m_Indicator2.GetItemDataPtr(m_Indicator2.GetCurSel()),
        indicatorHeight);

	// We may need to update the track line. If the mouse is moving on the chart, the track line 
    // will be updated in MouseMovePlotArea. Otherwise, we need to update the track line here.
	if (!viewer->isInMouseMoveEvent())
	{
		trackFinance(m, (0 == viewer->getChart()) ? 
			((XYChart*)m->getChart(0))->getPlotArea()->getRightX() : viewer->getPlotAreaMouseX());
	}

    // Set the chart to the viewer
	delete viewer->getChart();
    viewer->setChart(m);
}

//
// Draw track cursor when mouse is moving over plotarea
//
void CFinanceDemoDlg::OnMouseMovePlotArea()
{
	trackFinance((MultiChart *)m_ChartViewer.getChart(), m_ChartViewer.getPlotAreaMouseX());
	m_ChartViewer.updateDisplay();
}

//
// Draw finance chart track line with legend
//
void CFinanceDemoDlg::trackFinance(MultiChart *m, int mouseX)
{
	// Clear the current dynamic layer and get the DrawArea object to draw on it.
	DrawArea *d = m->initDynamicLayer();

	// It is possible for a FinanceChart to be empty, so we need to check for it.
	if (m->getChartCount() == 0)
		return;

	// Get the data x-value that is nearest to the mouse
	int xValue = (int)(((XYChart *)m->getChart(0))->getNearestXValue(mouseX));

	// Iterate the XY charts (main price chart and indicator charts) in the FinanceChart
	XYChart *c = 0;
	for (int i = 0; i < m->getChartCount(); ++i) {
		c = (XYChart *)m->getChart(i);

		// Variables to hold the legend entries
        std::ostringstream ohlcLegend;
        std::vector<std::string> legendEntries;

		// Iterate through all layers to find the highest data point
		for (int j = 0; j < c->getLayerCount(); ++j) {
			Layer *layer = c->getLayerByZ(j);
			int xIndex = layer->getXIndexOf(xValue);
			int dataSetCount = layer->getDataSetCount();

			// In a FinanceChart, only layers showing OHLC data can have 4 data sets
			if (dataSetCount == 4) {
				double highValue = layer->getDataSet(0)->getValue(xIndex);
				double lowValue = layer->getDataSet(1)->getValue(xIndex);
				double openValue = layer->getDataSet(2)->getValue(xIndex);
				double closeValue = layer->getDataSet(3)->getValue(xIndex);

				if (closeValue != Chart::NoValue) {
					// Build the OHLC legend
					ohlcLegend << "      <*block*>";
					ohlcLegend << "Open: " << c->formatValue(openValue, "{value|P4}");
					ohlcLegend << ", High: " << c->formatValue(highValue, "{value|P4}");
					ohlcLegend << ", Low: " << c->formatValue(lowValue, "{value|P4}");
					ohlcLegend << ", Close: " << c->formatValue(closeValue, "{value|P4}");

					// We also draw an upward or downward triangle for up and down days and the %
					// change
					double lastCloseValue = layer->getDataSet(3)->getValue(xIndex - 1);
					if (lastCloseValue != Chart::NoValue) {
						double change = closeValue - lastCloseValue;
						double percent = change * 100 / closeValue;
                        std::string symbol = (change >= 0) ?
							"<*font,color=008800*><*img=@triangle,width=8,color=008800*>" :
							"<*font,color=CC0000*><*img=@invertedtriangle,width=8,color=CC0000*>";

						ohlcLegend << "  " << symbol << " " << c->formatValue(change, "{value|P4}");
						ohlcLegend << " (" << c->formatValue(percent, "{value|2}") << "%)<*/font*>";
					}

					ohlcLegend << "<*/*>";
				}
			}
			else {
				// Iterate through all the data sets in the layer
				for (int k = 0; k < layer->getDataSetCount(); ++k) {
					DataSet *dataSet = layer->getDataSetByZ(k);

                    std::string name = dataSet->getDataName();
					double value = dataSet->getValue(xIndex);
					if ((0 != name.size()) && (value != Chart::NoValue)) {

						// In a FinanceChart, the data set name consists of the indicator name and 
                        // its latest value. It is like "Vol: 123M" or "RSI (14): 55.34". As we are
						// generating the values dynamically, we need to extract the indictor name
						// out, and also the volume unit (if any).

						// The volume unit
						std::string unitChar;

						// The indicator name is the part of the name up to the colon character.
						int delimiterPosition = (int)name.find(':');
						if (name.npos != delimiterPosition) {

							// The unit, if any, is the trailing non-digit character(s).
							int lastDigitPos = (int)name.find_last_of("0123456789");
							if ((name.npos != lastDigitPos) && (lastDigitPos + 1 < (int)name.size()) &&
								(lastDigitPos > delimiterPosition))
								unitChar = name.substr(lastDigitPos + 1);

							name.resize(delimiterPosition);
						}

						// In a FinanceChart, if there are two data sets, it must be representing a
						// range.
						if (dataSetCount == 2) {
							// We show both values in the range in a single legend entry
							value = layer->getDataSet(0)->getValue(xIndex);
							double value2 = layer->getDataSet(1)->getValue(xIndex);
							name = name + ": " + c->formatValue((std::min)(value, value2), "{value|P3}");
							name = name + " - " + c->formatValue((std::max)(value, value2), "{value|P3}");
						}
						else {
							// In a FinanceChart, only the layer for volume bars has 3 data sets for
							// up/down/flat days
							if (dataSetCount == 3) {
								// The actual volume is the sum of the 3 data sets.
								value = layer->getDataSet(0)->getValue(xIndex) + layer->getDataSet(1
									)->getValue(xIndex) + layer->getDataSet(2)->getValue(xIndex);
							}

							// Create the legend entry
							name = name + ": " + c->formatValue(value, "{value|P3}") + unitChar;
						}

						// Build the legend entry, consist of a colored square box and the name (with
						// the data value in it).
                        std::ostringstream legendEntry;
						legendEntry << "<*block*><*img=@square,width=8,edgeColor=000000,color="
							<< std::hex << dataSet->getDataColor() << "*> " << name << "<*/*>";
						legendEntries.push_back(legendEntry.str());
					}
				}
			}
		}

		// Get the plot area position relative to the entire FinanceChart
		PlotArea *plotArea = c->getPlotArea();
		int plotAreaLeftX = plotArea->getLeftX() + c->getAbsOffsetX();
		int plotAreaTopY = plotArea->getTopY() + c->getAbsOffsetY();

		// The legend begins with the date label, then the ohlcLegend (if any), and then the
		// entries for the indicators.
        std::ostringstream legendText;
		legendText << "<*block,valign=top,maxWidth=" << (plotArea->getWidth() - 5)
			<< "*><*font=Arial Bold*>[" << c->xAxis()->getFormattedLabel(xValue, "mmm dd, yyyy")
			<< "]<*/font*>" << ohlcLegend.str();
		for (int i = ((int)legendEntries.size()) - 1; i >= 0; --i) {
			legendText << "      " << legendEntries[i];
		}
		legendText << "<*/*>";

		// Draw a vertical track line at the x-position
		d->vline(plotAreaTopY, plotAreaTopY + plotArea->getHeight(), c->getXCoor(xValue) +
			c->getAbsOffsetX(), d->dashLineColor(0x000000, 0x0101));

		// Display the legend on the top of the plot area
		TTFText *t = d->text(legendText.str().c_str(), "Arial", 8);
		t->draw(plotAreaLeftX + 5, plotAreaTopY + 3, 0x000000, Chart::TopLeft);
		t->destroy();
	}
}

//
// The ViewPortChanged event handler. This event occurs if the user scrolls or zooms in or 
// out the chart by dragging or clicking on the chart. It can also be triggered by calling
// CChartViewer.updateViewPort.
//
void CFinanceDemoDlg::OnViewPortChanged()
{
	// Update the chart if necessary
	if (m_ChartViewer.needUpdateChart())
		drawChart(&m_ChartViewer);
}

/////////////////////////////////////////////////////////////////////////////
// General utilities

//
// Load an icon resource into a button
//
void CFinanceDemoDlg::loadButtonIcon(int buttonId, int iconId, int width, int height)
{
    // Resize the icon to match the screen DPI for high DPI support
    HDC screen = ::GetDC(0);
    double scaleFactor = GetDeviceCaps(screen, LOGPIXELSX) / 96.0;
    ::ReleaseDC(0, screen);
    width = (int)(width * scaleFactor + 0.5);
    height = (int)(height * scaleFactor + 0.5);

    GetDlgItem(buttonId)->SendMessage(BM_SETIMAGE, IMAGE_ICON, (LPARAM)::LoadImage(
        AfxGetResourceHandle(), MAKEINTRESOURCE(iconId), IMAGE_ICON, width, height,
        LR_DEFAULTCOLOR));
}

//
// Convert std::vector to a DoubleArray
//
DoubleArray CFinanceDemoDlg::vectorToArray(const std::vector<double>& v, int startIndex, int length)
{
    if ((length < 0) || (length + startIndex > (int)v.size()))
        length = ((int)v.size()) - startIndex;
    return (length <= 0) ? DoubleArray() : DoubleArray(&(v[startIndex]), length);
}

//
// Convert DoubleArray to std::vector
//
std::vector<double> CFinanceDemoDlg::arrayToVector(DoubleArray a)
{
    return std::vector<double>(a.data, a.data + a.len);
}
