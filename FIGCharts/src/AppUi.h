#pragma once
enum {
    OpenCsv = 1000, OpenBinary, Refresh, ExitApp, About, Settings, AddStudiesMenu, CopyImage,
    AddTitle, EditSelected, ZoomAll, EnableLegend, DisableLegend, AddFinancial, NextSignal, PreviousSignal, BackgroundRulesMenu, LoadWorkspaceMenu, SaveWorkspaceMenu, SaveWorkspaceAsMenu, NewWorkspaceMenu,
    DefaultType = 1100, ThemeChoice, TimeChoice, ShowLegendSetting, ShowFinancialLegendSetting, ChartTitle,
    ApplyPreferences, ClosePreferences, SignalReport, CsvExportPath, CsvExportBrowse, OpenDatabase, SettingsTabs, DatabaseConnection, TestDatabase, DatabaseList, LoadDatabase, DatabaseHistory, DatabasePerformance, ShowDatabasePerformance
};
HWND settingsWindow = nullptr;
std::vector<HWND> generalSettings,databaseSettings;
void settingsTab(){bool db=TabCtrl_GetCurSel(GetDlgItem(settingsWindow,SettingsTabs))==1;for(auto h:generalSettings)ShowWindow(h,db?SW_HIDE:SW_SHOW);for(auto h:databaseSettings)ShowWindow(h,db?SW_SHOW:SW_HIDE);}
int32_t databaseHistoryLimit=0;
void loadDatabaseCollection(int32_t id,bool refresh=false){
 auto totalStart=DatabaseClock::now();
 {
  LoadingIndicator indicator(L"Reading all database history and study overrides...");
  auto connection=preferences.databaseConnection;auto limit=databaseHistoryLimit;
  auto loaded=loadingTask([connection,id,limit]{return databasePrices(connection,id,INT64_MIN,INT64_MAX,limit);});
  auto chartStart=DatabaseClock::now();auto first=options.first;
  double anchor=data.time.empty()?0:data.time[std::min(first,data.time.size()-1)];bool atEnd=first+options.count>=data.time.size();auto active=options.activeStudy;
  saveStudies();data=std::move(loaded);currentFile=L"database:"+std::to_wstring(id)+L":"+std::to_wstring(limit);crossVisible=false;options.panExtra=0;pendingExtra=0;restoreStudies(!refresh);
  if(refresh&&!atEnd){auto position=std::lower_bound(data.time.begin(),data.time.end(),anchor);options.first=std::min(size_t(position-data.time.begin()),data.time.size()-1);}else options.first=data.time.size()>options.count?data.time.size()-options.count:0;
  if(refresh)options.activeStudy=active;
  backgroundPreview=false;refreshLoadedCalculations(indicator);if(financialWindow)fillFinancialControls();redraw();SetWindowTextW(window,(L"FIGCharts - Study collection "+std::to_wstring(id)).c_str());if(!testing&&preferences.legend)showLegend();
  lastDatabasePerformance.chart=databaseSeconds(chartStart);lastDatabasePerformance.chartMeasured=true;
 }
 lastDatabasePerformance.total=databaseSeconds(totalStart);
 if(!testing&&preferences.showDatabasePerformance)MessageBoxW(window,lastDatabasePerformance.report().c_str(),L"Database load performance",MB_OK);
}

HWND databasePicker=nullptr;std::vector<DatabaseCollection> databaseChoices;
LRESULT CALLBACK databasePickerProc(HWND h,UINT msg,WPARAM wp,LPARAM lp){try{if(msg>=WM_CTLCOLORMSGBOX&&msg<=WM_CTLCOLORSTATIC)return themedControl(wp);if(msg==WM_CLOSE){ShowWindow(h,SW_HIDE);return 0;}if(msg==WM_COMMAND&&(LOWORD(wp)==LoadDatabase||(LOWORD(wp)==DatabaseList&&HIWORD(wp)==LBN_DBLCLK))){int i=int(SendDlgItemMessage(h,DatabaseList,LB_GETCURSEL,0,0));if(i>=0&&size_t(i)<databaseChoices.size()){databaseHistoryLimit=0;loadDatabaseCollection(databaseChoices[i].id);ShowWindow(h,SW_HIDE);}return 0;}}catch(const std::exception& e){MessageBoxA(h,e.what(),"Database",MB_ICONERROR);}return DefWindowProcW(h,msg,wp,lp);}
void showSettings(bool focusTitle);
void showDatabase(){if(preferences.databaseConnection.empty()){showSettings(false);TabCtrl_SetCurSel(GetDlgItem(settingsWindow,SettingsTabs),1);settingsTab();SetFocus(GetDlgItem(settingsWindow,DatabaseConnection));return;}
{LoadingIndicator indicator(L"Reading enabled study collections...");auto connection=preferences.databaseConnection;databaseChoices=loadingTask([connection]{return databaseCollections(connection);});}
if(!databasePicker){WNDCLASSW wc{};wc.lpfnWndProc=databasePickerProc;wc.hInstance=GetModuleHandle(nullptr);wc.lpszClassName=L"FIGDatabasePicker";wc.hCursor=LoadCursor(nullptr,IDC_ARROW);wc.hbrBackground=appBackground();RegisterClassW(&wc);databasePicker=CreateWindowW(wc.lpszClassName,L"Get prices from database - enabled study collections",WS_OVERLAPPED|WS_CAPTION|WS_SYSMENU,CW_USEDEFAULT,CW_USEDEFAULT,660,435,window,nullptr,wc.hInstance,nullptr);auto list=CreateWindowW(L"LISTBOX",L"",WS_CHILD|WS_VISIBLE|WS_BORDER|WS_VSCROLL|LBS_NOTIFY|WS_TABSTOP,15,15,610,270,databasePicker,(HMENU)DatabaseList,nullptr,nullptr);SendMessage(list,WM_SETFONT,(WPARAM)font,TRUE);auto label=CreateWindowW(L"STATIC",L"History to load",WS_CHILD|WS_VISIBLE,15,300,140,24,databasePicker,nullptr,nullptr,nullptr);SendMessage(label,WM_SETFONT,(WPARAM)font,TRUE);
 auto history=CreateWindowW(L"COMBOBOX",L"",WS_CHILD|WS_VISIBLE|WS_TABSTOP|CBS_DROPDOWNLIST,160,295,210,150,databasePicker,(HMENU)DatabaseHistory,nullptr,nullptr);SendMessage(history,WM_SETFONT,(WPARAM)font,TRUE);for(auto text:{L"All history"})SendMessageW(history,CB_ADDSTRING,0,(LPARAM)text);SendMessage(history,CB_SETCURSEL,0,0);
 auto note=CreateWindowW(L"STATIC",L"All history. Study overrides are applied.",WS_CHILD|WS_VISIBLE,15,340,360,35,databasePicker,nullptr,nullptr,nullptr);SendMessage(note,WM_SETFONT,(WPARAM)font,TRUE);
 auto b=CreateWindowW(L"BUTTON",L"Load selected collection",WS_CHILD|WS_VISIBLE|WS_TABSTOP,385,345,240,30,databasePicker,(HMENU)LoadDatabase,nullptr,nullptr);SendMessage(b,WM_SETFONT,(WPARAM)font,TRUE);}
SendDlgItemMessage(databasePicker,DatabaseList,LB_RESETCONTENT,0,0);for(auto& c:databaseChoices)SendDlgItemMessageW(databasePicker,DatabaseList,LB_ADDSTRING,0,(LPARAM)c.label.c_str());SendDlgItemMessage(databasePicker,DatabaseList,LB_SETCURSEL,0,0);EnableWindow(GetDlgItem(databasePicker,LoadDatabase),!databaseChoices.empty());ShowWindow(databasePicker,SW_SHOW);SetForegroundWindow(databasePicker);if(databaseChoices.empty())MessageBoxW(databasePicker,L"No enabled study collections found.",L"Database",MB_OK);}

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
    for (auto top : {window, legendWindow, gotoWindow, settingsWindow, databasePicker, studyPicker, financialWindow, backgroundWindow, signalReportWindow}) if (top) {
        themeTitleBar(top);
        // Keep native chart scrolling out of the custom scrollbar paint/input path.
        if(top!=window)themedScroll::attach(top);
        SetClassLongPtr(top, GCLP_HBRBACKGROUND, reinterpret_cast<LONG_PTR>(appBackground()));
        EnumChildWindows(top, [](HWND h, LPARAM) -> BOOL {
            SetWindowTheme(h, preferences.dark ? L"" : nullptr, preferences.dark ? L"" : nullptr);
            wchar_t name[32]{}; GetClassNameW(h, name, 32);
            if (_wcsicmp(name, L"Button") == 0) SetWindowSubclass(h, themedButtonProc, 10, 0);
            if(_wcsicmp(name,WC_TABCONTROLW)==0)SetWindowSubclass(h,themedTabProc,11,0);
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
    auto file = section(L"&File"); item(file, OpenCsv, L"Open &CSV..."); item(file, OpenBinary, L"Open &binary..."); item(file, Open, L"Open data..."); item(file, OpenDatabase, L"Get prices from &database...");
    auto workspaces=CreatePopupMenu();AppendMenuW(file,MF_POPUP,reinterpret_cast<UINT_PTR>(workspaces),L"&Workspace");item(workspaces,NewWorkspaceMenu,L"New Workspace");AppendMenuW(workspaces,MF_SEPARATOR,0,nullptr);item(workspaces,LoadWorkspaceMenu,L"Load Workspace...");item(workspaces,SaveWorkspaceMenu,L"Save Workspace");item(workspaces,SaveWorkspaceAsMenu,L"Save Workspace As...");
    item(file, Export, L"Export &PNG..."); item(file, Refresh, L"&Refresh data"); AppendMenu(file, MF_SEPARATOR, 0, nullptr); item(file, ExitApp, L"E&xit");
    auto edit = section(L"&Edit"); item(edit, AddStudiesMenu, L"Add &Legend..."); item(edit, AddFinancial, L"Add &Financial Legend..."); item(edit, BackgroundRulesMenu, L"Background color rules..."); item(edit, CopyImage, L"&Copy chart image"); AppendMenu(edit, MF_SEPARATOR, 0, nullptr); item(edit, Settings, L"&Settings...");
    auto view = section(L"&View"); item(view, ZoomIn, L"Zoom &in"); item(view, ZoomOut, L"Zoom &out"); item(view, ZoomAll, L"Zoom &all");
    AppendMenu(view, MF_SEPARATOR, 0, nullptr);
    item(view, Latest, L"Go to &end"); item(view, Goto, L"Go to date/time..."); item(view, PreviousSignal, L"Go to Previous Signal"); item(view, NextSignal, L"Go to Next Signal"); AppendMenu(view, MF_SEPARATOR, 0, nullptr); item(view, EnableLegend, L"Enable legend"); item(view, DisableLegend, L"Disable legend");
    item(view, SignalReport, L"Signal performance report...");item(view,DatabasePerformance,L"Database load performance...");
    auto help = section(L"&Help"); item(help, About, L"&About FIGCharts"); return menu;
}
std::wstring controlText(HWND parent, int id) {
    auto h = GetDlgItem(parent, id); int length = GetWindowTextLengthW(h); std::wstring text(length + 1, L'\0'); GetWindowTextW(h, text.data(), length + 1); text.resize(length); return text;
}
LRESULT CALLBACK settingsProc(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp) {
    try {
        if (msg >= WM_CTLCOLORMSGBOX && msg <= WM_CTLCOLORSTATIC) return themedControl(wp);
        if (msg == WM_CLOSE) { ShowWindow(hwnd, SW_HIDE); return 0; }
        if(msg==WM_NOTIFY && reinterpret_cast<NMHDR*>(lp)->idFrom==SettingsTabs && reinterpret_cast<NMHDR*>(lp)->code==TCN_SELCHANGE){settingsTab();return 0;}
        if (msg == WM_COMMAND) {
            int id = LOWORD(wp);
            if(id==TestDatabase){LoadingIndicator indicator(L"Testing database connection...");auto connection=controlText(hwnd,DatabaseConnection);loadingTask([connection]{Database db(connection);db.query(L"SELECT 1");return true;});MessageBoxW(hwnd,L"Connection successful.",L"Database",MB_OK);return 0;}
            if(id==CsvExportBrowse){auto path=chooseSignalCsvPath();if(!path.empty())SetDlgItemTextW(hwnd,CsvExportPath,path.c_str());return 0;}
            if (id == ClosePreferences || id == IDCANCEL) { ShowWindow(hwnd, SW_HIDE); return 0; }
            if (id == ApplyPreferences || id == IDOK) {
                preferences.databaseConnection=controlText(hwnd,DatabaseConnection);
                preferences.showDatabasePerformance=SendDlgItemMessage(hwnd,ShowDatabasePerformance,BM_GETCHECK,0,0)==BST_CHECKED;
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
        settingsWindow = CreateWindowExW(WS_EX_DLGMODALFRAME, wc.lpszClassName, L"FIGCharts - Settings", WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_CLIPCHILDREN, CW_USEDEFAULT, CW_USEDEFAULT, 700, 455, window, nullptr, wc.hInstance, nullptr);
        auto child = [&](const wchar_t* type, const wchar_t* label, int id, int x, int y, int w, int h, DWORD style = 0) {
            auto c = CreateWindowW(type, label, WS_CHILD | WS_VISIBLE | style, x, y+40, w, h, settingsWindow, reinterpret_cast<HMENU>(static_cast<INT_PTR>(id)), nullptr, nullptr);
            SendMessage(c, WM_SETFONT, reinterpret_cast<WPARAM>(font), TRUE);if(id!=ApplyPreferences&&id!=ClosePreferences)generalSettings.push_back(c);return c;
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
        auto tabs=CreateWindowW(WC_TABCONTROLW,L"",WS_CHILD|WS_VISIBLE,10,5,660,32,settingsWindow,(HMENU)SettingsTabs,nullptr,nullptr);SendMessage(tabs,WM_SETFONT,(WPARAM)font,TRUE);TCITEMW item{};item.mask=TCIF_TEXT;item.pszText=(wchar_t*)L"General";TabCtrl_InsertItem(tabs,0,&item);item.pszText=(wchar_t*)L"Database";TabCtrl_InsertItem(tabs,1,&item);
        size_t start=generalSettings.size();child(L"STATIC",L"ODBC connection string",0,20,25,610,24);child(L"EDIT",L"",DatabaseConnection,20,55,625,28,WS_BORDER|ES_AUTOHSCROLL|ES_PASSWORD|WS_TABSTOP);child(L"STATIC",L"Driver={ODBC Driver 18 for SQL Server};Server=...;Database=...;Trusted_Connection=Yes;\nStored locally in application preferences.",0,20,100,625,60);child(L"BUTTON",L"Test connection",TestDatabase,20,180,170,30,WS_TABSTOP);child(L"BUTTON",L"Show performance report after database load / refresh",ShowDatabasePerformance,20,230,625,26,BS_AUTOCHECKBOX|WS_TABSTOP);databaseSettings.assign(generalSettings.begin()+start,generalSettings.end());generalSettings.resize(start);
        child(L"BUTTON", L"Apply settings", ApplyPreferences, 395, 325, 120, 30, BS_DEFPUSHBUTTON | WS_TABSTOP);
        child(L"BUTTON", L"Close", ClosePreferences, 525, 325, 120, 30, WS_TABSTOP);
    }
    SendDlgItemMessage(settingsWindow,ShowDatabasePerformance,BM_SETCHECK,preferences.showDatabasePerformance?BST_CHECKED:BST_UNCHECKED,0);
    SetDlgItemTextW(settingsWindow,DatabaseConnection,preferences.databaseConnection.c_str());if(focusTitle)TabCtrl_SetCurSel(GetDlgItem(settingsWindow,SettingsTabs),0);settingsTab();
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
    case DatabasePerformance: MessageBoxW(window,lastDatabasePerformance.report().c_str(),L"Database load performance",MB_OK);return true;
    case OpenDatabase: showDatabase();return true;
    case Refresh: if(currentFile.wstring().starts_with(L"database:")){auto source=currentFile.wstring();databaseHistoryLimit=0;loadDatabaseCollection(std::stoi(source.substr(9)),true);return true;} if (!currentFile.empty()) { auto first = options.first;auto active=options.activeStudy;saveStudies(); load(currentFile,false);options.activeStudy=active; options.first = std::min(first, data.time.size() > options.count ? data.time.size() - options.count : 0); redraw(); } return true;
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


