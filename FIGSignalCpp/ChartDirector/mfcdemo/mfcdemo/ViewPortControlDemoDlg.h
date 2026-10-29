// viewportcontroldemoDlg.h : header file
//

#pragma once
#include "afxwin.h"
#include "ChartViewer.h"


// CViewPortControlDemoDlg dialog
class CViewPortControlDemoDlg : public CDialog
{
public:
	// Constructor and Destructor
	CViewPortControlDemoDlg(CWnd* pParent = NULL);
	~CViewPortControlDemoDlg();

	// Dialog Data
#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_VIEWPORTCONTROLDEMO };
#endif

protected:
	// DDX/DDV support
	virtual void DoDataExchange(CDataExchange* pDX);
	DECLARE_MESSAGE_MAP()

protected:
	// Controls
	CButton m_PointerPB;
	CChartViewer m_ChartViewer;
	CViewPortControl m_ViewPortControl;

	// Overrides
	virtual BOOL OnInitDialog();

	// Generated message map functions
	afx_msg void OnPointerPB();
	afx_msg void OnZoomInPB();
	afx_msg void OnZoomOutPB();
	afx_msg void OnSavePB();
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
	void drawFullChart(CViewPortControl *vpc, CChartViewer *viewer);
    void trackLineLegend(XYChart *c, int mouseX);

	// utility to load icon resource to a button
	void loadButtonIcon(int buttonId, int iconId, int width, int height);
};
