// ThreeDChartRotationDlg.h : header file
//

#pragma once

#include "ChartViewer.h"
#include "afxwin.h"


class CThreeDChartRotationDlg : public CDialog
{
public:
    CThreeDChartRotationDlg(CWnd* pParent = NULL);  // standard constructor
    ~CThreeDChartRotationDlg();

#ifdef AFX_DESIGN_TIME
    enum { IDD = IDD_THREEDCHARTROTATION };
#endif

protected:
    virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	DECLARE_MESSAGE_MAP()

protected:
	// Controls
	CChartViewer m_ChartViewer;
	CButton m_DrawFrameOnRotate;

    // OVerrides
    virtual BOOL OnInitDialog();

	// Generated message map functions
    afx_msg void OnViewPortChanged();
	afx_msg void OnMouseMoveChart();
	afx_msg void OnMouseUpChart();

	// 3D view angles
	double m_elevationAngle;
	double m_rotationAngle;

	// Keep track of mouse drag
	int m_lastMouseX;
	int m_lastMouseY;
	bool m_isDragging;

    // Draw chart
	void drawChart(CChartViewer* viewer);
};
