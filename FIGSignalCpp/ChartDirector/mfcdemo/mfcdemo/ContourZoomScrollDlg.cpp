// ContourZoomScrollDlg.cpp : implementation file
//

#include "stdafx.h"
#include "resource.h"
#include "ContourZoomScrollDlg.h"
#include "chartdir.h"
#include <math.h>
#include <sstream>
#include <algorithm>

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

//
// Constructor
//
CContourZoomScrollDlg::CContourZoomScrollDlg(CWnd* pParent /*=NULL*/)
    : CDialog(IDD_CONTOURZOOMSCROLL, pParent)
{
    firstChart = 0;
    firstColorAxis = 0;
}

//
// Destructor
//
CContourZoomScrollDlg::~CContourZoomScrollDlg()
{
    // Delete the old chart in m_ChartViewer unless it is the first chart
    if (m_ChartViewer.getChart() != firstChart)
        delete m_ChartViewer.getChart();

    // Delete the first chart
    delete firstChart;

    // Delete the chart in the m_ViewPortControl
    delete m_ViewPortControl.getChart();
}


void CContourZoomScrollDlg::DoDataExchange(CDataExchange* pDX)
{
    CDialog::DoDataExchange(pDX);
    DDX_Control(pDX, IDC_PointerPB, m_PointerPB);
    DDX_Control(pDX, IDC_ZoomBar, m_ZoomBar);
    DDX_Control(pDX, IDC_ChartViewer, m_ChartViewer);
    DDX_Control(pDX, IDC_ViewPortControl, m_ViewPortControl);
}

BEGIN_MESSAGE_MAP(CContourZoomScrollDlg, CDialog)
    ON_WM_PAINT()
    ON_WM_QUERYDRAGICON()
    ON_BN_CLICKED(IDC_PointerPB, OnPointerPB)
    ON_BN_CLICKED(IDC_ZoomInPB, OnZoomInPB)
    ON_BN_CLICKED(IDC_ZoomOutPB, OnZoomOutPB)
    ON_BN_CLICKED(IDC_SavePB, OnSavePB)
    ON_WM_HSCROLL()
    ON_CONTROL(CVN_ViewPortChanged, IDC_ChartViewer, OnViewPortChanged)
END_MESSAGE_MAP()


//
// Initialization
//
BOOL CContourZoomScrollDlg::OnInitDialog()
{
    CDialog::OnInitDialog();
   
    //
    // Initialize controls
    //

    // Load icons to mouse usage buttons
    loadButtonIcon(IDC_PointerPB, IDI_PointerPB, 100, 20);  
    loadButtonIcon(IDC_ZoomInPB, IDI_ZoomInPB, 100, 20);    
    loadButtonIcon(IDC_ZoomOutPB, IDI_ZoomOutPB, 100, 20);
    loadButtonIcon(IDC_SavePB, IDI_SavePB, 100, 20);  

    // Set Pointer pushbutton into clicked state
    m_PointerPB.SetCheck(1);

    // Initialize zoom bar
    m_ZoomBar.SetRange(1, 100);
    m_ZoomBar.SetPageSize(5);
    m_ZoomBar.SetTicFreq(10);

    // Set initial mouse usage for ChartViewer
    m_ChartViewer.setMouseUsage(Chart::MouseUsageScroll);
    m_ChartViewer.setScrollDirection(Chart::DirectionHorizontalVertical);

    // By default, when the user drags a rectangle to zoom in, the x and y dimensions can zoom by
    // different factors. KeepAspectRatio can be used to ensure the x and y dimensions are zoom by
    // the same factor, thereby keeping the aspect ratio unchanged.
    m_ChartViewer.setZoomDirection(Chart::KeepAspectRatio);
    
    // Enable mouse wheel zooming by setting the zoom ratio to 1.1 per wheel event
    m_ChartViewer.setMouseWheelZoomRatio(1.1);

    // Configure CDML tooltip to use a semi-transparent rounded rectangle as container and 
    // Arial Bold as font
    m_ChartViewer.setCDMLToolTipPrefix(
        _T("<*block,bgColor=60FFFFDD,roundedCorners=5,margin=5,edgeColor=000000*><*font=Arial Bold*>"));

    // Load the data
    loadData();

    // Trigger the ViewPortChanged event to draw the chart
    m_ChartViewer.updateViewPort(true, true);

    // Configure the CViewPortControl to use transparent black for region outside the viewport,
    // that is, to darken the outside region.
    m_ViewPortControl.setViewPortExternalColor(0x7f000000);
    m_ViewPortControl.setViewPortBorderColor(0x7fffffff);
    m_ViewPortControl.setSelectionBorderColor(0x7fffffff); 
    
    // Draw and display the full chart in the CViewPortControl
    drawFullChart(&m_ViewPortControl, &m_ChartViewer);

    // Bind the CChartViewer to the CViewPortControl
    m_ViewPortControl.setViewer(&m_ChartViewer);

    return TRUE;
}

//
// User clicks on the Scroll pushbutton
//
void CContourZoomScrollDlg::OnPointerPB() 
{
    m_ChartViewer.setMouseUsage(Chart::MouseUsageScroll);   
}

//
// User clicks on the Zoom In pushbutton
//
void CContourZoomScrollDlg::OnZoomInPB() 
{
    m_ChartViewer.setMouseUsage(Chart::MouseUsageZoomIn);   
}

//
// User clicks on the Zoom Out pushbutton
//
void CContourZoomScrollDlg::OnZoomOutPB() 
{
    m_ChartViewer.setMouseUsage(Chart::MouseUsageZoomOut);  
}

//
// User clicks on the Save pushbutton
//
void CContourZoomScrollDlg::OnSavePB()
{
    // Supported formats = PNG, JPG, GIF, BMP, SVG and PDF
    TCHAR szFilters[]= _T("PNG (*.png)|*.png|JPG (*.jpg)|*.jpg|GIF (*.gif)|*.gif|")
        _T("BMP (*.bmp)|*.bmp|SVG (*.svg)|*.svg|PDF (*.pdf)|*.pdf||");

    // The standard CFileDialog
    CFileDialog fileDlg(FALSE, _T("png"), _T("chartdirector_demo"), OFN_HIDEREADONLY | 
        OFN_OVERWRITEPROMPT, szFilters);
    if(fileDlg.DoModal() != IDOK)
        return;

    // Save the chart
    CString path = fileDlg.GetPathName();
    BaseChart *c = m_ChartViewer.getChart();
    if (0 != c)
        c->makeChart(TCHARtoUTF8(path));
}

//
// The ViewPortChanged event handler. This event occurs if the user scrolls or zooms in or 
// out the chart by dragging or clicking on the chart. It can also be triggered by calling
// CChartViewer.updateViewPort and by the CViewPortControl.
//
void CContourZoomScrollDlg::OnViewPortChanged()
{
    // In addition to updating the chart, we may also need to update other controls that
    // changes based on the view port.
    updateControls(&m_ChartViewer);

    // Update the chart if necessary
    if (m_ChartViewer.needUpdateChart())
        drawChart(&m_ChartViewer);
}

//
// User moves the Zoom slider control
//
void CContourZoomScrollDlg::OnHScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar) 
{
    if (nSBCode != SB_ENDSCROLL)
    {
        //
        // The slider is moving. Update the chart.
        //

        // Remember the center point
        double centerX = m_ChartViewer.getViewPortLeft() + 
            m_ChartViewer.getViewPortWidth() / 2;
        double centerY = m_ChartViewer.getViewPortTop() + 
            m_ChartViewer.getViewPortHeight() / 2;
            
        // Aspect ratio and zoom factor
        double aspectRatio = m_ChartViewer.getViewPortWidth() / 
            m_ChartViewer.getViewPortHeight();
        double zoomTo = ((double)m_ZoomBar.GetPos()) / m_ZoomBar.GetRangeMax();

        // Zoom by adjusting ViewPortWidth and ViewPortHeight while maintaining the aspect 
        // ratio
        m_ChartViewer.setViewPortWidth(zoomTo * ((aspectRatio < 1) ? 1 : aspectRatio));
        m_ChartViewer.setViewPortHeight(zoomTo * ((aspectRatio > 1) ? 1 : (1 / aspectRatio)));
        
        // Adjust ViewPortLeft and ViewPortTop to keep center point unchanged
        m_ChartViewer.setViewPortLeft(centerX - m_ChartViewer.getViewPortWidth() / 2);
        m_ChartViewer.setViewPortTop(centerY - m_ChartViewer.getViewPortHeight() / 2);
        
        // Update the chart image only, but no need to update the image map.
        m_ChartViewer.updateViewPort(true, false);
    }
        
    CDialog::OnHScroll(nSBCode, nPos, pScrollBar);
}

//
// Load the data
//
void CContourZoomScrollDlg::loadData()
{
    //
    // For simplicity, in this demo, we just use hard coded data. 
    //

    // The x and y coordinates of the grid
    double x[] = {-10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
    double y[] = {-10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
    dataX.assign(x, x + sizeof(x) / sizeof(*x));
    dataY.assign(y, y + sizeof(y) / sizeof(*y));
    
    // The values at the grid points. In this example, we will compute the values using the
    // formula z = x * sin(y) + y * sin(x).
    dataZ.resize(dataX.size() * dataY.size());
    for (int yIndex = 0; yIndex < (int)dataY.size(); ++yIndex)
    {
        double y = dataY[yIndex];
        for (int xIndex = 0; xIndex < (int)dataX.size(); ++xIndex)
        {
            double x = dataX[xIndex];
            dataZ[yIndex * dataX.size() + xIndex] = x * sin(y) + y * sin(x);
        }
    }
}

//
// Draw the chart and display it in the given viewer
//
void CContourZoomScrollDlg::drawChart(CChartViewer *viewer)
{               
    // Create an XYChart object 500 x 480 pixels in size, with the same background color
    // as the container
    XYChart *c = new XYChart(540, 490);

    // Set the plotarea at (75, 40) and of size 400 x 400 pixels. Use semi-transparent black
    // (80000000) dotted lines for both horizontal and vertical grid lines
    c->setPlotArea(50, 40, 400, 400, -1, -1, -1, c->dashLineColor(0x80000000, Chart::DotLine), -1);

    // Enable clipping mode to clip the part of the data that is outside the plot area.
    c->setClipping();

    // Add a title to the chart using 15 points Arial Bold Italic font
    c->addTitle("z = x * sin(y) + y * sin(x)      ", "Arial Bold Italic", 15);

    // Set x-axis and y-axis title using 12 points Arial Bold Italic font
    c->xAxis()->setTitle("X-Axis Title Place Holder", "Arial Bold Italic", 12);
    c->yAxis()->setTitle("Y-Axis Title Place Holder", "Arial Bold Italic", 12);

    // Set x-axis and y-axis labels to use Arial Bold font
    c->xAxis()->setLabelStyle("Arial Bold");
    c->yAxis()->setLabelStyle("Arial Bold");

    // When auto-scaling, use tick spacing of 40 pixels as a guideline
    c->yAxis()->setTickDensity(40);
    c->xAxis()->setTickDensity(40);

    // Add a contour layer using the given data
    ContourLayer* layer = c->addContourLayer(vectorToArray(dataX), vectorToArray(dataY), 
        vectorToArray(dataZ));

    // Move the grid lines in front of the contour layer
    c->getPlotArea()->moveGridBefore(layer);

    // Add a color axis (the legend) in which the top left corner is anchored at (505, 40). Set the
    // length to 400 pixels and the labels on the right side.
    ColorAxis* cAxis = layer->setColorAxis(470, 40, Chart::TopLeft, 400, Chart::Right);

    // Add a title to the color axis using 12 points Arial Bold Italic font
    cAxis->setTitle("Color Legend Title Place Holder", "Arial Bold Italic", 12)
        ->setAlignment(Chart::Right);

    // Set color axis labels to use Arial Bold font
    cAxis->setLabelStyle("Arial Bold");

    // In this example, we have not explicitly configured the full x and y range. In this case, 
    // the first time syncLinearAxisWithViewPort is called, ChartDirector will auto-scale the axis
    // and assume the resulting range is the full range. In subsequent calls, ChartDirector will 
    // set the axis range based on the view port and the full range.
    viewer->syncLinearAxisWithViewPort("x", c->xAxis());
    viewer->syncLinearAxisWithViewPort("y", c->yAxis());
    
    // To ensure the color is consistent, we must keep the color axis the same when the zooming or
    // scrolling the chart. To do this, we save the color axis of the first chart and synchronize 
    // all future charts with this color axis.
    if (0 == firstChart)
    {
        // We need to save the first chart as well as the first color axis. Otherwise the first
        // chart may get deleted, and this will delete the first color axis as well.
        firstChart = c;
        firstColorAxis = cAxis;
    }
    else
        cAxis->syncAxis(firstColorAxis);

    // Delete the old chart unless it is the first chart. The first chart is needed as it defines
    // the color scale for subsequent charts.
    if (viewer->getChart() != firstChart)
        delete viewer->getChart();

    // Display chart
    viewer->setChart(c);

    // Tooltip for the contour chart
    viewer->setImageMap(c->getHTMLImageMap("", "", "title='<*cdml*>X={x|2}\nY={y|2}\nZ={z|2}'"));
}

//
// Draw the full thumbnail chart and display it in the given CViewPortControl
//
void CContourZoomScrollDlg::drawFullChart(CViewPortControl *vpc, CChartViewer *viewer)
{
    // Create an XYChart object 110 x 110 pixels in size
    XYChart* c = new XYChart(110, 110);

    // Set the plotarea to cover the entire chart. Disable grid lines by setting their colors
    // to transparent. 
    c->setPlotArea(0, 0, c->getWidth() - 1, c->getHeight() - 1, -1, -1, -1, Chart::Transparent, 
        Chart::Transparent);

    // Add a contour layer using the given data
    ContourLayer* layer = c->addContourLayer(vectorToArray(dataX), vectorToArray(dataY),
        vectorToArray(dataZ));
    layer->setContourColor(0xcf000000);

    // To maintain color consistency, the color axis is synchronized to that of the first chart.
    layer->colorAxis()->syncAxis(firstColorAxis);
    
    // The x and y axis scales reflect the full range of the view port
    c->xAxis()->setLinearScale(viewer->getValueAtViewPort("x", 0), viewer->getValueAtViewPort("x", 1),
        Chart::NoValue);
    c->yAxis()->setLinearScale(viewer->getValueAtViewPort("y", 0), viewer->getValueAtViewPort("y", 1),
        Chart::NoValue);

    // Set the chart image to the WinChartViewer
    delete vpc->getChart();
    vpc->setChart(c);
}

//
// Update other controls that may be affected by the viewport.
//
void CContourZoomScrollDlg::updateControls(CChartViewer *viewer)
{
    // Update the Zoom slider to reflect the current zoom level of the view port
    double smallerSide = (std::min)(viewer->getViewPortWidth(), viewer->getViewPortHeight());
    m_ZoomBar.SetPos((int)(smallerSide * m_ZoomBar.GetRangeMax() + 0.5));
}

/////////////////////////////////////////////////////////////////////////////
// General utilities

//
// Load an icon resource into a button
//
void CContourZoomScrollDlg::loadButtonIcon(int buttonId, int iconId, int width, int height)
{
    // Resize the icon to match the screen DPI for high DPI support
    HDC screen = ::GetDC(0);
    double scaleFactor = GetDeviceCaps(screen, LOGPIXELSX) / 96.0;
    ::ReleaseDC(0, screen);
    width = (int)(width * scaleFactor + 0.5);
    height = (int)(height * scaleFactor + 0.5);

    GetDlgItem(buttonId)->SendMessage(BM_SETIMAGE, IMAGE_ICON, (LPARAM)::LoadImage(
        AfxGetResourceHandle(), MAKEINTRESOURCE(iconId), IMAGE_ICON, width, height, LR_DEFAULTCOLOR));
}

//
// Convert std::vector to a DoubleArray
//
DoubleArray CContourZoomScrollDlg::vectorToArray(std::vector<double> &v)
{
    return (v.size() == 0) ? DoubleArray() : DoubleArray(&(v[0]), (int)v.size());
}
