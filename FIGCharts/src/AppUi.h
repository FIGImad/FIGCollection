#pragma once
enum {
    OpenCsv = 1000, OpenBinary, Refresh, ExitApp, About, Settings, AddStudiesMenu, CopyImage,
    AddTitle, EditSelected, ZoomAll, EnableLegend, DisableLegend, AddFinancial, NextSignal, PreviousSignal, BackgroundRulesMenu, LoadWorkspaceMenu, SaveWorkspaceMenu, SaveWorkspaceAsMenu, NewWorkspaceMenu,
    DefaultType = 1100, ThemeChoice, TimeChoice, ShowLegendSetting, ShowFinancialLegendSetting, ChartTitle,
    ApplyPreferences, ClosePreferences, SignalReport, CsvExportPath, CsvExportBrowse
};
HWND settingsWindow = nullptr;
void refreshMenus() {
    syncToolbar();
    auto menu = GetMenu(window); if (!menu) return;
    CheckMenuItem(menu, EnableLegend, MF_BYCOMMAND | (preferences.legend ? MF_CHECKED : MF_UNCHECKED));
    for(int id:{NextSignal,PreviousSignal,SignalReport}) EnableMenuItem(menu,id,MF_BYCOMMAND|(financialResult.signals.empty()?MF_GRAYED:MF_ENABLED));
    const int chartCommands[]{Export, Refresh, CopyImage, ZoomAll, ZoomIn, ZoomOut, Latest, Goto, EnableLegend, DisableLegend, AddStudiesMenu, AddFinancial, BackgroundRulesMenu, EditSelected};
    for (int id : chartCommands) EnableMenuItem(menu, id, MF_BYCOMMAND | (data.time.empty() ? MF_GRAYED : MF_ENABLED));
}
void saveAppPreferences() {
    refreshMenus();
    if (!testing && !restoringWorkspace) try { writePreferences(preferenceFolder() / L"preferences.txt"); }
    catch (const std::exception&) { if (statusLabel) SetWindowTextW(statusLabel, L"Preferences applied but could not be saved."); }
}
void applyTheme() {
    themeMenu(GetMenu(window));DrawMenuBar(window);
    for (auto top : {window, legendWindow, gotoWindow, settingsWindow, studyPicker, financialWindow, backgroundWindow, signalReportWindow}) if (top) {
        themeTitleBar(top);
        // Keep native chart scrolling out of the custom scrollbar paint/input path.
        if(top!=window)themedScroll::attach(top);
        SetClassLongPtr(top, GCLP_HBRBACKGROUND, reinterpret_cast<LONG_PTR>(appBackground()));
        EnumChildWindows(top, [](HWND h, LPARAM) -> BOOL {
            SetWindowTheme(h, preferences.dark ? L"" : nullptr, preferences.dark ? L"" : nullptr);
            wchar_t name[32]{}; GetClassNameW(h, name, 32);
            if (_wcsicmp(name, L"Button") == 0) SetWindowSubclass(h, themedButtonProc, 10, 0);
            if(_wcsicmp(name,WC_LISTVIEWW)==0 || _wcsicmp(name,WC_TREEVIEWW)==0 || _wcsicmp(name,L"ListBox")==0 || _wcsicmp(name,L"Edit")==0)themedScroll::attach(h);
            if(_wcsicmp(name,L"ComboBox")==0){COMBOBOXINFO info{sizeof(info)};if(GetComboBoxInfo(h,&info))themedScroll::attach(info.hwndList);}
            InvalidateRect(h, nullptr, TRUE); return TRUE;
        }, 0);
        RedrawWindow(top, nullptr, nullptr, RDW_INVALIDATE | RDW_ERASE | RDW_ALLCHILDREN);
    }
    if(backgroundTree) {TreeView_SetBkColor(backgroundTree,preferences.dark?RGB(24,32,43):RGB(255,255,255));TreeView_SetTextColor(backgroundTree,preferences.dark?RGB(220,228,239):RGB(38,50,68));TreeView_SetLineColor(backgroundTree,RGB(75,85,95));}
    for(auto list:{financialSummary,financialValues,financialSides,signalReportList}) if(list) {
        ListView_SetBkColor(list,appBackgroundColor());ListView_SetTextBkColor(list,appBackgroundColor());ListView_SetTextColor(list,appTextColor());
        SetWindowSubclass(ListView_GetHeader(list),legendHeaderProc,1,0);
    }
    if (legendStudies) {
        ListView_SetBkColor(legendStudies, appBackgroundColor()); ListView_SetTextBkColor(legendStudies, appBackgroundColor()); ListView_SetTextColor(legendStudies, appTextColor());
    }
}
HMENU buildMenu() {
    auto menu = CreateMenu();
    auto section = [&](const wchar_t* name) { auto popup = CreatePopupMenu(); AppendMenuW(menu, MF_POPUP, reinterpret_cast<UINT_PTR>(popup), name); return popup; };
    auto item = [](HMENU m, int id, const wchar_t* label) { AppendMenuW(m, MF_STRING, id, label); };
    auto file = section(L"&File"); item(file, OpenCsv, L"Open &CSV..."); item(file, OpenBinary, L"Open &binary..."); item(file, Open, L"Open data...");
    auto workspaces=CreatePopupMenu();AppendMenuW(file,MF_POPUP,reinterpret_cast<UINT_PTR>(workspaces),L"&Workspace");item(workspaces,NewWorkspaceMenu,L"New Workspace");AppendMenuW(workspaces,MF_SEPARATOR,0,nullptr);item(workspaces,LoadWorkspaceMenu,L"Load Workspace...");item(workspaces,SaveWorkspaceMenu,L"Save Workspace");item(workspaces,SaveWorkspaceAsMenu,L"Save Workspace As...");
    item(file, Export, L"Export &PNG..."); item(file, Refresh, L"&Refresh file"); AppendMenu(file, MF_SEPARATOR, 0, nullptr); item(file, ExitApp, L"E&xit");
    auto edit = section(L"&Edit"); item(edit, AddStudiesMenu, L"Add &Legend..."); item(edit, AddFinancial, L"Add &Financial Legend..."); item(edit, BackgroundRulesMenu, L"Background color rules..."); item(edit, CopyImage, L"&Copy chart image"); AppendMenu(edit, MF_SEPARATOR, 0, nullptr); item(edit, Settings, L"&Settings...");
    auto view = section(L"&View"); item(view, ZoomIn, L"Zoom &in"); item(view, ZoomOut, L"Zoom &out"); item(view, ZoomAll, L"Zoom &all");
    AppendMenu(view, MF_SEPARATOR, 0, nullptr);
    item(view, Latest, L"Go to &end"); item(view, Goto, L"Go to date/time..."); item(view, PreviousSignal, L"Go to Previous Signal"); item(view, NextSignal, L"Go to Next Signal"); AppendMenu(view, MF_SEPARATOR, 0, nullptr); item(view, EnableLegend, L"Enable legend"); item(view, DisableLegend, L"Disable legend");
    item(view, SignalReport, L"Signal performance report...");
    auto help = section(L"&Help"); item(help, About, L"&About FIGCharts"); return menu;
}
std::wstring controlText(HWND parent, int id) {
    auto h = GetDlgItem(parent, id); int length = GetWindowTextLengthW(h); std::wstring text(length + 1, L'\0'); GetWindowTextW(h, text.data(), length + 1); text.resize(length); return text;
}
LRESULT CALLBACK settingsProc(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp) {
    try {
        if (msg >= WM_CTLCOLORMSGBOX && msg <= WM_CTLCOLORSTATIC) return themedControl(wp);
        if (msg == WM_CLOSE) { ShowWindow(hwnd, SW_HIDE); return 0; }
        if (msg == WM_COMMAND) {
            int id = LOWORD(wp);
            if(id==CsvExportBrowse){auto path=chooseSignalCsvPath();if(!path.empty())SetDlgItemTextW(hwnd,CsvExportPath,path.c_str());return 0;}
            if (id == ClosePreferences || id == IDCANCEL) { ShowWindow(hwnd, SW_HIDE); return 0; }
            if (id == ApplyPreferences || id == IDOK) {
                signalCsvPath=controlText(hwnd,CsvExportPath);
                preferences.chartType = static_cast<int>(SendDlgItemMessage(hwnd, DefaultType, CB_GETCURSEL, 0, 0));
                preferences.dark = SendDlgItemMessage(hwnd, ThemeChoice, CB_GETCURSEL, 0, 0) == 1;
                int zone = static_cast<int>(SendDlgItemMessage(hwnd, TimeChoice, CB_GETCURSEL, 0, 0));
                preferences.timeMode = zone < 2 ? std::max(0, zone) : 2;
                preferences.zoneKey = zone >= 2 ? timeZones.at(zone - 2).TimeZoneKeyName : L"";
                preferences.title = utf8(controlText(hwnd, ChartTitle));
                preferences.legend = SendDlgItemMessage(hwnd, ShowLegendSetting, BM_GETCHECK, 0, 0) == BST_CHECKED;
                preferences.financialLegend = SendDlgItemMessage(hwnd, ShowFinancialLegendSetting, BM_GETCHECK, 0, 0) == BST_CHECKED;
                if(preferences.financialLegend && !data.time.empty()) showFinancial(); else if(financialWindow) ShowWindow(financialWindow,SW_HIDE);
                options.type = static_cast<ChartType>(preferences.chartType);
                crossVisible = false; if (preferences.legend) showLegend(); else if (legendWindow) ShowWindow(legendWindow, SW_HIDE);
                applyTheme(); redraw(); saveAppPreferences(); return 0;
            }

        }
    } catch (const std::exception& e) { MessageBoxA(hwnd, e.what(), "FIGCharts settings", MB_ICONERROR); }
    return DefWindowProcW(hwnd, msg, wp, lp);
}
void showSettings(bool focusTitle = false) {
    if (!settingsWindow) {
        WNDCLASSW wc{}; wc.lpfnWndProc = settingsProc; wc.hInstance = GetModuleHandle(nullptr); wc.lpszClassName = L"FIGChartsSettings"; wc.hCursor = LoadCursor(nullptr, IDC_ARROW); wc.hbrBackground = appBackground(); RegisterClassW(&wc);
        settingsWindow = CreateWindowExW(WS_EX_DLGMODALFRAME, wc.lpszClassName, L"FIGCharts - Settings", WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_CLIPCHILDREN, CW_USEDEFAULT, CW_USEDEFAULT, 700, 405, window, nullptr, wc.hInstance, nullptr);
        auto child = [&](const wchar_t* type, const wchar_t* label, int id, int x, int y, int w, int h, DWORD style = 0) {
            auto c = CreateWindowW(type, label, WS_CHILD | WS_VISIBLE | style, x, y, w, h, settingsWindow, reinterpret_cast<HMENU>(static_cast<INT_PTR>(id)), nullptr, nullptr);
            SendMessage(c, WM_SETFONT, reinterpret_cast<WPARAM>(font), TRUE); return c;
        };
        auto combo = [&](int id, int y, std::initializer_list<const wchar_t*> values) {
            auto c = child(L"COMBOBOX", L"", id, 185, y, 460, 300, CBS_DROPDOWNLIST | WS_TABSTOP | WS_VSCROLL);
            for (auto value : values) SendMessageW(c, CB_ADDSTRING, 0, reinterpret_cast<LPARAM>(value)); return c;
        };
        child(L"STATIC", L"Default chart type", 0, 20, 25, 160, 24); combo(DefaultType, 20, {L"OHLC", L"Candlestick", L"Close line"});
        child(L"STATIC", L"Theme", 0, 20, 65, 160, 24); combo(ThemeChoice, 60, {L"Light", L"Dark"});
        child(L"STATIC", L"Date/time zone", 0, 20, 105, 160, 24); auto zones = combo(TimeChoice, 100, {L"UTC", L"Local computer time"});
        for (auto& z : timeZones) SendMessageW(zones, CB_ADDSTRING, 0, reinterpret_cast<LPARAM>(z.TimeZoneKeyName));
        child(L"BUTTON", L"Show legend", ShowLegendSetting, 185, 140, 160, 25, BS_AUTOCHECKBOX | WS_TABSTOP);
        child(L"BUTTON", L"Show Financial Legend", ShowFinancialLegendSetting, 365, 140, 260, 25, BS_AUTOCHECKBOX | WS_TABSTOP);
        child(L"STATIC", L"Chart title", 0, 20, 185, 160, 24); child(L"EDIT", L"", ChartTitle, 185, 180, 460, 26, WS_BORDER | ES_AUTOHSCROLL | WS_TABSTOP);
        child(L"STATIC", L"Save chart configurations using File > Workspace.\nGo To uses the selected time zone.", 0, 20, 225, 630, 40);
        child(L"STATIC",L"Signal report CSV",0,20,275,160,24);
        child(L"EDIT",L"",CsvExportPath,185,270,360,26,WS_BORDER|ES_AUTOHSCROLL|ES_READONLY);
        child(L"BUTTON",L"Change...",CsvExportBrowse,555,270,90,26,WS_TABSTOP);
        child(L"BUTTON", L"Apply settings", ApplyPreferences, 395, 325, 120, 30, BS_DEFPUSHBUTTON | WS_TABSTOP);
        child(L"BUTTON", L"Close", ClosePreferences, 525, 325, 120, 30, WS_TABSTOP);
    }
    SendDlgItemMessage(settingsWindow, DefaultType, CB_SETCURSEL, preferences.chartType, 0);
    SetDlgItemTextW(settingsWindow,CsvExportPath,signalCsvPath.c_str());
    SendDlgItemMessage(settingsWindow, ThemeChoice, CB_SETCURSEL, preferences.dark ? 1 : 0, 0);
    SendDlgItemMessage(settingsWindow, ShowFinancialLegendSetting, BM_SETCHECK, preferences.financialLegend ? BST_CHECKED : BST_UNCHECKED, 0);
    int zone = preferences.timeMode;
    if (zone == 2) { zone = 0; for (size_t i = 0; i < timeZones.size(); ++i) if (preferences.zoneKey == timeZones[i].TimeZoneKeyName) zone = static_cast<int>(i + 2); }
    SendDlgItemMessage(settingsWindow, TimeChoice, CB_SETCURSEL, zone, 0);
    SendDlgItemMessage(settingsWindow, ShowLegendSetting, BM_SETCHECK, preferences.legend ? BST_CHECKED : BST_UNCHECKED, 0);
    SetDlgItemTextW(settingsWindow, ChartTitle, fromUtf8(preferences.title).c_str());
    applyTheme();
    if (!testing) { ShowWindow(settingsWindow, SW_SHOW); SetForegroundWindow(settingsWindow); SetFocus(GetDlgItem(settingsWindow, focusTitle ? ChartTitle : DefaultType)); }
}
void goToSignal(size_t target) {
    if(target>=data.time.size())return;
    options.panExtra=0;pendingExtra=0;
    if(renderedStride>1) options.count=std::min(size_t(200),data.time.size());
    size_t visible=std::min(options.count,data.time.size());options.first=std::min(target>visible/2?target-visible/2:0,data.time.size()-visible);
    signalNavigationBar=target;signalNavigationFirst=options.first;signalNavigationCount=options.count;crossVisible=false;redraw();
    if(!chartPanels.empty()) {auto p=chartPanels.front();auto r=panelRect(p);crossPoint={chartLeft+p->getAbsOffsetX()+p->getXCoor(static_cast<double>((target-options.first)/renderedStride)),(r.top+r.bottom)/2};crossVisible=true;}
    updateLegend();invalidateChart();
}
#include "SignalReport.h"
void navigateSignal(bool forward) {
    if(financialResult.signals.empty() || data.time.empty()) {SetWindowTextW(statusLabel,L"Set up a financial SIDE study first.");return;}
    bool anchored=signalNavigationBar<data.time.size() && signalNavigationFirst==options.first && signalNavigationCount==options.count;
    size_t anchor=anchored?signalNavigationBar:crossVisible?selectedBar():std::min(data.time.size()-1,options.first+std::min(options.count,data.time.size()-options.first)/2);
    auto text=signalFilterText();const wchar_t* start=text.c_str();while(iswspace(*start))++start;
    double filter=0;bool filtered=*start!=0;
    if(filtered){wchar_t* end=nullptr;filter=wcstod(start,&end);while(end && iswspace(*end))++end;
        if(end==start || !end || *end || !validSide(filter) || sideKey(filter)==0){SetWindowTextW(statusLabel,L"Enter a nonzero numeric SIDE, or clear the filter.");return;}}
    std::wstring prefix=start;while(!prefix.empty() && iswspace(prefix.back()))prefix.pop_back();
    if(!prefix.empty() && prefix.front()==L'+')prefix.erase(0,1);
    size_t target=financialResult.signalFrom(anchor,forward,financialConfig.sides,nullptr,utf8(prefix));
    if(target>=data.time.size()) {SetWindowTextW(statusLabel,filtered?L"No matching enabled signal in this direction.":forward?L"No next enabled signal.":L"No previous enabled signal.");return;}
    goToSignal(target);
}
// Packed 24-bit DIB is supported by image editors and document/chat paste targets.
HGLOBAL chartClipboardDib() {
    if(!chartPixels) throw std::runtime_error("Open a chart before copying its image.");
    BITMAP source{};if(!GetObject(chartPixels,sizeof(source),&source) || source.bmWidth<=0 || source.bmHeight<=0) throw std::runtime_error("Could not read chart image.");
    size_t stride=(static_cast<size_t>(source.bmWidth)*3+3)&~size_t(3),bytes=stride*source.bmHeight;
    if(bytes>MAXDWORD) throw std::runtime_error("Chart image is too large for the clipboard.");
    auto memory=GlobalAlloc(GMEM_MOVEABLE|GMEM_ZEROINIT,sizeof(BITMAPINFOHEADER)+bytes);
    if(!memory)throw std::runtime_error("Could not allocate clipboard image.");
    auto header=static_cast<BITMAPINFOHEADER*>(GlobalLock(memory));
    if(!header){GlobalFree(memory);throw std::runtime_error("Could not access clipboard image.");}
    header->biSize=sizeof(*header);header->biWidth=source.bmWidth;header->biHeight=source.bmHeight;header->biPlanes=1;header->biBitCount=24;header->biCompression=BI_RGB;header->biSizeImage=static_cast<DWORD>(bytes);
    auto dc=GetDC(window);int lines=GetDIBits(dc,chartPixels,0,source.bmHeight,header+1,reinterpret_cast<BITMAPINFO*>(header),DIB_RGB_COLORS);ReleaseDC(window,dc);GlobalUnlock(memory);
    if(lines!=source.bmHeight){GlobalFree(memory);throw std::runtime_error("Could not convert chart image for the clipboard.");}
    return memory;
}
bool menuCommand(int id) {
    if(id==ZoomAll || id==ZoomIn || id==ZoomOut || id==Latest){options.panExtra=0;pendingExtra=0;}
    switch (id) {
    case SignalReport: showSignalReport(); return true;
    case NewWorkspaceMenu: if(confirmWorkspaceSave())newWorkspace();return true;
    case LoadWorkspaceMenu: {if(!confirmWorkspaceSave())return true;auto path=chooseWorkspace(false);if(!path.empty())loadWorkspace(path);return true;}
    case SaveWorkspaceMenu: saveWorkspace();return true;
    case SaveWorkspaceAsMenu: saveWorkspace(true);return true;
    case NextSignal: navigateSignal(true); return true;
    case PreviousSignal: navigateSignal(false); return true;
    case OpenCsv: case OpenBinary: { auto path = choose(false, id == OpenCsv ? 3 : 2); if (!path.empty()) load(path); return true; }
    case Refresh: if (!currentFile.empty()) { auto first = options.first;auto active=options.activeStudy;saveStudies(); load(currentFile,false);options.activeStudy=active; options.first = std::min(first, data.time.size() > options.count ? data.time.size() - options.count : 0); redraw(); } return true;
    case ExitApp: SendMessage(window, WM_CLOSE, 0, 0); return true;
    case About: MessageBoxW(window, L"FIGCharts\nFinancial charts for FIGSignalCpp reports\n\nCSV and FIGDATA v1 support\nPowered by ChartDirector", L"About FIGCharts", MB_OK); return true;
    case Settings: showSettings(); return true;
    case AddTitle: showSettings(true); return true;
    case BackgroundRulesMenu: showBackgroundRules(); return true;
    case AddFinancial: showFinancial(); return true;
    case AddStudiesMenu: showLegend(); return true;
    case EditSelected: showLegend(); SetForegroundWindow(legendWindow); SetFocus(legendStudies); if (legendSelection() >= 0) beginCellEdit(legendSelection(), LineColumn); return true;
    case ZoomAll: if (!data.time.empty()) { options.first = 0; options.count = data.time.size(); crossVisible = false; redraw(); } return true;
    case EnableLegend: showLegend(); return true;
    case DisableLegend: if (legendWindow) ShowWindow(legendWindow, SW_HIDE); preferences.legend = false; saveAppPreferences(); return true;
    case CopyImage: {
        auto image=chartClipboardDib();
        if(!OpenClipboard(window)){GlobalFree(image);throw std::runtime_error("Clipboard is busy. Try again.");}
        bool success=EmptyClipboard() && SetClipboardData(CF_DIB,image)!=nullptr;CloseClipboard();
        if(!success){GlobalFree(image);throw std::runtime_error("Could not put chart image on clipboard.");}
        SetWindowTextW(statusLabel,L"Chart image copied to clipboard.");return true;
    }
    }
    return false;
}


