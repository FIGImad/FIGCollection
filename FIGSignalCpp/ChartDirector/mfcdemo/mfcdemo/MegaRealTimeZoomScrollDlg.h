#pragma once

#include "ChartViewer.h"
#include <afxmt.h>
#include <vector>


class CMegaRealTimeZoomScrollDlg : public CDialog
{
public:
	CMegaRealTimeZoomScrollDlg(CWnd* pParent = nullptr);   // standard constructor
	virtual ~CMegaRealTimeZoomScrollDlg();

#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_MEGAREALTIMEZOOMSCROLL };
#endif

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	DECLARE_MESSAGE_MAP()

public:
	// Controls
	CChartViewer m_ChartViewer;
	CScrollBar m_HScrollBar;
	CButton m_PointerPB;
	CButton m_PlotChartPB;

	// Override
	virtual BOOL OnInitDialog();

	// Message handlers
	afx_msg void OnTimer(UINT_PTR nIDEvent);
	afx_msg void OnPointerPB();
	afx_msg void OnZoomInPB();
	afx_msg void OnZoomOutPB();
	afx_msg void OnClickPlotChart();
	afx_msg void OnHScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar);
	afx_msg void OnViewPortChanged();
	afx_msg void OnMouseMovePlotArea();

	// Timer event to update the chart
	void OnChartUpdateTimer();

	//
	// Data arrays
	//
	std::vector<double> m_dataSeriesA;
	std::vector<double> m_dataSeriesB;
	std::vector<double> m_dataSeriesC;
	std::vector<double> m_timeStamps;
	
	// The index position that new data will be added.
	int m_currentIndex;
	
	// Data Accelerator for handle large data sets
	DataAccelerator* m_fastData;

	// Flag to indicate the chart has been drawn so the zoom/scroll and track cursor
	// event handlers can run.
	bool m_hasFinishedInitialization;

	// Load data into data arrays
	void loadData();

	// One real-time random number generator
	RanSeries m_realTimeData;
	clock_t m_lastUpdateTime;
	bool getRealTimeData();

	// Update the axis scale to accomodate new data
	void updateAxisScale(CChartViewer* viewer);

	// Initialize the CChartViewer
	void initChartViewer(CChartViewer* viewer);

	// Draw chart
	void drawChart(CChartViewer* viewer);
	void trackLineLabel(XYChart* c, int mouseX);

	// Update other controls when viewport changed
	void updateControls(CChartViewer* viewer);
	// Moves the scroll bar when the user clicks on it
	double moveScrollBar(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar);

	// utility to load icon resource to a button
	void loadButtonIcon(int buttonId, int iconId, int width, int height);
};

