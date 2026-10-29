// CRealTimeZoomScrollDlg.h : header file
//

#pragma once

#include "ChartViewer.h"
#include "afxwin.h"
#include "resource.h"


class CRealTimeZoomScrollDlg : public CDialog
{
public:
	CRealTimeZoomScrollDlg(CWnd* pParent = NULL);	// standard constructor
	~CRealTimeZoomScrollDlg();

#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_REALTIMEZOOMSCROLL };
#endif

protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	DECLARE_MESSAGE_MAP()

protected:
	// Controls
	CStatic	m_ValueC;
	CStatic	m_ValueB;
	CStatic	m_ValueA;
	CButton m_PointerPB;
	CChartViewer m_ChartViewer;
	CComboBox m_UpdatePeriod;
	CScrollBar m_HScrollBar;

	// Override
	virtual BOOL OnInitDialog();
	
	// Message handlers
	afx_msg void OnTimer(UINT_PTR nIDEvent);
	afx_msg void OnSelchangeUpdatePeriod();
	afx_msg void OnViewPortChanged();
	afx_msg void OnMouseMovePlotArea();
	afx_msg void OnPointerPB();
	afx_msg void OnZoomInPB();
	afx_msg void OnZoomOutPB();
	afx_msg void OnSavePB();
	afx_msg void OnHScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar);

	// Timers to update the data and the chart
	void OnDataRateTimer();
	void OnChartUpdateTimer();

	// The number of samples per data series used in this demo
	static const int sampleSize = 10000;
	
	// The initial full range is set to 60 seconds of data.
    static const int initialFullRange = 60;

	// The maximum zoom in is 10 seconds.
    static const int zoomInLimit = 10;

	double m_timeStamps[sampleSize];	// The timestamps for the data series
	double m_dataSeriesA[sampleSize];	// The values for the data series A
	double m_dataSeriesB[sampleSize];	// The values for the data series B
	double m_dataSeriesC[sampleSize];	// The values for the data series C

    // The index of the array position to which new data values are added.
    int m_currentIndex;

	// Used by the random number generator to generate real time data.
	double m_nextDataTime;	

	// Draw chart
	void drawChart(CChartViewer *viewer);
	void trackLineLabel(XYChart *c, int mouseX);
	
	// Update controls when the view port changed
	void updateControls(CChartViewer *viewer);
	// Moves the scroll bar when the user clicks on it
	double moveScrollBar(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar);

	// utility to load icon resource to a button
	void loadButtonIcon(int buttonId, int iconId, int width, int height);
};
