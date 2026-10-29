// CZoomScrollTrack2Dlg.h : header file
//

#pragma once

#include "ChartViewer.h"
#include <afxmt.h>
#include "afxdtctl.h"


// CZoomScrollTrack2Dlg dialog
class CZoomScrollTrack2Dlg : public CDialog
{
public:
	CZoomScrollTrack2Dlg(CWnd* pParent = NULL);	    // standard constructor
	~CZoomScrollTrack2Dlg();

#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_ZOOMSCROLLTRACK2 };
#endif

protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	DECLARE_MESSAGE_MAP()

protected:
	// Controls
	CButton m_PointerPB;
	CButton m_AlphaCB;
	CButton m_BetaCB;
	CButton m_GammaCB;
	CDateTimeCtrl m_StartDate;
	CDateTimeCtrl m_EndDate;
	CScrollBar m_HScrollBar;
	CChartViewer m_ChartViewer;

	// Generated message map functions
	virtual BOOL OnInitDialog();
	afx_msg void OnPointerPB();
	afx_msg void OnZoomInPB();
	afx_msg void OnZoomOutPB();
	afx_msg void OnClickCheckBox();
	afx_msg void OnStartDateChanged(NMHDR* pNMHDR, LRESULT* pResult);
	afx_msg void OnEndDateChanged(NMHDR* pNMHDR, LRESULT* pResult);
	afx_msg void OnHScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar);
	afx_msg void OnViewPortChanged();	
	afx_msg void OnMouseMovePlotArea();

	//
	// Data arrays for the scrollable / zoomable chart.
	// - In this demo, we just use a RanSeries object to generate random data for the chart.
	//
	RanSeries *m_ranSeries;
	DoubleArray m_timeStamps;
	DoubleArray m_dataSeriesA;
	DoubleArray m_dataSeriesB;
	DoubleArray m_dataSeriesC;
	
 	// Load data into data arrays
	void loadData();
	
    // Initialize the CChartViewer
    void initChartViewer(CChartViewer *viewer);

	// Draw chart
	void drawChart(CChartViewer *viewer);
    void trackLineLabel(XYChart *c, int mouseX);

	// Moves the scroll bar when the user clicks on it
	double moveScrollBar(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar);

	// Update controls when the view port changed
    void updateControls(CChartViewer *viewer);

	// utility to load icon resource to a button
	void loadButtonIcon(int buttonId, int iconId, int width, int height);
};
