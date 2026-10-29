// RealTimeDemoDlg.h : header file
//

#pragma once

#include "ChartViewer.h"
#include <afxmt.h>
#include <vector>


class CRealTimeDemoDlg : public CDialog
{
public:
	CRealTimeDemoDlg(CWnd* pParent = NULL);	// standard constructor

#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_REALTIMEDEMO };
#endif
		
protected:
	virtual void DoDataExchange(CDataExchange* pDX);  // DDX/DDV support
	DECLARE_MESSAGE_MAP()

protected:
	// Controls
	CButton	m_RunPB;
	CComboBox	m_UpdatePeriod;
	CChartViewer m_ChartViewer;	
	CStatic	m_ValueA;
	CStatic	m_ValueB;
	CStatic	m_ValueC;

	// Overrides
	virtual BOOL OnInitDialog();

	// Generated message map functions
	afx_msg void OnTimer(UINT_PTR nIDEvent);
	afx_msg void OnRunPB();
	afx_msg void OnFreezePB();
	afx_msg void OnSelchangeUpdatePeriod();
	afx_msg void OnViewPortChanged();

	// Storage for the realtime data
	std::vector<double> m_timeStamps;	// The timestamps for the data series
	std::vector<double> m_dataSeriesA;	// The values for the data series A
	std::vector<double> m_dataSeriesB;	// The values for the data series B
	std::vector<double> m_dataSeriesC;	// The values for the data series C

	double m_nextDataTime;	// Used by the random number generator to generate real time data.
	int m_extBgColor;		// The default background color.

	// Shift new data values into the real time data arrays 
	void getData();
	
	// Draw chart
	void drawChart(CChartViewer *viewer);

	// utility to get default background color
	int getDefaultBgColor();
	// utility to load icon resource to a button
	void loadButtonIcon(int buttonId, int iconId, int width, int height);
	// Convert vector to DoubleArray
	DoubleArray vectorToArray(const std::vector<double>& v, int startIndex = 0, int length = -1);
};
