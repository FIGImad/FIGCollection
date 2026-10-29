// CrossHairDlg.h : header file
//

#pragma once

#include "ChartViewer.h"
#include "afxdialogex.h"


class CCrossHairDlg : public CDialogEx
{
public:
	CCrossHairDlg(CWnd* pParent = NULL);	// standard constructor
	~CCrossHairDlg();

#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_TRACKCURSOR_DEMO };
#endif

protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	DECLARE_MESSAGE_MAP()

protected:
	// Controls
	CChartViewer m_ChartViewer;
	
	// Overrides
	virtual BOOL OnInitDialog();
	
	// Generated message map functions
	afx_msg void OnMouseMovePlotArea();

	void drawChart(CChartViewer *viewer);
	void crosshair(XYChart *c, int mouseX, int mouseY);
};
