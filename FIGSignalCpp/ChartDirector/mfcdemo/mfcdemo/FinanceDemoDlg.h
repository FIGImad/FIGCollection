// CFinanceDemoDlg.h : header file
//

#pragma once

#include "ChartViewer.h"
#include <vector>


// CFinanceDemoDlg dialog
class CFinanceDemoDlg : public CDialog
{

public:
	CFinanceDemoDlg(CWnd* pParent = NULL);	       // standard constructor
	virtual ~CFinanceDemoDlg();

#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_FINANCEDEMO_DIALOG };
#endif

protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	DECLARE_MESSAGE_MAP()

protected:
	// Controls
	CButton m_PointerPB;
	CEdit m_TickerSymbol;
	CEdit m_CompareWith;
	CComboBox m_TimeRange;
	CButton	m_Volume;
	CButton	m_ParabolicSAR;
	CButton	m_LogScale;
	CButton	m_PercentageScale;
	CComboBox m_ChartType;
	CComboBox m_Band;
	CComboBox m_AvgType1;
	CComboBox m_AvgType2;
	CEdit m_MovAvg1;
	CEdit m_MovAvg2;
	CComboBox m_Indicator1;
	CComboBox m_Indicator2;
	CChartViewer m_ChartViewer;
	
	// Overrides
	virtual BOOL OnInitDialog();
	virtual void OnOK();

	// Generated message map functions
	afx_msg void OnPointerPB();
	afx_msg void OnZoomInPB();
	afx_msg void OnZoomOutPB();
	afx_msg void OnSelectChange();
	afx_msg void OnTextChange();
	afx_msg void OnTimeRangeChange();
	afx_msg void OnViewPortChanged();
	afx_msg void OnMouseMovePlotArea();

	// The main ticker key and the compare to ticker key
	CString m_tickerKey;
	CString m_compareKey;

	struct PriceData
	{
		std::vector<double> timeStamps;
		std::vector<double> highData;
		std::vector<double> lowData;
		std::vector<double> openData;
		std::vector<double> closeData;
		std::vector<double> volData;
		
		// An extra field for price comparison
		std::vector<double> compareData;
	};

	// In this example, we can use daily, weekly or monthly data depending on the time range
	PriceData m_dailyPrice;
	PriceData m_weeklyPrice;
	PriceData m_monthlyPrice;
	
	void aggregateData(const ArrayMath& aggregator, PriceData& p);

	// The moving average periods
	int m_avgPeriod1;
	int m_avgPeriod2;

	// Draw the chart
	virtual void loadData(const CString &ticker, const CString &compare);
	virtual void drawChart(CChartViewer *viewer);
	virtual void trackFinance(MultiChart *m, int mouseX);

	// Utility to convert a std::vector to a DoubleArray
	DoubleArray vectorToArray(const std::vector<double>& v, int startIndex = 0, int length = -1);
	// Utility to convert a std::vector to a DoubleArray
	std::vector<double> arrayToVector(DoubleArray a);

	// utility to load icon resource to a button
	void loadButtonIcon(int buttonId, int iconId, int width, int height);
};
