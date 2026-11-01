#pragma once
enum LegendColumn { EnableColumn, NameColumn, ValueColumn, DisplayColumn, ColorColumn, LineColumn, WidthColumn, TypeColumn, DecimalsColumn, DeleteColumn };
const wchar_t* studyTypeNames[]{L"Line",L"Flat Lines",L"Dots"};
HWND cellEditor = nullptr, studyPicker = nullptr, pickerList = nullptr;
HWND legendPrices = nullptr, legendVolume = nullptr;
COLORREF headerPriceColor = RGB(25,148,119);
COLORREF legendBorderColor() { return preferences.dark ? RGB(43,53,66) : RGB(210,216,224); }
void legendGridBorder(HDC dc, RECT r) {
    auto pen = CreatePen(PS_SOLID, 1, legendBorderColor()); auto old = SelectObject(dc, pen);
    MoveToEx(dc, r.left, r.bottom - 1, nullptr); LineTo(dc, r.right - 1, r.bottom - 1); LineTo(dc, r.right - 1, r.top);
    SelectObject(dc, old); DeleteObject(pen);
}
LRESULT CALLBACK legendHeaderProc(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp, UINT_PTR, DWORD_PTR) {
    if (preferences.dark && (msg == WM_PAINT || msg == WM_PRINTCLIENT)) {
        PAINTSTRUCT ps{}; HDC dc = msg == WM_PAINT ? BeginPaint(hwnd, &ps) : reinterpret_cast<HDC>(wp);
        int saved = SaveDC(dc); RECT all; GetClientRect(hwnd, &all); FillRect(dc, &all, appBackground());
        SetBkMode(dc, TRANSPARENT); SetTextColor(dc, appTextColor()); SelectObject(dc, reinterpret_cast<HFONT>(SendMessage(hwnd,WM_GETFONT,0,0)));
        for (int i = 0; i < Header_GetItemCount(hwnd); ++i) {
            RECT r; Header_GetItemRect(hwnd, i, &r); legendGridBorder(dc, r);
            wchar_t text[128]{}; HDITEMW item{}; item.mask = HDI_TEXT; item.pszText = text; item.cchTextMax = 128; Header_GetItem(hwnd, i, &item);
            r.left += 5; r.right -= 3; DrawTextW(dc, text, -1, &r, DT_SINGLELINE | DT_VCENTER | DT_END_ELLIPSIS);
        }
        RestoreDC(dc, saved); if (msg == WM_PAINT) EndPaint(hwnd, &ps); return 0;
    }
    return DefSubclassProc(hwnd, msg, wp, lp);
}
bool legendAdvanced = false, pickerExpanded = false;
constexpr int pickerPaneWidth = 290;
int fittedPickerOffset = 0;
void fitLegend(bool reset = false);
int editingRow = -1, editingColumn = -1;
const wchar_t* lineNames[]{L"Solid", L"Dashed", L"Dotted", L"Dash-dot"};
void refreshStudyPicker();
void showStudyPicker();
void addStudy(int index, bool overlay);
int legendSelection() { return legendStudies ? ListView_GetNextItem(legendStudies, -1, LVNI_SELECTED) : -1; }
void closeCellEditor() {
    if (cellEditor) ShowWindow(cellEditor, SW_HIDE);
    editingRow = editingColumn = -1;
}
size_t legendDisplayBar=SIZE_MAX;
std::vector<size_t> legendValueBars;
size_t legendFormattedCells=0;
const std::wstring& legendValue(size_t row){
    static const std::wstring empty;
    if(row>=options.studies.size() || row>=legendValues.size() || legendDisplayBar>=data.time.size())return empty;
    if(legendValueBars[row]!=legendDisplayBar){
        double value=data.studies[options.studies[row].index].values[legendDisplayBar];
        if(value==Chart::NoValue || !std::isfinite(value))legendValues[row]=L"--";else{wchar_t text[512];swprintf_s(text,L"%.*f",options.studies[row].decimals,value);legendValues[row]=text;}
        legendValueBars[row]=legendDisplayBar;++legendFormattedCells;
    }return legendValues[row];
}
void rebuildStudyRows(int selection = -1) {
    if (!legendStudies) return;
    closeCellEditor(); syncingLegend = true; SendMessage(legendStudies, WM_SETREDRAW, FALSE, 0);
    ListView_DeleteAllItems(legendStudies); legendValues.assign(options.studies.size(), L"");legendValueBars.assign(options.studies.size(),SIZE_MAX);legendDisplayBar=SIZE_MAX;
    for (size_t i = 0; i < options.studies.size(); ++i) {
        auto& e = options.studies[i]; auto name = wide(data.studies[e.index].name);
        LVITEMW item{}; item.mask = LVIF_TEXT; item.iItem = static_cast<int>(i); item.pszText = const_cast<wchar_t*>(e.enabled?L"Enabled":L"Disabled"); ListView_InsertItem(legendStudies, &item);
        ListView_SetItemText(legendStudies,static_cast<int>(i),NameColumn,name.data());
        ListView_SetItemText(legendStudies,static_cast<int>(i),ValueColumn,LPSTR_TEXTCALLBACKW);
        ListView_SetItemText(legendStudies, static_cast<int>(i), DisplayColumn, const_cast<wchar_t*>(e.overlay ? L"Text unchecked (overlay)" : L"Text checked"));
        wchar_t color[16]; swprintf_s(color, L"#%06X", e.color); ListView_SetItemText(legendStudies, static_cast<int>(i), ColorColumn, color);
        ListView_SetItemText(legendStudies, static_cast<int>(i), LineColumn, const_cast<wchar_t*>(lineNames[e.pattern]));
        ListView_SetItemText(legendStudies,static_cast<int>(i),TypeColumn,const_cast<wchar_t*>(studyTypeNames[e.type]));
        auto decimals=std::to_wstring(e.decimals);ListView_SetItemText(legendStudies,static_cast<int>(i),DecimalsColumn,decimals.data());
        auto width = std::to_wstring(e.width) + L" pt"; ListView_SetItemText(legendStudies, static_cast<int>(i), WidthColumn, width.data());
        ListView_SetItemText(legendStudies, static_cast<int>(i), DeleteColumn, const_cast<wchar_t*>(L"Delete"));
    }
    if (selection >= 0 && selection < static_cast<int>(options.studies.size())) ListView_SetItemState(legendStudies, selection, LVIS_SELECTED | LVIS_FOCUSED, LVIS_SELECTED | LVIS_FOCUSED);
    SendMessage(legendStudies, WM_SETREDRAW, TRUE, 0); InvalidateRect(legendStudies, nullptr, FALSE); syncingLegend = false;
    refreshStudyPicker();
}
void restoreStudies(bool readDisk = true) {
    if (!testing && readDisk) readSettings(settingsPath());
    options.studies.clear(); options.activeStudy = -1;
    for (size_t i = 0; i < data.studies.size(); ++i) {
        auto it = savedStudies.find(data.studies[i].name);
        if (it != savedStudies.end()) { auto e = it->second; e.index = static_cast<int>(i); options.studies.push_back(e); }
    }
    std::stable_sort(options.studies.begin(),options.studies.end(),[](const auto& a,const auto& b){return a.order<b.order;});
    rebuildStudyRows();
    refreshStudyPicker();
}
void updateLegend() {
    updateFinancial();
    if (!legendText || data.time.empty() || (!testing && !IsWindowVisible(legendWindow))) return;
    auto bar = selectedBar();bool changed=legendDisplayBar!=bar;legendDisplayBar=bar;
    std::wostringstream out; out << std::fixed << std::setprecision(2);
    auto stamp = timestamp(data.time[bar]);
    if (stamp != lastLegendText) { SetWindowTextW(legendText, stamp.c_str()); lastLegendText = stamp; }
    bool up = options.type == ChartType::Candle ? data.close[bar] >= data.open[bar] : (bar <= options.first || data.close[bar] >= data.close[bar - 1]);
    auto color = options.type == ChartType::Line ? RGB(39,104,215) : up ? RGB(25,148,119) : RGB(214,83,96);
    if(options.signalStudy >= 0) {
        double side = data.studies[options.signalStudy].values[bar];int rgb=0xffffff;
        auto found=options.signalStyles.find(validSide(side)?sideKey(side):0);
        if(found!=options.signalStyles.end() && found->second.show) rgb=found->second.color;
        color=RGB((rgb>>16)&255,(rgb>>8)&255,rgb&255);
    }
    if (color != headerPriceColor) { headerPriceColor = color; InvalidateRect(legendPrices, nullptr, FALSE); }
    out << L"Open " << data.open[bar] << L"    High " << data.high[bar]
        << L"\r\nLow  " << data.low[bar] << L"    Close " << data.close[bar];
    static std::wstring lastPrices, lastVolume;
    auto prices = out.str(); if (prices != lastPrices) { SetWindowTextW(legendPrices, prices.c_str()); lastPrices = prices; }
    std::wostringstream vol; vol << std::fixed << std::setprecision(2); if (options.volume) vol << L"Volume " << data.volume[bar];
    auto volume = vol.str(); if (volume != lastVolume) { SetWindowTextW(legendVolume, volume.c_str()); lastVolume = volume; }
    if(changed)InvalidateRect(legendStudies,nullptr,FALSE);
}
void fitLegend(bool reset) {
    if (!legendStudies) return;
    auto dc = GetDC(legendStudies); auto old = SelectObject(dc, reinterpret_cast<HFONT>(SendMessage(legendStudies,WM_GETFONT,0,0)));
    auto measure = [&](const std::wstring& text) { SIZE sz{}; GetTextExtentPoint32W(dc, text.c_str(), static_cast<int>(text.size()), &sz); return static_cast<int>(sz.cx); };
    int nameWidth = measure(L"Study (select to inspect)") + 16, valueWidth = measure(L"Value") + 16;
    for (auto& entry : options.studies) nameWidth = std::max(nameWidth, measure(wide(data.studies[entry.index].name)) + 16);
    legendDisplayBar=selectedBar();
    for(size_t row=0;row<legendValues.size();++row)valueWidth=std::max(valueWidth,measure(legendValue(row))+16);
    if (!reset) valueWidth = std::max(valueWidth, ListView_GetColumnWidth(legendStudies, ValueColumn));
    int widths[]{54,nameWidth,valueWidth,42,45,82,72,90,80,55}; int total = 0;
    for (int i = 0; i < 10; ++i) { int width = (i <= DisplayColumn || i == DeleteColumn || legendAdvanced) ? widths[i] : 0; total += width;
        if (ListView_GetColumnWidth(legendStudies, i) != width) ListView_SetColumnWidth(legendStudies, i, width);
    }
    int content = 342;
    for (HWND h : {legendText, legendPrices, legendVolume}) {
        wchar_t label[1024]{}; GetWindowTextW(h, label, 1024); std::wistringstream lines(label); std::wstring line;
        while (std::getline(lines, line)) content = std::max(content, measure(line) + 24);
    }
    SelectObject(dc, old); ReleaseDC(legendStudies, dc);
    RECT header{}; GetWindowRect(ListView_GetHeader(legendStudies), &header);
    RECT item{}; int rowHeight = 20; if (ListView_GetItemRect(legendStudies, 0, &item, LVIR_BOUNDS)) rowHeight = item.bottom - item.top;
    int tableHeight = header.bottom - header.top + std::max(1, static_cast<int>(options.studies.size())) * rowHeight + 4;
    int offset = pickerExpanded ? pickerPaneWidth : 0;
    RECT desired{0,0,std::max(content, total + 28) + offset,std::max(160 + tableHeight, pickerExpanded ? 400 : 0)};
    AdjustWindowRectEx(&desired, static_cast<DWORD>(GetWindowLongPtr(legendWindow, GWL_STYLE)), FALSE, static_cast<DWORD>(GetWindowLongPtr(legendWindow, GWL_EXSTYLE)));
    MONITORINFO monitor{sizeof(monitor)}; GetMonitorInfo(MonitorFromWindow(legendWindow, MONITOR_DEFAULTTONEAREST), &monitor);
    RECT current{}; GetWindowRect(legendWindow, &current);
    int width = std::min(desired.right - desired.left, monitor.rcWork.right - monitor.rcWork.left), height = std::min(desired.bottom - desired.top, monitor.rcWork.bottom - monitor.rcWork.top);
    int x = std::clamp(static_cast<int>(current.left) - (offset - fittedPickerOffset), static_cast<int>(monitor.rcWork.left), static_cast<int>(monitor.rcWork.right) - width);
    int y = std::clamp(static_cast<int>(current.top), static_cast<int>(monitor.rcWork.top), static_cast<int>(monitor.rcWork.bottom) - height);
    fittedPickerOffset = offset;
    if (current.right - current.left != width || current.bottom - current.top != height || current.left != x || current.top != y)
        SetWindowPos(legendWindow, nullptr, x, y, width, height, SWP_NOZORDER | SWP_NOACTIVATE);
}
void resizePickerPane() {
    int offset=pickerExpanded?pickerPaneWidth:0,delta=offset-fittedPickerOffset;fittedPickerOffset=offset;
    if(!delta) return;
    RECT r;GetWindowRect(legendWindow,&r);MONITORINFO monitor{sizeof(monitor)};GetMonitorInfo(MonitorFromWindow(legendWindow,MONITOR_DEFAULTTONEAREST),&monitor);
    int width=std::min(std::max(350L,r.right-r.left+delta),monitor.rcWork.right-monitor.rcWork.left);
    int x=std::clamp(static_cast<int>(r.left)-delta,static_cast<int>(monitor.rcWork.left),static_cast<int>(monitor.rcWork.right)-width);
    SetWindowPos(legendWindow,nullptr,x,r.top,width,r.bottom-r.top,SWP_NOZORDER|SWP_NOACTIVATE);
}
void layoutLegend() {
    if (!legendStudies) return;
    closeCellEditor(); RECT r; GetClientRect(legendWindow, &r);
    int offset = pickerExpanded ? pickerPaneWidth : 0;
    if (studyPicker) MoveWindow(studyPicker, 0, 0, pickerPaneWidth, r.bottom, TRUE);
    int width=std::max(20L,r.right-offset-24);
    auto heightFor=[&](HWND control,int minimum){
        auto dc=GetDC(control);auto previous=SelectObject(dc,reinterpret_cast<HFONT>(SendMessage(control,WM_GETFONT,0,0)));
        wchar_t text[1024]{};GetWindowTextW(control,text,1024);RECT bounds{0,0,width,0};
        DrawTextW(dc,text,-1,&bounds,DT_CALCRECT|DT_WORDBREAK|DT_NOPREFIX);
        TEXTMETRICW metrics{};GetTextMetricsW(dc,&metrics);SelectObject(dc,previous);ReleaseDC(control,dc);
        return std::max(minimum,std::max(static_cast<int>(bounds.bottom)+4,static_cast<int>(metrics.tmHeight)+4));
    };
    int dateHeight=heightFor(legendText,24),priceHeight=heightFor(legendPrices,38),volumeHeight=heightFor(legendVolume,20);
    int priceTop=10+dateHeight+9,volumeTop=priceTop+priceHeight+2,toolbarTop=volumeTop+volumeHeight+7,tableTop=toolbarTop+38;
    MoveWindow(legendText,offset+12,10,width,dateHeight,TRUE);
    MoveWindow(legendPrices,offset+12,priceTop,width,priceHeight,TRUE);
    MoveWindow(legendVolume,offset+12,volumeTop,width,volumeHeight,TRUE);
    MoveWindow(GetDlgItem(legendWindow,LegendAdd),offset+12,toolbarTop,30,28,TRUE);
    MoveWindow(GetDlgItem(legendWindow,LegendAdvanced),offset+122,toolbarTop,92,28,TRUE);
    MoveWindow(GetDlgItem(legendWindow,LegendMoveUp),offset+48,toolbarTop,30,28,TRUE);
    MoveWindow(GetDlgItem(legendWindow,LegendMoveDown),offset+84,toolbarTop,30,28,TRUE);
    if(GetDlgItem(legendWindow,1953))layoutPanelFontControls(legendWindow,offset+222,toolbarTop);
    MoveWindow(legendStudies,offset+12,tableTop,width,std::max(20L,r.bottom-tableTop-12),TRUE);
}
void activateLegendCell(int row,int column);
LRESULT CALLBACK legendListProc(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp, UINT_PTR, DWORD_PTR) {
    if(msg==WM_LBUTTONDOWN){LVHITTESTINFO hit{};hit.pt={GET_X_LPARAM(lp),GET_Y_LPARAM(lp)};ListView_SubItemHitTest(hwnd,&hit);
        if(hit.iItem>=0 && hit.iSubItem==EnableColumn){activateLegendCell(hit.iItem,EnableColumn);return 0;}
        if(hit.iItem<0 || (hit.iItem==legendSelection() && hit.iSubItem>=NameColumn && hit.iSubItem<=ValueColumn)){closeCellEditor();syncingLegend=true;ListView_SetItemState(hwnd,-1,0,LVIS_SELECTED|LVIS_FOCUSED);syncingLegend=false;options.activeStudy=-1;crossVisible=false;redraw();return 0;}}
    if (msg == WM_VSCROLL || msg == WM_HSCROLL || msg == WM_MOUSEWHEEL || msg == WM_MOUSEHWHEEL) closeCellEditor();
    if (msg == WM_NOTIFY) { auto n = reinterpret_cast<NMHDR*>(lp); if (n->code == HDN_BEGINTRACKW || n->code == HDN_BEGINTRACKA || n->code == HDN_ITEMCHANGEDW || n->code == HDN_ITEMCHANGEDA) closeCellEditor(); }
    return DefSubclassProc(hwnd, msg, wp, lp);
}
void showLegend() {
    if(data.time.empty()) return;
    const bool firstOpen = !legendWindow;
    if (!legendWindow) {
        legendWindow = CreateWindowExW(0, L"FIGChartsAux", L"FIGCharts - Legend", WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_CLIPCHILDREN,
            CW_USEDEFAULT, CW_USEDEFAULT, 940, 560, window, nullptr, GetModuleHandle(nullptr), nullptr);
        auto child = [&](const wchar_t* type, const wchar_t* label, int id, DWORD style) {
            auto h = CreateWindowW(type, label, WS_CHILD | WS_VISIBLE | style, 0, 0, 100, 24, legendWindow, reinterpret_cast<HMENU>(static_cast<INT_PTR>(id)), nullptr, nullptr);
            SendMessage(h, WM_SETFONT, reinterpret_cast<WPARAM>(font), TRUE); return h;
        };
        legendText = child(L"EDIT", L"Open a data file to view values.", 0, ES_MULTILINE | ES_READONLY);
        legendPrices = child(L"STATIC", L"", 0, 0);
        legendVolume = child(L"STATIC", L"", 0, 0);
        for(auto control:{legendText,legendPrices,legendVolume})SetPropW(control,L"FIGCharts.PanelFont",reinterpret_cast<HANDLE>(1));
        legendStudies = child(WC_LISTVIEWW, L"", LegendStudies, LVS_REPORT | LVS_SINGLESEL | LVS_SHOWSELALWAYS | WS_TABSTOP | WS_BORDER);
        ListView_SetExtendedListViewStyle(legendStudies, LVS_EX_FULLROWSELECT | LVS_EX_DOUBLEBUFFER);
        SetWindowSubclass(legendStudies, legendListProc, 1, 0);
        const wchar_t* titles[]{L"Enable",L"Study (select to inspect)", L"Value", L"Text", L"Color", L"Line", L"Thickness", L"Type", L"Decimals", L"Delete"};
        int widths[]{54,300,110,42,45,82,72,90,80,55};
        for (int i = 0; i < 10; ++i) { LVCOLUMNW c{}; c.mask = LVCF_TEXT | LVCF_WIDTH; c.pszText = const_cast<wchar_t*>(titles[i]); c.cx = i <= DisplayColumn || i == DeleteColumn || legendAdvanced ? widths[i] : 0; ListView_InsertColumn(legendStudies, i, &c); }
        SetWindowSubclass(ListView_GetHeader(legendStudies), legendHeaderProc, 1, 0);
        iconButton(child(L"BUTTON", L"Add study...", LegendAdd, WS_TABSTOP),2012,L"Add study");
        iconButton(child(L"BUTTON",L"Move up",LegendMoveUp,WS_TABSTOP),2013,L"Move selected study up");
        iconButton(child(L"BUTTON",L"Move down",LegendMoveDown,WS_TABSTOP),2014,L"Move selected study down");
        SetPropW(child(L"BUTTON", L"Advanced", LegendAdvanced, WS_TABSTOP | BS_AUTOCHECKBOX), L"FIGCharts.NoFocusBorder", reinterpret_cast<HANDLE>(1));
        addPanelFontControls(legendWindow,0,222,110);
        cellEditor = child(L"COMBOBOX", L"", CellEditor, CBS_DROPDOWNLIST | WS_TABSTOP | WS_VSCROLL);
        ShowWindow(cellEditor, SW_HIDE); layoutLegend(); rebuildStudyRows();
    }
    preferences.legend = true; saveAppPreferences(); applyTheme();
    ShowWindow(legendWindow, testing ? SW_HIDE : SW_SHOWNOACTIVATE); updateLegend();
    if (firstOpen) fitLegend(true);
}
void addStudy(int index, bool overlay) {
    if (index < 0 || index >= static_cast<int>(data.studies.size())) return;
    auto it = std::find_if(options.studies.begin(), options.studies.end(), [&](const auto& e) { return e.index == index; });
    if (it != options.studies.end()) { showLegend(); return; }
    const int colors[]{0x2768d7, 0xb05ba5, 0xe19a32, 0x138f7b};
    options.studies.push_back({index, overlay, colors[options.studies.size() % 4], 0, 1});
    showLegend(); rebuildStudyRows(static_cast<int>(options.studies.size() - 1)); redraw(); fitLegend(true); saveStudies();
}
void studyChanged(int i) { rebuildStudyRows(i); crossVisible = false; redraw(); saveStudies(); }
void moveLegendStudy(int delta) {
    int row=legendSelection(),target=row+delta;
    if(row<0 || target<0 || target>=static_cast<int>(options.studies.size()))return;
    closeCellEditor();std::swap(options.studies[row],options.studies[target]);
    if(options.activeStudy==row)options.activeStudy=target;else if(options.activeStudy==target)options.activeStudy=row;
    studyChanged(target);ListView_EnsureVisible(legendStudies,target,FALSE);
}
void highlightChartStudy(int row) {
    options.activeStudy=row;
    if(legendStudies){syncingLegend=true;ListView_SetItemState(legendStudies,-1,0,LVIS_SELECTED|LVIS_FOCUSED);
        if(row>=0){ListView_SetItemState(legendStudies,row,LVIS_SELECTED|LVIS_FOCUSED,LVIS_SELECTED|LVIS_FOCUSED);ListView_EnsureVisible(legendStudies,row,FALSE);}syncingLegend=false;}
}
int chartStudyAt(POINT point) {
    if(chartPanels.empty() || data.time.empty())return -1;
    int chosen=-1;double best=64;size_t end=std::min(data.time.size(),options.first+options.count);
    for(size_t row=0;row<options.studies.size();++row){auto& entry=options.studies[row];if(!entry.enabled || (!entry.overlay && static_cast<int>(row)!=options.activeStudy))continue;
        auto panel=entry.overlay?chartPanels.front():chartPanels.back();auto bounds=panelRect(panel);if(!PtInRect(&bounds,point))continue;
        const auto& values=data.studies[entry.index].values;bool previous=false;double px=0,py=0;
        auto distance=[&](double ax,double ay,double bx,double by){double dx=bx-ax,dy=by-ay,den=dx*dx+dy*dy;double t=den?std::clamp(((point.x-ax)*dx+(point.y-ay)*dy)/den,0.0,1.0):0;double x=point.x-ax-t*dx,y=point.y-ay-t*dy;double d=x*x+y*y;if(d<best){best=d;chosen=static_cast<int>(row);}};
        for(size_t start=options.first,index=0;start<end;start+=renderedStride,++index){double value=values[std::min(start+renderedStride,end)-1];
            if(!std::isfinite(value)||std::abs(value)>=1e300){previous=false;continue;}
            double x=chartLeft+panel->getAbsOffsetX()+panel->getXCoor(static_cast<double>(index)),y=chartTop+panel->getAbsOffsetY()+panel->getYCoor(value);
            if(entry.type==0 && previous)distance(px,py,x,y);
            else if(entry.type==1)distance(chartLeft+panel->getAbsOffsetX()+panel->getXCoor(index-0.4),y,chartLeft+panel->getAbsOffsetX()+panel->getXCoor(index+0.4),y);
            else distance(x,y,x,y);
            px=x;py=y;previous=true;
        }
    }return chosen;
}
void removeStudy(int i) {
    if (i < 0 || i >= static_cast<int>(options.studies.size())) return;
    savedStudies.erase(data.studies[options.studies[i].index].name); options.studies.erase(options.studies.begin() + i); options.activeStudy = -1;

    studyChanged(-1);
}
void beginCellEdit(int row, int column) {
    if (row < 0 || row >= static_cast<int>(options.studies.size()) || (column != LineColumn && column != WidthColumn && column != TypeColumn && column != DecimalsColumn)) return;
    closeCellEditor(); editingRow = row; editingColumn = column;
    SendMessage(cellEditor, CB_RESETCONTENT, 0, 0);
    if (column == LineColumn) for (auto text : lineNames) SendMessageW(cellEditor, CB_ADDSTRING, 0, reinterpret_cast<LPARAM>(text));
    else if(column==TypeColumn) for(auto text:studyTypeNames)SendMessageW(cellEditor,CB_ADDSTRING,0,reinterpret_cast<LPARAM>(text));
    else if(column==DecimalsColumn) for(int i=0;i<=10;++i){auto text=std::to_wstring(i);SendMessageW(cellEditor,CB_ADDSTRING,0,reinterpret_cast<LPARAM>(text.c_str()));}
    else for (int i = 1; i <= 6; ++i) { auto text = std::to_wstring(i) + L" pt"; SendMessageW(cellEditor, CB_ADDSTRING, 0, reinterpret_cast<LPARAM>(text.c_str())); }
    SendMessage(cellEditor, CB_SETCURSEL, column == LineColumn ? options.studies[row].pattern : column==TypeColumn?options.studies[row].type:column==DecimalsColumn?options.studies[row].decimals:options.studies[row].width - 1, 0);
    RECT r; ListView_GetSubItemRect(legendStudies, row, column, LVIR_BOUNDS, &r);
    MapWindowPoints(legendStudies, legendWindow, reinterpret_cast<POINT*>(&r), 2);
    SetWindowPos(cellEditor, HWND_TOP, r.left, r.top, r.right - r.left, 180, SWP_SHOWWINDOW);
    if (!testing) { SetFocus(cellEditor); SendMessage(cellEditor, CB_SHOWDROPDOWN, TRUE, 0); }
}
void activateLegendCell(int row, int column) {
    if (row < 0 || row >= static_cast<int>(options.studies.size())) return;
    closeCellEditor();
    if(column==EnableColumn){options.studies[row].enabled=!options.studies[row].enabled;crossVisible=false;
        ListView_SetItemText(legendStudies,row,EnableColumn,const_cast<wchar_t*>(options.studies[row].enabled?L"Enabled":L"Disabled"));
        InvalidateRect(legendStudies,nullptr,FALSE);redraw();saveStudies();return;}
    if (column == DeleteColumn) { removeStudy(row); return; }
    if (column == DisplayColumn) {
        options.studies[row].overlay = !options.studies[row].overlay; options.activeStudy = row; studyChanged(row); return;
    }
    if (column == ColorColumn) {
        auto color = options.studies[row].color; static COLORREF custom[16]{};
        CHOOSECOLORW dialog{sizeof(dialog)}; dialog.hwndOwner = legendWindow; dialog.rgbResult = RGB((color >> 16) & 255, (color >> 8) & 255, color & 255);
        dialog.lpCustColors = custom; dialog.Flags = CC_FULLOPEN | CC_RGBINIT;
        if (ChooseColorW(&dialog)) { options.studies[row].color = (GetRValue(dialog.rgbResult) << 16) | (GetGValue(dialog.rgbResult) << 8) | GetBValue(dialog.rgbResult); studyChanged(row); } return;
    }
    if (column == LineColumn || column == WidthColumn || column == TypeColumn || column == DecimalsColumn) { beginCellEdit(row, column); return; }
    options.activeStudy = row; crossVisible = false; redraw();
}
bool handleLegend(UINT msg, WPARAM wp, LPARAM lp) {
    if (msg == WM_PAINT) {
        PAINTSTRUCT ps{}; auto dc = BeginPaint(legendWindow, &ps); RECT r; GetClientRect(legendWindow, &r);
        RECT divider{(pickerExpanded ? pickerPaneWidth : 0) + 12, 35, r.right - 12, 37}; legendGridBorder(dc, divider); EndPaint(legendWindow, &ps); return true;
    }
    if (msg == WM_DRAWITEM && wp == LegendAdd) {
        auto item = reinterpret_cast<DRAWITEMSTRUCT*>(lp); auto dc = item->hDC; int saved = SaveDC(dc);
        auto brush = CreateSolidBrush(preferences.dark ? ((item->itemState & ODS_SELECTED) ? RGB(55,69,87) : RGB(38,50,66)) : RGB(238,241,246));
        FillRect(dc, &item->rcItem, brush); DeleteObject(brush);
        brush = CreateSolidBrush(legendBorderColor()); FrameRect(dc, &item->rcItem, brush); DeleteObject(brush);
        SetTextColor(dc, appTextColor()); SetBkMode(dc, TRANSPARENT); SelectObject(dc, font);
        RECT r = item->rcItem; DrawTextW(dc, L"Add study...", -1, &r, DT_CENTER | DT_VCENTER | DT_SINGLELINE);
        if (item->itemState & ODS_FOCUS) { InflateRect(&r, -3, -3); DrawFocusRect(dc, &r); }
        RestoreDC(dc, saved); return true;
    }
    if (msg == WM_GETMINMAXINFO) { reinterpret_cast<MINMAXINFO*>(lp)->ptMinTrackSize = {350, 220}; return true; }
    if (msg == WM_SIZE) { layoutLegend(); return true; }
    if (msg == WM_COMMAND) {
        if(LOWORD(wp)==LegendMoveUp || LOWORD(wp)==LegendMoveDown){moveLegendStudy(LOWORD(wp)==LegendMoveUp?-1:1);return true;}
        if (LOWORD(wp) == LegendAdvanced) { closeCellEditor(); legendAdvanced = SendDlgItemMessage(legendWindow, LegendAdvanced, BM_GETCHECK, 0, 0) == BST_CHECKED; fitLegend(true); return true; }
        if (LOWORD(wp) == LegendAdd) { if (pickerExpanded) { pickerExpanded = false; ShowWindow(studyPicker, SW_HIDE); resizePickerPane(); layoutLegend(); } else showStudyPicker(); return true; }
        if (LOWORD(wp) == CellEditor) {
            if (HIWORD(wp) == CBN_SELENDOK && editingRow >= 0) {
                int row = editingRow, column = editingColumn, selected = static_cast<int>(SendMessage(cellEditor, CB_GETCURSEL, 0, 0));
                if (selected >= 0 && row < static_cast<int>(options.studies.size())) {
                    if (column == LineColumn) options.studies[row].pattern = selected; else if(column==TypeColumn) options.studies[row].type=selected; else if(column==DecimalsColumn)options.studies[row].decimals=selected;else options.studies[row].width = selected + 1;
                    studyChanged(row);
                }
            } else if (HIWORD(wp) == CBN_SELENDCANCEL || HIWORD(wp) == CBN_KILLFOCUS) closeCellEditor();
            return true;
        }
    }
    if (msg == WM_NOTIFY) {
        auto nm = reinterpret_cast<NMHDR*>(lp);
        if (nm->idFrom != LegendStudies || syncingLegend) return false;
        if(nm->code==LVN_GETDISPINFOW){auto info=reinterpret_cast<NMLVDISPINFOW*>(lp);
            if((info->item.mask&LVIF_TEXT) && info->item.iSubItem==ValueColumn){const auto& text=legendValue(static_cast<size_t>(info->item.iItem));
                if(info->item.pszText && info->item.cchTextMax>0)wcsncpy_s(info->item.pszText,info->item.cchTextMax,text.c_str(),_TRUNCATE);
            }return true;}
        if (nm->code == NM_CLICK) { auto click = reinterpret_cast<NMITEMACTIVATE*>(lp); activateLegendCell(click->iItem, click->iSubItem); return true; }
        if (nm->code == LVN_ITEMCHANGED) {
            auto change = reinterpret_cast<NMLISTVIEW*>(lp);
            if ((change->uChanged & LVIF_STATE) && (change->uNewState & LVIS_SELECTED) && !(change->uOldState & LVIS_SELECTED)) {
                closeCellEditor(); options.activeStudy = change->iItem; crossVisible = false; redraw();
            } else if((change->uChanged&LVIF_STATE) && (change->uOldState&LVIS_SELECTED) && !(change->uNewState&LVIS_SELECTED) && legendSelection()<0){
                closeCellEditor();options.activeStudy=-1;crossVisible=false;redraw();
            }
        }
        if (nm->code == LVN_KEYDOWN) {
            auto key = reinterpret_cast<NMLVKEYDOWN*>(lp); int row = legendSelection();
            if (key->wVKey == VK_DELETE) removeStudy(row);
            else if (key->wVKey == VK_SPACE) activateLegendCell(row, EnableColumn);
            else if (key->wVKey == VK_F2) beginCellEdit(row, LineColumn);
        }
    }
    return false;
}
void refreshStudyPicker() {
    if (!pickerList) return;
    auto top=SendMessage(pickerList,LB_GETTOPINDEX,0,0);
    wchar_t query[512]{};GetDlgItemTextW(studyPicker,PickerSearch,query,512);
    SendMessage(pickerList, WM_SETREDRAW, FALSE, 0);
    SendMessage(pickerList, LB_RESETCONTENT, 0, 0);
    auto dc = GetDC(pickerList); auto previousFont = SelectObject(dc, font); LONG extent = 0;
    for (size_t i = 0; i < data.studies.size(); ++i) {
        bool added = std::any_of(options.studies.begin(), options.studies.end(), [&](const auto& e) { return e.index == static_cast<int>(i); });
        if (added) continue;
        auto name = wide(data.studies[i].name);
        if(query[0] && !StrStrIW(name.c_str(),query)) continue;
        SIZE size{}; GetTextExtentPoint32W(dc, name.c_str(), static_cast<int>(name.size()), &size); extent = std::max(extent, size.cx + 8);
        auto row = SendMessageW(pickerList, LB_ADDSTRING, 0, reinterpret_cast<LPARAM>(name.c_str()));
        SendMessage(pickerList, LB_SETITEMDATA, row, static_cast<LPARAM>(i));
    }
    SelectObject(dc, previousFont); ReleaseDC(pickerList, dc);
    SendMessage(pickerList, LB_SETHORIZONTALEXTENT, extent, 0);
    auto count=SendMessage(pickerList,LB_GETCOUNT,0,0);
    if(count>0)SendMessage(pickerList,LB_SETTOPINDEX,std::clamp<LRESULT>(top,0,count-1),0);
    SendMessage(pickerList, WM_SETREDRAW, TRUE, 0);InvalidateRect(pickerList,nullptr,TRUE);
    EnableWindow(GetDlgItem(studyPicker, PickerOverlay), FALSE); EnableWindow(GetDlgItem(studyPicker, PickerText), FALSE);
    SetWindowTextW(studyPicker, SendMessage(pickerList, LB_GETCOUNT, 0, 0) ? L"Add study - select a study and display mode" : L"Add study - no unselected studies available");
}
LRESULT CALLBACK studyPickerProc(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp) {
    try {
        if (msg >= WM_CTLCOLORMSGBOX && msg <= WM_CTLCOLORSTATIC) return themedControl(wp);
        if (msg == WM_SIZE && pickerList) {
            RECT r; GetClientRect(hwnd, &r);
            auto dc=GetDC(hwnd);auto old=SelectObject(dc,font);TEXTMETRICW metrics{};GetTextMetricsW(dc,&metrics);SelectObject(dc,old);ReleaseDC(hwnd,dc);
            int textHeight=std::min(22L,metrics.tmHeight);
            MoveWindow(GetDlgItem(hwnd,PickerSearchFrame),12,38,r.right-24,26,TRUE);
            MoveWindow(GetDlgItem(hwnd,PickerSearch),17,38+(26-textHeight)/2,r.right-60,textHeight,TRUE);
            MoveWindow(GetDlgItem(hwnd,PickerClear),r.right-38,40,24,22,TRUE);
            MoveWindow(pickerList, 12, 72, r.right - 24, std::max(40L, r.bottom - 124), TRUE);
            int width = (r.right - 32) / 2;
            MoveWindow(GetDlgItem(hwnd, PickerOverlay), 12, r.bottom - 40, width, 28, TRUE);
            MoveWindow(GetDlgItem(hwnd, PickerText), 20 + width, r.bottom - 40, width, 28, TRUE); return 0;
        }
        if (msg == WM_CLOSE) { SetFocus(GetDlgItem(legendWindow, LegendAdd)); pickerExpanded = false; ShowWindow(hwnd, SW_HIDE); resizePickerPane(); layoutLegend(); return 0; }
        if (msg == WM_COMMAND) {
            int id = LOWORD(wp);
            if(id==PickerSearch && HIWORD(wp)==EN_CHANGE){refreshStudyPicker();SendMessage(pickerList,LB_SETTOPINDEX,0,0);return 0;}
            if(id==PickerClear){SetDlgItemTextW(hwnd,PickerSearch,L"");SetFocus(GetDlgItem(hwnd,PickerSearch));return 0;}
            if (id == PickerClose || id == IDCANCEL) { SendMessage(hwnd, WM_CLOSE, 0, 0); return 0; }
            if (id == PickerList && HIWORD(wp) == LBN_SELCHANGE) {
                bool selected = SendMessage(pickerList, LB_GETCURSEL, 0, 0) != LB_ERR;
                EnableWindow(GetDlgItem(hwnd, PickerOverlay), selected); EnableWindow(GetDlgItem(hwnd, PickerText), selected); return 0;
            }
            if (id == PickerOverlay || id == PickerText || (id == PickerList && HIWORD(wp) == LBN_DBLCLK)) {
                auto row = SendMessage(pickerList, LB_GETCURSEL, 0, 0);
                if (row != LB_ERR) {
                    auto top=SendMessage(pickerList,LB_GETTOPINDEX,0,0);
                    addStudy(static_cast<int>(SendMessage(pickerList,LB_GETITEMDATA,row,0)),id!=PickerText);
                    // Adding may resize the legend and rebuild the picker more than once.
                    auto count=SendMessage(pickerList,LB_GETCOUNT,0,0);
                    if(count>0)SendMessage(pickerList,LB_SETTOPINDEX,std::clamp<LRESULT>(top,0,count-1),0);
                    SetFocus(pickerList);
                }
                return 0;
            }
        }
    } catch (const std::exception& e) { MessageBoxA(hwnd, e.what(), "Add study", MB_ICONERROR); }
    return DefWindowProcW(hwnd, msg, wp, lp);
}
void showStudyPicker() {
    if(data.time.empty()) return;
    showLegend();
    if (!studyPicker) {
        WNDCLASSW wc{}; wc.lpfnWndProc = studyPickerProc; wc.hInstance = GetModuleHandle(nullptr); wc.lpszClassName = L"FIGChartsStudyPicker"; wc.hCursor = LoadCursor(nullptr, IDC_ARROW); wc.hbrBackground = appBackground(); RegisterClassW(&wc);
        studyPicker = CreateWindowExW(WS_EX_CONTROLPARENT, wc.lpszClassName, L"Add study", WS_CHILD | WS_CLIPCHILDREN,
            0, 0, pickerPaneWidth, 400, legendWindow, nullptr, wc.hInstance, nullptr);
        auto child = [&](const wchar_t* type, const wchar_t* label, int id, int x, int y, int w, int h, DWORD style) {
            auto c = CreateWindowW(type, label, WS_CHILD | WS_VISIBLE | style, x, y, w, h, studyPicker, reinterpret_cast<HMENU>(static_cast<INT_PTR>(id)), nullptr, nullptr);
            SendMessage(c, WM_SETFONT, reinterpret_cast<WPARAM>(font), TRUE); return c;
        };
        child(L"STATIC", L"Available studies", 0, 12, 10, 180, 22, 0);
        child(L"BUTTON", L"Hide", PickerClose, 208, 6, 70, 26, WS_TABSTOP);
        RECT client{}; GetClientRect(studyPicker, &client);
        int contentWidth = client.right - 24, buttonWidth = (contentWidth - 8) / 2;
        child(L"STATIC",L"",PickerSearchFrame,12,38,contentWidth,26,WS_BORDER);
        auto search=child(L"EDIT",L"",PickerSearch,17,44,contentWidth-36,14,ES_AUTOHSCROLL|WS_TABSTOP);
        SendMessageW(search,EM_SETCUEBANNER,TRUE,reinterpret_cast<LPARAM>(L"Search studies..."));
        SendMessage(search,EM_SETLIMITTEXT,511,0);SendMessage(search,EM_SETMARGINS,EC_LEFTMARGIN|EC_RIGHTMARGIN,0);
        auto clear=child(L"BUTTON",L"Clear search",PickerClear,client.right-38,40,24,22,WS_TABSTOP);
        SetPropW(clear,L"FIGCharts.ClearSearch",reinterpret_cast<HANDLE>(1));
        pickerList = child(L"LISTBOX", L"", PickerList, 12, 38, contentWidth, 310, LBS_NOTIFY | WS_BORDER | WS_VSCROLL | WS_HSCROLL | WS_TABSTOP);

        child(L"BUTTON", L"Add overlay", PickerOverlay, 12, 365, buttonWidth, 30, WS_TABSTOP);
        child(L"BUTTON", L"Add text", PickerText, 20 + buttonWidth, 365, buttonWidth, 30, WS_TABSTOP);

    }
    SendMessage(studyPicker, WM_SIZE, 0, 0); // Layout after all children exist, including on first open.
    pickerExpanded = true; refreshStudyPicker(); ShowWindow(studyPicker, SW_SHOW); resizePickerPane(); layoutLegend(); applyTheme();
    if (!testing) { SetForegroundWindow(legendWindow); SetFocus(pickerList); }
}
LRESULT drawLegend(NMLVCUSTOMDRAW* draw) {
    if (draw->nmcd.dwDrawStage == CDDS_PREPAINT) return CDRF_NOTIFYITEMDRAW;
    if (draw->nmcd.dwDrawStage == CDDS_ITEMPREPAINT) return CDRF_NOTIFYSUBITEMDRAW;
    if (draw->nmcd.dwDrawStage == (CDDS_ITEMPOSTPAINT | CDDS_SUBITEM)) {
        RECT r; ListView_GetSubItemRect(legendStudies, static_cast<int>(draw->nmcd.dwItemSpec), draw->iSubItem, LVIR_BOUNDS, &r);
        if (draw->iSubItem == 0) r.right = r.left + ListView_GetColumnWidth(legendStudies, 0);
        legendGridBorder(draw->nmcd.hdc, r); return CDRF_DODEFAULT;
    }
    if (draw->nmcd.dwDrawStage != (CDDS_ITEMPREPAINT | CDDS_SUBITEM)) return CDRF_DODEFAULT;
    auto index = static_cast<size_t>(draw->nmcd.dwItemSpec); if (index >= options.studies.size()) return CDRF_DODEFAULT;
    const auto& e = options.studies[index]; int color = e.color, column = draw->iSubItem;
    draw->clrText = RGB((color >> 16) & 255, (color >> 8) & 255, color & 255); draw->clrTextBk = appBackgroundColor();
    if (column == NameColumn || column == ValueColumn) return CDRF_NEWFONT | CDRF_NOTIFYPOSTPAINT;
    auto dc = draw->nmcd.hdc; RECT r; ListView_GetSubItemRect(legendStudies, static_cast<int>(index), column, LVIR_BOUNDS, &r);
    if(column==EnableColumn)r.right=r.left+ListView_GetColumnWidth(legendStudies,EnableColumn);
    int saved = SaveDC(dc); IntersectClipRect(dc, r.left, r.top, r.right, r.bottom); FillRect(dc, &r, appBackground());
    SetBkMode(dc, TRANSPARENT); SetTextColor(dc, draw->clrText); SelectObject(dc, reinterpret_cast<HFONT>(SendMessage(legendStudies,WM_GETFONT,0,0)));
    if (column == DisplayColumn || column == EnableColumn) {
        bool checked=column==EnableColumn?e.enabled:!e.overlay;
        RECT box{(r.left + r.right) / 2 - 7, (r.top + r.bottom) / 2 - 7, (r.left + r.right) / 2 + 7, (r.top + r.bottom) / 2 + 7};
        if (preferences.dark) {
            auto brush = CreateSolidBrush(RGB(105,120,138)); FrameRect(dc, &box, brush); DeleteObject(brush);
            if (checked) { auto pen = CreatePen(PS_SOLID, 2, draw->clrText); auto old = SelectObject(dc, pen);
                MoveToEx(dc, box.left + 3, box.top + 7, nullptr); LineTo(dc, box.left + 6, box.top + 10); LineTo(dc, box.right - 2, box.top + 3);
                SelectObject(dc, old); DeleteObject(pen); }
        } else DrawFrameControl(dc, &box, DFC_BUTTON, DFCS_BUTTONCHECK | (checked ? DFCS_CHECKED : 0));

    } else if (column == ColorColumn) {
        int side = std::max(4L, std::min(16L, r.bottom - r.top - 6)); int x = (r.left + r.right - side) / 2, y = (r.top + r.bottom - side) / 2; RECT swatch{x, y, x + side, y + side}; auto brush = CreateSolidBrush(draw->clrText); FillRect(dc, &swatch, brush); DeleteObject(brush); FrameRect(dc, &swatch, reinterpret_cast<HBRUSH>(GetStockObject(GRAY_BRUSH)));
    } else if (column == LineColumn || column == WidthColumn || column == TypeColumn || column == DecimalsColumn) {
        RECT arrow = r; arrow.left = arrow.right - 18; InflateRect(&arrow, -1, -1); if (preferences.dark) {
            auto pen = CreatePen(PS_SOLID, 1, appTextColor()); auto old = SelectObject(dc, pen);
            int x = (arrow.left + arrow.right) / 2, y = (arrow.top + arrow.bottom) / 2;
            MoveToEx(dc, x - 3, y - 2, nullptr); LineTo(dc, x, y + 1); LineTo(dc, x + 4, y - 3);
            SelectObject(dc, old); DeleteObject(pen);
        } else DrawFrameControl(dc, &arrow, DFC_SCROLL, DFCS_SCROLLCOMBOBOX);
        RECT text = r; text.left += 5; text.right -= 20;
        auto width = column==DecimalsColumn?std::to_wstring(e.decimals):std::to_wstring(e.width) + L" pt"; DrawTextW(dc, column == LineColumn ? lineNames[e.pattern] : column==TypeColumn?studyTypeNames[e.type]:width.c_str(), -1, &text, DT_SINGLELINE | DT_VCENTER);
    } else if (column == DeleteColumn) {
        auto pen = CreatePen(PS_SOLID, 1, draw->clrText); SelectObject(dc, pen); SelectObject(dc, GetStockObject(NULL_BRUSH));
        int x = (r.left + r.right) / 2, y = (r.top + r.bottom) / 2;
        Rectangle(dc, x - 5, y - 3, x + 6, y + 6); MoveToEx(dc, x - 7, y - 5, nullptr); LineTo(dc, x + 8, y - 5);
        MoveToEx(dc, x - 2, y - 7, nullptr); LineTo(dc, x + 3, y - 7);
        for (int dx : {-2, 2}) { MoveToEx(dc, x + dx, y - 1, nullptr); LineTo(dc, x + dx, y + 4); }
        RestoreDC(dc, saved); DeleteObject(pen); legendGridBorder(dc, r); return CDRF_SKIPDEFAULT;
    }
    RestoreDC(dc, saved); legendGridBorder(dc, r); return CDRF_SKIPDEFAULT;
}



