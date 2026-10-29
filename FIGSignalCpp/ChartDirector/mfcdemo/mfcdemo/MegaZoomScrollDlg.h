#pragma once

#include "ChartViewer.h"
#include <afxmt.h>
#include <vector>


class CMegaZoomScrollDlg : public CDialog
{
public:
	CMegaZoomScrollDlg(CWnd* pParent = nullptr);   // standard constructor
	virtual ~CMegaZoomScrollDlg();

#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_MEGAZOOMSCROLL };
#endif

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	DECLARE_MESSAGE_MAP()

public:
	// Controls
	CChartViewer m_ChartViewer;
	CScrollBar m_HScrollBar;
	CButton m_PointerPB;

	// Override
	virtual BOOL OnInitDialog();

	// Message handlers
	afx_msg void OnPointerPB();
	afx_msg void OnZoomInPB();
	afx_msg void OnZoomOutPB();
	afx_msg void OnClickPlotChart();
	afx_msg void OnHScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar);
	afx_msg void OnViewPortChanged();
	afx_msg void OnMouseMovePlotArea();

	//
	// Data arrays
	//
	std::vector<double> m_dataSeriesA;
	std::vector<double> m_dataSeriesB;
	std::vector<double> m_dataSeriesC;
	std::vector<double> m_timeStamps;
	
	// Flag to indicate the chart has been drawn so the zoom/scroll and track cursor
	// event handlers can run.
	bool m_hasFinishedInitialization;

	// Load data into data arrays
	void loadData();

	// Data Accelerator for handle large data sets
	DataAccelerator* m_fastData;

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
