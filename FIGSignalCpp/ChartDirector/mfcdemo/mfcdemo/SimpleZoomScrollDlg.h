// SimpleZoomScrollDlg.h : header file
//

#pragma once

#include "ChartViewer.h"
#include <afxmt.h>


// CSimpleZoomScrollDlg dialog
class CSimpleZoomScrollDlg : public CDialog
{
// Construction
public:
	CSimpleZoomScrollDlg(CWnd* pParent = NULL);	// standard constructor
	~CSimpleZoomScrollDlg();

#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_SIMPLEZOOMSCROLL };
#endif

protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	DECLARE_MESSAGE_MAP()

protected:
	// Controls
	CButton m_PointerPB;
	CChartViewer m_ChartViewer;

	// Overrides
	virtual BOOL OnInitDialog();

	// Generated message map functions
	afx_msg void OnPointerPB();
	afx_msg void OnZoomInPB();
	afx_msg void OnZoomOutPB();
	afx_msg void OnViewPortChanged();	

	// The default background color.
	int m_extBgColor;	

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
	void updateImageMap(CChartViewer *viewer);

	// utility to get default background color
	int getDefaultBgColor();
	// utility to load icon resource to a button
	void loadButtonIcon(int buttonId, int iconId, int width, int height);
};
