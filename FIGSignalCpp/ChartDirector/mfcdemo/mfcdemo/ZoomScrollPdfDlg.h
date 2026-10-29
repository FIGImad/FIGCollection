// ZoomScrollPdfDlg.h : header file
//

#pragma once

#include "ChartViewer.h"
#include <afxmt.h>


// CZoomScrollPdfDlg dialog
class CZoomScrollPdfDlg : public CDialog
{
// Construction
public:
	CZoomScrollPdfDlg(CWnd* pParent = NULL);	// standard constructor
	~CZoomScrollPdfDlg();

#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_ZOOMSCROLLPDF };
#endif

protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	DECLARE_MESSAGE_MAP()

protected:
	// Controls
	CButton m_PointerPB;
	CScrollBar m_HScrollBar;
	CChartViewer m_ChartViewer;

	// Overrides
	virtual BOOL OnInitDialog();

	// Generated message map functions
	afx_msg void OnPointerPB();
	afx_msg void OnZoomInPB();
	afx_msg void OnZoomOutPB();
	afx_msg void OnHScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar);
	afx_msg void OnViewPortChanged();	
	afx_msg void OnMouseMovePlotArea();
	afx_msg void OnSaveChart();
	afx_msg void OnSaveReport();

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
	XYChart* drawXYChart(double startX, double endX);
    void trackLineLabel(XYChart *c, int mouseX);
	void createPdfReport(const char* filename);

	// Moves the scroll bar when the user clicks on it
	double moveScrollBar(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar);

	// Update controls when the view port changed
    void updateControls(CChartViewer *viewer);

	// utility to load icon resource to a button
	void loadButtonIcon(int buttonId, int iconId, int width, int height);
};
