#pragma once
struct SignalReportRow {
    size_t entry=0, exit=0; double side=0, quantity=0, gross=0, fees=0, net=0, percent=0; bool open=false;
};

std::vector<SignalReportRow> signalReportRows;
int signalReportSort=8; bool signalReportDescending=true;
std::wstring signalReportStamp;
std::vector<int> signalReportWidths{100,100,180,180,100,100,100,100,100,100,100,100};
std::vector<int> currentSignalReportWidths(){
    auto widths=signalReportWidths;if(signalReportList)for(int i=0;i<12;++i)widths[i]=ListView_GetColumnWidth(signalReportList,i);return widths;
}
void applySignalReportWidths(){if(signalReportList)for(int i=0;i<12;++i)ListView_SetColumnWidth(signalReportList,i,signalReportWidths[i]);}
void fitSignalReportColumns(){
    if(!signalReportList)return;
    auto dc=GetDC(signalReportList);auto old=SelectObject(dc,reinterpret_cast<HFONT>(SendMessage(signalReportList,WM_GETFONT,0,0)));
    auto widths=currentSignalReportWidths();wchar_t text[1024]{};
    SendMessage(signalReportList,WM_SETREDRAW,FALSE,0);
    for(int col=0;col<12;++col){if(widths[col]==0)continue;
        LVCOLUMNW column{};column.mask=LVCF_TEXT;column.pszText=text;column.cchTextMax=1024;ListView_GetColumn(signalReportList,col,&column);
        SIZE size{};GetTextExtentPoint32W(dc,text,lstrlenW(text),&size);int width=static_cast<int>(size.cx)+28;
        for(int row=0;row<ListView_GetItemCount(signalReportList);++row){ListView_GetItemText(signalReportList,row,col,text,1024);GetTextExtentPoint32W(dc,text,lstrlenW(text),&size);width=std::max(width,static_cast<int>(size.cx)+20);}
        widths[col]=width;ListView_SetColumnWidth(signalReportList,col,width);
    }
    SelectObject(dc,old);ReleaseDC(signalReportList,dc);signalReportWidths=std::move(widths);
    SendMessage(signalReportList,WM_SETREDRAW,TRUE,0);InvalidateRect(signalReportList,nullptr,FALSE);
}
bool reportSideMatches(double side,std::wstring prefix) {
    auto first=prefix.find_first_not_of(L" \t");if(first==std::wstring::npos)return true;
    prefix=prefix.substr(first,prefix.find_last_not_of(L" \t")-first+1);
    if(prefix.front()==L'+' || prefix.front()==L'-')prefix.erase(0,1);
    return fromUtf8(sideName(std::abs(side))).starts_with(prefix);
}
std::wstring reportNumber(double value) {std::wostringstream s;s<<std::fixed<<std::setprecision(2)<<value;return s.str();}
void refreshSignalReport() {
    size_t selected=SIZE_MAX;int old=ListView_GetNextItem(signalReportList,-1,LVNI_SELECTED);
    if(old>=0 && old<static_cast<int>(signalReportRows.size()))selected=signalReportRows[old].entry;
    signalReportRows.clear();
    auto prefix=controlText(signalReportWindow,1902);
    for(const auto& [entry,side]:financialResult.signals) {
        if(!reportSideMatches(side,prefix))continue;
        size_t end=entry+1;while(end<financialResult.bars.size() && financialResult.bars[end].side==side)++end;
        bool open=end==financialResult.bars.size();size_t exit=open?end-1:end;
        double qty=financialResult.bars[entry].position;
        double gross=(data.close[exit]-data.close[entry])*qty*financialConfig.multiplier;
        double fees=std::abs(qty)*financialConfig.commission*(open?1:2),net=gross-fees;
        double basis=std::abs(qty)*data.close[entry]*financialConfig.multiplier;
        signalReportRows.push_back({entry,exit,side,std::abs(qty),gross,fees,net,basis>0?100*net/basis:0,open});
    }
    auto value=[](const SignalReportRow& r,int col)->double {switch(col){case 0:return static_cast<double>(r.entry);case 1:return r.side;case 2:return static_cast<double>(r.entry);case 3:return r.open?1:0;case 4:return data.close[r.entry];case 5:return data.close[r.exit];case 6:return r.quantity;case 7:return r.gross;case 8:return r.net;case 9:return r.fees;case 10:return r.percent;default:return static_cast<double>(r.exit-r.entry);}};
    std::stable_sort(signalReportRows.begin(),signalReportRows.end(),[&](const auto& a,const auto& b){double av=value(a,signalReportSort),bv=value(b,signalReportSort);return av==bv?a.entry<b.entry:signalReportDescending?av>bv:av<bv;});
    SendMessage(signalReportList,WM_SETREDRAW,FALSE,0);ListView_DeleteAllItems(signalReportList);
    for(size_t i=0;i<signalReportRows.size();++i){const auto& r=signalReportRows[i];
        std::wstring cells[]{std::to_wstring(r.entry+1),fromUtf8(sideName(r.side)),timestamp(data.time[r.entry]),r.open?L"Open (marked)":timestamp(data.time[r.exit]),reportNumber(data.close[r.entry]),reportNumber(data.close[r.exit]),reportNumber(r.quantity),reportNumber(r.gross),reportNumber(r.net),reportNumber(r.fees),reportNumber(r.percent),std::to_wstring(r.exit-r.entry)};
        LVITEMW item{};item.mask=LVIF_TEXT;item.iItem=static_cast<int>(i);item.pszText=cells[0].data();ListView_InsertItem(signalReportList,&item);
        for(int c=1;c<12;++c)ListView_SetItemText(signalReportList,static_cast<int>(i),c,cells[c].data());
        if(r.entry==selected)ListView_SetItemState(signalReportList,static_cast<int>(i),LVIS_SELECTED|LVIS_FOCUSED,LVIS_SELECTED|LVIS_FOCUSED);
    }
    SendMessage(signalReportList,WM_SETREDRAW,TRUE,0);InvalidateRect(signalReportList,nullptr,TRUE);
    SetWindowTextW(signalReportWindow,(L"Signal performance report - "+std::to_wstring(signalReportRows.size())+L" signals (click column to sort)").c_str());
}
void reportGoTo(){int i=ListView_GetNextItem(signalReportList,-1,LVNI_SELECTED);if(i>=0 && i<static_cast<int>(signalReportRows.size()))goToSignal(signalReportRows[i].entry);}
std::wstring signalReportCsv() {
    std::wstring result;wchar_t text[1024]{};
    for(int row=-1;row<ListView_GetItemCount(signalReportList);++row){for(int col=0;col<12;++col){
        if(col)result+=L',';
        if(row<0){LVCOLUMNW c{};c.mask=LVCF_TEXT;c.pszText=text;c.cchTextMax=1024;ListView_GetColumn(signalReportList,col,&c);}else ListView_GetItemText(signalReportList,row,col,text,1024);
        result+=L'"';for(const wchar_t* p=text;*p;++p){if(*p==L'"')result+=L'"';result+=*p;}result+=L'"';
    }result+=L"\r\n";}return result;
}
std::filesystem::path chooseSignalCsvPath(){
    wchar_t path[32768]=L"signals.csv";if(!signalCsvPath.empty())wcsncpy_s(path,signalCsvPath.c_str(),_TRUNCATE);
    OPENFILENAMEW dialog{sizeof(dialog)};dialog.hwndOwner=IsWindowVisible(settingsWindow)?settingsWindow:signalReportWindow;dialog.lpstrFilter=L"CSV file\0*.csv\0\0";dialog.lpstrFile=path;dialog.nMaxFile=32768;dialog.lpstrDefExt=L"csv";dialog.Flags=OFN_OVERWRITEPROMPT|OFN_PATHMUSTEXIST|OFN_NOCHANGEDIR;
    return GetSaveFileNameW(&dialog)?std::filesystem::path(path):std::filesystem::path{};
}
void exportSignalReport(bool clipboard) {
    SetDlgItemTextW(signalReportWindow,1906,L"");
    try {auto csv=signalReportCsv();
        if(clipboard){size_t bytes=(csv.size()+1)*sizeof(wchar_t);auto memory=GlobalAlloc(GMEM_MOVEABLE,bytes);if(!memory)throw std::runtime_error("Could not allocate clipboard text.");
            auto target=GlobalLock(memory);if(!target){GlobalFree(memory);throw std::runtime_error("Could not access clipboard text.");}memcpy(target,csv.c_str(),bytes);GlobalUnlock(memory);
            if(!OpenClipboard(signalReportWindow)){GlobalFree(memory);throw std::runtime_error("Could not open clipboard.");}EmptyClipboard();auto ok=SetClipboardData(CF_UNICODETEXT,memory);CloseClipboard();if(!ok){GlobalFree(memory);throw std::runtime_error("Could not copy report.");}
            SetDlgItemTextW(signalReportWindow,1906,L"Copied to clipboard successfully.");
        }else {auto path=signalCsvPath.empty()?chooseSignalCsvPath():signalCsvPath;if(path.empty())return;
            std::ofstream out(path,std::ios::binary);out<<utf8(csv);out.close();if(!out)throw std::runtime_error("Could not save CSV report.");signalCsvPath=path;
            SetDlgItemTextW(signalReportWindow,1906,(L"CSV exported successfully: "+path.wstring()).c_str());}
    }catch(const std::exception& e){SetDlgItemTextW(signalReportWindow,1906,clipboard?L"Copy to clipboard failed.":L"CSV export failed.");MessageBoxA(signalReportWindow,e.what(),"Export signal report",MB_OK|MB_ICONERROR);}
}
LRESULT CALLBACK signalReportProc(HWND hwnd,UINT msg,WPARAM wp,LPARAM lp){
    if(msg>=WM_CTLCOLORMSGBOX && msg<=WM_CTLCOLORSTATIC)return themedControl(wp);
    switch(msg){
    case WM_CLOSE:ShowWindow(hwnd,SW_HIDE);syncToolbar();return 0;
    case WM_SIZE:MoveWindow(signalReportList,8,44,std::max(20,LOWORD(lp)-16),std::max(20,HIWORD(lp)-92),TRUE);MoveWindow(GetDlgItem(hwnd,1901),8,HIWORD(lp)-40,130,30,TRUE);MoveWindow(GetDlgItem(hwnd,1906),150,HIWORD(lp)-40,std::max(0,LOWORD(lp)-158),30,TRUE);return 0;
    case WM_COMMAND:if(LOWORD(wp)==1901)reportGoTo();else if(LOWORD(wp)==1902 && HIWORD(wp)==EN_CHANGE && signalReportList)refreshSignalReport();else if(LOWORD(wp)==1903){SetDlgItemTextW(hwnd,1902,L"");SetFocus(GetDlgItem(hwnd,1902));}else if(LOWORD(wp)==1904)exportSignalReport(false);else if(LOWORD(wp)==1905)exportSignalReport(true);return 0;
    case WM_TIMER:{
        if(!IsWindowVisible(hwnd))return 0;
        std::wostringstream s;s<<data.time.size()<<L' '<<financialResult.signals.size()<<L' '<<financialResult.bars.data()<<L' '<<financialConfig.study.c_str()<<L' '<<std::setprecision(17)<<financialConfig.multiplier<<L' '<<financialConfig.commission;
        if(!financialResult.bars.empty())s<<financialResult.bars.back().net;
        s<<preferences.dark<<preferences.timeMode<<preferences.zoneKey;
        for(const auto& [side,style]:financialConfig.sides)s<<side<<style.color;
        auto stamp=s.str();if(stamp!=signalReportStamp){signalReportStamp=stamp;refreshSignalReport();}
        return 0;}
    case WM_NOTIFY:{auto n=reinterpret_cast<NMHDR*>(lp);
        if(n->code==NM_DBLCLK){reportGoTo();return 0;}
        if(n->code==LVN_KEYDOWN){if(reinterpret_cast<NMLVKEYDOWN*>(lp)->wVKey==VK_RETURN)reportGoTo();return 0;}
        if(n->code==LVN_COLUMNCLICK){int col=reinterpret_cast<NMLISTVIEW*>(lp)->iSubItem;if(col==signalReportSort)signalReportDescending=!signalReportDescending;else {signalReportSort=col;signalReportDescending=col>=6;}refreshSignalReport();return 0;}
        if(n->code==NM_CUSTOMDRAW && n->hwndFrom==signalReportList){auto d=reinterpret_cast<NMLVCUSTOMDRAW*>(lp);if(d->nmcd.dwDrawStage==CDDS_PREPAINT)return CDRF_NOTIFYITEMDRAW|CDRF_NOTIFYPOSTPAINT;
            if(d->nmcd.dwDrawStage==CDDS_POSTPAINT){
                RECT client{};GetClientRect(signalReportList,&client);int saved=SaveDC(d->nmcd.hdc);
                auto pen=CreatePen(PS_SOLID,1,legendBorderColor());SelectObject(d->nmcd.hdc,pen);
                int top=ListView_GetTopIndex(signalReportList),count=ListView_GetCountPerPage(signalReportList)+1;
                for(int row=top;row<std::min(top+count,ListView_GetItemCount(signalReportList));++row){
                    RECT r{};ListView_GetItemRect(signalReportList,row,&r,LVIR_BOUNDS);
                    MoveToEx(d->nmcd.hdc,0,r.bottom-1,nullptr);LineTo(d->nmcd.hdc,client.right,r.bottom-1);
                    for(int col=0;col<12;++col){RECT cell{};ListView_GetSubItemRect(signalReportList,row,col,LVIR_BOUNDS,&cell);
                        if(col==0)cell.right=cell.left+ListView_GetColumnWidth(signalReportList,0);
                        MoveToEx(d->nmcd.hdc,cell.right-1,r.top,nullptr);LineTo(d->nmcd.hdc,cell.right-1,r.bottom);}
                }
                RestoreDC(d->nmcd.hdc,saved);DeleteObject(pen);return CDRF_DODEFAULT;
            }
            if(d->nmcd.dwDrawStage==CDDS_ITEMPREPAINT && d->nmcd.dwItemSpec<signalReportRows.size()){auto it=financialConfig.sides.find(signalReportRows[d->nmcd.dwItemSpec].side);if(it!=financialConfig.sides.end()){int c=it->second.color;d->clrText=RGB((c>>16)&255,(c>>8)&255,c&255);}return CDRF_NEWFONT;}}
        break;}
    }return DefWindowProcW(hwnd,msg,wp,lp);
}
void showSignalReport(){
    if(!signalReportWindow){WNDCLASSW wc{};wc.lpfnWndProc=signalReportProc;wc.hInstance=GetModuleHandle(nullptr);wc.lpszClassName=L"FIGChartsSignalReport";wc.hCursor=LoadCursor(nullptr,IDC_ARROW);wc.hbrBackground=appBackground();RegisterClassW(&wc);
        signalReportWindow=CreateWindowExW(0,wc.lpszClassName,L"Signal performance report",WS_OVERLAPPEDWINDOW|WS_CLIPCHILDREN,CW_USEDEFAULT,CW_USEDEFAULT,1250,620,window,nullptr,wc.hInstance,nullptr);
        auto label=CreateWindowW(L"STATIC",L"SIDE filter (+ / -)",WS_CHILD|WS_VISIBLE,8,14,120,22,signalReportWindow,nullptr,wc.hInstance,nullptr);
        auto filter=CreateWindowExW(WS_EX_CLIENTEDGE,L"EDIT",L"",WS_CHILD|WS_VISIBLE|WS_CLIPSIBLINGS|WS_TABSTOP|ES_AUTOHSCROLL,132,8,190,26,signalReportWindow,reinterpret_cast<HMENU>(1902),wc.hInstance,nullptr);
        SendMessage(filter,EM_SETMARGINS,EC_RIGHTMARGIN,MAKELPARAM(0,28));
        auto clear=CreateWindowW(L"BUTTON",L"Clear SIDE filter",WS_CHILD|WS_VISIBLE|WS_TABSTOP,296,10,24,22,signalReportWindow,reinterpret_cast<HMENU>(1903),wc.hInstance,nullptr);
        SetPropW(clear,L"FIGCharts.ClearSearch",reinterpret_cast<HANDLE>(1));SetWindowSubclass(clear,themedButtonProc,10,0);controlTooltip(clear,L"Clear SIDE filter");
        auto csv=CreateWindowW(L"BUTTON",L"Export CSV",WS_CHILD|WS_VISIBLE|WS_TABSTOP,332,8,30,26,signalReportWindow,reinterpret_cast<HMENU>(1904),wc.hInstance,nullptr);iconButton(csv,2015,L"Export displayed signals to CSV file...");
        auto copy=CreateWindowW(L"BUTTON",L"Copy CSV",WS_CHILD|WS_VISIBLE|WS_TABSTOP,370,8,30,26,signalReportWindow,reinterpret_cast<HMENU>(1905),wc.hInstance,nullptr);iconButton(copy,2016,L"Copy displayed signals as CSV to clipboard");
        for(auto h:{label,filter,clear})SendMessage(h,WM_SETFONT,reinterpret_cast<WPARAM>(font),TRUE);
        SendMessage(filter,EM_SETCUEBANNER,FALSE,reinterpret_cast<LPARAM>(L"e.g. 4.9"));SendMessage(filter,EM_SETLIMITTEXT,100,0);
        signalReportList=CreateWindowW(WC_LISTVIEWW,L"",WS_CHILD|WS_VISIBLE|WS_TABSTOP|LVS_REPORT|LVS_SINGLESEL|LVS_SHOWSELALWAYS,8,8,1200,520,signalReportWindow,nullptr,wc.hInstance,nullptr);
        SendMessage(signalReportList,WM_SETFONT,reinterpret_cast<WPARAM>(font),TRUE);ListView_SetExtendedListViewStyle(signalReportList,LVS_EX_FULLROWSELECT|LVS_EX_DOUBLEBUFFER);
        const wchar_t* names[]{L"Entry bar",L"SIDE",L"Entry time",L"Exit time / status",L"Entry price",L"Exit / mark",L"Quantity",L"Gross P/L",L"Net P/L",L"Commission",L"Return %",L"Bars held"};
        for(int i=0;i<12;++i){LVCOLUMNW c{};c.mask=LVCF_TEXT|LVCF_WIDTH|LVCF_FMT;c.pszText=const_cast<wchar_t*>(names[i]);c.cx=i==2||i==3?180:100;c.fmt=i>=4?LVCFMT_RIGHT:LVCFMT_LEFT;ListView_InsertColumn(signalReportList,i,&c);}
        auto button=CreateWindowW(L"BUTTON",L"Go to signal",WS_CHILD|WS_VISIBLE|WS_TABSTOP,8,540,130,30,signalReportWindow,reinterpret_cast<HMENU>(1901),wc.hInstance,nullptr);SendMessage(button,WM_SETFONT,reinterpret_cast<WPARAM>(font),TRUE);SetWindowSubclass(button,themedButtonProc,10,0);
        controlTooltip(button,L"Scroll the chart to the selected signal's entry");
        auto status=CreateWindowW(L"STATIC",L"",WS_CHILD|WS_VISIBLE|SS_CENTERIMAGE|SS_ENDELLIPSIS|SS_NOPREFIX,150,540,1000,30,signalReportWindow,reinterpret_cast<HMENU>(1906),wc.hInstance,nullptr);SendMessage(status,WM_SETFONT,reinterpret_cast<WPARAM>(font),TRUE);
        addPanelFontControls(signalReportWindow,2,420,8);applySignalReportWidths();
        SetTimer(signalReportWindow,1,500,nullptr);
    }
    applyTheme();refreshSignalReport();ShowWindow(signalReportWindow,SW_SHOW);RECT r{};GetClientRect(signalReportWindow,&r);SendMessage(signalReportWindow,WM_SIZE,0,MAKELPARAM(r.right,r.bottom));SetForegroundWindow(signalReportWindow);syncToolbar();
}



