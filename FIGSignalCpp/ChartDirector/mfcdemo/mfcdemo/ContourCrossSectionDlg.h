#pragma once

#include "chartdir.h"
#include "ChartViewer.h"
#include "afxdialogex.h"

// ConturCrossSectionDlg dialog

class CContourCrossSectionDlg : public CDialogEx
{
public:
	CContourCrossSectionDlg(CWnd* pParent = NULL);   // standard constructor
	virtual ~CContourCrossSectionDlg();

#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_CONTOURCROSSSECTION };
#endif

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	DECLARE_MESSAGE_MAP()

public:
	// Controls
	CChartViewer m_ChartViewer;
	CChartViewer m_CrossSectionViewerX;
	CChartViewer m_CrossSectionViewerY;
	ContourLayer* m_ContourLayer;

	// Override
	virtual BOOL OnInitDialog();

	// Message handlers
	afx_msg void OnMouseMoveChart();

	// Keep track of mouse dragging state
	enum DragState { Drag_None, Drag_Vertical, Drag_Horizontal };
	DragState m_isDragging;
	int m_dragOffset;

	// Crosshair position
	int m_crossHairX;
	int m_crossHairY;

	// Draw chart, cross section  and crosshair
	void drawChart(CChartViewer* viewer);
	void drawCrossSectionX(CChartViewer* viewer);
	void drawCrossSectionY(CChartViewer* viewer);
	void drawCrossHair(CChartViewer* viewer);
};
