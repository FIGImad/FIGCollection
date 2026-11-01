#pragma once
std::filesystem::path workspacePath;
std::string workspaceBaseline;

struct WorkspaceWindow {bool exists=false,visible=false;int show=SW_SHOWNORMAL;RECT rect{};};
std::map<int,WorkspaceWindow> workspaceWindows;
std::vector<HWND> workspaceHandles(){return {window,legendWindow,financialWindow,settingsWindow,gotoWindow,backgroundWindow};}
std::string workspaceSnapshot() {
    std::ostringstream out;out<<std::setprecision(17)<<"FIGChartsWorkspace 1\n";
    out<<std::quoted(utf8(currentFile.empty()?L"":(currentFile.wstring().starts_with(L"database:")?currentFile.wstring():std::filesystem::absolute(currentFile).wstring())))<<'\n'<<preferences.chartType<<' '<<preferences.dark<<' '<<preferences.legend<<' '<<preferences.financialLegend<<' '<<preferences.timeMode<<' '<<std::quoted(utf8(preferences.zoneKey))<<' '<<std::quoted(preferences.title)<<'\n';
    out<<options.first<<' '<<options.count<<' '<<options.volume<<' '<<options.activeStudy<<' '<<legendAdvanced<<' '<<financialCompact<<' '<<pickerExpanded<<'\n';
    out<<std::quoted(utf8(studyPicker?controlText(studyPicker,PickerSearch):L""))<<'\n';
    auto old=savedStudies;savedStudies.clear();for(auto e:options.studies)savedStudies[data.studies[e.index].name]=e;
    std::ostringstream settings;try{writeSettingsStream(settings);}catch(...){savedStudies=old;throw;}savedStudies=old;
    out<<std::quoted(settings.str())<<'\n'<<options.studies.size();for(auto e:options.studies)out<<' '<<std::quoted(data.studies[e.index].name);out<<'\n';
    auto handles=workspaceHandles();for(size_t i=0;i<handles.size();++i){WorkspaceWindow state;auto h=handles[i];if(h){WINDOWPLACEMENT p{sizeof(p)};GetWindowPlacement(h,&p);state={true,IsWindowVisible(h)!=FALSE,static_cast<int>(p.showCmd),p.rcNormalPosition};if(state.show==SW_SHOWMINIMIZED)state.show=SW_SHOWNORMAL;}else if(workspaceWindows.contains(static_cast<int>(i)))state=workspaceWindows.at(static_cast<int>(i));
        out<<state.exists<<' '<<state.visible<<' '<<state.show<<' '<<state.rect.left<<' '<<state.rect.top<<' '<<state.rect.right<<' '<<state.rect.bottom<<'\n';}
    out<<financialExpandedRect.left<<' '<<financialExpandedRect.top<<' '<<financialExpandedRect.right<<' '<<financialExpandedRect.bottom<<'\n';
    for(auto list:{legendStudies,financialValues,financialSides}){int count=list?Header_GetItemCount(ListView_GetHeader(list)):0;out<<count;for(int i=0;i<count;++i)out<<' '<<ListView_GetColumnWidth(list,i);out<<'\n';}
    out<<"VERTICAL "<<options.autoScale<<' '<<options.priceOnly<<' '<<options.scaleMin<<' '<<options.scaleMax<<'\n';
    out<<"GOTO "<<gotoSelectionSet<<' '<<gotoSelection.wYear<<' '<<gotoSelection.wMonth<<' '<<gotoSelection.wDay<<' '<<gotoSelection.wHour<<' '<<gotoSelection.wMinute<<' '<<gotoSelection.wSecond<<'\n';
    out<<"SIGNALFILTER "<<std::quoted(utf8(signalFilterText()))<<'\n';
    out<<"FINAUTOSCROLL "<<financialAutoScroll<<'\n';
    out<<"REPORT "<<std::quoted(utf8(signalCsvPath.wstring()))<<' '<<panelFontSizes[0]<<' '<<panelFontSizes[1]<<' '<<panelFontSizes[2]<<' '<<options.panExtra<<'\n';
    out<<"REPORTCOLUMNS";for(int width:currentSignalReportWidths())out<<' '<<width;out<<'\n';
    out<<"DISPLAY "<<panelFontBold[0]<<' '<<panelFontBold[1]<<' '<<panelFontBold[2]<<' '<<true<<' '<<signalReportSorts.size();for(auto [col,descending]:signalReportSorts)out<<' '<<col<<' '<<descending;out<<'\n';
    out<<"FINMETRICS";for(bool enabled:financialTopEnabled)out<<' '<<enabled;out<<'\n';
    return out.str();
}
void rememberWorkspace(){if(testing)return;auto path=preferenceFolder()/L"last-workspace.txt";std::filesystem::create_directories(path.parent_path());std::ofstream out(path);out<<std::quoted(utf8(workspacePath.wstring()));}
std::filesystem::path chooseWorkspace(bool save){wchar_t file[32768]{};if(!workspacePath.empty())wcsncpy_s(file,workspacePath.c_str(),_TRUNCATE);OPENFILENAMEW ofn{sizeof(ofn)};ofn.hwndOwner=window;ofn.lpstrFile=file;ofn.nMaxFile=32768;ofn.lpstrFilter=L"FIGCharts workspace\0*.figworkspace\0\0";ofn.lpstrDefExt=L"figworkspace";ofn.Flags=OFN_NOCHANGEDIR|OFN_PATHMUSTEXIST|(save?OFN_OVERWRITEPROMPT:OFN_FILEMUSTEXIST);return (save?GetSaveFileNameW(&ofn):GetOpenFileNameW(&ofn))?file:L"";}
bool saveWorkspace(bool saveAs){auto path=workspacePath;if(path.empty()||saveAs)path=chooseWorkspace(true);if(path.empty())return false;auto snapshot=workspaceSnapshot();auto temp=path;temp+=L".tmp";{std::ofstream out(temp,std::ios::binary);out<<snapshot;out.flush();if(!out)throw std::runtime_error("Could not write workspace.");}if(!MoveFileExW(temp.c_str(),path.c_str(),MOVEFILE_REPLACE_EXISTING|MOVEFILE_WRITE_THROUGH))throw std::runtime_error("Could not replace workspace file.");workspacePath=path;workspaceBaseline=snapshot;rememberWorkspace();SetWindowTextW(statusLabel,L"Workspace saved.");return true;}
bool confirmWorkspaceSave(){if(testing||restoringWorkspace||workspaceSnapshot()==workspaceBaseline)return true;int answer=MessageBoxW(window,workspacePath.empty()?L"Save this new workspace before continuing?":L"Save changes to the current workspace?",L"FIGCharts workspace",MB_YESNOCANCEL|MB_ICONQUESTION);return answer==IDNO||(answer==IDYES&&saveWorkspace());}
void applyWorkspaceWindow(HWND h,const WorkspaceWindow& state){if(!h||!state.exists)return;RECT r=state.rect;MONITORINFO m{sizeof(m)};GetMonitorInfo(MonitorFromRect(&r,MONITOR_DEFAULTTONEAREST),&m);int w=std::clamp(r.right-r.left,100L,m.rcWork.right-m.rcWork.left),height=std::clamp(r.bottom-r.top,80L,m.rcWork.bottom-m.rcWork.top);int x=std::clamp(r.left,m.rcWork.left,m.rcWork.right-w),y=std::clamp(r.top,m.rcWork.top,m.rcWork.bottom-height);SetWindowPos(h,nullptr,x,y,w,height,SWP_NOZORDER|SWP_NOACTIVATE);ShowWindow(h,testing?SW_HIDE:state.visible?(state.show==SW_SHOWMAXIMIZED?SW_SHOWMAXIMIZED:SW_SHOWNOACTIVATE):SW_HIDE);}
void loadWorkspace(const std::filesystem::path& path){
    std::ifstream in(path,std::ios::binary);std::string magic,file,zone,title,filter,settings;int version;Preferences p;size_t first,count;int active;bool volume,advanced,compact,picker;
    if(!(in>>magic>>version)||magic!="FIGChartsWorkspace"||version!=1)throw std::runtime_error("Invalid workspace file.");
    in>>std::quoted(file)>>p.chartType>>p.dark>>p.legend>>p.financialLegend>>p.timeMode>>std::quoted(zone)>>std::quoted(p.title)>>first>>count>>volume>>active>>advanced>>compact>>picker>>std::quoted(filter)>>std::quoted(settings);
    size_t studyCount;in>>studyCount;if(!in||studyCount>100000||count==0||p.chartType<0||p.chartType>2||p.timeMode<0||p.timeMode>2)throw std::runtime_error("Invalid workspace settings.");
    std::vector<std::string> order(studyCount);for(auto& name:order)in>>std::quoted(name);std::map<int,WorkspaceWindow> positions;for(int i=0;i<6;++i){auto& w=positions[i];in>>w.exists>>w.visible>>w.show>>w.rect.left>>w.rect.top>>w.rect.right>>w.rect.bottom;}
    RECT expanded{};in>>expanded.left>>expanded.top>>expanded.right>>expanded.bottom;std::vector<std::vector<int>> widths(3);for(auto& columns:widths){int n;in>>n;if(!in||n<0||n>10)throw std::runtime_error("Invalid workspace columns.");for(int i=0;i<n;++i){int w;in>>w;if(w<0||w>10000)throw std::runtime_error("Invalid workspace column width.");columns.push_back(w);}}
    bool autoScale=true,priceOnly=true;double scaleMin=0,scaleMax=1;in>>std::ws;
    if(!in.eof()){std::string tag;if(!(in>>tag>>autoScale>>priceOnly>>scaleMin>>scaleMax)||tag!="VERTICAL"||!std::isfinite(scaleMin)||!std::isfinite(scaleMax)||scaleMin>=scaleMax)throw std::runtime_error("Invalid workspace price scale.");}
    SYSTEMTIME savedGoto{};bool savedGotoSet=false;
    if(!in.eof())in>>std::ws;
    if(!in.eof()) {
        std::string tag;FILETIME valid{};
        if(!(in>>tag>>savedGotoSet>>savedGoto.wYear>>savedGoto.wMonth>>savedGoto.wDay>>savedGoto.wHour>>savedGoto.wMinute>>savedGoto.wSecond)||tag!="GOTO" || (savedGotoSet&&!SystemTimeToFileTime(&savedGoto,&valid)))throw std::runtime_error("Invalid workspace Go To date/time.");
    }
    std::string savedSignalFilter;
    if(!in.eof())in>>std::ws;
    if(!in.eof()){std::string tag;if(!(in>>tag>>std::quoted(savedSignalFilter)) || tag!="SIGNALFILTER" || savedSignalFilter.size()>100)throw std::runtime_error("Invalid workspace signal filter.");}
    bool savedAutoScroll=false;if(!in.eof())in>>std::ws;
    if(!in.eof()){std::string tag;if(!(in>>tag>>savedAutoScroll)||tag!="FINAUTOSCROLL")throw std::runtime_error("Invalid workspace financial auto-scroll.");}
    std::string csvPath;double fontSizes[3]{9,9,9};double panExtra=0;
    if(!in.eof())in>>std::ws;
    if(!in.eof()){std::string tag;if(!(in>>tag>>std::quoted(csvPath)>>fontSizes[0]>>fontSizes[1]>>fontSizes[2]>>panExtra)||tag!="REPORT"||!std::isfinite(panExtra))throw std::runtime_error("Invalid report settings.");for(double size:fontSizes)if(!std::isfinite(size)||size<7||size>24)throw std::runtime_error("Invalid panel font size.");}
    std::vector<int> reportWidths{100,100,180,180,100,100,100,100,100,100,100,100};
    if(!in.eof())in>>std::ws;
    if(!in.eof()){std::string tag;if(!(in>>tag)||tag!="REPORTCOLUMNS")throw std::runtime_error("Invalid report columns.");for(auto& width:reportWidths)if(!(in>>width)||width<0||width>10000)throw std::runtime_error("Invalid report column width.");}
    bool bold[3]{},topEntries=true;std::vector<std::pair<int,bool>> reportSorts{{8,true}};
    if(!in.eof())in>>std::ws;
    if(!in.eof()){std::string tag;int sortCount;if(!(in>>tag>>bold[0]>>bold[1]>>bold[2]>>topEntries>>sortCount)||tag!="DISPLAY"||sortCount<1||sortCount>12)throw std::runtime_error("Invalid display settings.");reportSorts.clear();for(int i=0;i<sortCount;++i){int col;bool desc;if(!(in>>col>>desc)||col<0||col>=12)throw std::runtime_error("Invalid report sort.");reportSorts.emplace_back(col,desc);}}
    std::vector<bool> topEnabled(pinnedFinancialRows,true);
    if(!in.eof())in>>std::ws;
    if(!in.eof()){std::string tag;if(!(in>>tag)||tag!="FINMETRICS")throw std::runtime_error("Invalid financial metrics.");for(int i=0;i<pinnedFinancialRows;++i){bool enabled;if(!(in>>enabled))throw std::runtime_error("Invalid financial metric visibility.");topEnabled[i]=enabled;}}
    if(widths[0].size()==7)widths[0].insert(widths[0].begin()+6,advanced?90:0);
    if(widths[0].size()==8)widths[0].insert(widths[0].begin(),54);
    if(widths[0].size()==9)widths[0].insert(widths[0].begin()+8,advanced?80:0);
    if(!in||settings.rfind("FIGChartsStudies 2",0)!=0)throw std::runtime_error("Incomplete workspace file.");
    auto oldStudies=savedStudies;auto oldFinancial=financialConfig;auto oldBackground=backgroundRules;
    std::map<std::string,StudyEntry> newStudies;FinancialConfig newFinancial;std::vector<BackgroundRule> newBackground;
    try{savedStudies.clear();std::istringstream stream(settings);readSettingsStream(stream);newStudies=savedStudies;newFinancial=financialConfig;newBackground=backgroundRules;}catch(...){savedStudies=oldStudies;financialConfig=oldFinancial;backgroundRules=oldBackground;throw;}
    savedStudies=oldStudies;financialConfig=oldFinancial;backgroundRules=oldBackground;
    std::filesystem::path dataset=fromUtf8(file);if(!dataset.empty()&&!dataset.wstring().starts_with(L"database:")&&dataset.is_relative())dataset=path.parent_path()/dataset;
    LoadingIndicator indicator(L"Loading workspace data...\n"+dataset.filename().wstring());
    CsvData loaded;if(!dataset.empty())loaded=loadingTask([dataset]{if(!dataset.wstring().starts_with(L"database:"))return CsvData::load(dataset);auto source=dataset.wstring();// Older workspaces may contain a recent-bars limit; database charts now load all history.
 auto limit=0;return databasePrices(preferences.databaseConnection,std::stoi(source.substr(9)),INT64_MIN,INT64_MAX,limit);}); // Read successfully before replacing the open workspace.
    restoringWorkspace=true;
    try{
        SetWindowTextW(signalFilterEdit,fromUtf8(savedSignalFilter).c_str());
        financialAutoScroll=savedAutoScroll;financialScrolledSide=std::numeric_limits<double>::quiet_NaN();
        signalCsvPath=fromUtf8(csvPath);for(int i=0;i<3;++i)panelFontSizes[i]=fontSizes[i];
        for(int i=0;i<3;++i)panelFontBold[i]=bold[i];financialTopEnabled=topEnabled;signalReportSorts=std::move(reportSorts);
        if(financialWindow)SendDlgItemMessage(financialWindow,FinAutoScroll,BM_SETCHECK,financialAutoScroll?BST_CHECKED:BST_UNCHECKED,0);
        gotoSelection=savedGoto;gotoSelectionSet=savedGotoSet;
        p.databaseConnection=preferences.databaseConnection;p.showDatabasePerformance=preferences.showDatabasePerformance;data=std::move(loaded);currentFile=dataset;preferences=p;preferences.zoneKey=fromUtf8(zone);savedStudies=std::move(newStudies);financialConfig=std::move(newFinancial);backgroundRules=std::move(newBackground);backgroundPreview=false;signalNavigationBar=SIZE_MAX;crossVisible=false;
        options.studies.clear();for(auto& name:order)for(size_t i=0;i<data.studies.size();++i)if(data.studies[i].name==name&&savedStudies.contains(name)){auto e=savedStudies[name];e.index=static_cast<int>(i);options.studies.push_back(e);break;}
        options.first=data.time.empty()?0:std::min(first,data.time.size()-1);options.count=count;options.volume=volume;options.autoScale=autoScale;options.priceOnly=priceOnly;options.scaleMin=scaleMin;options.scaleMax=scaleMax;options.activeStudy=active>=0&&active<static_cast<int>(options.studies.size())?active:-1;
        legendAdvanced=advanced;if(financialWindow && financialCompact)setFinancialCompact(false);financialCompact=false;pickerExpanded=false;fittedPickerOffset=0;
        refreshLoadedCalculations(indicator);rebuildStudyRows();
        options.panExtra=std::clamp(panExtra,-static_cast<double>(std::min(options.count,data.time.size()))*2.0/3.0,std::min(options.count,data.time.size())*2.0/3.0);
        if(!data.time.empty()){
            if(positions[1].exists||p.legend||picker)showLegend();
            SendDlgItemMessage(legendWindow,LegendAdvanced,BM_SETCHECK,advanced?BST_CHECKED:BST_UNCHECKED,0);
            if(picker||!filter.empty()){showStudyPicker();SetDlgItemTextW(studyPicker,PickerSearch,fromUtf8(filter).c_str());}
            pickerExpanded=picker;fittedPickerOffset=picker?pickerPaneWidth:0;if(studyPicker)ShowWindow(studyPicker,picker?SW_SHOW:SW_HIDE);
            if(positions[2].exists||p.financialLegend){showFinancial();setFinancialCompact(compact);}financialExpandedRect=expanded;
            if(positions[4].exists)showGoto();if(positions[5].exists)showBackgroundRules();
        }
        if(positions[3].exists)showSettings();preferences=p;preferences.zoneKey=fromUtf8(zone);workspaceWindows=positions;
        auto handles=workspaceHandles();for(size_t i=0;i<handles.size();++i){auto state=positions[static_cast<int>(i)];if(i==1)state.visible=p.legend&&!data.time.empty();if(i==2)state.visible=p.financialLegend&&!data.time.empty();if(!state.exists&&handles[i]&&i)ShowWindow(handles[i],SW_HIDE);else applyWorkspaceWindow(handles[i],state);}
        int n=0;for(auto list:{legendStudies,financialValues,financialSides}){if(list)for(size_t i=0;i<widths[n].size();++i)ListView_SetColumnWidth(list,static_cast<int>(i),widths[n][i]);++n;}
        if(legendStudies && options.activeStudy>=0){syncingLegend=true;ListView_SetItemState(legendStudies,options.activeStudy,LVIS_SELECTED|LVIS_FOCUSED,LVIS_SELECTED|LVIS_FOCUSED);syncingLegend=false;}
        applyPanelFont(legendWindow,0);applyPanelFont(financialWindow,1);applyPanelFont(signalReportWindow,2);
        signalReportWidths=std::move(reportWidths);applySignalReportWidths();
        layoutLegend();if(financialWindow)layoutFinancial();applyTheme();if(data.time.empty()){clearImage();chart.reset();chartPanels.clear();}redraw();invalidateChart();SetWindowTextW(window,(L"FIGCharts - "+path.filename().wstring()).c_str());workspacePath=std::filesystem::absolute(path);workspaceBaseline=workspaceSnapshot();rememberWorkspace();restoringWorkspace=false;saveStudies();saveAppPreferences();
    }catch(...){restoringWorkspace=false;throw;}
}
void loadLastWorkspace(){auto path=preferenceFolder()/L"last-workspace.txt";std::ifstream in(path);std::string last;if((in>>std::quoted(last)) && !last.empty())try{loadWorkspace(fromUtf8(last));}catch(const std::exception& e){error(e);}workspaceBaseline=workspaceSnapshot();}

void newWorkspace(){
    if(signalReportWindow)DestroyWindow(signalReportWindow);signalReportWindow=signalReportList=nullptr;signalReportRows.clear();signalReportStamp.clear();signalReportWidths={100,100,180,180,100,100,100,100,100,100,100,100};
    endDrag();KillTimer(window,LegendTimer);legendRefreshPending=false;crossVisible=false;closeCellEditor();
    for(HWND h:{legendWindow,financialWindow,settingsWindow,gotoWindow,backgroundWindow})if(h)DestroyWindow(h);
    legendWindow=legendText=legendStudies=legendPrices=legendVolume=studyPicker=pickerList=cellEditor=nullptr;
    financialWindow=financialValues=financialSummary=financialSides=settingsWindow=gotoWindow=datePicker=timePicker=backgroundWindow=backgroundList=backgroundTree=nullptr;
    auto connection=preferences.databaseConnection;auto showPerformance=preferences.showDatabasePerformance;data={};options={};preferences={};preferences.databaseConnection=connection;preferences.showDatabasePerformance=showPerformance;currentFile.clear();savedStudies.clear();financialConfig={};financialResult={};signalCsvPath.clear();for(auto& size:panelFontSizes)size=9;
    for(auto& bold:panelFontBold)bold=false;financialTopEnabled.assign(pinnedFinancialRows,true);signalReportSorts={{8,true}};
    applyPanelFont(legendWindow,0);applyPanelFont(financialWindow,1);applyPanelFont(signalReportWindow,2);
    backgroundRules.clear();backgroundCache.clear();backgroundDraft.clear();backgroundPaths.clear();backgroundRuleIndex=backgroundNodeIndex=-1;backgroundSync=backgroundPreview=false;
    legendAdvanced=pickerExpanded=false;fittedPickerOffset=0;editingRow=editingColumn=-1;legendDisplayBar=SIZE_MAX;legendValues.clear();legendValueBars.clear();lastLegendText.clear();
    financialCompact=financialAutoScroll=false;financialExpandedRect={};financialRows.clear();financialRowColors.clear();financialSideRows.clear();financialSideMetricRows.clear();financialRowsDirty=true;financialDisplayBar=SIZE_MAX;financialScrolledSide=std::numeric_limits<double>::quiet_NaN();
    signalNavigationBar=SIZE_MAX;signalNavigationFirst=signalNavigationCount=0;renderedStride=1;gotoSelection={};gotoSelectionSet=false;
    SetWindowTextW(signalFilterEdit,L"");clearImage();chart.reset();chartPanels.clear();releaseFrame();
    SCROLLINFO scroll{sizeof(scroll),SIF_RANGE|SIF_PAGE|SIF_POS};SetScrollInfo(window,SB_HORZ,&scroll,TRUE);
    workspacePath.clear();workspaceWindows.clear();SetWindowTextW(window,L"FIGCharts");SetWindowTextW(statusLabel,L"New workspace");
    applyTheme();syncToolbar();invalidateChart();saveStudies();saveAppPreferences();rememberWorkspace();workspaceBaseline=workspaceSnapshot();
}



