#pragma once
// Included with the main window's shared state.
enum { FinStudy=1300, FinAum, FinMultiplier, FinCommission, FinQuantity, FinAllocation, FinReinvest, FinApply, FinRemove, FinValues, FinSides, FinStamp, FinCompact, FinWinLoss, FinSelectAll, FinDeselectAll, FinSummary, FinAutoScroll };
FinancialResult financialResult;
bool financialCompact=false, financialAutoScroll=false;
double financialScrolledSide=std::numeric_limits<double>::quiet_NaN();
std::map<double,int> financialSideMetricRows;
RECT financialExpandedRect{};
void layoutFinancial();
HWND financialValues=nullptr, financialSides=nullptr, financialSummary=nullptr;
constexpr int pinnedFinancialRows=17;
std::vector<bool> financialTopEnabled(pinnedFinancialRows,true);
int summaryRow(int row){if(!financialCompact)return row;for(int i=0;i<pinnedFinancialRows;++i)if(financialTopEnabled[i] && row--==0)return i;return -1;}
int summaryCount(){return financialCompact?static_cast<int>(std::count(financialTopEnabled.begin(),financialTopEnabled.end(),true)):pinnedFinancialRows;}
int financialSummaryHeight(){RECT header{},row{};if(!financialSummary)return 210;GetWindowRect(ListView_GetHeader(financialSummary),&header);int height=16;if(ListView_GetItemRect(financialSummary,0,&row,LVIR_BOUNDS))height=row.bottom-row.top;return header.bottom-header.top+summaryCount()*height+4;}
std::wstring financialCell(size_t row,int column);
void fitFinancialCompact() {
    if(!financialCompact || !financialValues) return;
    auto dc=GetDC(financialValues);auto old=SelectObject(dc,reinterpret_cast<HFONT>(SendMessage(financialValues,WM_GETFONT,0,0)));int widths[]{80,65};
    int rows=ListView_GetItemCount(financialValues)+pinnedFinancialRows;
    for(int row=0;row<rows;++row) for(int col=0;col<2;++col) {auto text=financialCell(row,col);SIZE size{};GetTextExtentPoint32W(dc,text.c_str(),static_cast<int>(text.size()),&size);widths[col]=std::max(widths[col],static_cast<int>(size.cx)+22);}
    SelectObject(dc,old);ReleaseDC(financialValues,dc);
    for(int col=0;col<2;++col) if(ListView_GetColumnWidth(financialValues,col)!=widths[col]) ListView_SetColumnWidth(financialValues,col,widths[col]);
    RECT header{},rowRect{};GetWindowRect(ListView_GetHeader(financialValues),&header);
    int rowHeight=20;if(rows && ListView_GetItemRect(financialValues,0,&rowRect,LVIR_BOUNDS)) rowHeight=rowRect.bottom-rowRect.top;
    RECT desired{0,0,std::max(430,widths[0]+widths[1]+28),54+header.bottom-header.top+std::max(1,rows)*rowHeight+4};
    AdjustWindowRectEx(&desired,static_cast<DWORD>(GetWindowLongPtr(financialWindow,GWL_STYLE)),FALSE,static_cast<DWORD>(GetWindowLongPtr(financialWindow,GWL_EXSTYLE)));
    MONITORINFO monitor{sizeof(monitor)};GetMonitorInfo(MonitorFromWindow(financialWindow,MONITOR_DEFAULTTONEAREST),&monitor);
    int w=std::min(desired.right-desired.left,monitor.rcWork.right-monitor.rcWork.left),h=std::min(desired.bottom-desired.top,monitor.rcWork.bottom-monitor.rcWork.top);
    // Reserve space for vertical scrolling if the metrics exceed the screen height.
    if(h<desired.bottom-desired.top) w=std::min(w+GetSystemMetrics(SM_CXVSCROLL),static_cast<int>(monitor.rcWork.right-monitor.rcWork.left));
    RECT current{};GetWindowRect(financialWindow,&current);
    int x=std::clamp(current.left,monitor.rcWork.left,monitor.rcWork.right-w),y=std::clamp(current.top,monitor.rcWork.top,monitor.rcWork.bottom-h);
    if(current.right-current.left!=w || current.bottom-current.top!=h || current.left!=x || current.top!=y) SetWindowPos(financialWindow,nullptr,x,y,w,h,SWP_NOACTIVATE|SWP_NOZORDER);
}

bool syncingFinancial=false;
std::vector<int> financialRowColors;
std::vector<double> financialSideRows;
enum class FinancialMetric { Number, TotalBars, AllFrequency, SideFrequency, SideClosed, SideWinLoss };
struct FinancialRow {std::wstring name;FinancialMetric metric=FinancialMetric::Number;double FinancialBar::*member=nullptr;double side=0;size_t cachedBar=SIZE_MAX;std::wstring cachedValue;};
std::vector<FinancialRow> financialRows;
bool financialRowsDirty=true;
size_t financialDisplayBar=SIZE_MAX;
size_t financialFormattedCells=0; // Regression instrumentation: only requested cells are formatted.

std::wstring finNumber(double v) { return financialNumber(v); }
void rebuildFinancial();
void setFinancialCompact(bool compact) {
    if(compact==financialCompact) return;
    if(compact) GetWindowRect(financialWindow,&financialExpandedRect);
    financialCompact=compact;
    SendDlgItemMessage(financialWindow,FinCompact,BM_SETCHECK,compact?BST_CHECKED:BST_UNCHECKED,0);
    for(HWND h=GetWindow(financialWindow,GW_CHILD);h;h=GetWindow(h,GW_HWNDNEXT)) {
        int id=GetDlgCtrlID(h);if(id!=FinValues && id!=FinSummary && id!=FinCompact && id!=FinAutoScroll && id!=1950 && id!=1951 && id!=1953) ShowWindow(h,compact?SW_HIDE:SW_SHOW);
    }
    if(compact) fitFinancialCompact();
    else {
        ListView_SetColumnWidth(financialValues,0,360);ListView_SetColumnWidth(financialValues,1,210);
        SetWindowPos(financialWindow,nullptr,financialExpandedRect.left,financialExpandedRect.top,financialExpandedRect.right-financialExpandedRect.left,financialExpandedRect.bottom-financialExpandedRect.top,SWP_NOACTIVATE|SWP_NOZORDER);
    }
    ListView_SetItemCountEx(financialSummary,summaryCount(),0);
    layoutFinancial();InvalidateRect(financialWindow,nullptr,TRUE);
}
const std::wstring& financialValue(size_t row) {
    static const std::wstring empty;
    if(row>=financialRows.size() || financialDisplayBar>=financialResult.bars.size())return empty;
    auto& r=financialRows[row];if(r.cachedBar==financialDisplayBar)return r.cachedValue;
    const auto& f=financialResult.bars[financialDisplayBar];++financialFormattedCells;
    switch(r.metric){
    case FinancialMetric::Number:if(r.member==&FinancialBar::side){std::wostringstream s;s<<std::fixed<<std::setprecision(4)<<f.side;r.cachedValue=s.str();}else r.cachedValue=finNumber(f.*r.member);break;
    case FinancialMetric::TotalBars:r.cachedValue=financialNumber(static_cast<double>(financialDisplayBar+1),0);break;
    case FinancialMetric::AllFrequency:r.cachedValue=financialNumber(static_cast<double>(f.signalBars),0)+L"_"+financialNumber(static_cast<double>(f.signalFrequency),0);break;
    default:{auto t=financialResult.at(r.side,financialDisplayBar);
        if(r.metric==FinancialMetric::SideFrequency)r.cachedValue=financialNumber(static_cast<double>(t.bars),0)+L"_"+financialNumber(static_cast<double>(t.frequency),0);
        else if(r.metric==FinancialMetric::SideClosed)r.cachedValue=finNumber(t.closed);
        else r.cachedValue=finNumber(t.winningPL)+L" ("+financialNumber(static_cast<double>(t.wins),0)+L") / "+finNumber(t.losingPL)+L" ("+financialNumber(static_cast<double>(t.losses),0)+L")";
        break;}
    }
    r.cachedBar=financialDisplayBar;return r.cachedValue;
}
std::wstring financialCell(size_t row,int column){return row<financialRows.size()?(column==0?financialRows[row].name:financialValue(row)):L"";}
void updateFinancial() {
    if(!financialValues || (!testing && !IsWindowVisible(financialWindow)))return;
    size_t bar=selectedBar();if(financialResult.bars.empty() || bar>=financialResult.bars.size())return;
    bool changed=financialDisplayBar!=bar;financialDisplayBar=bar;
    if(financialRowsDirty){
        financialRows.clear();financialRowColors.clear();financialSideMetricRows.clear();financialScrolledSide=std::numeric_limits<double>::quiet_NaN();
        auto add=[&](std::wstring name,FinancialMetric metric,double FinancialBar::*member,int color=0xff8585,double side=0){financialRows.push_back({std::move(name),metric,member,side,SIZE_MAX,{}});financialRowColors.push_back(color);};
        for(auto pair:{std::pair{L"NetPOS",&FinancialBar::position},{L"NetClosePL",&FinancialBar::closed},{L"NetOpenPL",&FinancialBar::open},{L"Commission",&FinancialBar::commission},{L"Net",&FinancialBar::net},
            {L"NAV",&FinancialBar::nav},{L"NAVMax",&FinancialBar::peak},{L"NAVDrawDown",&FinancialBar::drawdown},{L"MaxDrawDown",&FinancialBar::maxDrawdown},{L"NAV_Trade",&FinancialBar::tradeNav},
            {L"NAV_TradeMax",&FinancialBar::tradePeak},{L"NAV_TradeDrawDown",&FinancialBar::tradeDrawdown},{L"NAV_TradeMaxDrawDown",&FinancialBar::tradeMaxDrawdown}})add(pair.first,FinancialMetric::Number,pair.second);
        auto prefix=wide((isSideStudy(financialConfig.study)?financialConfig.study.substr(0,financialConfig.study.size()-5):financialConfig.study));
        add(wide(financialConfig.study),FinancialMetric::Number,&FinancialBar::side,0xffb525);
        add(L"TotalBars",FinancialMetric::TotalBars,nullptr,0x8599b5);
        add(prefix+L"_bar_Frq",FinancialMetric::AllFrequency,nullptr,0xffb525);
        add(prefix+L"_ClosePL",FinancialMetric::Number,&FinancialBar::closed,0xffb525);
        for(const auto& [side,points]:financialResult.sides){const auto& style=financialConfig.sides.at(side);if(!style.show)continue;
            financialSideMetricRows[side]=static_cast<int>(financialRows.size())-pinnedFinancialRows;
            auto label=prefix+L"_"+wide(sideName(side));
            add(label+L"_bar_Frq",FinancialMetric::SideFrequency,nullptr,style.color,side);
            add(label+L"_ClosePL",FinancialMetric::SideClosed,nullptr,style.color,side);
            if(financialConfig.showWinLoss)add(label+L"_W_L",FinancialMetric::SideWinLoss,nullptr,style.color,side);
        }
        add(prefix+L"_TradePrice",FinancialMetric::Number,&FinancialBar::tradePrice,0x8599b5);
        financialRowsDirty=false;changed=true;
        ListView_SetItemCountEx(financialSummary,std::min(summaryCount(),static_cast<int>(financialRows.size())),LVSICF_NOINVALIDATEALL|LVSICF_NOSCROLL);
        ListView_SetItemCountEx(financialValues,std::max(0,static_cast<int>(financialRows.size())-pinnedFinancialRows),LVSICF_NOINVALIDATEALL|LVSICF_NOSCROLL);
        layoutFinancial();
    }
    auto stamp=timestamp(data.time[bar]);if(financialResult.missing)stamp+=L" | Missing SIDE values held unchanged";
    wchar_t previousStamp[256]{};GetDlgItemTextW(financialWindow,FinStamp,previousStamp,256);
    if(stamp!=previousStamp)SetDlgItemTextW(financialWindow,FinStamp,stamp.c_str());
    if(financialAutoScroll){double side=financialResult.bars[bar].side;
        if(side!=financialScrolledSide){financialScrolledSide=side;auto found=financialSideMetricRows.find(side);
            if(found!=financialSideMetricRows.end()){
                int row=found->second,last=row+(financialConfig.showWinLoss?2:1);
                ListView_EnsureVisible(financialValues,last,FALSE);ListView_EnsureVisible(financialValues,row,FALSE);
            }
        }
    }
    if(!changed)return;
    // Owner-data list requests values only for the rows it paints, including newly scrolled rows.
    InvalidateRect(financialSummary,nullptr,FALSE);InvalidateRect(financialValues,nullptr,FALSE);
}
void refreshFinancialData() {
    signalNavigationBar=SIZE_MAX;financialResult=calculateFinancial(data,financialConfig);
    options.signalStudy=financialResult.studyIndex; options.signalStyles=financialConfig.sides;
    financialRowsDirty=true;financialDisplayBar=SIZE_MAX;financialRows.clear();financialRowColors.clear();
    if(financialValues) ListView_SetItemCountEx(financialValues,0,0);if(financialSummary)ListView_SetItemCountEx(financialSummary,0,0);
    if(financialSides) rebuildFinancial();
    updateFinancial();
}
void rebuildFinancial() {
    if(!financialSides) return;
    syncingFinancial=true;ListView_DeleteAllItems(financialSides);financialSideRows.clear();
    for(const auto& [side,points]:financialResult.sides) {
        auto style=financialConfig.sides.at(side); int row=static_cast<int>(financialSideRows.size());financialSideRows.push_back(side);
        auto name=wide(sideName(side));LVITEMW item{};item.mask=LVIF_TEXT;item.iItem=row;item.pszText=name.data();ListView_InsertItem(financialSides,&item);
        ListView_SetCheckState(financialSides,row,style.show);
        ListView_SetItemText(financialSides,row,1,const_cast<wchar_t*>(L"Change color..."));
    }
    syncingFinancial=false;
}
void layoutFinancial() {
    RECT r;GetClientRect(financialWindow,&r);int width=std::max(100L,r.right-24),fixedHeight=financialSummaryHeight();
    int top=financialCompact?42:230;
    fixedHeight=std::min(fixedHeight,std::max(120,static_cast<int>(r.bottom)-top-(financialCompact?120:200)));
    if(GetDlgItem(financialWindow,1953))layoutPanelFontControls(financialWindow,r.right-120,financialCompact?4:194);
    ShowWindow(financialSummary,SW_SHOW);
    MoveWindow(financialSummary,12,top,width,fixedHeight,TRUE);
    int nameWidth=std::max(100,width*62/100);ListView_SetColumnWidth(financialSummary,0,nameWidth);ListView_SetColumnWidth(financialSummary,1,width-nameWidth-4);
    int sidesTop=top+fixedHeight+6;
    if(financialCompact){
        MoveWindow(GetDlgItem(financialWindow,FinCompact),12,8,135,26,TRUE);
        MoveWindow(GetDlgItem(financialWindow,FinAutoScroll),152,8,140,26,TRUE);
        MoveWindow(financialValues,12,sidesTop,width,std::max(60L,r.bottom-sidesTop-12),TRUE);return;
    }
    MoveWindow(GetDlgItem(financialWindow,FinCompact),400,150,110,28,TRUE);
    MoveWindow(GetDlgItem(financialWindow,FinAutoScroll),510,150,125,28,TRUE);
    int infoTop=std::max(sidesTop+86,static_cast<int>(r.bottom)-210);
    MoveWindow(financialValues,12,sidesTop,width,infoTop-sidesTop-8,TRUE);
    MoveWindow(GetDlgItem(financialWindow,0x7ffe),12,infoTop,width,48,TRUE);
    MoveWindow(GetDlgItem(financialWindow,FinSelectAll),12,infoTop+54,120,28,TRUE);
    MoveWindow(GetDlgItem(financialWindow,FinDeselectAll),142,infoTop+54,120,28,TRUE);
    MoveWindow(financialSides,12,infoTop+90,width,std::max(60L,r.bottom-infoTop-102),TRUE);
    InvalidateRect(financialWindow,nullptr,TRUE);
}
bool filteringFinancialStudy=false;
std::wstring financialStudyText(){auto combo=GetDlgItem(financialWindow,FinStudy);int length=GetWindowTextLengthW(combo);std::wstring text(length+1,L'\0');GetWindowTextW(combo,text.data(),length+1);text.resize(length);return text;}
void filterFinancialStudies(const std::wstring& text,bool filter){
    auto combo=GetDlgItem(financialWindow,FinStudy);auto query=text;for(auto& ch:query)ch=towlower(ch);
    auto selection=SendMessage(combo,CB_GETEDITSEL,0,0);filteringFinancialStudy=true;SendMessage(combo,CB_RESETCONTENT,0,0);
    for(size_t i=0;i<data.studies.size();++i){auto name=wide(data.studies[i].name),lower=name;for(auto& ch:lower)ch=towlower(ch);
        if(filter && lower.find(query)==std::wstring::npos)continue;
        auto row=SendMessageW(combo,CB_ADDSTRING,0,reinterpret_cast<LPARAM>(name.c_str()));SendMessage(combo,CB_SETITEMDATA,row,i);
    }
    if(filter && !testing)SendMessage(combo,CB_SHOWDROPDOWN,TRUE,0);
    SetWindowTextW(combo,text.c_str());SendMessage(combo,CB_SETEDITSEL,0,selection);filteringFinancialStudy=false;
}
void fillFinancialControls() {
    filterFinancialStudies(wide(financialConfig.study.empty()?(data.studies.empty()?std::string{}:data.studies.front().name):financialConfig.study),false);
    const int ids[]{FinAum,FinMultiplier,FinCommission,FinQuantity,FinAllocation};
    const double values[]{financialConfig.aum,financialConfig.multiplier,financialConfig.commission,financialConfig.quantity,financialConfig.allocation*100};
    for(int i=0;i<5;++i) SetDlgItemTextW(financialWindow,ids[i],financialNumber(values[i]).c_str());
    SendDlgItemMessage(financialWindow,FinWinLoss,BM_SETCHECK,financialConfig.showWinLoss?BST_CHECKED:BST_UNCHECKED,0);
    SendDlgItemMessage(financialWindow,FinReinvest,BM_SETCHECK,financialConfig.reinvest?BST_CHECKED:BST_UNCHECKED,0);
}
LRESULT CALLBACK financialProc(HWND hwnd,UINT msg,WPARAM wp,LPARAM lp) {
    try {
        if(msg>=WM_CTLCOLORMSGBOX && msg<=WM_CTLCOLORSTATIC) return themedControl(wp);
        if(msg==WM_COMMAND && LOWORD(wp)==FinStudy && HIWORD(wp)==CBN_EDITCHANGE && !filteringFinancialStudy){filterFinancialStudies(financialStudyText(),true);return 0;}
        if(msg==WM_CLOSE) { preferences.financialLegend=false;ShowWindow(hwnd,SW_HIDE);saveAppPreferences();return 0; }
        if(msg==WM_GETMINMAXINFO) { reinterpret_cast<MINMAXINFO*>(lp)->ptMinTrackSize=financialCompact?POINT{430,42+financialSummaryHeight()+110}:POINT{640,230+financialSummaryHeight()+310};return 0; }
        if(msg==WM_SIZE && financialValues) {layoutFinancial();return 0;}
        if(msg==WM_COMMAND && (LOWORD(wp)==FinSelectAll || LOWORD(wp)==FinDeselectAll)) {
            financialRowsDirty=true;bool show=LOWORD(wp)==FinSelectAll;syncingFinancial=true;SendMessage(financialSides,WM_SETREDRAW,FALSE,0);
            for(size_t i=0;i<financialSideRows.size();++i){financialConfig.sides[financialSideRows[i]].show=show;ListView_SetCheckState(financialSides,static_cast<int>(i),show);}
            SendMessage(financialSides,WM_SETREDRAW,TRUE,0);syncingFinancial=false;options.signalStyles=financialConfig.sides;
            InvalidateRect(financialSides,nullptr,TRUE);redraw();saveStudies();return 0;
        }
        if(msg==WM_COMMAND && LOWORD(wp)==FinWinLoss) {financialRowsDirty=true;financialConfig.showWinLoss=SendDlgItemMessage(hwnd,FinWinLoss,BM_GETCHECK,0,0)==BST_CHECKED;updateFinancial();saveStudies();return 0;}
        if(msg==WM_COMMAND && LOWORD(wp)==FinAutoScroll){financialAutoScroll=SendDlgItemMessage(hwnd,FinAutoScroll,BM_GETCHECK,0,0)==BST_CHECKED;financialScrolledSide=std::numeric_limits<double>::quiet_NaN();updateFinancial();return 0;}
        if(msg==WM_COMMAND && LOWORD(wp)==FinCompact) {setFinancialCompact(SendDlgItemMessage(hwnd,FinCompact,BM_GETCHECK,0,0)==BST_CHECKED);return 0;}
        if(msg==WM_COMMAND && LOWORD(wp)==FinApply) {
            auto text=financialStudyText();auto found=std::find_if(data.studies.begin(),data.studies.end(),[&](const auto& study){return _wcsicmp(wide(study.name).c_str(),text.c_str())==0;});
            if(found==data.studies.end())throw std::runtime_error("Select an available signal study from the matching list.");
            FinancialConfig next=financialConfig;next.study=found->name;
            // SIDE appearance is keyed by numeric identity, including absent sides.
            auto number=[&](int id) { wchar_t text[128]{};GetDlgItemTextW(hwnd,id,text,128); std::wstring cleaned=text;cleaned.erase(std::remove(cleaned.begin(),cleaned.end(),static_cast<wchar_t>(44)),cleaned.end());wcsncpy_s(text,cleaned.c_str(),_TRUNCATE);wchar_t* end=nullptr;double v=wcstod(text,&end);while(end && iswspace(*end)) ++end;if(end==text || !end || *end) throw std::runtime_error("Enter valid numeric financial settings.");return v; };
            next.aum=number(FinAum);next.multiplier=number(FinMultiplier);next.commission=number(FinCommission);next.quantity=number(FinQuantity);next.allocation=number(FinAllocation)/100;
            next.reinvest=SendDlgItemMessage(hwnd,FinReinvest,BM_GETCHECK,0,0)==BST_CHECKED;
            auto result=calculateFinancial(data,next);signalNavigationBar=SIZE_MAX;financialConfig=std::move(next);financialResult=std::move(result);
            options.signalStudy=financialResult.studyIndex;options.signalStyles=financialConfig.sides;financialRowsDirty=true;financialDisplayBar=SIZE_MAX;financialRows.clear();financialRowColors.clear();rebuildFinancial();redraw();saveStudies();return 0;
        }
        if(msg==WM_COMMAND && LOWORD(wp)==FinRemove) {
            financialConfig.study.clear();financialConfig.sides.clear();refreshFinancialData();redraw();saveStudies();SetDlgItemTextW(hwnd,FinStamp,L"Choose a Signal Study and press Apply / Calculate.");return 0;
        }
        if(msg==WM_NOTIFY) {
            auto n=reinterpret_cast<NMHDR*>(lp);
            if((n->hwndFrom==financialValues || n->hwndFrom==financialSummary) && n->code==LVN_GETDISPINFOW){
                auto info=reinterpret_cast<NMLVDISPINFOW*>(lp);size_t row=n->hwndFrom==financialValues?info->item.iItem+pinnedFinancialRows:summaryRow(info->item.iItem);
                if((info->item.mask&LVIF_TEXT) && row<financialRows.size()){
                    const auto& value=info->item.iSubItem==0?(n->hwndFrom==financialSummary && !financialCompact?L"     "+financialRows[row].name:financialRows[row].name):(n->hwndFrom==financialSummary && !financialTopEnabled[row]?std::wstring{}:financialValue(row));
                    if(info->item.pszText && info->item.cchTextMax>0)wcsncpy_s(info->item.pszText,info->item.cchTextMax,value.c_str(),_TRUNCATE);
                }return 0;
            }
            if(n->hwndFrom==financialSummary && !financialCompact && (n->code==NM_CLICK || n->code==LVN_KEYDOWN)){
                int row=-1;
                if(n->code==NM_CLICK){auto click=reinterpret_cast<NMITEMACTIVATE*>(lp);if(click->ptAction.x<32)row=click->iItem;}
                else if(reinterpret_cast<NMLVKEYDOWN*>(lp)->wVKey==VK_SPACE)row=ListView_GetNextItem(financialSummary,-1,LVNI_FOCUSED);
                if(row>=0 && row<pinnedFinancialRows){financialTopEnabled[row]=!financialTopEnabled[row];InvalidateRect(financialSummary,nullptr,FALSE);}return 0;
            }
            if(n->code==NM_CUSTOMDRAW && (n->hwndFrom==financialValues || n->hwndFrom==financialSummary || n->hwndFrom==financialSides)) {
                auto d=reinterpret_cast<NMLVCUSTOMDRAW*>(lp);
                if(d->nmcd.dwDrawStage==CDDS_PREPAINT) return CDRF_NOTIFYITEMDRAW;
                if(d->nmcd.dwDrawStage==CDDS_ITEMPREPAINT) {
                    size_t i=n->hwndFrom==financialSummary?summaryRow(static_cast<int>(d->nmcd.dwItemSpec)):d->nmcd.dwItemSpec+(n->hwndFrom==financialValues?pinnedFinancialRows:0);int color=0x8599b5;
                    if((n->hwndFrom==financialValues || n->hwndFrom==financialSummary) && i<financialRowColors.size()) color=financialRowColors[i];
                    if(n->hwndFrom==financialSides && i<financialSideRows.size()) color=financialConfig.sides.at(financialSideRows[i]).color;
                    if(n->hwndFrom==financialSummary && !financialTopEnabled[i])color=0x707b8a;
                    d->clrText=RGB((color>>16)&255,(color>>8)&255,color&255);d->clrTextBk=appBackgroundColor();return CDRF_NEWFONT | (n->hwndFrom==financialSummary && !financialCompact?CDRF_NOTIFYPOSTPAINT:0);
                }
            }
            if(n->hwndFrom==financialSummary && n->code==NM_CUSTOMDRAW && !financialCompact){auto d=reinterpret_cast<NMLVCUSTOMDRAW*>(lp);if(d->nmcd.dwDrawStage==CDDS_ITEMPOSTPAINT){RECT r{};ListView_GetItemRect(financialSummary,static_cast<int>(d->nmcd.dwItemSpec),&r,LVIR_BOUNDS);r.left=4;r.right=18;r.top+=(r.bottom-r.top-14)/2;r.bottom=r.top+14;DrawFrameControl(d->nmcd.hdc,&r,DFC_BUTTON,DFCS_BUTTONCHECK|(financialTopEnabled[d->nmcd.dwItemSpec]?DFCS_CHECKED:0));return CDRF_DODEFAULT;}}
            if(n->hwndFrom==financialSides && !syncingFinancial) {
                auto changed=[&]() { financialRowsDirty=true;options.signalStyles=financialConfig.sides;updateFinancial();InvalidateRect(financialValues,nullptr,FALSE);InvalidateRect(financialSides,nullptr,FALSE);redraw();saveStudies(); };
                if(n->code==LVN_ITEMCHANGED) {
                    auto c=reinterpret_cast<NMLISTVIEW*>(lp);
                    if(c->iItem>=0 && c->iItem<static_cast<int>(financialSideRows.size()) && ((c->uOldState^c->uNewState)&LVIS_STATEIMAGEMASK)) {
                        financialConfig.sides[financialSideRows[c->iItem]].show=ListView_GetCheckState(financialSides,c->iItem)!=0;changed();
                    }
                }
                if(n->code==NM_CLICK) {
                    auto c=reinterpret_cast<NMITEMACTIVATE*>(lp);
                    if(c->iItem>=0 && c->iSubItem==1 && c->iItem<static_cast<int>(financialSideRows.size())) {
                        auto& style=financialConfig.sides[financialSideRows[c->iItem]];static COLORREF custom[16]{};CHOOSECOLORW dlg{sizeof(dlg)};
                        dlg.hwndOwner=hwnd;dlg.lpCustColors=custom;dlg.Flags=CC_RGBINIT|CC_FULLOPEN;dlg.rgbResult=RGB((style.color>>16)&255,(style.color>>8)&255,style.color&255);
                        if(ChooseColorW(&dlg)) {style.color=(GetRValue(dlg.rgbResult)<<16)|(GetGValue(dlg.rgbResult)<<8)|GetBValue(dlg.rgbResult);changed();}
                    }
                }
            }
        }
    } catch(const std::exception& e) {MessageBoxA(hwnd,e.what(),"Financial legend",MB_ICONERROR);}
    return DefWindowProcW(hwnd,msg,wp,lp);
}
void showFinancial() {
    if(data.time.empty()) {MessageBoxW(window,L"Open a report first.",L"Financial legend",MB_OK);return;}
    if(!financialWindow) {
        WNDCLASSW wc{};wc.lpfnWndProc=financialProc;wc.hInstance=GetModuleHandle(nullptr);wc.lpszClassName=L"FIGChartsFinancial";wc.hCursor=LoadCursor(nullptr,IDC_ARROW);wc.hbrBackground=appBackground();RegisterClassW(&wc);
        financialWindow=CreateWindowExW(0,wc.lpszClassName,L"FIGCharts - Financial legend",(WS_OVERLAPPEDWINDOW & ~(WS_MINIMIZEBOX | WS_MAXIMIZEBOX))|WS_CLIPCHILDREN,CW_USEDEFAULT,CW_USEDEFAULT,660,860,window,nullptr,wc.hInstance,nullptr);
        auto child=[&](const wchar_t* type,const wchar_t* text,int id,int x,int y,int w,int h,DWORD style=0) {auto c=CreateWindowW(type,text,WS_CHILD|WS_VISIBLE|style,x,y,w,h,financialWindow,reinterpret_cast<HMENU>(static_cast<INT_PTR>(id)),nullptr,nullptr);SendMessage(c,WM_SETFONT,reinterpret_cast<WPARAM>(font),TRUE);return c;};
        child(L"STATIC",L"Signal Study",0,12,12,100,22);child(L"COMBOBOX",L"",FinStudy,120,8,510,240,CBS_DROPDOWN|WS_VSCROLL|WS_TABSTOP);
        const wchar_t* labels[]{L"Initial AUM",L"Multiplier",L"Commission / fill",L"Fixed quantity (0=auto)",L"Allocation %"};
        const int ids[]{FinAum,FinMultiplier,FinCommission,FinQuantity,FinAllocation};
        for(int i=0;i<5;++i) {int col=i%2,row=i/2;child(L"STATIC",labels[i],0,12+col*315,46+row*34,150,22);child(L"EDIT",L"",ids[i],166+col*315,42+row*34,140,25,WS_BORDER|ES_AUTOHSCROLL|WS_TABSTOP);}
        child(L"BUTTON",L"Reinvest profits",FinReinvest,327,110,145,24,BS_AUTOCHECKBOX|WS_TABSTOP);
        SetPropW(child(L"BUTTON",L"Show W/L stats",FinWinLoss,481,110,150,24,BS_AUTOCHECKBOX|WS_TABSTOP),L"FIGCharts.NoFocusBorder",reinterpret_cast<HANDLE>(1));
        child(L"BUTTON",L"Apply / Calculate",FinApply,12,150,160,28,WS_TABSTOP);child(L"BUTTON",L"Remove financial legend",FinRemove,185,150,205,28,WS_TABSTOP);
        SetPropW(child(L"BUTTON",L"Compact mode",FinCompact,410,150,220,28,BS_AUTOCHECKBOX|WS_TABSTOP),L"FIGCharts.NoFocusBorder",reinterpret_cast<HANDLE>(1));
        SetPropW(child(L"BUTTON",L"Auto-scroll sides",FinAutoScroll,510,150,125,28,BS_AUTOCHECKBOX|WS_TABSTOP),L"FIGCharts.NoFocusBorder",reinterpret_cast<HANDLE>(1));
        SendDlgItemMessage(financialWindow,FinAutoScroll,BM_SETCHECK,financialAutoScroll?BST_CHECKED:BST_UNCHECKED,0);
        child(L"STATIC",L"Choose a Signal Study and press Apply / Calculate.",FinStamp,12,194,290,32);
        financialSummary=child(WC_LISTVIEWW,L"",FinSummary,12,230,618,210,LVS_OWNERDATA|LVS_REPORT|LVS_SINGLESEL|WS_BORDER|WS_TABSTOP);
        ListView_SetExtendedListViewStyle(financialSummary,LVS_EX_FULLROWSELECT|LVS_EX_DOUBLEBUFFER);
        financialValues=child(WC_LISTVIEWW,L"",FinValues,12,230,618,280,LVS_OWNERDATA|LVS_REPORT|LVS_SINGLESEL|WS_BORDER|WS_TABSTOP);
        child(L"BUTTON",L"Select all",FinSelectAll,12,530,120,28,WS_TABSTOP);
        child(L"BUTTON",L"Deselect all",FinDeselectAll,142,530,120,28,WS_TABSTOP);
        financialSides=child(WC_LISTVIEWW,L"",FinSides,12,560,618,200,LVS_REPORT|LVS_SINGLESEL|WS_BORDER|WS_TABSTOP);
        ListView_SetExtendedListViewStyle(financialValues,LVS_EX_FULLROWSELECT|LVS_EX_DOUBLEBUFFER);
        ListView_SetExtendedListViewStyle(financialSides,LVS_EX_FULLROWSELECT|LVS_EX_DOUBLEBUFFER|LVS_EX_CHECKBOXES);
        for(auto list:{financialSummary,financialValues,financialSides}) for(int i=0;i<2;++i) {LVCOLUMNW c{};c.mask=LVCF_TEXT|LVCF_WIDTH|LVCF_FMT;c.fmt=list!=financialSides && i==1?LVCFMT_RIGHT:LVCFMT_LEFT;c.pszText=const_cast<wchar_t*>(list!=financialSides?(i?L"Value":L"Metric"):(i?L"Color (click to change)":L"Show signal / SIDE"));c.cx=i?210:360;ListView_InsertColumn(list,i,&c);}
        child(L"STATIC",L"Unchecked = white bars and hidden side rows; totals still include all trades.\nbar_Frq = active bar count_signal count",0x7ffe,12,520,618,36);
    }
    if(!GetDlgItem(financialWindow,1950))addPanelFontControls(financialWindow,1,550,194);
    fillFinancialControls();rebuildFinancial();layoutFinancial();applyTheme();
    preferences.financialLegend=true;saveAppPreferences();
    ShowWindow(financialWindow,testing?SW_HIDE:SW_SHOW);updateFinancial();
    if(!testing) SetForegroundWindow(financialWindow);
}





