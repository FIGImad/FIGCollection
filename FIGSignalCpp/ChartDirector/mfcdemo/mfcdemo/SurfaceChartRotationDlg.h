// ThreeDChartRotationDlg.h : header file
//

#pragma once

#include "ChartViewer.h"
#include "afxwin.h"


// CThreeDChartRotationDlg dialog
class CThreeDChartRotationDlg : public CDialog
{
// Construction
public:
    CThreeDChartRotationDlg(CWnd* pParent = NULL);  // standard constructor
    ~CThreeDChartRotationDlg();

// Dialog Data
    enum { IDD = IDD_SURFACECHARTROTATION_DIALOG };
    CChartViewer m_ChartViewer;

	protected:
    virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

// Implementation
protected:

    // Generated message map functions
    virtual BOOL OnInitDialog();
    afx_msg void OnPaint();
    afx_msg HCURSOR OnQueryDragIcon();
    afx_msg void OnViewPortChanged();
	afx_msg void OnMouseMoveChart();
	afx_msg void OnMouseUpChart();

    DECLARE_MESSAGE_MAP()

private:

	// 3D view angles
	double m_elevationAngle;
	double m_rotationAngle;

	// Keep track of mouse drag
	int m_lastMouseX;
	int m_lastMouseY;
	bool m_isDragging;

    // Draw chart
    void drawChart(CChartViewer *viewer);
public:
	CButton m_DrawFrameOnRotate;
	CFont m_ButtonFont;
};
