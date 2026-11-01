#include <windows.h>
#include <windowsx.h>
#include <commdlg.h>
#include <commctrl.h>
#include <shellapi.h>
#include <shlobj.h>
#include <map>
#include <shlwapi.h>
#include <gdiplus.h>
#include "ChartView.h"
#include "FigDataTests.h"
#include <fstream>
#include <cmath>
#include <sstream>
#include <iomanip>
#include <uxtheme.h>
#include <future>
#include <chrono>
#include "DemoOverlay.h"
#include "DatabaseData.h"
#include "StudyValueCodecTests.h"

using namespace figcharts;
namespace {
HWND window, statusLabel, toolbar;
HWND signalFilterEdit=nullptr,signalFilterClear=nullptr,signalFilterFrame=nullptr;
constexpr int SignalFilterSlot=1800,SignalFilterClear=1801,SignalFilterEdit=1802;
std::wstring signalFilterText(){wchar_t text[128]{};if(signalFilterEdit)GetWindowTextW(signalFilterEdit,text,128);return text;}
HWND financialWindow = nullptr, backgroundWindow = nullptr;
HWND signalReportWindow=nullptr, signalReportList=nullptr;
void updateFinancial();
void refreshFinancialData();
size_t signalNavigationBar = SIZE_MAX, signalNavigationFirst = 0, signalNavigationCount = 0;
constexpr int chartLeft = 0, chartTop = 36;
void syncToolbar();
CsvData data;
ChartOptions options;
std::filesystem::path currentFile;
std::unique_ptr<FinanceChart> chart;
std::unique_ptr<Gdiplus::Bitmap> bitmap;
IStream* imageStream = nullptr;
HFONT font;
std::filesystem::path signalCsvPath;
std::filesystem::path chooseSignalCsvPath();
HBITMAP chartPixels = nullptr, framePixels = nullptr;
HDC frameDC = nullptr;
HGDIOBJ originalFrameBitmap = nullptr;
int frameWidth = 0, frameHeight = 0;
std::wstring lastLegendText;
bool panPending = false;
bool verticalDragging=false, verticalPending=false;int verticalStartY=0;double verticalMid=0,verticalSpan=1;
size_t pendingFirst = 0, pendingCount = 0;
double pendingExtra=0,dragExtra=0;
constexpr UINT_PTR PanTimer = 1, LegendTimer = 2;
bool legendRefreshPending=false;
HWND legendWindow = nullptr, legendText = nullptr, gotoWindow = nullptr, datePicker = nullptr, timePicker = nullptr;
SYSTEMTIME gotoSelection{};
bool gotoSelectionSet=false, syncingGoto=false;
std::vector<XYChart*> chartPanels;
POINT crossPoint{};
bool crossVisible = false;
bool testing = false;
bool restoringWorkspace=false;
bool saveWorkspace(bool saveAs=false);
bool confirmWorkspaceSave();
void newWorkspace();
void loadWorkspace(const std::filesystem::path&);
std::filesystem::path chooseWorkspace(bool);
void redraw();
void updateLegend();
void requestLegendRefresh(){
    if(legendRefreshPending)return;
    if(!testing && !IsWindowVisible(legendWindow) && !IsWindowVisible(financialWindow))return;
    legendRefreshPending=true;SetTimer(window,LegendTimer,75,nullptr);
}
void flushLegendRefresh(){KillTimer(window,LegendTimer);if(!legendRefreshPending)return;legendRefreshPending=false;updateLegend();}
void invalidateChart() {
    RECT r; GetClientRect(window, &r); r.left = chartLeft; r.top = chartTop;
    InvalidateRect(window, &r, FALSE);
}
void releaseFrame() {
    if (frameDC) { SelectObject(frameDC, originalFrameBitmap); DeleteDC(frameDC); frameDC = nullptr; }
    if (framePixels) { DeleteObject(framePixels); framePixels = nullptr; }
    frameWidth = frameHeight = 0;
}
void flushPan() {
    if(verticalPending){verticalPending=false;redraw();}
    if (!panPending) return;
    panPending = false;
    if (options.first != pendingFirst || options.count != pendingCount || options.panExtra!=pendingExtra) { options.first = pendingFirst; options.count = pendingCount;options.panExtra=pendingExtra; redraw(); }
}
bool dragging = false, zoomDragging = false;
size_t zoomStartCount = 0, zoomEnd = 0, renderedStride = 1;
int dragStartX = 0;
size_t dragFirst = 0;
double dragBarsPerPixel = 0;
bool overPlot(int x, int y) {
    return bitmap && !data.time.empty() &&
        x >= chartLeft + 65 && x < chartLeft + static_cast<int>(bitmap->GetWidth()) - 70 &&
        y >= chartTop + 32 && y < chartTop + static_cast<int>(bitmap->GetHeight()) - 35;
}
void endDrag() {
    KillTimer(window, PanTimer); panPending = false;
    bool wasDragging = dragging || zoomDragging || verticalDragging;
    verticalDragging=false;verticalPending=false;
    dragging = zoomDragging = false;
    // Native move/resize also captures the window; only release our chart-pan capture.
    if (wasDragging && GetCapture() == window) ReleaseCapture();
}
enum { Open = 100, Export, ZoomIn, ZoomOut, Latest, AddOverlay, AddText, Studies, Type, Volume, Goto, Legend };
std::wstring timestamp(double seconds) { return formatTimestamp(seconds); }
RECT panelRect(XYChart* p) {
    auto area = p->getPlotArea();
    int x = chartLeft + p->getAbsOffsetX() + area->getLeftX(), y = chartTop + p->getAbsOffsetY() + area->getTopY();
    return {x, y, x + area->getWidth(), y + area->getHeight()};
}
bool chartEndButtonRect(RECT& button){
    if(!bitmap || chartPanels.empty() || data.time.empty() || options.first>=data.time.size() || options.count>=data.time.size()-options.first)return false;
    auto plot=panelRect(chartPanels.front());
    button={plot.right-34,plot.bottom-28,plot.right-4,plot.bottom-4};
    return button.left>=plot.left && button.top>=plot.top;
}
bool overChartEndButton(POINT point){RECT button{};return chartEndButtonRect(button) && PtInRect(&button,point);}
void paintChartEndButton(HDC dc){
    RECT button{};if(!chartEndButtonRect(button))return;
    POINT mouse{};GetCursorPos(&mouse);ScreenToClient(window,&mouse);bool hot=PtInRect(&button,mouse)!=FALSE;
    int saved=SaveDC(dc);
    // Draw directly over the chart; keep the background transparent, including on hover.
    auto pen=CreatePen(PS_SOLID,2,hot?RGB(255,255,255):RGB(224,234,244));SelectObject(dc,pen);
    int x=(button.left+button.right)/2,y=(button.top+button.bottom)/2;
    MoveToEx(dc,x-8,y,nullptr);LineTo(dc,x+3,y);
    MoveToEx(dc,x-2,y-5,nullptr);LineTo(dc,x+3,y);LineTo(dc,x-2,y+5);
    MoveToEx(dc,x+7,y-6,nullptr);LineTo(dc,x+7,y+7);
    RestoreDC(dc,saved);DeleteObject(pen);
}
bool overPriceAxis(int x,int y) {
    if(!bitmap||chartPanels.empty()||data.time.empty())return false;
    auto r=panelRect(chartPanels.front());return x>r.right && x<chartLeft+static_cast<int>(bitmap->GetWidth()) && y>=r.top && y<=r.bottom;
}
void setManualScale(double midpoint,double span){
    span=std::clamp(span,std::max(1e-6,std::abs(midpoint)*1e-10),std::max(1.0,std::abs(midpoint))*1e6);
    double low=midpoint-span/2,high=midpoint+span/2;
    if(!options.autoScale && options.scaleMin==low && options.scaleMax==high)return;
    options.autoScale=false;options.scaleMin=low;options.scaleMax=high;verticalPending=true;
}
bool overTimeAxis(int x, int y) {
    if (!bitmap || chartPanels.empty() || data.time.empty()) return false;
    auto r = panelRect(chartPanels.back());
    return x >= r.left && x <= r.right && y >= r.bottom && y < chartTop + static_cast<int>(bitmap->GetHeight());
}
XYChart* hitPanel(POINT point) {
    for (auto p : chartPanels) { auto r = panelRect(p); if (PtInRect(&r, point)) return p; }
    return nullptr;
}
size_t selectedBar() {
    auto end = std::min(data.time.size(), options.first + options.count);
    if (!crossVisible || chartPanels.empty()) return signalNavigationBar < data.time.size() && signalNavigationFirst == options.first && signalNavigationCount == options.count ? signalNavigationBar : end - 1;
    auto p = chartPanels.front();
    auto x = p->getNearestXValue(crossPoint.x - chartLeft - p->getAbsOffsetX());
    return static_cast<size_t>(std::clamp((std::llround(x) + 1) * static_cast<long long>(renderedStride) - 1 + static_cast<long long>(options.first), static_cast<long long>(options.first), static_cast<long long>(end - 1)));
}
#include "Theme.h"
#include "LoadingUi.h"
#include "ThemedScrollbars.h"
#include "StudyUi.h"
#include "FinancialUi.h"
#include "BackgroundUi.h"

void refreshLoadedCalculations(LoadingIndicator& indicator){
    indicator.status(L"Calculating signals and background rules...");
    auto config=financialConfig;auto rules=backgroundRules;
    auto prepared=loadingTask([config=std::move(config),rules=std::move(rules)]()mutable{
        auto background=calculateBackground(data,rules);auto result=calculateFinancial(data,config);
        return std::make_tuple(std::move(background),std::move(result),std::move(config));
    });
    backgroundCache=std::move(std::get<0>(prepared));financialResult=std::move(std::get<1>(prepared));financialConfig=std::move(std::get<2>(prepared));
    signalNavigationBar=SIZE_MAX;options.signalStudy=financialResult.studyIndex;options.signalStyles=financialConfig.sides;
    financialRowsDirty=true;financialDisplayBar=SIZE_MAX;financialRows.clear();financialRowColors.clear();
    if(financialSummary)ListView_SetItemCountEx(financialSummary,0,0);if(financialValues)ListView_SetItemCountEx(financialValues,0,0);if(financialSides)rebuildFinancial();updateFinancial();
    indicator.status(L"Drawing chart...");
}

void paintCrosshair(HDC dc) {
    if (!crossVisible || chartPanels.empty()) return;
    auto active = hitPanel(crossPoint); if (!active) return;
    auto bar = selectedBar();
    auto pen = CreatePen(PS_DOT, 1, preferences.dark ? RGB(185, 195, 210) : RGB(65, 75, 90)); auto oldPen = SelectObject(dc, pen);
    SetBkMode(dc, TRANSPARENT);
    int x = chartLeft + active->getAbsOffsetX() + active->getXCoor(static_cast<double>((bar - options.first) / renderedStride));
    for (auto p : chartPanels) { auto r = panelRect(p); MoveToEx(dc, x, r.top, nullptr); LineTo(dc, x, r.bottom); }
    auto r = panelRect(active); MoveToEx(dc, r.left, crossPoint.y, nullptr); LineTo(dc, r.right, crossPoint.y);
    SelectObject(dc, oldPen); DeleteObject(pen);
    SetBkMode(dc, OPAQUE);
    SetBkColor(dc, preferences.dark ? RGB(176, 190, 208) : RGB(45, 55, 70));
    SetTextColor(dc, preferences.dark ? RGB(18, 25, 34) : RGB(255, 255, 255));
    auto oldFont = SelectObject(dc, font);
    wchar_t value[64]; swprintf_s(value, L" %.2f ", active->getYValue(crossPoint.y - chartTop - active->getAbsOffsetY()));
    TextOutW(dc, r.right + 1, crossPoint.y - 8, value, static_cast<int>(wcslen(value)));
    auto label = timestamp(data.time[bar]);
    SIZE size; GetTextExtentPoint32W(dc, label.c_str(), static_cast<int>(label.size()), &size);
    auto bottom = panelRect(chartPanels.back());
    TextOutW(dc, std::clamp(x - size.cx / 2, bottom.left, std::max(bottom.left, bottom.right - size.cx)), bottom.bottom + 3, label.c_str(), static_cast<int>(label.size()));
    SelectObject(dc, oldFont);
}
#include "GotoInput.h"
LRESULT CALLBACK auxiliaryProc(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp) {
    if (msg == WM_CLOSE) { if (hwnd == legendWindow) closeCellEditor(); ShowWindow(hwnd, SW_HIDE); if (hwnd == legendWindow) { preferences.legend = false; saveAppPreferences(); } return 0; }
    if (msg >= WM_CTLCOLORMSGBOX && msg <= WM_CTLCOLORSTATIC) {
        auto brush = themedControl(wp);
        if (reinterpret_cast<HWND>(lp) == legendPrices) SetTextColor(reinterpret_cast<HDC>(wp), headerPriceColor);
        return brush;
    }
    if (hwnd == legendWindow && msg == WM_NOTIFY) {
        auto nm = reinterpret_cast<NMHDR*>(lp);
        if (nm->idFrom == LegendStudies && nm->code == NM_CUSTOMDRAW) return drawLegend(reinterpret_cast<NMLVCUSTOMDRAW*>(lp));
    }
    if (hwnd == legendWindow) {
        try { if (handleLegend(msg, wp, lp)) return 0; }
        catch (const std::exception& e) { MessageBoxA(hwnd, e.what(), "FIGCharts", MB_ICONERROR); return 0; }
    }
    if(msg==WM_NOTIFY && hwnd==gotoWindow && !syncingGoto) {
        auto nm=reinterpret_cast<NMHDR*>(lp);
        if(nm->code==DTN_DATETIMECHANGE && (nm->hwndFrom==datePicker || nm->hwndFrom==timePicker)) {
            SYSTEMTIME date{},time{};
            if(DateTime_GetSystemtime(datePicker,&date)==GDT_VALID && DateTime_GetSystemtime(timePicker,&time)==GDT_VALID) {
                date.wHour=time.wHour;date.wMinute=time.wMinute;date.wSecond=time.wSecond;date.wMilliseconds=0;
                setGotoInputs(date);
            }
        }
    }
    if (msg == WM_COMMAND && hwnd == gotoWindow) {
        if(LOWORD(wp)>=GotoYear && LOWORD(wp)<GotoYear+5 && HIWORD(wp)==EN_CHANGE){gotoInputChanged(LOWORD(wp)-GotoYear);return 0;}
        if (LOWORD(wp) == IDCANCEL) { ShowWindow(hwnd, SW_HIDE); return 0; }
        if (LOWORD(wp) == IDOK && !data.time.empty()) {
            SYSTEMTIME st{};
            if(!readGotoInputs(st)){MessageBoxW(hwnd,L"Enter a valid year, month, day, hour and minute.",L"Go to",MB_OK|MB_ICONINFORMATION);return 0;}
            gotoSelection=st;gotoSelectionSet=true;
            FILETIME ft{};if(!SystemTimeToFileTime(&st,&ft))return 0;
            ULARGE_INTEGER ticks; ticks.LowPart = ft.dwLowDateTime; ticks.HighPart = ft.dwHighDateTime;
            double t;
            try { t = displayToUtc(st); } catch (const std::exception& e) { MessageBoxA(hwnd, e.what(), "Go to", MB_ICONINFORMATION); return 0; }
            if (t < data.time.front() || t > data.time.back()) {
                auto message = L"Choose a time within the loaded file:\n" + timestamp(data.time.front()) + L" through " + timestamp(data.time.back()) + L"";
                MessageBoxW(hwnd, message.c_str(), L"Outside loaded history", MB_ICONINFORMATION); return 0;
            }
            auto it = std::lower_bound(data.time.begin(), data.time.end(), t);
            size_t index = static_cast<size_t>(it - data.time.begin());
            options.panExtra=0;pendingExtra=0;options.first = std::min(index > options.count / 2 ? index - options.count / 2 : 0, data.time.size() > options.count ? data.time.size() - options.count : 0);
            crossVisible = false; redraw(); ShowWindow(hwnd, SW_HIDE); SetForegroundWindow(window); return 0;
        }
    }
    return DefWindowProcW(hwnd, msg, wp, lp);
}
void showGoto() {
    if (data.time.empty()) { MessageBoxW(window, L"Open a data file first.", L"Go to date/time", MB_OK); return; }
    if (!gotoWindow) {
        gotoWindow = CreateWindowExW(WS_EX_DLGMODALFRAME, L"FIGChartsAux", L"Go to date/time", WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU,
            CW_USEDEFAULT, CW_USEDEFAULT, 460, 195, window, nullptr, GetModuleHandle(nullptr), nullptr);
        auto label = CreateWindowW(L"STATIC", L"Date/time in selected zone; gaps use next bar", WS_CHILD | WS_VISIBLE, 15, 8, 420, 20, gotoWindow, nullptr, nullptr, nullptr);
        datePicker = CreateWindowW(DATETIMEPICK_CLASSW, L"", WS_CHILD | WS_VISIBLE | WS_TABSTOP | DTS_SHORTDATEFORMAT, 335, 48, 90, 28, gotoWindow, nullptr, nullptr, nullptr);
        SendMessageW(datePicker, DTM_SETFORMATW, 0, reinterpret_cast<LPARAM>(L"'Calendar'"));
        // Native controls retain calendar/date notifications; keyboard entry uses explicit fields.
        timePicker = CreateWindowW(DATETIMEPICK_CLASSW,L"",WS_CHILD|DTS_TIMEFORMAT,0,0,0,0,gotoWindow,nullptr,nullptr,nullptr);
        SendMessageW(timePicker,DTM_SETFORMATW,0,reinterpret_cast<LPARAM>(L"HH:mm"));
        const wchar_t* names[]{L"Year",L"Month",L"Day",L"Hour",L"Minute"};int positions[]{15,90,145,215,270};
        for(int i=0;i<5;++i){auto fieldLabel=CreateWindowW(L"STATIC",names[i],WS_CHILD|WS_VISIBLE,positions[i],32,60,16,gotoWindow,nullptr,nullptr,nullptr);SendMessage(fieldLabel,WM_SETFONT,reinterpret_cast<WPARAM>(font),TRUE);
            auto edit=CreateWindowExW(WS_EX_CLIENTEDGE,L"EDIT",L"",WS_CHILD|WS_VISIBLE|WS_TABSTOP|ES_NUMBER|ES_CENTER,positions[i],50,i==0?65:45,26,gotoWindow,reinterpret_cast<HMENU>(static_cast<INT_PTR>(GotoYear+i)),nullptr,nullptr);SendMessage(edit,WM_SETFONT,reinterpret_cast<WPARAM>(font),TRUE);SendMessage(edit,EM_SETLIMITTEXT,i==0?4:2,0);SetWindowSubclass(edit,gotoInputProc,1,i);}
        auto go = CreateWindowW(L"BUTTON", L"Go", WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_DEFPUSHBUTTON, 235, 95, 90, 28, gotoWindow, reinterpret_cast<HMENU>(IDOK), nullptr, nullptr);
        auto cancel = CreateWindowW(L"BUTTON", L"Cancel", WS_CHILD | WS_VISIBLE | WS_TABSTOP, 335, 95, 90, 28, gotoWindow, reinterpret_cast<HMENU>(IDCANCEL), nullptr, nullptr);
        for (auto h : {label, datePicker, timePicker, go, cancel}) SendMessage(h, WM_SETFONT, reinterpret_cast<WPARAM>(font), TRUE);
    }
    if(!gotoSelectionSet){gotoSelection=displaySystemTime(data.time[selectedBar()]);gotoSelectionSet=true;}
    syncingGoto=true;
    setGotoInputs(gotoSelection);
    syncingGoto=false;
    SetWindowTextW(gotoWindow, (L"Go to: " + zoneLabel()).c_str());
    applyTheme();
    if (!testing) { ShowWindow(gotoWindow, SW_SHOW); SetForegroundWindow(gotoWindow); SetFocus(GetDlgItem(gotoWindow,GotoYear)); }
}
void error(const std::exception& e) { MessageBoxA(window, e.what(), "FIGCharts", MB_ICONERROR); }
void clearImage() {
    if (chartPixels) { DeleteObject(chartPixels); chartPixels = nullptr; }
    bitmap.reset(); if (imageStream) { imageStream->Release(); imageStream = nullptr; }
}

void redraw() {
    if (data.time.empty()) return;
    RECT r; GetClientRect(window, &r);

    options.type = static_cast<ChartType>(preferences.chartType);
    options.backgroundColors.clear();
    if(backgroundCache.size()==data.time.size()) options.backgroundColors.assign(backgroundCache.begin()+options.first,backgroundCache.begin()+std::min(data.time.size(),options.first+options.count));
    options.signalLabels.clear();
    if(options.signalStudy>=0) {
        const auto& closes=financialResult.closedSignals;
        auto it=std::lower_bound(closes.begin(),closes.end(),options.first,[](const auto& mark,size_t bar){return mark.bar<bar;});
        size_t end=std::min(data.time.size(),options.first+options.count);
        for(;it!=closes.end() && it->bar<end;++it) options.signalLabels.push_back(*it);
    }
    renderedStride = displayStride(std::min(options.count, data.time.size() - options.first), std::max(300L, r.right));
    chart = render(data, options, std::max(300L, r.right), std::max(230L, r.bottom - chartTop - 24), &chartPanels);
    // Live frames need no compression: BMP avoids PNG encoding/decoding on every pan.
    auto frame = chart->makeChart(Chart::BMP);
    clearImage(); imageStream = SHCreateMemStream(reinterpret_cast<const BYTE*>(frame.data), frame.len);
    if (!imageStream) throw std::runtime_error("Could not allocate chart image.");
    bitmap.reset(Gdiplus::Bitmap::FromStream(imageStream));
    if (bitmap->GetLastStatus() != Gdiplus::Ok) throw std::runtime_error("Could not display chart image.");
    if (bitmap->GetHBITMAP(Gdiplus::Color(255, 255, 255), &chartPixels) != Gdiplus::Ok)
        throw std::runtime_error("Could not cache chart pixels.");
    SCROLLINFO si{sizeof(si), SIF_RANGE | SIF_PAGE | SIF_POS};
    si.nMax = static_cast<int>(data.time.size() - 1); si.nPage = static_cast<UINT>(std::min(options.count, data.time.size())); si.nPos = static_cast<int>(options.first);
    SetScrollInfo(window, SB_HORZ, &si, TRUE);
    auto info = std::to_wstring(data.time.size()) + L" bars | viewing " + std::to_wstring(options.first + 1) + L" - " + std::to_wstring(std::min(data.time.size(), options.first + options.count)) + L" | " + zoneLabel();
    SetWindowTextW(statusLabel, info.c_str()); syncToolbar(); invalidateChart();
    if(testing)updateLegend();else requestLegendRefresh();
}
void load(const std::filesystem::path& path, bool readDisk=true) {
    LoadingIndicator indicator(L"Reading data file...\n"+path.filename().wstring());
    auto loaded = loadingTask([path]{return CsvData::load(path);});
    indicator.status(L"Preparing chart and studies...");
    data = std::move(loaded); currentFile = path; signalNavigationBar=SIZE_MAX;options.panExtra=0;pendingExtra=0;
    crossVisible = false;
    options.first = data.time.size() > options.count ? data.time.size() - options.count : 0;
    restoreStudies(readDisk);
    backgroundPreview=false;refreshLoadedCalculations(indicator);if(backgroundWindow)ShowWindow(backgroundWindow,SW_HIDE);
    if(financialWindow)fillFinancialControls();
    auto title = L"FIGCharts - " + path.filename().wstring(); SetWindowTextW(window, title.c_str()); redraw();
    if(!testing && preferences.legend && IsWindowVisible(window)) showLegend();
    if(!testing && preferences.financialLegend && IsWindowVisible(window)) showFinancial();
}
std::filesystem::path choose(bool save, DWORD filter = 1) {
    wchar_t path[32768] = {};
    OPENFILENAMEW ofn{sizeof(ofn)}; ofn.hwndOwner = window; ofn.lpstrFile = path; ofn.nMaxFile = 32768;
    ofn.lpstrFilter = save ? L"PNG image\0*.png\0\0" : L"FIGSignalCpp data\0*.figdata;*.raw;*.bin;*.csv\0FIGDATA binary\0*.figdata;*.raw;*.bin\0CSV\0*.csv\0All files\0*.*\0\0";
    ofn.nFilterIndex = filter; ofn.lpstrDefExt = save ? L"png" : nullptr;
    ofn.Flags = OFN_NOCHANGEDIR | OFN_PATHMUSTEXIST | (save ? OFN_OVERWRITEPROMPT : OFN_FILEMUSTEXIST);
    return (save ? GetSaveFileNameW(&ofn) : GetOpenFileNameW(&ofn)) ? path : L"";
}
void savePng(const std::filesystem::path& path, FinanceChart& c) {
    auto png = c.makeChart(Chart::PNG); std::ofstream out(path, std::ios::binary);
    out.write(png.data, png.len); if (!out) throw std::runtime_error("Could not save PNG.");
}
HWND control(const wchar_t* type, const wchar_t* label, int id, int x, int y, int w, int h, DWORD style = 0) {
    auto child = CreateWindowW(type, label, WS_CHILD | WS_VISIBLE | style, x, y, w, h, window, reinterpret_cast<HMENU>(static_cast<INT_PTR>(id)), nullptr, nullptr);
    SendMessage(child, WM_SETFONT, reinterpret_cast<WPARAM>(font), TRUE); return child;
}
#include "AppUi.h"
#include "Workspace.h"

struct ToolbarAction { int id; const wchar_t* glyph; const wchar_t* tip; };
const ToolbarAction toolbarActions[]{
    {OpenCsv,L"CSV",L"Open CSV"},{OpenBinary,L"BIN",L"Open binary"},{OpenDatabase,L"",L"Get prices from database"},{Refresh,L"",L"Refresh data"},{0,L"",L""},
    {LoadWorkspaceMenu,L"",L"Open workspace"},{SaveWorkspaceMenu,L"",L"Save workspace"},{0,L"",L""},
    {ZoomIn,L"+",L"Zoom in"},{ZoomOut,L"",L"Zoom out"},{0,L"",L""},
    {Latest,L"",L"Go to end"},{Goto,L"",L"Go to date/time"},{PreviousSignal,L"",L"Go to previous signal"},{NextSignal,L"",L"Go to next signal"},{SignalFilterSlot,L"",L"SIDE filter"},{0,L"",L""},
    {Legend,L"",L"Legend"},{AddFinancial,L"",L"Financial legend"},{SignalReport,L"",L"Signal performance report"},{0,L"",L""},{BackgroundRulesMenu,L"",L"Open background color rules dialog"},{Settings,L"",L"Settings"}
};
void syncToolbar() {
    if (!toolbar) return;
    // Chart frames change continuously during dragging; button states usually do not.
    auto update=[&](int id,UINT message,int flag,bool value){auto state=SendMessage(toolbar,TB_GETSTATE,id,0);if(state!=-1 && ((state&flag)!=0)!=value)SendMessage(toolbar,message,id,MAKELONG(value,0));};
    update(Legend,TB_CHECKBUTTON,TBSTATE_CHECKED,!data.time.empty() && preferences.legend);
    update(SignalReport,TB_CHECKBUTTON,TBSTATE_CHECKED,IsWindowVisible(signalReportWindow)!=FALSE);
    update(SignalReport,TB_ENABLEBUTTON,TBSTATE_ENABLED,!financialResult.signals.empty());
    update(AddFinancial,TB_CHECKBUTTON,TBSTATE_CHECKED,!data.time.empty() && preferences.financialLegend);
    for(int id:std::initializer_list<int>{ZoomIn,ZoomOut,Latest,Goto,AddFinancial,Legend,Refresh,BackgroundRulesMenu})update(id,TB_ENABLEBUTTON,TBSTATE_ENABLED,!data.time.empty());
    for(int id:{NextSignal,PreviousSignal})update(id,TB_ENABLEBUTTON,TBSTATE_ENABLED,!financialResult.signals.empty());
}
LRESULT CALLBACK signalFilterFrameProc(HWND hwnd,UINT msg,WPARAM wp,LPARAM lp,UINT_PTR,DWORD_PTR){
    if(msg==WM_PAINT || msg==WM_PRINTCLIENT){PAINTSTRUCT ps{};auto dc=msg==WM_PAINT?BeginPaint(hwnd,&ps):reinterpret_cast<HDC>(wp);
        RECT r{};GetClientRect(hwnd,&r);FillRect(dc,&r,appBackground());auto brush=CreateSolidBrush(preferences.dark?RGB(92,96,102):RGB(150,154,160));FrameRect(dc,&r,brush);DeleteObject(brush);
        if(msg==WM_PAINT)EndPaint(hwnd,&ps);return 0;}
    return DefSubclassProc(hwnd,msg,wp,lp);
}
void layoutSignalFilter(){
    if(!signalFilterFrame)return;RECT r{};SendMessage(toolbar,TB_GETRECT,SignalFilterSlot,reinterpret_cast<LPARAM>(&r));
    MoveWindow(signalFilterFrame,r.left+3,3,134,26,TRUE);
    MoveWindow(signalFilterEdit,r.left+8,8,103,17,TRUE);
    MoveWindow(signalFilterClear,r.left+113,5,22,22,TRUE);
    // These are siblings of the toolbar. Keep the frame above it and the input/button above the frame.
    for(HWND control:{signalFilterFrame,signalFilterEdit,signalFilterClear})SetWindowPos(control,HWND_TOP,0,0,0,0,SWP_NOMOVE|SWP_NOSIZE|SWP_NOACTIVATE);
}
void createToolbar() {
    toolbar = CreateWindowExW(0, TOOLBARCLASSNAMEW, L"Chart toolbar", WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS | TBSTYLE_FLAT | TBSTYLE_TOOLTIPS | CCS_NORESIZE | CCS_NOPARENTALIGN | CCS_NODIVIDER,
        0,0,600,chartTop,window,nullptr,GetModuleHandle(nullptr),nullptr);
    SendMessage(toolbar, TB_BUTTONSTRUCTSIZE, sizeof(TBBUTTON), 0);
    SendMessage(toolbar, TB_SETUNICODEFORMAT, TRUE, 0);
    SendMessage(toolbar, TB_SETMAXTEXTROWS, 0, 0);
    SendMessage(toolbar, TB_SETBUTTONSIZE,0,MAKELONG(36,30));
    for (const auto& a : toolbarActions) {
        TBBUTTON b{}; b.idCommand = a.id; b.fsState = TBSTATE_ENABLED;
        b.fsStyle = (!a.id || a.id==SignalFilterSlot) ? BTNS_SEP : (a.id==AddFinancial || a.id==SignalReport) ? BTNS_CHECK : BTNS_BUTTON; b.iBitmap = a.id==SignalFilterSlot?140:a.id ? I_IMAGENONE : 12;
        b.iString = reinterpret_cast<INT_PTR>(a.tip); SendMessage(toolbar,TB_ADDBUTTONSW,1,reinterpret_cast<LPARAM>(&b));
    }
    SendMessage(toolbar, TB_SETBUTTONSIZE,0,MAKELONG(36,30));
    signalFilterFrame=CreateWindowW(L"STATIC",L"",WS_CHILD|WS_VISIBLE|WS_CLIPSIBLINGS,0,0,0,0,window,nullptr,GetModuleHandle(nullptr),nullptr);
    SetWindowSubclass(signalFilterFrame,signalFilterFrameProc,11,0);
    signalFilterEdit=CreateWindowW(L"EDIT",L"",WS_CHILD|WS_VISIBLE|WS_TABSTOP|ES_AUTOHSCROLL,0,0,0,0,window,reinterpret_cast<HMENU>(static_cast<INT_PTR>(SignalFilterEdit)),GetModuleHandle(nullptr),nullptr);
    signalFilterClear=CreateWindowW(L"BUTTON",L"Clear SIDE filter",WS_CHILD|WS_VISIBLE|WS_TABSTOP,0,0,0,0,window,reinterpret_cast<HMENU>(static_cast<INT_PTR>(SignalFilterClear)),GetModuleHandle(nullptr),nullptr);
    SetPropW(signalFilterClear,L"FIGCharts.ClearSearch",reinterpret_cast<HANDLE>(1));SetWindowSubclass(signalFilterClear,themedButtonProc,10,0);
    SendMessage(signalFilterEdit,WM_SETFONT,reinterpret_cast<WPARAM>(font),TRUE);SendMessage(signalFilterEdit,EM_SETLIMITTEXT,100,0);
    SendMessage(signalFilterEdit,EM_SETCUEBANNER,FALSE,reinterpret_cast<LPARAM>(L"SIDE filter"));layoutSignalFilter();syncToolbar();
}
struct ToolbarPng {
    IStream* stream=nullptr;
    std::unique_ptr<Gdiplus::Bitmap> image;
    ~ToolbarPng(){image.reset();if(stream)stream->Release();}
};
std::map<int,std::unique_ptr<ToolbarPng>> toolbarPngs;
Gdiplus::Bitmap* toolbarPng(int id) {
    const int actions[]{OpenCsv,OpenBinary,ZoomIn,ZoomOut,Latest,Goto,Legend,AddFinancial,Settings,Refresh,SignalReport};
    auto found=std::find(std::begin(actions),std::end(actions),id);if(found==std::end(actions)&&id!=OpenDatabase)return nullptr;
    auto cached=toolbarPngs.find(id);if(cached!=toolbarPngs.end())return cached->second->image.get();
    auto module=GetModuleHandle(nullptr);auto resource=FindResourceW(module,MAKEINTRESOURCEW(id==OpenDatabase?2017:2001+static_cast<int>(found-std::begin(actions))),RT_RCDATA);
    if(!resource)return nullptr;auto loaded=LoadResource(module,resource);auto bytes=static_cast<const BYTE*>(LockResource(loaded));
    auto icon=std::make_unique<ToolbarPng>();icon->stream=SHCreateMemStream(bytes,SizeofResource(module,resource));if(!icon->stream)return nullptr;
    icon->image.reset(Gdiplus::Bitmap::FromStream(icon->stream));if(icon->image->GetLastStatus()!=Gdiplus::Ok)return nullptr;
    auto image=icon->image.get();toolbarPngs[id]=std::move(icon);return image;
}
LRESULT toolbarDraw(NMCUSTOMDRAW* d) {
    if (d->dwDrawStage == CDDS_PREPAINT) { FillRect(d->hdc,&d->rc,appBackground()); return CDRF_NOTIFYITEMDRAW | CDRF_NOTIFYPOSTPAINT; }
    if (d->dwDrawStage == CDDS_POSTPAINT) {
        RECT filterSlot{};SendMessage(toolbar,TB_GETRECT,SignalFilterSlot,reinterpret_cast<LPARAM>(&filterSlot));FillRect(d->hdc,&filterSlot,appBackground());
        if(!preferences.dark)return CDRF_DODEFAULT;
        // Native toolbar separators use bright system edges; paint a muted single line.
        auto pen=CreatePen(PS_SOLID,1,RGB(43,53,66));auto old=SelectObject(d->hdc,pen);
        int count=static_cast<int>(SendMessage(toolbar,TB_BUTTONCOUNT,0,0));
        for(int i=0;i<count;++i) {TBBUTTON button{};SendMessage(toolbar,TB_GETBUTTON,i,reinterpret_cast<LPARAM>(&button));
            if(!(button.fsStyle&BTNS_SEP) || button.idCommand==SignalFilterSlot)continue;
            RECT r{};SendMessage(toolbar,TB_GETITEMRECT,i,reinterpret_cast<LPARAM>(&r));FillRect(d->hdc,&r,appBackground());
            int x=(r.left+r.right)/2;MoveToEx(d->hdc,x,r.top+7,nullptr);LineTo(d->hdc,x,r.bottom-7);
        }
        SelectObject(d->hdc,old);DeleteObject(pen);return CDRF_DODEFAULT;
    }
    if (d->dwDrawStage != CDDS_ITEMPREPAINT) return CDRF_DODEFAULT;
    int saved = SaveDC(d->hdc); RECT r = d->rc;
    bool active = (d->uItemState & (CDIS_HOT | CDIS_SELECTED | CDIS_CHECKED)) != 0;
    auto brush = CreateSolidBrush(active ? (preferences.dark ? RGB(55,69,87) : RGB(215,226,240)) : appBackgroundColor()); FillRect(d->hdc,&r,brush); DeleteObject(brush);
    auto dc = d->hdc; int id = static_cast<int>(d->dwItemSpec);
    COLORREF color = (d->uItemState & CDIS_DISABLED) ? RGB(110,120,132) : appTextColor();
    if(auto icon=toolbarPng(id)) {
        // Preserve the supplied silhouette/alpha; tint black artwork for both themes.
        Gdiplus::ColorMatrix tint{};tint.m[3][3]=1;tint.m[4][4]=1;
        tint.m[4][0]=GetRValue(color)/255.0f;tint.m[4][1]=GetGValue(color)/255.0f;tint.m[4][2]=GetBValue(color)/255.0f;
        Gdiplus::ImageAttributes attributes;attributes.SetColorMatrix(&tint);
        Gdiplus::Graphics graphics(dc);graphics.SetInterpolationMode(Gdiplus::InterpolationModeHighQualityBicubic);graphics.SetPixelOffsetMode(Gdiplus::PixelOffsetModeHalf);
        Gdiplus::Rect target((r.left+r.right-24)/2,(r.top+r.bottom-24)/2,24,24);
        graphics.DrawImage(icon,target,0,0,icon->GetWidth(),icon->GetHeight(),Gdiplus::UnitPixel,&attributes);
        RestoreDC(d->hdc,saved);return CDRF_SKIPDEFAULT;
    }
    auto pen = CreatePen(PS_SOLID, 2, color); SelectObject(dc, pen); SelectObject(dc, GetStockObject(NULL_BRUSH));
    int x = (r.left + r.right)/2, y = (r.top + r.bottom)/2;
    SetMapMode(dc, MM_ANISOTROPIC); SetWindowExtEx(dc, 5, 5, nullptr); SetViewportExtEx(dc, 4, 4, nullptr);
    SetWindowOrgEx(dc, x, y, nullptr); SetViewportOrgEx(dc, x, y, nullptr);
    auto line = [&](int x1,int y1,int x2,int y2) { MoveToEx(dc,x+x1,y+y1,nullptr); LineTo(dc,x+x2,y+y2); };
    if (id == OpenCsv || id == OpenBinary) {
        Rectangle(dc,x-15,y-10,x+16,y+11); line(-15,-10,-15,-14); line(-15,-14,-3,-14); line(-3,-14,1,-10);
        SetBkMode(dc,TRANSPARENT); SetTextColor(dc,color); SelectObject(dc,font);
        RECT text{x-14,y-9,x+15,y+10}; DrawTextW(dc,id == OpenCsv ? L"CSV" : L"BIN",-1,&text,DT_CENTER|DT_VCENTER|DT_SINGLELINE);
    } else if(id==LoadWorkspaceMenu){
        // Open folder with a small workspace grid.
        line(-13,10,-13,-10);line(-13,-10,-4,-10);line(-4,-10,0,-6);line(0,-6,12,-6);
        line(-13,10,10,10);line(10,10,14,-2);line(14,-2,-9,-2);line(-9,-2,-13,10);
        line(-5,2,6,2);line(-5,6,6,6);line(0,2,0,6);
    } else if(id==SaveWorkspaceMenu){
        // Floppy disk: familiar Save action, drawn with the current theme color.
        line(-12,-12,7,-12);line(7,-12,12,-7);line(12,-7,12,12);line(12,12,-12,12);line(-12,12,-12,-12);
        Rectangle(dc,x-7,y-12,x+6,y-3);line(2,-10,2,-5);Rectangle(dc,x-7,y+2,x+8,y+12);
    } else if (id == ZoomIn || id == ZoomOut) {
        Ellipse(dc,x-11,y-11,x+7,y+7); line(5,5,12,12); line(-7,-2,3,-2); if (id == ZoomIn) line(-2,-7,-2,3);
    } else if (id == Latest) {
        line(-12,0,7,0); line(0,-7,7,0); line(7,0,0,7); line(11,-10,11,11);
    } else if (id == Goto) {
        Rectangle(dc,x-11,y-9,x+12,y+12); line(-11,-3,12,-3); line(-6,-13,-6,-6); line(6,-13,6,-6);
        for (int row : {2,7}) for (int col : {-6,0,6}) Rectangle(dc,x+col-1,y+row-1,x+col+1,y+row+1);
    } else if (id == Legend) {
        for (int row : {-8,0,8}) { Rectangle(dc,x-12,y+row-2,x-8,y+row+2); line(-4,row,12,row); }
    } else if (id == Refresh) {
        Arc(dc,x-11,y-11,x+12,y+12,x+10,y-6,x-9,y-6);line(10,-6,4,-7);line(10,-6,10,-12);
    } else if (id == NextSignal || id == PreviousSignal) {
        int direction=id==NextSignal?1:-1;
        line(-11*direction,0,9*direction,0);line(2*direction,-7,9*direction,0);line(9*direction,0,2*direction,7);
        line(-9*direction,-10,-9*direction,-5);line(-4*direction,-13,-4*direction,-5);
    } else if (id == AddFinancial) {
        Rectangle(dc,x-12,y-13,x+13,y+14);line(-7,-8,7,-8);line(-7,-3,7,-3);
        line(-6,9,-6,4);line(0,9,0,1);line(6,9,6,5);
    } else if (id == Volume) {
        Rectangle(dc,x-11,y+2,x-6,y+11); Rectangle(dc,x-2,y-5,x+3,y+11); Rectangle(dc,x+7,y-12,x+12,y+11);
    } else if (id == BackgroundRulesMenu) {
        // Colored background bands with a chart line.
        const COLORREF bands[]{RGB(54,106,79),RGB(126,68,73),RGB(67,89,136)};
        for(int i=0;i<3;++i){RECT band{x-12+i*8,y-11,x-4+i*8,y+12};auto fill=CreateSolidBrush((d->uItemState&CDIS_DISABLED)?RGB(90,96,104):bands[i]);FillRect(dc,&band,fill);DeleteObject(fill);}
        Rectangle(dc,x-12,y-11,x+13,y+13);line(-9,6,-3,-2);line(-3,-2,3,2);line(3,2,10,-7);
    } else if(id==NewWorkspaceMenu){
        Rectangle(dc,x-9,y-12,x+10,y+13);line(-5,-6,5,-6);line(0,-10,0,-2);
    } else if(id==CopyImage || id==Export){
        if(id==CopyImage)Rectangle(dc,x-13,y-12,x+6,y+7);
        Rectangle(dc,x-8,y-7,x+13,y+13);line(-5,9,1,1);line(1,1,5,6);line(5,6,9,2);Ellipse(dc,x+5,y-4,x+9,y);
    } else if(id==ZoomAll){
        Rectangle(dc,x-11,y-11,x+12,y+12);line(-7,0,7,0);line(0,-7,0,7);line(-7,0,-3,-4);line(-7,0,-3,4);line(7,0,3,-4);line(7,0,3,4);
    } else if(id==ExitApp){
        line(-2,-12,-12,-12);line(-12,-12,-12,12);line(-12,12,-2,12);line(-5,0,12,0);line(12,0,5,-7);line(12,0,5,7);
    } else if(id==About){
        Ellipse(dc,x-12,y-12,x+13,y+13);line(0,-1,0,8);line(-3,8,4,8);Ellipse(dc,x-1,y-7,x+2,y-4);
    } else if (id == Settings) {
        POINT teeth[32];
        for(int i=0;i<32;++i) {double angle=(i-0.5)*3.141592653589793/16;double radius=(i%4==1 || i%4==2)?14:10;teeth[i]={x+static_cast<LONG>(std::lround(radius*std::cos(angle))),y+static_cast<LONG>(std::lround(radius*std::sin(angle)))};}
        auto fill=CreateSolidBrush(color);SelectObject(dc,fill);Polygon(dc,teeth,32);
        auto hole=CreateSolidBrush(active?(preferences.dark?RGB(55,69,87):RGB(215,226,240)):appBackgroundColor());SelectObject(dc,hole);Ellipse(dc,x-5,y-5,x+6,y+6);
        SelectObject(dc,GetStockObject(NULL_BRUSH));DeleteObject(fill);DeleteObject(hole);
    }
    SelectObject(dc, GetStockObject(BLACK_PEN)); DeleteObject(pen);
    RestoreDC(d->hdc,saved); return CDRF_SKIPDEFAULT;
}

void drawMenuIcon(HDC dc,RECT target,UINT command,bool submenu,bool selected,bool disabled){
    int id=static_cast<int>(command);
    if(submenu || id==Open)id=LoadWorkspaceMenu;
    if(id==SaveWorkspaceAsMenu)id=SaveWorkspaceMenu;
    if(id==AddStudiesMenu || id==EnableLegend || id==DisableLegend)id=Legend;
    auto memory=CreateCompatibleDC(dc);auto pixels=CreateCompatibleBitmap(dc,36,32);auto old=SelectObject(memory,pixels);
    NMCUSTOMDRAW draw{};draw.hdc=memory;draw.dwDrawStage=CDDS_ITEMPREPAINT;draw.dwItemSpec=id;draw.rc={0,0,36,32};draw.uItemState=(selected?CDIS_SELECTED:0)|(disabled?CDIS_DISABLED:0);toolbarDraw(&draw);
    int saved=SaveDC(dc);SetStretchBltMode(dc,HALFTONE);SetBrushOrgEx(dc,0,0,nullptr);StretchBlt(dc,target.left,target.top,target.right-target.left,target.bottom-target.top,memory,2,0,32,32,SRCCOPY);RestoreDC(dc,saved);
    SelectObject(memory,old);DeleteObject(pixels);DeleteDC(memory);
}
LRESULT CALLBACK proc(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp) {
    try {
        if (msg >= WM_CTLCOLORMSGBOX && msg <= WM_CTLCOLORSTATIC) return themedControl(wp);
        switch (msg) {
        case WM_NCPAINT:
        case WM_NCACTIVATE: {auto result=DefWindowProcW(hwnd,msg,wp,lp);paintMenuRemainder(hwnd);return result;}
        case WM_MEASUREITEM: if(reinterpret_cast<MEASUREITEMSTRUCT*>(lp)->CtlType==ODT_MENU){measureThemedMenu(reinterpret_cast<MEASUREITEMSTRUCT*>(lp));return TRUE;}break;
        case WM_DRAWITEM: if(reinterpret_cast<DRAWITEMSTRUCT*>(lp)->CtlType==ODT_MENU){drawThemedMenu(reinterpret_cast<DRAWITEMSTRUCT*>(lp));return TRUE;}break;
        case WM_MENUCHAR: return themedMenuChar(wp,lp);
        case WM_CREATE: {
            window = hwnd; font = static_cast<HFONT>(GetStockObject(DEFAULT_GUI_FONT));
            createToolbar();
            statusLabel = control(L"STATIC", L"Open a data file to begin", 0, 8, 0, 650, 20);
            SetMenu(hwnd, buildMenu()); applyTheme(); DragAcceptFiles(hwnd, TRUE); return 0;
        }
        case WM_NOTIFY: {
            auto nm = reinterpret_cast<NMHDR*>(lp);
            if (nm->hwndFrom == toolbar && nm->code == NM_CUSTOMDRAW) return toolbarDraw(reinterpret_cast<NMCUSTOMDRAW*>(lp));
            if (nm->hwndFrom == toolbar && nm->code == TBN_GETINFOTIPA) {
                auto tip=reinterpret_cast<NMTBGETINFOTIPA*>(lp);
                for(const auto& a:toolbarActions) if(a.id==tip->iItem) WideCharToMultiByte(CP_ACP,0,a.tip,-1,tip->pszText,tip->cchTextMax,nullptr,nullptr);
                return 0;
            }
            if (nm->hwndFrom == toolbar && nm->code == TBN_GETINFOTIPW) {
                auto tip = reinterpret_cast<NMTBGETINFOTIPW*>(lp);
                for (const auto& a : toolbarActions) if (a.id == tip->iItem) wcsncpy_s(tip->pszText, tip->cchTextMax, a.tip, _TRUNCATE);
                return 0;
            }
            break;
        }
        case WM_INITMENUPOPUP: refreshMenus(); return 0;
        case WM_GETMINMAXINFO: reinterpret_cast<MINMAXINFO*>(lp)->ptMinTrackSize = {600, 420}; return 0;
        case WM_SIZE:
            endDrag();
            crossVisible = false;
            if (wp == SIZE_MINIMIZED) return 0;
            MoveWindow(toolbar, 0, 0, LOWORD(lp), chartTop, TRUE);layoutSignalFilter();
            MoveWindow(statusLabel, 8, std::max(0, int(HIWORD(lp)) - 22), std::max(0, int(LOWORD(lp)) - 16), 20, TRUE);
            redraw(); return 0;
        case WM_COMMAND: {
            if(loadingFile)return 0;
            endDrag();
            int id = LOWORD(wp);
            if(id==SignalFilterClear){SetWindowTextW(signalFilterEdit,L"");SetFocus(signalFilterEdit);return 0;}
            if(id==SignalFilterEdit){return 0;}
            if(id!=NextSignal && id!=PreviousSignal) signalNavigationBar=SIZE_MAX;
            if(id==AddFinancial && reinterpret_cast<HWND>(lp)==toolbar) {
                if(preferences.financialLegend && financialWindow){ShowWindow(financialWindow,SW_HIDE);preferences.financialLegend=false;saveAppPreferences();}
                else showFinancial();
                syncToolbar();return 0;
            }
            if(id==SignalReport && reinterpret_cast<HWND>(lp)==toolbar && IsWindowVisible(signalReportWindow)){ShowWindow(signalReportWindow,SW_HIDE);syncToolbar();return 0;}
            options.panExtra=0;pendingExtra=0;
            if (menuCommand(id)) return 0;
            if (id == Open) { auto p = choose(false); if (!p.empty()) load(p); }
            else if (id == Export && chart) { auto p = choose(true); if (!p.empty()) savePng(p, *chart); }
            else if (id == Goto) showGoto();
            else if (id == Legend) { if (preferences.legend && legendWindow && IsWindowVisible(legendWindow)) { ShowWindow(legendWindow, SW_HIDE); preferences.legend = false; saveAppPreferences(); } else showLegend(); }
            else if (id == Volume) { options.volume = !options.volume; syncToolbar(); redraw(); }
            else if (id >= ZoomIn && id <= Volume) {
                auto oldFirst = options.first, oldCount = options.count;
                crossVisible = false;
                if (id == ZoomIn) options.count = std::max(size_t(20), options.count / 2);
                if (id == ZoomOut) options.count = std::min(std::max(size_t(20), data.time.size()), options.count * 2);
                if (id == Latest || options.first + options.count > data.time.size()) options.first = data.time.size() > options.count ? data.time.size() - options.count : 0;
                if (options.first != oldFirst || options.count != oldCount || id == Type) redraw();
            }
            return 0;
        }
        case WM_CONTEXTMENU: {
            POINT pt{GET_X_LPARAM(lp),GET_Y_LPARAM(lp)},client=pt;ScreenToClient(hwnd,&client);
            if(!overPriceAxis(client.x,client.y))break;
            flushPan();endDrag();auto menu=CreatePopupMenu();
            AppendMenuW(menu,MF_STRING|(options.autoScale?MF_CHECKED:0),1700,L"Auto Scale");
            AppendMenuW(menu,MF_STRING|(options.priceOnly?MF_CHECKED:0)|(options.autoScale?0:MF_GRAYED),1701,L"Scale Price Data Only");
            size_t labels=menuLabels.size();themeMenu(menu,false);int command=TrackPopupMenu(menu,TPM_RETURNCMD|TPM_RIGHTBUTTON,pt.x,pt.y,0,hwnd,nullptr);DestroyMenu(menu);menuLabels.resize(labels);
            if(command==1700){if(options.autoScale){auto axis=chartPanels.front()->yAxis();setManualScale((axis->getMinValue()+axis->getMaxValue())/2,axis->getMaxValue()-axis->getMinValue());verticalPending=false;}else options.autoScale=true;redraw();}
            else if(command==1701 && options.autoScale){options.priceOnly=!options.priceOnly;redraw();}return 0;
        }
        case WM_MOUSEWHEEL: {
            POINT pt{GET_X_LPARAM(lp),GET_Y_LPARAM(lp)};ScreenToClient(hwnd,&pt);if(!overPriceAxis(pt.x,pt.y))break;
            flushPan();auto axis=chartPanels.front()->yAxis();double low=axis->getMinValue(),high=axis->getMaxValue();
            setManualScale((low+high)/2,(high-low)*std::exp(-GET_WHEEL_DELTA_WPARAM(wp)/120.0*0.12));verticalPending=false;crossVisible=false;redraw();return 0;
        }
        case WM_SETCURSOR: {
            POINT pt; GetCursorPos(&pt); ScreenToClient(hwnd, &pt);
            if(LOWORD(lp)==HTCLIENT && !dragging && !zoomDragging && !verticalDragging && overChartEndButton(pt)){SetCursor(LoadCursor(nullptr,IDC_HAND));return TRUE;}
            if(LOWORD(lp)==HTCLIENT && (verticalDragging||overPriceAxis(pt.x,pt.y))){SetCursor(LoadCursor(nullptr,IDC_SIZENS));return TRUE;}
            if (LOWORD(lp) == HTCLIENT && (dragging || zoomDragging || overTimeAxis(pt.x, pt.y) || overPlot(pt.x, pt.y))) {
                SetCursor(LoadCursor(nullptr, dragging || zoomDragging || overTimeAxis(pt.x, pt.y) ? IDC_SIZEWE : IDC_CROSS)); return TRUE;
            }
            break;
        }
        case WM_LBUTTONDOWN:
            if(overChartEndButton({GET_X_LPARAM(lp),GET_Y_LPARAM(lp)})){SendMessage(hwnd,WM_COMMAND,Latest,0);return 0;}
            signalNavigationBar=SIZE_MAX;
            if(overPriceAxis(GET_X_LPARAM(lp),GET_Y_LPARAM(lp))){
                flushPan();verticalDragging=true;verticalStartY=GET_Y_LPARAM(lp);auto axis=chartPanels.front()->yAxis();verticalMid=(axis->getMinValue()+axis->getMaxValue())/2;verticalSpan=axis->getMaxValue()-axis->getMinValue();
                SetCapture(hwnd);SetTimer(hwnd,PanTimer,16,nullptr);crossVisible=false;return 0;
            }
            if (overTimeAxis(GET_X_LPARAM(lp), GET_Y_LPARAM(lp))) {
                zoomDragging = true; dragStartX = GET_X_LPARAM(lp);
                zoomStartCount = std::min(options.count, data.time.size()); zoomEnd = std::min(data.time.size(), options.first + zoomStartCount);
                SetCapture(hwnd); SetCursor(LoadCursor(nullptr, IDC_SIZEWE)); SetTimer(hwnd, PanTimer, 16, nullptr);
                crossVisible = false; invalidateChart(); return 0;
            }
            {int study=chartStudyAt({GET_X_LPARAM(lp),GET_Y_LPARAM(lp)});if(study>=0){highlightChartStudy(options.activeStudy==study?-1:study);redraw();}}
            if (!data.time.empty() && overPlot(GET_X_LPARAM(lp), GET_Y_LPARAM(lp))) {
                dragging = true;
                dragStartX = GET_X_LPARAM(lp); dragFirst = options.first;dragExtra=options.panExtra;
                dragBarsPerPixel = static_cast<double>(std::min(options.count, data.time.size())) /
                    std::max(1, static_cast<int>(bitmap->GetWidth()) - 65 - 70);
                SetCapture(hwnd); SetCursor(LoadCursor(nullptr, IDC_SIZEWE));
                SetTimer(hwnd, PanTimer, 16, nullptr);
            }
            return 0;
        case WM_MOUSEMOVE:
            crossPoint = {GET_X_LPARAM(lp), GET_Y_LPARAM(lp)};
            crossVisible = hitPanel(crossPoint) != nullptr && !overChartEndButton(crossPoint);
            { TRACKMOUSEEVENT tracking{sizeof(tracking), TME_LEAVE, hwnd, 0}; TrackMouseEvent(&tracking); }
            if(verticalDragging){if(!(wp&MK_LBUTTON)){flushPan();endDrag();return 0;}crossVisible=false;
                int delta=GET_Y_LPARAM(lp)-verticalStartY;if(delta)setManualScale(verticalMid,verticalSpan*std::exp(std::clamp(delta/180.0,-20.0,20.0)));return 0;}
            if (zoomDragging) {
                if (!(wp & MK_LBUTTON)) { flushPan(); endDrag(); return 0; }
                crossVisible = false;
                // Scale from the initial gesture, keeping the rightmost visible bar anchored.
                double scale = std::exp(std::clamp((GET_X_LPARAM(lp) - dragStartX) / 180.0, -20.0, 20.0));
                pendingExtra=0;pendingCount = static_cast<size_t>(std::clamp(std::round(zoomStartCount * scale), static_cast<double>(std::min(size_t(20), data.time.size())), static_cast<double>(data.time.size())));
                pendingFirst = zoomEnd > pendingCount ? zoomEnd - pendingCount : 0;
                panPending = pendingCount != options.count || pendingFirst != options.first;
                if (pendingCount == data.time.size() || pendingCount == std::min(size_t(20), data.time.size())) {
                    // Reset the gesture anchor at a limit so reversing direction responds immediately.
                    dragStartX = GET_X_LPARAM(lp); zoomStartCount = pendingCount; zoomEnd = pendingFirst + pendingCount;
                }
                return 0;
            } else if (dragging) {
                if (!(wp & MK_LBUTTON)) { endDrag(); return 0; }
                // Anchor to the initial viewport so rounding cannot accumulate while dragging.
                auto shift = std::llround((GET_X_LPARAM(lp) - dragStartX) * dragBarsPerPixel);
                double maxFirst=static_cast<double>(data.time.size()-std::min(options.count,data.time.size()));
                double limit=std::min(options.count,data.time.size())*2.0/3.0;
                double position=std::clamp(static_cast<double>(dragFirst)+dragExtra-shift,-limit,maxFirst+limit);
                auto first=static_cast<size_t>(std::clamp(position,0.0,maxFirst));
                pendingExtra=position-first;pendingFirst = first; pendingCount = options.count; panPending = first != options.first || pendingExtra!=options.panExtra;
                return 0;
            }
            invalidateChart(); requestLegendRefresh();
            return 0;
        case WM_TIMER:
            if(wp==LegendTimer){flushLegendRefresh();return 0;}
            if (wp == PanTimer) { flushPan(); return 0; }
            break;
        case WM_MOUSELEAVE:
            crossVisible = false; invalidateChart(); requestLegendRefresh();
            return 0;
        case WM_LBUTTONUP:
            flushPan(); endDrag(); return 0;
        case WM_CANCELMODE:
            endDrag(); return 0;
        case WM_CAPTURECHANGED:
            verticalDragging=false;verticalPending=false;dragging = zoomDragging = false; KillTimer(hwnd, PanTimer); panPending = false; return 0;
        case WM_HSCROLL: {
            signalNavigationBar=SIZE_MAX;
            if(data.time.empty())return 0;
            const int action=LOWORD(wp);
            if(action==SB_ENDSCROLL){flushPan();KillTimer(hwnd,PanTimer);return 0;}
            if(dragging || zoomDragging || verticalDragging)endDrag();
            SCROLLINFO si{sizeof(si),SIF_ALL};GetScrollInfo(hwnd,SB_HORZ,&si);
            int pos=int(options.first),step=std::max(1,int(options.count/10));
            switch(action){
            case SB_LINELEFT:pos-=step;break;case SB_LINERIGHT:pos+=step;break;
            case SB_PAGELEFT:pos-=int(options.count);break;case SB_PAGERIGHT:pos+=int(options.count);break;
            case SB_LEFT:pos=0;break;case SB_RIGHT:pos=si.nMax;break;
            case SB_THUMBTRACK:case SB_THUMBPOSITION:pos=si.nTrackPos;break;
            default:return 0;
            }
            auto next=size_t(std::clamp(pos,0,std::max(0,int(data.time.size())-int(options.count))));
            crossVisible=false;
            if(action==SB_THUMBTRACK){
                // Do not restart the timer on each move: a continuous drag must still render.
                if(!panPending && next!=options.first)SetTimer(hwnd,PanTimer,16,nullptr);
                pendingExtra=0;pendingFirst=next;pendingCount=options.count;panPending=next!=options.first || options.panExtra!=0;
            }else{
                KillTimer(hwnd,PanTimer);panPending=false;
                if(next!=options.first || options.panExtra!=0){options.first=next;options.panExtra=0;pendingExtra=0;redraw();}
            }
            return 0;
        }
        case WM_DROPFILES: {
            if(loadingFile){DragFinish(reinterpret_cast<HDROP>(wp));return 0;}
            endDrag();
            auto drop = reinterpret_cast<HDROP>(wp); wchar_t path[32768]; DragQueryFileW(drop, 0, path, 32768); DragFinish(drop); load(path); return 0;
        }
        case WM_ERASEBKGND:
            // WM_PAINT presents a fully composed frame; never expose a cleared background.
            return 1;
        case WM_PAINT: {
            PAINTSTRUCT ps; auto dc = BeginPaint(hwnd, &ps);
            RECT r; GetClientRect(hwnd, &r);
            if (r.right > 0 && r.bottom > 0) {
                if (!frameDC || frameWidth != r.right || frameHeight != r.bottom) {
                    releaseFrame(); frameDC = CreateCompatibleDC(dc);
                    framePixels = CreateCompatibleBitmap(dc, r.right, r.bottom);
                    originalFrameBitmap = SelectObject(frameDC, framePixels);
                    frameWidth = r.right; frameHeight = r.bottom;
                }
                int saved = SaveDC(frameDC);
                FillRect(frameDC, &r, appBackground()); SetTextColor(frameDC, appTextColor()); SetBkColor(frameDC, appBackgroundColor());
                if (chartPixels) {
                    auto source = CreateCompatibleDC(dc); auto old = SelectObject(source, chartPixels);
                    BitBlt(frameDC, chartLeft, chartTop, bitmap->GetWidth(), bitmap->GetHeight(), source, 0, 0, SRCCOPY);
                    SelectObject(source, old); DeleteDC(source);
                    paintCrosshair(frameDC);
                    paintChartEndButton(frameDC);
                    demoOverlay::paint(frameDC, chartLeft, chartTop, bitmap->GetWidth(), bitmap->GetHeight(), font, preferences.dark);
                } else {
                    r.left = 30; r.top = 100; SelectObject(frameDC, font);
                    DrawTextW(frameDC, L"FIGCharts\n\nOpen or drop a data file to explore prices and studies.\n\nDrag the chart to navigate through history.\nStudy values are displayed exactly as exported.", -1, &r, DT_LEFT);
                }
                RestoreDC(frameDC, saved);
                // One copy presents both chart and crosshair, clipped to the invalid area.
                BitBlt(dc, ps.rcPaint.left, ps.rcPaint.top, ps.rcPaint.right - ps.rcPaint.left,
                    ps.rcPaint.bottom - ps.rcPaint.top, frameDC, ps.rcPaint.left, ps.rcPaint.top, SRCCOPY);
            }
            EndPaint(hwnd, &ps); return 0;
        }
        case WM_CLOSE: if(loadingFile)return 0;if(!confirmWorkspaceSave())return 0;DestroyWindow(hwnd);return 0;
        case WM_DESTROY: KillTimer(hwnd,LegendTimer);endDrag(); releaseFrame(); clearImage(); chart.reset(); PostQuitMessage(0); return 0;
        }
    } catch (const std::exception& e) { endDrag(); error(e); }
    return DefWindowProcW(hwnd, msg, wp, lp);
}
void testInterface(const std::filesystem::path& output) {
    {LoadingIndicator indicator(L"Testing responsive loading...");
        SetTimer(indicator.dialog,99,10,[](HWND h,UINT,UINT_PTR,DWORD){SetPropW(h,L"LoadingTimerServiced",reinterpret_cast<HANDLE>(1));});
        auto uiThread=GetCurrentThreadId();auto workerThread=loadingTask([]{std::this_thread::sleep_for(std::chrono::milliseconds(80));return GetCurrentThreadId();});
        bool responsive=GetPropW(indicator.dialog,L"LoadingTimerServiced")!=nullptr;
        KillTimer(indicator.dialog,99);RemovePropW(indicator.dialog,L"LoadingTimerServiced");
        if(workerThread==uiThread || !responsive || IsWindowEnabled(window))throw std::runtime_error("Loading must run on a worker while UI messages are serviced.");
    }
    if(loadingFile || !IsWindowEnabled(window))throw std::runtime_error("Loading must restore enabled controls.");
    {auto oldFile=currentFile;auto oldBars=data.time.size();bool failed=false;
        try{load(output.wstring()+L".missing-file");}catch(const std::exception&){failed=true;}
        if(!failed || loadingFile || !IsWindowEnabled(window) || currentFile!=oldFile || data.time.size()!=oldBars)throw std::runtime_error("Failed loading must restore controls and keep the previous dataset.");
    }

    auto require = [](bool condition, const char* message) { if (!condition) throw std::runtime_error(message); };
    require(IsWindow(signalFilterEdit) && IsWindow(signalFilterClear),"Signal filter controls missing");
    layoutSignalFilter();
    for(HWND control:{signalFilterEdit,signalFilterClear}){RECT rect{};GetWindowRect(control,&rect);POINT point{(rect.left+rect.right)/2,(rect.top+rect.bottom)/2};ScreenToClient(window,&point);
        require(ChildWindowFromPointEx(window,point,CWP_SKIPINVISIBLE|CWP_SKIPDISABLED)==control,"Toolbar must not cover the SIDE filter or clear button");}
    SendMessage(signalFilterEdit,WM_CHAR,L'-',0);SendMessage(signalFilterEdit,WM_CHAR,L'7',0);
    require(signalFilterText()==L"-7","SIDE filter must accept keyboard input");
    SendMessage(signalFilterClear,BM_CLICK,0,0);require(signalFilterText().empty(),"SIDE filter clear button must receive clicks");
    require(SendMessage(toolbar, TB_BUTTONCOUNT, 0, 0) == 23, "Toolbar actions and separators missing");
    require(!GetDlgItem(window, Studies) && !GetDlgItem(window, AddOverlay), "Old sidebar controls must be removed");
    RECT original{}, resized{}; GetWindowRect(window, &original);
    SetCapture(window); dragging = false;
    SendMessage(window, WM_SIZE, SIZE_RESTORED, MAKELPARAM(720, 520));
    require(GetCapture() == window, "Resize must not release Windows sizing capture");
    ReleaseCapture();
    SetWindowPos(window, nullptr, 0, 0, 720, 520, SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
    GetClientRect(window, &resized);
    require(bitmap && bitmap->GetWidth() == static_cast<UINT>(resized.right) && bitmap->GetHeight() <= static_cast<UINT>(resized.bottom - chartTop - 22), "Chart must fit resized window");
    SetWindowPos(window, nullptr, 0, 0, original.right-original.left, original.bottom-original.top, SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
    require(data.time.size() >= 600, "UI test fixture requires at least 600 bars");
    require(options.type == ChartType::OHLC && !options.volume, "Default OHLC/volume settings failed");
    for (int type = 0; type < 3; ++type) {
        preferences.chartType = type;
        SendMessage(window, WM_COMMAND, MAKEWPARAM(Type, CBN_SELCHANGE), 0);
        require(options.type == static_cast<ChartType>(type) && bitmap, "Chart type selection failed");
    }
    preferences.chartType = 0;

    SendMessage(window, WM_COMMAND, Volume, 0);
    require(options.volume, "Show volume failed");
    SendMessage(window, WM_COMMAND, Volume, 0);
    require(data.studies.size() >= 2, "Study tests require two studies");
    options.studies.clear(); options.activeStudy = -1;
    addStudy(0, false); addStudy(1, true); addStudy(1, true);
    require(options.studies.size() == 2 && !options.studies[0].overlay && options.studies[1].overlay, "Study additions must persist without duplicates");
    showStudyPicker();
    {RECT fontButton{},addButton{};GetWindowRect(GetDlgItem(legendWindow,1950),&fontButton);GetWindowRect(GetDlgItem(legendWindow,LegendAdd),&addButton);
        require(fontButton.left-addButton.left==210 && fontButton.top==addButton.top,"Font controls must follow the compact legend toolbar when picker expands");}
    require(SendMessage(pickerList, LB_GETCOUNT, 0, 0) == static_cast<LRESULT>(data.studies.size() - 2), "Picker must exclude added studies");
    if(data.studies.size()>50)for(int command:{PickerOverlay,PickerText,PickerList}){
        SendMessage(pickerList,LB_SETCURSEL,23,0);SendMessage(pickerList,LB_SETTOPINDEX,20,0);
        auto top=SendMessage(pickerList,LB_GETTOPINDEX,0,0),count=SendMessage(pickerList,LB_GETCOUNT,0,0);
        int study=static_cast<int>(SendMessage(pickerList,LB_GETITEMDATA,23,0));
        SendMessage(studyPicker,WM_COMMAND,command==PickerList?MAKEWPARAM(PickerList,LBN_DBLCLK):command,0);
        require(options.studies.back().index==study && options.studies.back().overlay==(command!=PickerText),"Picker add action must use the selected study and display mode");
        require(SendMessage(pickerList,LB_GETCOUNT,0,0)==count-1 && SendMessage(pickerList,LB_GETTOPINDEX,0,0)==top,"Adding a study must preserve the picker scroll position");
        for(int row=0;row<count-1;++row)require(SendMessage(pickerList,LB_GETITEMDATA,row,0)!=study,"Added study must disappear from the picker");
        removeStudy(static_cast<int>(options.studies.size()-1));
    }
    SendMessage(pickerList,LB_SETTOPINDEX,0,0);
    for (int row = 0; row < SendMessage(pickerList, LB_GETCOUNT, 0, 0); ++row) {
        auto index = SendMessage(pickerList, LB_GETITEMDATA, row, 0);
        require(index != 0 && index != 1, "Already-added study in picker");
    }
    SetDlgItemTextW(studyPicker,PickerSearch,L"_ma");
    int expectedMatches=0;for(size_t i=2;i<data.studies.size();++i)if(StrStrIW(wide(data.studies[i].name).c_str(),L"_MA"))++expectedMatches;
    require(SendMessage(pickerList,LB_GETCOUNT,0,0)==expectedMatches,"Study search must match substrings without case sensitivity");
    SetDlgItemTextW(studyPicker,PickerSearch,L"__no_such_study__");require(SendMessage(pickerList,LB_GETCOUNT,0,0)==0,"Search must support no matches");
    SendMessage(studyPicker,WM_COMMAND,PickerClear,0);require(SendMessage(pickerList,LB_GETCOUNT,0,0)==static_cast<LRESULT>(data.studies.size()-2),"Clear search must restore available studies");
    RECT pickerBounds{}; GetWindowRect(pickerList, &pickerBounds); MapWindowPoints(nullptr, studyPicker, reinterpret_cast<POINT*>(&pickerBounds), 2);
    require(pickerBounds.top >= 38, "Study list must start below its title on first open");
    SendMessage(studyPicker, WM_COMMAND, PickerClose, 0);
    require(!pickerExpanded, "Hide button must collapse the study pane");
    showStudyPicker();
    require(GetParent(studyPicker) == legendWindow && (GetWindowLongPtr(studyPicker, GWL_STYLE) & WS_CHILD), "Study picker must be embedded in Legend");
    if (data.studies.size() > 2) {
        SendMessage(pickerList, LB_SETCURSEL, 0, 0);
        SendMessage(studyPicker, WM_COMMAND, MAKEWPARAM(PickerList, LBN_DBLCLK), reinterpret_cast<LPARAM>(pickerList));
        require(options.studies.size() == 3 && options.studies.back().overlay, "Double click must add study as overlay");
        removeStudy(2);
        SendMessage(legendWindow, WM_COMMAND, LegendAdd, 0);
        require(!pickerExpanded, "Add Study must collapse the pane");
        SendMessage(legendWindow, WM_COMMAND, LegendAdd, 0);
        require(pickerExpanded, "Add Study must expand the pane");
        SendMessage(pickerList, LB_SETCURSEL, 0, 0);
        SendMessage(studyPicker, WM_COMMAND, MAKEWPARAM(PickerList, LBN_SELCHANGE), 0);
        require(options.studies.size() == 2, "Picker selection must not add a study");
        SendMessage(studyPicker, WM_COMMAND, PickerText, 0);
        require(options.studies.size() == 3 && !options.studies.back().overlay, "Picker Add text failed");
        require(SendMessage(pickerList, LB_GETCOUNT, 0, 0) == static_cast<LRESULT>(data.studies.size() - 3), "Picker did not remove added study");
        removeStudy(2);
        require(SendMessage(pickerList, LB_GETCOUNT, 0, 0) == static_cast<LRESULT>(data.studies.size() - 2), "Removed study not returned to picker");
        SendMessage(pickerList, LB_SETCURSEL, 0, 0); SendMessage(studyPicker, WM_COMMAND, PickerOverlay, 0);
        require(options.studies.size() == 3 && options.studies.back().overlay, "Picker Add overlay failed"); removeStudy(2);
    }
    RECT manualLegend{},afterLegend{};SetWindowPos(legendWindow,nullptr,0,0,790,510,SWP_NOMOVE|SWP_NOZORDER|SWP_NOACTIVATE);GetWindowRect(legendWindow,&manualLegend);
    updateLegend();GetWindowRect(legendWindow,&afterLegend);require(EqualRect(&manualLegend,&afterLegend),"Chart updates must preserve manual legend size");
    require(!legendAdvanced && ListView_GetColumnWidth(legendStudies, DisplayColumn) > 0 && ListView_GetColumnWidth(legendStudies, ColorColumn) == 0, "Compact legend must hide advanced columns");
    RECT compact{}, expanded{}; GetWindowRect(legendWindow, &compact);
    SendDlgItemMessage(legendWindow, LegendAdvanced, BM_SETCHECK, BST_CHECKED, 0);
    SendMessage(legendWindow, WM_COMMAND, LegendAdvanced, 0); GetWindowRect(legendWindow, &expanded);
    require(legendAdvanced && ListView_GetColumnWidth(legendStudies, WidthColumn) > 0 && expanded.right - expanded.left > compact.right - compact.left, "Advanced legend must expand");
    SendDlgItemMessage(legendWindow, LegendAdvanced, BM_SETCHECK, BST_UNCHECKED, 0);
    SendMessage(legendWindow, WM_COMMAND, LegendAdvanced, 0);
    require(!legendAdvanced && ListView_GetColumnWidth(legendStudies, DeleteColumn) > 0 && ListView_GetColumnWidth(legendStudies, WidthColumn) == 0, "Compact legend must restore");
    require(chartPanels.size() == 1, "Text study must not automatically add a panel");
    ListView_SetItemState(legendStudies, 0, LVIS_SELECTED | LVIS_FOCUSED, LVIS_SELECTED | LVIS_FOCUSED);
    require(chartPanels.size() == 2 && options.activeStudy == 0, "Selecting text study must show its panel");
    beginCellEdit(0, LineColumn); SendMessage(cellEditor, CB_SETCURSEL, 2, 0);
    SendMessage(legendWindow, WM_COMMAND, MAKEWPARAM(CellEditor, CBN_SELENDOK), 0);
    beginCellEdit(0, WidthColumn); SendMessage(cellEditor, CB_SETCURSEL, 3, 0);
    SendMessage(legendWindow, WM_COMMAND, MAKEWPARAM(CellEditor, CBN_SELENDOK), 0);
    options.studies[0].color = 0xab1234; saveStudies(); redraw();
    require(options.studies[0].pattern == 2 && options.studies[0].width == 4, "Study line settings failed");
    require(options.studies[1].pattern == 0 && options.studies[1].width == 1, "Cell editor modified another row");
    beginCellEdit(0, LineColumn); SendMessage(cellEditor, CB_SETCURSEL, 1, 0);
    SendMessage(legendWindow, WM_COMMAND, MAKEWPARAM(CellEditor, CBN_SELENDCANCEL), 0);
    require(options.studies[0].pattern == 2 && editingRow == -1, "Cancelled dropdown changed the study");
    beginCellEdit(0, WidthColumn); SendMessage(legendStudies, WM_VSCROLL, SB_LINEDOWN, 0);
    require(editingRow == -1, "Scrolling left cell editor on wrong row");
    auto settingsFile = output; settingsFile += L".settings-test";
    writeSettings(settingsFile); savedStudies.clear(); readSettings(settingsFile); std::filesystem::remove(settingsFile);
    auto saved = savedStudies.at(data.studies[0].name);
    require(saved.pattern == 2 && saved.width == 4 && saved.color == 0xab1234 && !saved.overlay, "Study settings persistence failed");
    ListView_SetItemState(legendStudies,1,LVIS_SELECTED|LVIS_FOCUSED,LVIS_SELECTED|LVIS_FOCUSED);
    SendMessage(legendWindow,WM_COMMAND,LegendMoveUp,0);
    require(options.studies[0].index==1 && legendSelection()==0,"Move up must preserve selection and swap studies");
    std::ostringstream orderedSettings;writeSettingsStream(orderedSettings);
    savedStudies.clear();std::istringstream orderedInput(orderedSettings.str());readSettingsStream(orderedInput);restoreStudies(false);
    require(options.studies[0].index==1 && options.studies[1].index==0,"Study order must survive settings reload");
    ListView_SetItemState(legendStudies,0,LVIS_SELECTED|LVIS_FOCUSED,LVIS_SELECTED|LVIS_FOCUSED);
    SendMessage(legendWindow,WM_COMMAND,LegendMoveDown,0);
    require(options.studies[0].index==0 && options.studies[1].index==1,"Move down must restore study order");
    options.activeStudy=0;redraw();
    {auto panel=chartPanels.back();size_t index=std::min(options.count,data.time.size()-options.first)/2/renderedStride;
        size_t source=std::min(options.first+(index+1)*renderedStride,data.time.size())-1;
        POINT studyPoint{chartLeft+panel->getAbsOffsetX()+panel->getXCoor(static_cast<double>(index)),chartTop+panel->getAbsOffsetY()+panel->getYCoor(data.studies[options.studies[0].index].values[source])};
        require(chartStudyAt(studyPoint)==0,"Chart hit detection must find the study line");
        syncingLegend=true;ListView_SetItemState(legendStudies,-1,0,LVIS_SELECTED|LVIS_FOCUSED);syncingLegend=false;
        SendMessage(window,WM_LBUTTONDOWN,MK_LBUTTON,MAKELPARAM(studyPoint.x,studyPoint.y));SendMessage(window,WM_LBUTTONUP,0,MAKELPARAM(studyPoint.x,studyPoint.y));
        require(options.activeStudy==-1 && legendSelection()==-1,"Clicking the highlighted study must clear chart and legend selection");}
    activateLegendCell(0, DisplayColumn);

    require(chartPanels.size() == 1 && options.studies[0].overlay, "Changing text study to overlay failed");
    activateLegendCell(0, DisplayColumn);

    require(chartPanels.size() == 2 && !options.studies[0].overlay, "Changing overlay to text failed");
    size_t bar = options.first + std::min(options.count, data.time.size()) / 2;
    auto p = chartPanels.front(); auto r = panelRect(p);
    int x = chartLeft + p->getAbsOffsetX() + p->getXCoor(static_cast<double>(bar - options.first));
    int y = (r.top + r.bottom) / 2;
    SendMessage(window, WM_MOUSEMOVE, 0, MAKELPARAM(x, y));
    if (!crossVisible || selectedBar() != bar) throw std::runtime_error("Crosshair mapping: expected " + std::to_string(bar) + " got " + std::to_string(selectedBar()) + " x " + std::to_string(x) + " first " + std::to_string(options.first) + " visible " + std::to_string(crossVisible) + " nearest " + std::to_string(p->getNearestXValue(x - chartLeft - p->getAbsOffsetX())));
    require(legendRefreshPending,"Crosshair should schedule a deferred legend refresh");
    auto requestedFinancial=financialFormattedCells,requestedStudies=legendFormattedCells;
    SendMessage(window,WM_TIMER,LegendTimer,0);
    require(!legendRefreshPending && financialFormattedCells==requestedFinancial && legendFormattedCells==requestedStudies,"Legend refresh should not format off-screen rows");
    wchar_t legend[16384]; GetWindowTextW(legendText, legend, 16384);
    require(std::wstring(legend).find(timestamp(data.time[bar])) != std::wstring::npos, "Legend timestamp failed");
    std::wostringstream expected; expected << std::fixed << std::setprecision(2);
    auto v = data.studies[0].values[bar];
    if (v == Chart::NoValue) expected << L"--"; else expected << v;
    wchar_t valueText[128]; ListView_GetItemText(legendStudies, 0, ValueColumn, valueText, 128);
    require(std::wstring(valueText) == expected.str(), "Legend study value does not match selected bar");
    auto cachedPixels = chartPixels;
    for (int dy = 0; dy < 20; ++dy) {
        SendMessage(window, WM_MOUSEMOVE, 0, MAKELPARAM(x, y + dy));
        SendMessage(window, WM_PAINT, 0, 0);
    }
    require(chartPixels == cachedPixels && frameDC && framePixels, "Crosshair must reuse chart pixels and buffered painting");
    auto studyRect = panelRect(chartPanels.back());
    SendMessage(window, WM_MOUSEMOVE, 0, MAKELPARAM(x, (studyRect.top + studyRect.bottom) / 2));
    require(hitPanel(crossPoint) == chartPanels.back() && selectedBar() == bar, "Study-panel crosshair failed");
    SendMessage(legendWindow, WM_CLOSE, 0, 0);
    require(IsWindow(legendWindow) && !IsWindowVisible(legendWindow), "Legend hide failed");
    showLegend();
    showGoto();
    auto target = data.time[data.time.size() / 3];
    ULARGE_INTEGER ticks; ticks.QuadPart = static_cast<ULONGLONG>((target + 11644473600.0) * 10000000.0);
    FILETIME ft{ticks.LowPart, ticks.HighPart}; SYSTEMTIME st{}; FileTimeToSystemTime(&ft, &st);setGotoInputs(st);
    ShowWindow(gotoWindow,SW_SHOW);SetFocus(GetDlgItem(gotoWindow,GotoYear));
    SetDlgItemTextW(gotoWindow,GotoYear,L"");auto year=std::to_wstring(st.wYear);for(wchar_t digit:year)SendDlgItemMessage(gotoWindow,GotoYear,WM_CHAR,digit,0);
    require(GetFocus()==GetDlgItem(gotoWindow,GotoYear+1),"Entering four year digits must advance to month");
    SetFocus(GetDlgItem(gotoWindow,GotoYear));SendDlgItemMessage(gotoWindow,GotoYear,WM_KEYDOWN,VK_TAB,0);
    require(GetFocus()==GetDlgItem(gotoWindow,GotoYear+1),"Tab from year must advance to month");
    for(int field=1;field<5;++field){WORD values[]{st.wYear,st.wMonth,st.wDay,st.wHour,st.wMinute};wchar_t digits[8];swprintf_s(digits,L"%02u",values[field]);auto input=GetDlgItem(gotoWindow,GotoYear+field);SetFocus(input);SetWindowTextW(input,L"");for(int n=0;n<2;++n)SendMessage(input,WM_CHAR,digits[n],0);require(GetFocus()==GetDlgItem(gotoWindow,field==4?IDOK:GotoYear+field+1),"Completed date/time field must advance to the next field");}
    setGotoInputs(st);
    SendMessage(gotoWindow, WM_COMMAND, IDOK, 0);
    showGoto();SYSTEMTIME remembered{};DateTime_GetSystemtime(timePicker,&remembered);
    require(remembered.wHour==st.wHour && remembered.wMinute==st.wMinute && gotoSelection.wDay==st.wDay && gotoSelection.wSecond==0,"Go To must remember selection at minute precision");
    require(!(GetWindowLongPtr(datePicker,GWL_STYLE)&DTS_UPDOWN),"Go To calendar dropdown missing");
    auto first = options.first;
    require(data.time[first] <= target && data.time[std::min(data.time.size(), first + options.count) - 1] >= target, "Go To did not reveal target");
    p = chartPanels.front(); r = panelRect(p); x = (r.left + r.right) / 2; y = (r.top + r.bottom) / 2;
    SendMessage(window, WM_LBUTTONDOWN, MK_LBUTTON, MAKELPARAM(x, y));
    SendMessage(window, WM_MOUSEMOVE, MK_LBUTTON, MAKELPARAM(x + 30, y));
    require(options.first == first && panPending, "Drag updates should be coalesced");
    SendMessage(window, WM_TIMER, PanTimer, 0);
    require(options.first < first && !panPending, "Drag timer did not render latest position");
    SendMessage(window, WM_MOUSEMOVE, MK_LBUTTON, MAKELPARAM(x + 40, y));
    SendMessage(window, WM_LBUTTONUP, 0, MAKELPARAM(x + 30, y));
    require(options.first < first && !dragging && GetCapture() != window, "Drag panning regression");
    syncToolbar();ValidateRect(toolbar,nullptr);
    redraw();
    require(!GetUpdateRect(toolbar,nullptr,FALSE),"Chart redraw must not invalidate an unchanged toolbar");
    auto axis = panelRect(chartPanels.back()); int ax = (axis.left + axis.right)/2, ay = axis.bottom + 8;
    size_t beforeZoom = options.count, rightEdge = options.first + options.count;
    SendMessage(window, WM_LBUTTONDOWN, MK_LBUTTON, MAKELPARAM(ax, ay));
    SendMessage(window, WM_MOUSEMOVE, MK_LBUTTON, MAKELPARAM(ax - 90, ay));
    SendMessage(window, WM_TIMER, PanTimer, 0);
    require(zoomDragging && options.count < beforeZoom && options.first + options.count == rightEdge, "Axis drag left must zoom in anchored at right edge");
    SendMessage(window, WM_MOUSEMOVE, MK_LBUTTON, MAKELPARAM(ax + 90, ay));
    SendMessage(window, WM_LBUTTONUP, 0, MAKELPARAM(ax - 90, ay));
    require(options.count > beforeZoom && !zoomDragging && GetCapture() != window, "Axis drag right must zoom out and release capture");
    // Render the same crosshair overlay used by WM_PAINT for visual verification.
    Gdiplus::Bitmap preview(1500, 950, PixelFormat32bppARGB);
    { Gdiplus::Graphics g(&preview); g.Clear(Gdiplus::Color(255, 255, 255)); g.DrawImage(bitmap.get(), chartLeft, chartTop);
      auto dc = g.GetHDC(); paintCrosshair(dc); g.ReleaseHDC(dc); }
    CLSID png{0x557cf406, 0x1a04, 0x11d3, {0x9a, 0x73, 0x00, 0x00, 0xf8, 0x1e, 0xf3, 0x2e}};
    require(preview.Save(output.c_str(), &png) == Gdiplus::Ok, "Preview save failed");
    require(GetMenuItemCount(GetMenu(window)) == 4, "File/Edit/View/Help menu missing");
    auto savedPreferences = preferences;
    auto preferenceTest = output; preferenceTest += L".preferences-test";
    preferences.dark = true; preferences.chartType = 2; preferences.timeMode = 2; preferences.zoneKey = L"Eastern Standard Time";
    preferences.title = "FIGCharts settings test"; preferences.legend = false;
    writePreferences(preferenceTest); preferences = {}; readPreferences(preferenceTest); std::filesystem::remove(preferenceTest);
    require(preferences.dark && preferences.chartType == 2 && preferences.title == "FIGCharts settings test" && !preferences.legend, "Preferences did not round-trip");
    require(preferences.timeMode == 2, "Named time zone unavailable");
    SYSTEMTIME winter{}; winter.wYear = 2025; winter.wMonth = 1; winter.wDay = 15; winter.wHour = 17;
    SYSTEMTIME summer = winter; summer.wMonth = 7;
    require(displaySystemTime(systemSeconds(winter)).wHour == 12 && displaySystemTime(systemSeconds(summer)).wHour == 13, "Time zone must apply winter/summer DST offsets");
    auto local = displaySystemTime(systemSeconds(summer)); require(displayToUtc(local) == systemSeconds(summer), "Go To timezone round-trip failed");
    SYSTEMTIME missing = winter; missing.wMonth = 3; missing.wDay = 9; missing.wHour = 2; missing.wMinute = 30;
    bool rejectedGap = false; try { displayToUtc(missing); } catch (...) { rejectedGap = true; }
    require(rejectedGap, "Nonexistent DST time accepted");
    showSettings();
    require(TabCtrl_GetItemCount(GetDlgItem(settingsWindow,SettingsTabs))==2,"Settings need General and Database tabs");
    TabCtrl_SetCurSel(GetDlgItem(settingsWindow,SettingsTabs),1);settingsTab();require((GetWindowLongPtr(GetDlgItem(settingsWindow,DatabaseConnection),GWL_STYLE)&WS_VISIBLE)!=0 && (GetWindowLongPtr(GetDlgItem(settingsWindow,DefaultType),GWL_STYLE)&WS_VISIBLE)==0,"Database tab visibility failed");
    TabCtrl_SetCurSel(GetDlgItem(settingsWindow,SettingsTabs),0);settingsTab();
    SendMessage(settingsWindow, WM_COMMAND, ApplyPreferences, 0);
    require(options.type == ChartType::Line && preferences.dark && !IsWindowVisible(legendWindow), "Apply settings failed");
    {auto tabs=GetDlgItem(settingsWindow,SettingsTabs);RECT bounds{};GetClientRect(tabs,&bounds);
     auto screen=GetDC(tabs),dc=CreateCompatibleDC(screen);auto pixels=CreateCompatibleBitmap(screen,bounds.right,bounds.bottom);auto old=SelectObject(dc,pixels);
     for(int selected=0;selected<2;++selected){TabCtrl_SetCurSel(tabs,selected);settingsTab();SendMessage(tabs,WM_PRINTCLIENT,reinterpret_cast<WPARAM>(dc),PRF_CLIENT);
      require(GetPixel(dc,bounds.right-3,3)==appBackgroundColor(),"Dark tab strip must not use system light background");
      RECT tab{};TabCtrl_GetItemRect(tabs,selected,&tab);require(GetPixel(dc,tab.left+4,tab.top+4)==RGB(55,69,87),"Selected settings tab must be dark");
      Gdiplus::Bitmap capture(pixels,nullptr);auto path=output;path+=selected?L".database-tab.png":L".general-tab.png";capture.Save(path.c_str(),&png);
     }
     TabCtrl_SetCurSel(tabs,0);settingsTab();SelectObject(dc,old);DeleteObject(pixels);DeleteDC(dc);ReleaseDC(tabs,screen);
    }
    auto darkPreview = output; darkPreview += L".dark.png"; savePng(darkPreview, *chart);
    NMLVCUSTOMDRAW custom{}; custom.nmcd.dwDrawStage = CDDS_ITEMPREPAINT | CDDS_SUBITEM; custom.nmcd.dwItemSpec = 0;
    drawLegend(&custom); require(custom.clrText == RGB(0xab, 0x12, 0x34), "Legend must use study color");
    require(Header_GetItemCount(ListView_GetHeader(legendStudies)) == 10, "Legend delete column missing");
    auto settingsRoundTrip = output; settingsRoundTrip += L".settings-test"; writeSettings(settingsRoundTrip);
    options.studies.clear(); savedStudies.clear(); readSettings(settingsRoundTrip); restoreStudies(false); redraw(); std::filesystem::remove(settingsRoundTrip);
    require(options.studies.size() == 2 && options.studies[0].width == 4, "Named settings properties not restored");
    menuCommand(DisableLegend); require(!preferences.legend, "Disable legend menu failed");
    menuCommand(EnableLegend); require(preferences.legend, "Enable legend menu failed");
    auto oldCount = options.count; menuCommand(ZoomAll); require(options.first == 0 && options.count == data.time.size(), "Zoom all menu failed");
    require((data.time.size() + renderedStride - 1) / renderedStride <= std::max(size_t(300), static_cast<size_t>(bitmap->GetWidth()) * 2), "Overview must limit rendered bar count");
    axis = panelRect(chartPanels.back()); ax = (axis.left + axis.right)/2; ay = axis.bottom + 8;
    auto unchangedChart = chart.get();
    SendMessage(window, WM_LBUTTONDOWN, MK_LBUTTON, MAKELPARAM(ax, ay));
    SendMessage(window, WM_MOUSEMOVE, MK_LBUTTON, MAKELPARAM(ax + 100, ay));
    SendMessage(window, WM_TIMER, PanTimer, 0);
    require(options.count == data.time.size() && options.first == 0 && chart.get() == unchangedChart, "Zoom-out limit must not redraw or overshoot");
    SendMessage(window, WM_MOUSEMOVE, MK_LBUTTON, MAKELPARAM(ax + 90, ay));
    SendMessage(window, WM_TIMER, PanTimer, 0);
    require(options.count < data.time.size(), "Reversing at zoom limit must respond immediately");
    SendMessage(window, WM_LBUTTONUP, 0, MAKELPARAM(ax + 90, ay));
    menuCommand(ZoomAll); unchangedChart = chart.get();
    auto plot = panelRect(chartPanels.front());
    SendMessage(window, WM_LBUTTONDOWN, MK_LBUTTON, MAKELPARAM(plot.left+20, plot.top+20));
    require(dragging, "Full overview must allow dragging into empty space");
    SendMessage(window,WM_LBUTTONUP,0,MAKELPARAM(plot.left+20,plot.top+20));
    SendMessage(window, WM_HSCROLL, SB_LINELEFT, 0);
    require(chart.get() == unchangedChart && options.first == 0, "Full overview scroll must not redraw");
    for(int direction:{1,-1}){
        options.panExtra=0;redraw();plot=panelRect(chartPanels.front());int panX=(plot.left+plot.right)/2,panY=(plot.top+plot.bottom)/2;
        SendMessage(window,WM_LBUTTONDOWN,MK_LBUTTON,MAKELPARAM(panX,panY));SendMessage(window,WM_MOUSEMOVE,MK_LBUTTON,MAKELPARAM(panX+direction*10000,panY));SendMessage(window,WM_LBUTTONUP,0,MAKELPARAM(panX+direction*10000,panY));
        require(std::abs(options.panExtra+direction*static_cast<double>(options.count)*2.0/3.0)<0.01,"Empty-space dragging must stop at two thirds of viewport");
        auto panel=chartPanels.front();int firstX=chartLeft+panel->getAbsOffsetX()+panel->getXCoor(0);
        if(direction==1)require(firstX>plot.left+(plot.right-plot.left)/2,"Beginning overscroll must leave blank space before prices");
        auto file=output;file+=direction==1?L".pan-start.png":L".pan-end.png";savePng(file,*chart);
    }
    menuCommand(ZoomAll);
    options.first = 0; options.count = std::min(size_t(20), data.time.size()); redraw();
    axis = panelRect(chartPanels.back()); ax = (axis.left + axis.right)/2; ay = axis.bottom + 8; unchangedChart = chart.get();
    SendMessage(window, WM_LBUTTONDOWN, MK_LBUTTON, MAKELPARAM(ax, ay));
    SendMessage(window, WM_MOUSEMOVE, MK_LBUTTON, MAKELPARAM(ax - 100, ay));
    SendMessage(window, WM_TIMER, PanTimer, 0);
    require(options.first == 0 && options.count == 20 && chart.get() == unchangedChart, "Zoom-in boundary must not redraw or underflow");
    SendMessage(window, WM_LBUTTONUP, 0, MAKELPARAM(ax - 100, ay));
    options.first = 0; options.count = oldCount; preferences = savedPreferences;
    applyTheme(); redraw();
    ListView_SetItemState(legendStudies, 0, LVIS_SELECTED, LVIS_SELECTED);
    SendMessage(window, WM_MOUSELEAVE, 0, 0); require(!crossVisible, "Crosshair mouse leave failed");
    activateLegendCell(1, NameColumn);
    require(chartPanels.size() == 1 && options.studies.size() == 2, "Hide panel must retain studies");
    NMITEMACTIVATE trash{}; trash.hdr.hwndFrom = legendStudies; trash.hdr.idFrom = LegendStudies; trash.hdr.code = NM_CLICK; trash.iItem = 0; trash.iSubItem = DeleteColumn;
    SendMessage(legendWindow, WM_NOTIFY, LegendStudies, reinterpret_cast<LPARAM>(&trash));
    require(options.studies.size() == 1 && options.studies[0].index == 1, "Remove selected study failed");
    restoreStudies(); require(options.studies.size() == 1, "Removed study was restored unexpectedly");
    auto originalBackground=backgroundRules;
    showBackgroundRules();SendMessage(backgroundWindow,WM_COMMAND,BgNew,0);
    filterBackgroundStudies(BgStudy,L"_ma",true);
    auto studyCombo=GetDlgItem(backgroundWindow,BgStudy);for(int row=0;row<SendMessage(studyCombo,CB_GETCOUNT,0,0);++row){auto source=SendMessage(studyCombo,CB_GETITEMDATA,row,0);auto name=data.studies[source].name;std::transform(name.begin(),name.end(),name.begin(),[](unsigned char c){return static_cast<char>(std::tolower(c));});require(name.find("_ma")!=std::string::npos,"Background study search must match case-insensitive substrings");}
    backgroundNodeIndex=1;fillBackgroundEditor();SendDlgItemMessage(backgroundWindow,BgValueType,CB_SETCURSEL,1,0);filterBackgroundStudies(BgValueStudy,wide(data.studies.front().name));saveBackgroundNode();
    require(backgroundDraft.back().root.children[0].valueStudy==data.studies.front().name,"Background comparison study must be saved");
    backgroundDraft.back().root.children[0].valueStudy.clear();backgroundNodeIndex=0;
    backgroundDraft.back().root.children[0].study=data.studies.front().name;
    backgroundDraft.back().root.children[0].op=3;backgroundDraft.back().root.children[0].value=0;
    backgroundDraft.back().color=0x304568;rebuildBackgroundTree();previewBackground(false);
    require(backgroundPreview && backgroundRules.size()==originalBackground.size(),"Preview must not save rules");
    closeBackground();require(backgroundCache==calculateBackground(data,originalBackground),"Closing preview must restore applied colors");
    previewBackground(true);require(!backgroundPreview && backgroundRules.size()==originalBackground.size()+1,"Apply must save background rule");
    auto bgPath=output;bgPath+=L".background-settings";writeSettings(bgPath);backgroundRules.clear();readSettings(bgPath);std::filesystem::remove(bgPath);
    require(backgroundRules.back().color==0x304568 && backgroundRules.back().root.children[0].study==data.studies.front().name,"Background settings round trip failed");
    preferences.dark=true;options.studies.clear();options.activeStudy=-1;options.first=0;options.count=300;
    backgroundRules.back().root.children[0].value=data.studies.front().values[150];backgroundCache=calculateBackground(data,backgroundRules);redraw();applyTheme();
    auto clipboardImage=chartClipboardDib();auto dib=static_cast<BITMAPINFOHEADER*>(GlobalLock(clipboardImage));
    require(dib && dib->biBitCount==24 && dib->biCompression==BI_RGB && dib->biWidth==static_cast<LONG>(bitmap->GetWidth()) && dib->biHeight==static_cast<LONG>(bitmap->GetHeight()),"Clipboard DIB format/dimensions incorrect");
    require(GlobalSize(clipboardImage)>=sizeof(BITMAPINFOHEADER)+dib->biSizeImage,"Clipboard DIB pixels missing");GlobalUnlock(clipboardImage);GlobalFree(clipboardImage);
    for(int action:std::initializer_list<int>{OpenCsv,OpenBinary,OpenDatabase,ZoomIn,ZoomOut,Latest,Goto,Legend,AddFinancial,Settings,Refresh}) {auto icon=toolbarPng(action);require(icon && icon->GetWidth()==32 && icon->GetHeight()==32,"Embedded toolbar PNG missing");}
    {auto screen=GetDC(window);auto dc=CreateCompatibleDC(screen);auto pixels=CreateCompatibleBitmap(screen,684,64);auto old=SelectObject(dc,pixels);bool dark=preferences.dark;
    for(int row=0;row<2;++row){preferences.dark=row==0;RECT strip{0,row*32,684,row*32+32};FillRect(dc,&strip,appBackground());int iconX=0;for(const auto& action:toolbarActions){if(action.id){NMCUSTOMDRAW draw{};draw.hdc=dc;draw.dwDrawStage=CDDS_ITEMPREPAINT;draw.dwItemSpec=action.id;draw.rc={iconX,row*32,iconX+36,row*32+32};toolbarDraw(&draw);}iconX+=36;}}
    preferences.dark=dark;Gdiplus::Bitmap snap(pixels,nullptr);auto path=output;path+=L".toolbar.png";snap.Save(path.c_str(),&png);SelectObject(dc,old);DeleteObject(pixels);DeleteDC(dc);ReleaseDC(window,screen);}
    require(toolbarActions[2].id==OpenDatabase && toolbarActions[3].id==Refresh && toolbarActions[4].id==0 && toolbarActions[5].id==LoadWorkspaceMenu && toolbarActions[6].id==SaveWorkspaceMenu && toolbarActions[7].id==0,"Workspace toolbar group/order incorrect");
    require(!toolbarPng(Volume) && !toolbarPng(NextSignal) && !toolbarPng(PreviousSignal),"Existing toolbar icons must remain");
    require(toolbarActions[12].id==Goto && toolbarActions[13].id==PreviousSignal && toolbarActions[14].id==NextSignal,"Signal toolbar order incorrect");
    MENUITEMINFOW themed{sizeof(themed)};themed.fMask=MIIM_FTYPE;GetMenuItemInfoW(GetMenu(window),0,TRUE,&themed);require((themed.fType&MFT_OWNERDRAW)!=0,"Dark menu styling missing");
    preferences.dark=false;applyTheme();GetMenuItemInfoW(GetMenu(window),0,TRUE,&themed);require(!(themed.fType&MFT_OWNERDRAW),"Light menu style not restored");preferences.dark=true;applyTheme();
    auto bgImage=output;bgImage+=L".background.png";chart->makeChart(bgImage.string().c_str());
    backgroundNodeIndex=1;fillBackgroundEditor();
    RECT br;GetClientRect(backgroundWindow,&br);auto bscreen=GetDC(backgroundWindow);auto bdc=CreateCompatibleDC(bscreen);auto bbm=CreateCompatibleBitmap(bscreen,br.right,br.bottom);auto bold=SelectObject(bdc,bbm);
    FillRect(bdc,&br,appBackground());SendMessage(backgroundWindow,WM_PRINT,reinterpret_cast<WPARAM>(bdc),PRF_CLIENT|PRF_CHILDREN|PRF_ERASEBKGND);
    Gdiplus::Bitmap bsnap(bbm,nullptr);auto bui=output;bui+=L".background-window.png";bsnap.Save(bui.c_str(),&png);SelectObject(bdc,bold);DeleteObject(bbm);DeleteDC(bdc);ReleaseDC(backgroundWindow,bscreen);
    backgroundRules=originalBackground;backgroundCache=calculateBackground(data,backgroundRules);redraw();
    require(SendMessage(toolbar,TB_COMMANDTOINDEX,Volume,0)==-1,"Volume toolbar toggle must be hidden");
    preferences.financialLegend=false;
    SendMessage(window,WM_COMMAND,AddFinancial,reinterpret_cast<LPARAM>(toolbar));
    require(preferences.financialLegend && SendMessage(toolbar,TB_ISBUTTONCHECKED,AddFinancial,0),"Financial toolbar must toggle on");
    SendMessage(window,WM_COMMAND,AddFinancial,reinterpret_cast<LPARAM>(toolbar));
    require(!preferences.financialLegend && !SendMessage(toolbar,TB_ISBUTTONCHECKED,AddFinancial,0),"Financial toolbar must toggle off");
    {CsvData many;many.studies.push_back({"TEST_SIDE",{}});for(int i=0;i<600;++i){many.time.push_back(i*300.0);many.open.push_back(100);many.high.push_back(101);many.low.push_back(99);many.close.push_back(100);many.volume.push_back(1);many.studies[0].values.push_back(i+1);}
    FinancialConfig config;config.study="TEST_SIDE";config.quantity=1;auto result=calculateFinancial(many,config);require(result.sides.size()==600,"SIDE values must not have a 256-value cap");}
    auto originalFinancial=financialConfig;
    auto sideStudy=std::find_if(data.studies.begin(),data.studies.end(),[](const auto& series) {return isSideStudy(series.name);});
    if(sideStudy!=data.studies.end()) {
        preferences.dark=true;options.studies.clear();options.activeStudy=-1;showFinancial();
        require(SendDlgItemMessage(financialWindow,FinStudy,CB_GETCOUNT,0,0)==data.studies.size(),"Signal study selector must list all studies");
        filterFinancialStudies(L"_sIdE",true);
        for(int i=0;i<SendDlgItemMessage(financialWindow,FinStudy,CB_GETCOUNT,0,0);++i){auto idx=SendDlgItemMessage(financialWindow,FinStudy,CB_GETITEMDATA,i,0);auto name=data.studies[idx].name;std::transform(name.begin(),name.end(),name.begin(),[](unsigned char c){return static_cast<char>(std::tolower(c));});require(name.find("_side")!=std::string::npos,"Signal study filter must ignore case");}
        filterFinancialStudies(wide(sideStudy->name),false);
        SetDlgItemTextW(financialWindow,FinQuantity,L"1");SendMessage(financialWindow,WM_COMMAND,FinApply,0);
        require(financialResult.bars.size()==data.time.size() && options.signalStudy>=0,"Financial UI must calculate all source bars");
        require(!financialResult.sides.empty() && ListView_GetItemCount(financialSides)>0,"Financial side controls missing");
        SendMessage(financialWindow,WM_COMMAND,FinDeselectAll,0);for(double side:financialSideRows)require(!financialConfig.sides.at(side).show,"Deselect all failed");
        SendMessage(financialWindow,WM_COMMAND,FinSelectAll,0);for(double side:financialSideRows)require(financialConfig.sides.at(side).show,"Select all failed");
        if(financialResult.signals.size()>1) {
            showSignalReport();
            require(reportSideMatches(4.9988,L"4.9") && reportSideMatches(-4.9988,L"4.9") && !reportSideMatches(-4.88,L"4.9"),"SIDE filter must match magnitude prefixes of either sign");
            auto reportTotal=signalReportRows.size();
            require(financialNumber(1234567.89)==L"1,234,567.89" && financialNumber(-1234.5,2,true)==L"(1,234.50)","Financial formatting must group thousands and use accounting losses");
            require(reportTime(data.time.front()).size()==16 && reportTime(data.time.front())[10]==L' ',"Report timestamps must use local minute precision without zone suffix");
            sortReportColumn(1,false);sortReportColumn(2,true);refreshSignalReport();
            for(size_t i=1;i<signalReportRows.size();++i){auto& a=signalReportRows[i-1];auto& b=signalReportRows[i];require(a.side<b.side || (a.side==b.side && a.entry<b.entry),"Report must sort by SIDE then entry time");}
            signalReportSorts={{8,true}};refreshSignalReport();
            for(size_t i=0;i<signalReportRows.size();++i)if(signalReportRows[i].net!=0){NMLVCUSTOMDRAW draw{};draw.nmcd.hdr.hwndFrom=signalReportList;draw.nmcd.hdr.code=NM_CUSTOMDRAW;draw.nmcd.dwDrawStage=CDDS_ITEMPREPAINT|CDDS_SUBITEM;draw.nmcd.dwItemSpec=i;draw.iSubItem=8;SendMessage(signalReportWindow,WM_NOTIFY,0,reinterpret_cast<LPARAM>(&draw));require(draw.clrText==(signalReportRows[i].net<0?RGB(240,70,70):RGB(35,190,90)),"Report P/L cells must use sign-dependent colors");}
            for(int id:{1901,1903,1904,1905,1950,1951}){auto button=GetDlgItem(signalReportWindow,id);auto tip=static_cast<HWND>(GetPropW(button,L"FIGCharts.Tooltip"));require(IsWindow(tip) && SendMessage(tip,TTM_GETTOOLCOUNT,0,0)==1,"Every report button must have a registered tooltip");}
            for(int id:{1904,1905}){auto button=GetDlgItem(signalReportWindow,id);require((GetWindowLongPtr(button,GWL_STYLE)&BS_TYPEMASK)==BS_OWNERDRAW,"Report icons must use owner drawing for pressed and focus states");SendMessage(button,BM_SETSTATE,TRUE,0);SendMessage(button,BM_SETSTATE,FALSE,0);}
            ListView_SetColumnWidth(signalReportList,0,321);ListView_SetColumnWidth(signalReportList,6,0);
            SendMessage(signalReportWindow,WM_COMMAND,1951,0);
            require(ListView_GetColumnWidth(signalReportList,0)!=321 && ListView_GetColumnWidth(signalReportList,6)==0,"Font resize must refit visible columns and preserve hidden columns");
            SendMessage(signalReportWindow,WM_COMMAND,1950,0);
            auto filterSide=std::abs(signalReportRows.front().side);
            SetDlgItemTextW(signalReportWindow,1902,fromUtf8(sideName(filterSide)).c_str());
            require(!signalReportRows.empty(),"Report filter must retain matching rows");
            for(const auto& row:signalReportRows)require(reportSideMatches(row.side,fromUtf8(sideName(filterSide))),"Report must show only matching SIDE rows");
            SetDlgItemTextW(signalReportWindow,1902,L"__no_match__");require(signalReportRows.empty(),"SIDE filter must support no matches");
            auto emptyCsv=signalReportCsv();require(emptyCsv.find(L"\"SIDE\"")!=std::wstring::npos && emptyCsv.find(L"\r\n")==emptyCsv.size()-2,"Empty CSV must retain column headers");
            SendMessage(signalReportWindow,WM_COMMAND,1903,0);require(signalReportRows.size()==reportTotal,"Clear must restore all report rows");
            auto reportCsv=signalReportCsv();require(static_cast<size_t>(std::count(reportCsv.begin(),reportCsv.end(),L'\n'))==reportTotal+1,"CSV must contain all displayed rows plus header");
            require(GetPropW(GetDlgItem(legendWindow,LegendAdd),L"FIGCharts.IconResource") && GetPropW(GetDlgItem(legendWindow,LegendMoveUp),L"FIGCharts.IconResource"),"Legend controls must use supplied icons");
            require(toolbarPng(SignalReport)!=nullptr,"Signal report icon must be embedded");
            require(SendMessage(toolbar,TB_ISBUTTONCHECKED,SignalReport,0),"Report toolbar toggle must be checked when open");
            require(ListView_GetBkColor(signalReportList)==appBackgroundColor(),"Report table must use the active theme");
            require(!(ListView_GetExtendedListViewStyle(signalReportList)&LVS_EX_GRIDLINES),"Report must use muted custom grid lines");
            SendMessage(window,WM_COMMAND,SignalReport,reinterpret_cast<LPARAM>(toolbar));
            require(!IsWindowVisible(signalReportWindow) && !SendMessage(toolbar,TB_ISBUTTONCHECKED,SignalReport,0),"Report toolbar toggle must hide report");
            SendMessage(window,WM_COMMAND,SignalReport,reinterpret_cast<LPARAM>(toolbar));
            require(IsWindowVisible(signalReportWindow),"Report toolbar toggle must reopen report");
            require(signalReportRows.size()==financialResult.signals.size(),"Report must include every signal entry");
            for(size_t i=1;i<signalReportRows.size();++i)require(signalReportRows[i-1].net>=signalReportRows[i].net,"Report must rank by net performance");
            ListView_SetItemState(signalReportList,0,LVIS_SELECTED|LVIS_FOCUSED,LVIS_SELECTED|LVIS_FOCUSED);
            auto reportEntry=signalReportRows.front().entry;
            SendMessage(signalReportWindow,WM_COMMAND,1901,0);
            require(selectedBar()==reportEntry,"Report Go To must select the entry bar");
            NMLISTVIEW reportClick{};reportClick.hdr.hwndFrom=signalReportList;reportClick.hdr.code=NM_DBLCLK;
            SendMessage(signalReportWindow,WM_NOTIFY,0,reinterpret_cast<LPARAM>(&reportClick));
            require(selectedBar()==reportEntry,"Report double click must select the entry bar");
            for(const auto& row:signalReportRows)require(std::abs(row.net-(row.gross-row.fees))<1e-8,"Report net must deduct commissions");
            ShowWindow(signalReportWindow,SW_HIDE);
            auto firstSignal=financialResult.signals.front().first;
            signalNavigationBar=firstSignal;signalNavigationFirst=options.first;signalNavigationCount=options.count;
            auto expectedSignal=financialResult.signalFrom(firstSignal,true,financialConfig.sides);menuCommand(NextSignal);
            require(signalNavigationBar==expectedSignal && selectedBar()==expectedSignal,"Next signal must select the exact entry bar");
            menuCommand(PreviousSignal);require(signalNavigationBar==firstSignal && selectedBar()==firstSignal,"Previous signal must return to prior entry even near chart boundary");
            auto priorFirst=options.first;menuCommand(PreviousSignal);require(signalNavigationBar==firstSignal && options.first==priorFirst,"Previous signal boundary must not wrap");
        }
        require(ListView_GetItemCount(financialSummary)==17,"Financial summary must pin 17 metrics");
        {auto savedOptions=options;options.studies={{0,true},{1,false}};options.activeStudy=1;options.count=std::min(size_t(200),data.time.size());
            for(int direction:{-1,1}){options.first=direction<0?0:data.time.size()-options.count;options.panExtra=direction*static_cast<double>(options.count)*2/3;redraw();
                require(chartPanels.size()==2,"Overscroll test requires a study panel");auto main=chartPanels.front(),study=chartPanels.back();auto bounds=panelRect(main);
                for(double index:{0.0,50.0,100.0})require(std::abs(main->getXCoor(index)-study->getXCoor(index))<=1,"Study and price panels must scroll together with signal labels enabled");
                {auto timeAxis=study->xAxis();auto timeTicks=timeAxis->getTicks();int previousX=-10000,labelCount=0;
                 for(int i=0;i<timeTicks.len;++i){auto label=timeAxis->getLabel(timeTicks[i]);if(!label||!*label)continue;std::string text=label;
                  require(text.size()==16&&text[4]=='-'&&text[10]=='\n'&&text[13]==':',"Overscroll must have only one custom date/time label set");
                  int labelX=study->getXCoor(timeTicks[i]);require(labelX-previousX>=100,"Overscroll time labels must not overlap");previousX=labelX;++labelCount;
                 }require(labelCount>0,"Overscroll must retain visible date/time labels");}
                if(direction<0)require(chartLeft+main->getAbsOffsetX()+main->getXCoor(0)>bounds.left+(bounds.right-bounds.left)/2,"Signal label layout must preserve left blank space");
                auto path=output;path+=direction<0?L".study-pan-start.png":L".study-pan-end.png";savePng(path,*chart);
            }options=savedOptions;redraw();}
        require(financialRows[13].member==&FinancialBar::side && financialValue(13).find(L'.')!=std::wstring::npos && financialValue(13).size()-financialValue(13).find(L'.')-1==4,"Pinned SIDE value must use four decimals");
        RECT pinnedBefore{};GetWindowRect(financialSummary,&pinnedBefore);
        ListView_Scroll(financialValues,0,200);RECT pinnedAfter{};GetWindowRect(financialSummary,&pinnedAfter);
        require(EqualRect(&pinnedBefore,&pinnedAfter) && ListView_GetTopIndex(financialSummary)==0,"SIDE scrolling must leave summary fixed");
        ListView_EnsureVisible(financialValues,0,FALSE);
        require(ListView_GetItemCount(financialValues)+ListView_GetItemCount(financialSummary)>=18,"Financial metrics missing");
        RECT fullFin{},compactFin{};GetWindowRect(financialWindow,&fullFin);
        SendDlgItemMessage(financialWindow,FinCompact,BM_SETCHECK,BST_CHECKED,0);SendMessage(financialWindow,WM_COMMAND,FinCompact,0);
        GetWindowRect(financialWindow,&compactFin);
        require(financialCompact && compactFin.right-compactFin.left<fullFin.right-fullFin.left,"Compact financial window must shrink");
        require(!(GetWindowLongPtr(GetDlgItem(financialWindow,FinStudy),GWL_STYLE)&WS_VISIBLE) && (GetWindowLongPtr(financialValues,GWL_STYLE)&WS_VISIBLE),"Compact mode must show only metrics and its toggle");
        SetWindowPos(financialWindow,nullptr,0,0,490,610,SWP_NOMOVE|SWP_NOZORDER|SWP_NOACTIVATE);GetWindowRect(financialWindow,&compactFin);
        updateFinancial();RECT afterFin{};GetWindowRect(financialWindow,&afterFin);require(EqualRect(&compactFin,&afterFin),"Chart updates must preserve compact financial size");
        SendDlgItemMessage(financialWindow,FinCompact,BM_SETCHECK,BST_UNCHECKED,0);SendMessage(financialWindow,WM_COMMAND,FinCompact,0);
        require(!financialCompact && (GetWindowLongPtr(GetDlgItem(financialWindow,FinStudy),GWL_STYLE)&WS_VISIBLE),"Full financial controls must be restored");
        RECT fr;GetClientRect(financialWindow,&fr);auto screen=GetDC(financialWindow);auto dc=CreateCompatibleDC(screen);auto bm=CreateCompatibleBitmap(screen,fr.right,fr.bottom);auto previous=SelectObject(dc,bm);
        FillRect(dc,&fr,appBackground());SendMessage(financialWindow,WM_PRINT,reinterpret_cast<WPARAM>(dc),PRF_CLIENT|PRF_CHILDREN|PRF_ERASEBKGND);
        Gdiplus::Bitmap snap(bm,nullptr);auto snapPath=output;snapPath+=L".financial-window.png";snap.Save(snapPath.c_str(),&png);
        SelectObject(dc,previous);DeleteObject(bm);DeleteDC(dc);ReleaseDC(financialWindow,screen);
        auto side=financialSideRows.front();auto net=financialResult.bars.back().net;
        int visibleMetrics=ListView_GetItemCount(financialValues);
        double beforeVisibility=financialResult.bars.back().net;
        SendDlgItemMessage(financialWindow,FinWinLoss,BM_SETCHECK,BST_UNCHECKED,0);SendMessage(financialWindow,WM_COMMAND,FinWinLoss,0);
        require(ListView_GetItemCount(financialValues)==visibleMetrics-static_cast<int>(financialSideRows.size()) && financialResult.bars.back().net==beforeVisibility,"W/L toggle must hide only W/L rows without changing calculations");
        auto wlPath=output;wlPath+=L".wl-settings";writeSettings(wlPath);financialConfig.showWinLoss=true;readSettings(wlPath);std::filesystem::remove(wlPath);
        require(!financialConfig.showWinLoss,"Settings must preserve W/L visibility");
        SendDlgItemMessage(financialWindow,FinWinLoss,BM_SETCHECK,BST_CHECKED,0);SendMessage(financialWindow,WM_COMMAND,FinWinLoss,0);
        require(ListView_GetItemCount(financialValues)==visibleMetrics,"W/L toggle must restore rows");
        ListView_SetCheckState(financialSides,0,FALSE);
        require(ListView_GetItemCount(financialValues)==visibleMetrics-3,"Hidden side must remove bar frequency, P/L, and W/L rows");
        ListView_SetCheckState(financialSides,0,TRUE);
        require(ListView_GetItemCount(financialValues)==visibleMetrics,"Showing side must restore its metrics");
        ListView_SetCheckState(financialSides,0,FALSE);
        require(!financialConfig.sides.at(side).show && financialResult.bars.back().net==net,"Signal visibility must not alter financial calculations");
        financialConfig.sides[side].color=0x123456;options.signalStyles=financialConfig.sides;
        auto path=output;path+=L".financial-settings";writeSettings(path);financialConfig={};readSettings(path);std::filesystem::remove(path);
        require(financialConfig.quantity==1 && financialConfig.sides.at(side).color==0x123456 && !financialConfig.sides.at(side).show,"Financial settings must preserve calculation and side appearance settings");
        refreshFinancialData();require(financialResult.bars.back().net==net,"Settings restored financial results changed");
        SendDlgItemMessage(financialWindow,FinAutoScroll,BM_SETCHECK,BST_CHECKED,0);SendMessage(financialWindow,WM_COMMAND,FinAutoScroll,0);
        require(financialAutoScroll && financialScrolledSide==financialResult.bars[selectedBar()].side,"Auto-scroll must follow the displayed bar SIDE");
        SendDlgItemMessage(financialWindow,FinAutoScroll,BM_SETCHECK,BST_UNCHECKED,0);SendMessage(financialWindow,WM_COMMAND,FinAutoScroll,0);
        require(!financialAutoScroll,"Auto-scroll must switch off");
        auto neutral=std::find_if(financialResult.bars.begin(),financialResult.bars.end(),[](const auto& b){return b.side!=0;});
        if(neutral!=financialResult.bars.end()) options.first=static_cast<size_t>(neutral-financialResult.bars.begin());
        options.count=200;options.first=std::min(options.first,data.time.size()-std::min(options.count,data.time.size()));
        financialConfig.sides[side].show=true;options.signalStyles=financialConfig.sides;
        for(int type=0;type<3;++type) { preferences.chartType=type;redraw();auto image=output;image+=L".signals-"+std::to_wstring(type)+L".png";savePng(image,*chart); }
        options.first=0;options.count=data.time.size();redraw();
        require(financialResult.bars.back().net==net,"Overview aggregation must not change full-resolution financial results");
        SendMessage(financialWindow,WM_COMMAND,FinRemove,0);require(options.signalStudy==-1 && financialResult.bars.empty(),"Removing financial legend must restore normal coloring");
    }
    financialConfig=originalFinancial;refreshFinancialData();
    if(options.studies.empty())addStudy(0,false);
    for(int type=0;type<3;++type){beginCellEdit(0,TypeColumn);SendMessage(cellEditor,CB_SETCURSEL,type,0);SendMessage(legendWindow,WM_COMMAND,MAKEWPARAM(CellEditor,CBN_SELENDOK),0);require(options.studies[0].type==type,"Study type editor failed");redraw();}
    ListView_SetItemState(legendStudies,0,LVIS_SELECTED|LVIS_FOCUSED,LVIS_SELECTED|LVIS_FOCUSED);RECT selectedRow{};ListView_GetItemRect(legendStudies,0,&selectedRow,LVIR_BOUNDS);
    SendMessage(legendStudies,WM_LBUTTONDOWN,MK_LBUTTON,MAKELPARAM(ListView_GetColumnWidth(legendStudies,EnableColumn)+20,(selectedRow.top+selectedRow.bottom)/2));require(options.activeStudy==-1 && legendSelection()==-1 && chartPanels.size()==1,"Clicking selected study must clear selection and panel");
    options.autoScale=true;options.priceOnly=true;redraw();auto priceArea=panelRect(chartPanels.front());int px=priceArea.right+20,py=(priceArea.top+priceArea.bottom)/2;
    double oldSpan=chartPanels.front()->yAxis()->getMaxValue()-chartPanels.front()->yAxis()->getMinValue();
    SendMessage(window,WM_LBUTTONDOWN,MK_LBUTTON,MAKELPARAM(px,py));SendMessage(window,WM_MOUSEMOVE,MK_LBUTTON,MAKELPARAM(px,py-60));SendMessage(window,WM_LBUTTONUP,0,MAKELPARAM(px,py-60));
    require(!options.autoScale && options.priceOnly && options.scaleMax-options.scaleMin<oldSpan && !verticalDragging,"Vertical axis drag must disable auto scale and zoom in");
    double savedLow=options.scaleMin,savedHigh=options.scaleMax;
    require(StudyEntry{}.decimals==2,"Study precision must default to two decimals");
    for(int precision:{0,4,10,2,4}){
        beginCellEdit(0,DecimalsColumn);SendMessage(cellEditor,CB_SETCURSEL,precision,0);
        SendMessage(legendWindow,WM_COMMAND,MAKEWPARAM(CellEditor,CBN_SELENDOK),0);
        require(options.studies[0].decimals==precision,"Decimal editor did not update study");
        auto value=data.studies[options.studies[0].index].values[selectedBar()];wchar_t expectedDecimals[512]{};swprintf_s(expectedDecimals,L"%.*f",precision,value);
        require(legendValue(0)==((value==Chart::NoValue || !std::isfinite(value))?L"--":expectedDecimals),"Legend decimal formatting incorrect");
    }
    auto retainedCount=options.studies.size();options.activeStudy=0;options.studies[0].overlay=false;redraw();
    require(chartPanels.size()==2,"Enabled text study must have detail panel");
    RECT enableCell{};ListView_GetSubItemRect(legendStudies,0,EnableColumn,LVIR_BOUNDS,&enableCell);
    SendMessage(legendStudies,WM_LBUTTONDOWN,MK_LBUTTON,MAKELPARAM(20,(enableCell.top+enableCell.bottom)/2));require(!options.studies[0].enabled && options.studies.size()==retainedCount && chartPanels.size()==1,"Disabled study must remain listed without a detail panel");
    activateLegendCell(0,EnableColumn);require(options.studies[0].enabled && chartPanels.size()==2,"Re-enabling study must restore detail panel");
    options.studies[0].overlay=true;activateLegendCell(0,EnableColumn);require(!options.studies[0].enabled && chartPanels.size()==1,"Disabled overlay must not create a panel");

    // Main chart must keep its native scrollbar; large-range navigation stays 32-bit.
    options.count=120;options.first=0;redraw();applyTheme();
    DWORD_PTR scrollbarState=0;
    require(!GetWindowSubclass(window,themedScroll::proc,themedScroll::subclassId,&scrollbarState),"Main chart must use the native scrollbar");
    SendMessage(window,WM_HSCROLL,SB_RIGHT,0);
    require(options.first==data.time.size()-options.count,"Native scrollbar end navigation failed");
    SendMessage(window,WM_HSCROLL,SB_LEFT,0);require(options.first==0,"Native scrollbar start navigation failed");
    auto priorChart=chart.get();SendMessage(window,WM_HSCROLL,SB_THUMBTRACK,0);
    require(chart.get()==priorChart,"Scrollbar tracking should defer rendering");
    SendMessage(window,WM_HSCROLL,SB_ENDSCROLL,0);
    // Compare image conversion costs without imposing machine-dependent timing assertions.
    LARGE_INTEGER frequency{},begin{},finish{};QueryPerformanceFrequency(&frequency);
    double conversionMs[2]{};int formats[]{Chart::PNG,Chart::BMP};
    for(int f=0;f<2;++f){QueryPerformanceCounter(&begin);for(int sample=0;sample<8;++sample){auto bytes=chart->makeChart(formats[f]);
        auto stream=SHCreateMemStream(reinterpret_cast<const BYTE*>(bytes.data),bytes.len);
        {Gdiplus::Bitmap decoded(stream);require(decoded.GetLastStatus()==Gdiplus::Ok && decoded.GetWidth()==bitmap->GetWidth(),"Live bitmap decoding failed");HBITMAP pixels{};require(decoded.GetHBITMAP(Gdiplus::Color(255,255,255),&pixels)==Gdiplus::Ok,"Live bitmap conversion failed");DeleteObject(pixels);}stream->Release();}
        QueryPerformanceCounter(&finish);conversionMs[f]=1000.0*(finish.QuadPart-begin.QuadPart)/frequency.QuadPart/8;}
    auto performanceFile=output;performanceFile+=L".performance.txt";{std::ofstream report(performanceFile);report<<"PNG frame conversion ms: "<<conversionMs[0]<<"\nBMP frame conversion ms: "<<conversionMs[1]<<'\n';}
    showStudyPicker();SetDlgItemTextW(studyPicker,PickerSearch,L"");
    int studyItems=static_cast<int>(SendMessage(pickerList,LB_GETCOUNT,0,0));
    if(studyItems>30){themedScroll::position(pickerList,SB_VERT,10);require(SendMessage(pickerList,LB_GETTOPINDEX,0,0)==10,"Custom study scrollbar failed");}
    // Export both palettes, including the non-client scrollbar, for visual inspection.
    RECT pickerRect{};GetWindowRect(pickerList,&pickerRect);auto scrollDC=GetDC(pickerList);auto scrollMemory=CreateCompatibleDC(scrollDC);
    auto scrollBitmap=CreateCompatibleBitmap(scrollDC,pickerRect.right-pickerRect.left,pickerRect.bottom-pickerRect.top);auto scrollOld=SelectObject(scrollMemory,scrollBitmap);
    bool originalDark=preferences.dark;
    for(bool dark:{true,false}){preferences.dark=dark;applyTheme();SendMessage(pickerList,WM_PRINT,reinterpret_cast<WPARAM>(scrollMemory),PRF_CLIENT|PRF_NONCLIENT|PRF_ERASEBKGND);
        Gdiplus::Bitmap capture(scrollBitmap,nullptr);auto file=output;file+=dark?L".scroll-dark.png":L".scroll-light.png";capture.Save(file.c_str(),&png);}
    preferences.dark=originalDark;applyTheme();SelectObject(scrollMemory,scrollOld);DeleteObject(scrollBitmap);DeleteDC(scrollMemory);ReleaseDC(pickerList,scrollDC);
    auto workspaceTest=output;workspaceTest+=L".figworkspace";
    preferences.title="Workspace round trip";options.volume=false;options.first=10;options.count=120;legendAdvanced=true;pickerExpanded=true;
    showStudyPicker();SetDlgItemTextW(studyPicker,PickerSearch,L"_MA");showFinancial();setFinancialCompact(true);
    SetWindowPos(legendWindow,nullptr,100,120,760,500,SWP_NOZORDER|SWP_NOACTIVATE);
    {RECT rect{};GetClientRect(legendWindow,&rect);auto screen=GetDC(legendWindow);auto dc=CreateCompatibleDC(screen);auto pixels=CreateCompatibleBitmap(screen,rect.right,rect.bottom);auto old=SelectObject(dc,pixels);
        FillRect(dc,&rect,appBackground());SendMessage(legendWindow,WM_PRINT,reinterpret_cast<WPARAM>(dc),PRF_CLIENT|PRF_CHILDREN|PRF_ERASEBKGND);
        Gdiplus::Bitmap snapshot(pixels,nullptr);auto file=output;file+=L".legend-enabled.png";snapshot.Save(file.c_str(),&png);
        SelectObject(dc,old);DeleteObject(pixels);DeleteDC(dc);ReleaseDC(legendWindow,screen);}
    financialAutoScroll=true;
    SetWindowTextW(signalFilterEdit,L"-7.7649");
    auto savedGotoSelection=gotoSelection;
    signalCsvPath=output;signalCsvPath+=L".csv";
    exportSignalReport(false);exportSignalReport(false);
    {std::ifstream csv(signalCsvPath,std::ios::binary);std::string content((std::istreambuf_iterator<char>(csv)),{});require(content==utf8(signalReportCsv()),"CSV exports must overwrite the saved destination with displayed rows");}
    financialConfig.sides[-4.9982]={0x123456,true};financialConfig.sides[7.1222]={0xabcdef,false};
    for(auto dialog:{legendWindow,financialWindow,signalReportWindow})if(dialog)SendMessage(dialog,WM_COMMAND,1951,0);
    require(panelFontSizes[0]==9.5 && panelFontSizes[1]==9.5 && panelFontSizes[2]==9.5,"Panel font controls must enlarge text independently");
    for(auto dialog:{legendWindow,financialWindow,signalReportWindow})SendMessage(dialog,WM_COMMAND,1953,0);
    require(panelFontBold[0] && panelFontBold[1] && panelFontBold[2],"All table font controls must support bold");
    for(auto dialog:{legendWindow,financialWindow,signalReportWindow})for(int id:{1950,1951,1953})require((GetWindowLongPtr(GetDlgItem(dialog,id),GWL_STYLE)&BS_TYPEMASK)==BS_OWNERDRAW,"Font buttons must use consistent owner drawing");
    setFinancialCompact(false);
    NMITEMACTIVATE toggle{};toggle.hdr={financialSummary,FinSummary,NM_CLICK};toggle.iItem=2;toggle.ptAction.x=8;
    SendMessage(financialWindow,WM_NOTIFY,FinSummary,reinterpret_cast<LPARAM>(&toggle));
    require(!financialTopEnabled[2],"Individual summary checkbox must disable its metric");
    setFinancialCompact(true);
    require(ListView_GetItemCount(financialSummary)==16 && summaryRow(2)==3,"Compact summary must omit disabled metrics");
    setFinancialCompact(false);
    require(ListView_GetItemCount(financialSummary)==17,"Expanded summary must retain all metric controls");
    RECT help{},buttons{};GetWindowRect(GetDlgItem(financialWindow,0x7ffe),&help);GetWindowRect(GetDlgItem(financialWindow,FinSelectAll),&buttons);
    require(help.bottom<buttons.top,"Financial help text must not overlap side selection buttons");

    setFinancialCompact(true);
    ListView_SetColumnWidth(signalReportList,0,321);ListView_SetColumnWidth(signalReportList,6,0);auto savedReportWidths=currentSignalReportWidths();
    workspacePath=workspaceTest;saveWorkspace();auto savedTitle=preferences.title;auto savedStudyCount=options.studies.size();auto savedFinancial=financialConfig;RECT savedRect{};GetWindowRect(legendWindow,&savedRect);
    preferences.title="changed";options.volume=true;options.first=0;options.studies.clear();
    ListView_SetColumnWidth(signalReportList,0,55);ListView_SetColumnWidth(signalReportList,6,99);
    require(workspaceSnapshot()!=workspaceBaseline,"Workspace dirty detection failed");
    options.autoScale=true;options.priceOnly=false;
    gotoSelectionSet=false;gotoSelection={};
    loadWorkspace(workspaceTest);
    require(panelFontBold[0] && panelFontBold[1] && panelFontBold[2],"Workspace must preserve bold table fonts");
    require(!financialTopEnabled[2],"Workspace must preserve individual financial metric visibility");
    require(currentSignalReportWidths()==savedReportWidths,"Workspace must restore report widths including hidden columns");
    DestroyWindow(signalReportWindow);signalReportWindow=signalReportList=nullptr;showSignalReport();ShowWindow(signalReportWindow,SW_HIDE);
    require(currentSignalReportWidths()==savedReportWidths,"Lazily created report must retain workspace column widths");
    require(signalCsvPath==std::filesystem::path(output.wstring()+L".csv") && panelFontSizes[0]==9.5 && panelFontSizes[1]==9.5 && panelFontSizes[2]==9.5,"Workspace must preserve CSV destination and panel fonts");
    require(financialConfig.sides.at(-4.9982).color==0x123456 && financialConfig.sides.at(7.1222).color==0xabcdef && !financialConfig.sides.at(7.1222).show,"Workspace must retain absent SIDE identities and colors");
    require(gotoSelectionSet && gotoSelection.wYear==savedGotoSelection.wYear && gotoSelection.wMonth==savedGotoSelection.wMonth && gotoSelection.wDay==savedGotoSelection.wDay && gotoSelection.wHour==savedGotoSelection.wHour && gotoSelection.wMinute==savedGotoSelection.wMinute && gotoSelection.wSecond==savedGotoSelection.wSecond,"Workspace Go To selection not restored");
    require(!options.autoScale && options.priceOnly && options.scaleMin==savedLow && options.scaleMax==savedHigh,"Workspace must restore manual price scale");
    require(preferences.title==savedTitle && !options.volume && options.first==10 && options.count==120 && options.studies.size()==savedStudyCount && options.studies[0].type==2 && !options.studies[0].enabled && options.studies[0].decimals==4,"Workspace chart settings not restored");
    require(financialCompact && legendAdvanced && pickerExpanded && controlText(studyPicker,PickerSearch)==L"_MA" && financialConfig.study==savedFinancial.study,"Workspace legend settings not restored");
    RECT restoredRect{};GetWindowRect(legendWindow,&restoredRect);require(EqualRect(&savedRect,&restoredRect),"Workspace legend geometry not restored");
    require(signalFilterText()==L"-7.7649","Workspace must preserve signal filter");
    require(financialAutoScroll && SendDlgItemMessage(financialWindow,FinAutoScroll,BM_GETCHECK,0,0)==BST_CHECKED,"Workspace must preserve financial auto-scroll");
    require(workspaceSnapshot()==workspaceBaseline,"Restored workspace must start clean");
    // Thousands of metric rows must not turn cursor updates into thousands of format calls.
    {auto originalResult=std::move(financialResult);auto originalConfig=financialConfig;auto originalFirst=options.first;auto originalCount=options.count;
        CsvData many;many.studies.push_back({"TEST_SIDE",{}});
        for(int i=0;i<2000;++i){many.time.push_back(i*300.0);many.open.push_back(100);many.high.push_back(101);many.low.push_back(99);many.close.push_back(100);many.volume.push_back(1);many.studies[0].values.push_back(i+1);}
        FinancialConfig config;config.study="TEST_SIDE";config.quantity=1;financialResult=calculateFinancial(many,config);financialConfig=config;
        options.first=0;options.count=120;crossVisible=false;signalNavigationBar=SIZE_MAX;financialRowsDirty=true;financialDisplayBar=SIZE_MAX;updateFinancial();
        require(financialRows.size()==6018,"Large financial legend row count incorrect");
        auto before=financialFormattedCells;auto studiesBefore=legendFormattedCells;
        for(int i=0;i<100;++i){options.first=i;requestLegendRefresh();}
        require(financialFormattedCells==before && legendFormattedCells==studiesBefore && legendRefreshPending,"Cursor requests must not calculate legend cells");
        SendMessage(window,WM_TIMER,LegendTimer,0);
        require(financialDisplayBar==selectedBar() && financialFormattedCells==before,"Refresh must use latest bar without eagerly formatting metrics");
        auto current=financialDisplayBar;auto point=financialResult.at(1,current);
        require(financialValue(17)==std::to_wstring(point.bars)+L"_"+std::to_wstring(point.frequency),"Lazy SIDE frequency incorrect");
        require(financialValue(18)==finNumber(point.closed),"Lazy SIDE closed P/L incorrect");
        require(financialValue(19)==L"0.00 (1) / 0.00 (0)","Lazy break-even W/L incorrect");
        auto after=financialFormattedCells;financialValue(19);require(after==financialFormattedCells,"Unchanged cell must reuse cache");
        require(financialValue(financialRows.size()-2)==L"0.00 (0) / 0.00 (0)","Off-screen metric should load on demand");
        wchar_t cell[128]{};NMLVDISPINFOW info{};info.hdr.hwndFrom=financialValues;info.hdr.code=LVN_GETDISPINFOW;info.item.mask=LVIF_TEXT;info.item.iItem=19-pinnedFinancialRows;info.item.iSubItem=1;info.item.pszText=cell;info.item.cchTextMax=128;
        SendMessage(financialWindow,WM_NOTIFY,FinValues,reinterpret_cast<LPARAM>(&info));require(std::wstring(cell)==financialValue(19),"Virtual financial list callback incorrect");
        financialResult=std::move(originalResult);financialConfig=originalConfig;options.first=originalFirst;options.count=originalCount;financialRowsDirty=true;financialDisplayBar=SIZE_MAX;updateLegend();
    }
    options.first=0;options.count=120;redraw();RECT endButton{};
    require(chartEndButtonRect(endButton),"Go-to-end button must appear when newer bars are off-screen");
    auto priorCount=options.count;auto priorScaleMin=options.scaleMin;auto priorScaleMax=options.scaleMax;
    SendMessage(window,WM_LBUTTONDOWN,MK_LBUTTON,MAKELPARAM((endButton.left+endButton.right)/2,(endButton.top+endButton.bottom)/2));
    require(options.first==data.time.size()-options.count && options.count==priorCount && !dragging && !chartEndButtonRect(endButton),"Go-to-end button must preserve zoom and hide at the end");
    require(options.scaleMin==priorScaleMin && options.scaleMax==priorScaleMax,"Go-to-end must preserve manual price scale");
    options.first=0;options.count=data.time.size();redraw();require(!chartEndButtonRect(endButton),"Go-to-end button must be hidden in full overview");
    SendMessage(window,WM_COMMAND,SignalFilterClear,0);require(signalFilterText().empty(),"Clear SIDE filter button failed");
    newWorkspace();
    require(data.time.empty() && !chart && !bitmap && currentFile.empty() && workspacePath.empty(),"New workspace must clear chart and file");
    require(options.studies.empty() && savedStudies.empty() && financialConfig.study.empty() && backgroundRules.empty(),"New workspace must clear studies and rules");
    require(!financialCompact && !financialAutoScroll && !legendAdvanced && signalFilterText().empty() && options.autoScale && options.priceOnly && !options.volume,"New workspace must reset settings");
    require(!legendWindow && !financialWindow && workspaceSnapshot()==workspaceBaseline,"New workspace must reset dialogs and start clean");
    std::filesystem::remove(workspaceTest);workspacePath.clear();
}
}
int WINAPI wWinMain(HINSTANCE instance, HINSTANCE, PWSTR, int show) {
    INITCOMMONCONTROLSEX common{sizeof(common), ICC_PROGRESS_CLASS | ICC_TREEVIEW_CLASSES | ICC_DATE_CLASSES | ICC_LISTVIEW_CLASSES | ICC_BAR_CLASSES}; InitCommonControlsEx(&common);
    Gdiplus::GdiplusStartupInput input; ULONG_PTR token; Gdiplus::GdiplusStartup(&token, &input, nullptr);
    int argc; auto argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    testing = argc == 4 && std::wstring(argv[1]) == L"--ui-test";
    enumerateZones(); if (!testing) readPreferences(preferenceFolder() / L"preferences.txt");
    int result = 0;
    try {
        if (argc > 1 && std::wstring(argv[1]) == L"--self-test") { testCsv(); testFigData(); testStudyCodec(); testFinancial(); testBackground(); }
        else if(argc==3 && std::wstring(argv[1])==L"--benchmark-study-decode"){benchmarkStudyDecode(argv[2]);}
        else if (argc == 4 && std::wstring(argv[1]) == L"--render") {
            auto d = CsvData::load(argv[2]); ChartOptions opts;
            opts.first = d.time.size() > opts.count ? d.time.size() - opts.count : 0;
            if (!d.studies.empty()) { opts.studies.push_back({0}); opts.activeStudy = 0; }
            auto c = render(d, opts, 1200, 850); savePng(argv[3], *c);
        } else {
            WNDCLASSW wc{}; wc.lpfnWndProc = proc; wc.hInstance = instance; wc.lpszClassName = L"FIGCharts"; wc.hIcon = LoadIconW(instance, MAKEINTRESOURCEW(101));
            wc.hCursor = LoadCursor(nullptr, IDC_ARROW); wc.hbrBackground = reinterpret_cast<HBRUSH>(COLOR_WINDOW + 1); RegisterClassW(&wc);
            auto aux = wc; aux.lpszClassName = L"FIGChartsAux"; aux.lpfnWndProc = auxiliaryProc; RegisterClassW(&aux);
            window = CreateWindowW(wc.lpszClassName, L"FIGCharts", WS_OVERLAPPEDWINDOW | WS_HSCROLL | WS_CLIPCHILDREN, CW_USEDEFAULT, CW_USEDEFAULT, 1400, 900, nullptr, nullptr, instance, nullptr);
            if (!window) throw std::runtime_error("Could not create FIGCharts window.");
            if(testing) {showLegend();showStudyPicker();if(legendWindow || studyPicker || financialWindow) throw std::runtime_error("Legends must not open before a dataset is loaded.");}
            ShowWindow(window, testing ? SW_HIDE : show);
            if(!testing) loadLastWorkspace();
            if (argc > 1) load(argv[testing ? 2 : 1]);
            if (testing) { testInterface(argv[3]); DestroyWindow(window); }

            MSG msg; while (GetMessage(&msg, nullptr, 0, 0) > 0) {
                if (backgroundWindow && IsWindowVisible(backgroundWindow) && IsDialogMessage(backgroundWindow, &msg)) continue;
                if (financialWindow && IsWindowVisible(financialWindow) && IsDialogMessage(financialWindow, &msg)) continue;
                if (studyPicker && IsWindowVisible(studyPicker) && IsDialogMessage(studyPicker, &msg)) continue;
                if (settingsWindow && IsWindowVisible(settingsWindow) && IsDialogMessage(settingsWindow, &msg)) continue;
                if (gotoWindow && IsWindowVisible(gotoWindow) && IsDialogMessage(gotoWindow, &msg)) continue;
                if (legendWindow && IsWindowVisible(legendWindow) && IsDialogMessage(legendWindow, &msg)) continue;
                if (!IsDialogMessage(window, &msg)) { TranslateMessage(&msg); DispatchMessage(&msg); }
            }
        }
    } catch (const std::exception& e) {
        if (argc <= 2) error(e);
        if (testing) { std::ofstream log(std::filesystem::path(argv[3]).concat(L".txt")); log << e.what(); }
        result = 1;
    }
    LocalFree(argv); releaseFrame(); clearImage(); toolbarPngs.clear(); Gdiplus::GdiplusShutdown(token); return result;
}





