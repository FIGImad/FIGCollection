#pragma once
// Included alongside the application UI; editing operates on a draft until Apply.
enum { BgRules=1500,BgNew,BgDelete,BgUp,BgDown,BgEnabled,BgColor,BgTree,BgGroup,BgStudy,BgOperator,BgValue,BgAddCondition,BgAddGroup,BgRemoveNode,BgUpdate,BgTest,BgApply,BgCancel,BgStatus,BgRename,BgValueType,BgValueStudy };
std::vector<BackgroundRule> backgroundDraft;
std::vector<std::vector<size_t>> backgroundPaths;
HWND backgroundList=nullptr,backgroundTree=nullptr;
int backgroundRuleIndex=-1,backgroundNodeIndex=-1;
bool backgroundSync=false,backgroundPreview=false;
std::wstring controlText(HWND,int);
// Both study selectors filter by a literal, case-insensitive substring.
void filterBackgroundStudies(int id,const std::wstring& text,bool open=false){
    auto combo=GetDlgItem(backgroundWindow,id);auto query=text;for(auto& c:query)c=towlower(c);
    auto selection=SendMessage(combo,CB_GETEDITSEL,0,0);bool previous=backgroundSync;backgroundSync=true;
    SendMessage(combo,CB_RESETCONTENT,0,0);
    for(size_t i=0;i<data.studies.size();++i){auto name=wide(data.studies[i].name),lower=name;for(auto& c:lower)c=towlower(c);
        if(open && lower.find(query)==std::wstring::npos)continue;auto row=SendMessageW(combo,CB_ADDSTRING,0,reinterpret_cast<LPARAM>(name.c_str()));SendMessage(combo,CB_SETITEMDATA,row,i);}
    if(open && !testing)SendMessage(combo,CB_SHOWDROPDOWN,TRUE,0);
    SetWindowTextW(combo,text.c_str());SendMessage(combo,CB_SETEDITSEL,0,selection);backgroundSync=previous;
}
std::string backgroundStudyText(int id){auto text=controlText(backgroundWindow,id);for(const auto& study:data.studies)if(_wcsicmp(wide(study.name).c_str(),text.c_str())==0)return study.name;throw std::runtime_error("Select an available study from the matching list.");}
struct RuleNamePrompt {bool done=false,accepted=false;std::wstring name;};
LRESULT CALLBACK ruleNameProc(HWND hwnd,UINT msg,WPARAM wp,LPARAM lp){
    auto state=reinterpret_cast<RuleNamePrompt*>(GetWindowLongPtr(hwnd,GWLP_USERDATA));
    if(msg==WM_NCCREATE){SetWindowLongPtr(hwnd,GWLP_USERDATA,reinterpret_cast<LONG_PTR>(reinterpret_cast<CREATESTRUCT*>(lp)->lpCreateParams));return TRUE;}
    if(msg>=WM_CTLCOLORMSGBOX && msg<=WM_CTLCOLORSTATIC)return themedControl(wp);
    if(msg==WM_CLOSE){state->done=true;return 0;}
    if(msg==WM_COMMAND){if(LOWORD(wp)==IDCANCEL){state->done=true;return 0;}if(LOWORD(wp)==IDOK){auto name=controlText(hwnd,100);auto first=name.find_first_not_of(L" \t\r\n"),last=name.find_last_not_of(L" \t\r\n");if(first==std::wstring::npos){SetFocus(GetDlgItem(hwnd,100));return 0;}state->name=name.substr(first,last-first+1);state->accepted=state->done=true;return 0;}}
    return DefWindowProcW(hwnd,msg,wp,lp);
}
bool askBackgroundName(std::string& name){
    if(testing)return true;
    WNDCLASSW wc{};wc.lpfnWndProc=ruleNameProc;wc.hInstance=GetModuleHandle(nullptr);wc.lpszClassName=L"FIGChartsRuleName";wc.hCursor=LoadCursor(nullptr,IDC_ARROW);wc.hbrBackground=appBackground();RegisterClassW(&wc);
    RuleNamePrompt state;RECT parent{};GetWindowRect(backgroundWindow,&parent);
    auto dialog=CreateWindowExW(WS_EX_DLGMODALFRAME|WS_EX_CONTROLPARENT,wc.lpszClassName,L"Rule name",WS_CAPTION|WS_SYSMENU|WS_POPUP,parent.left+220,parent.top+180,420,150,backgroundWindow,nullptr,wc.hInstance,&state);
    if(!dialog)throw std::runtime_error("Could not open rule name dialog.");
    auto add=[&](const wchar_t* cls,const wchar_t* label,int id,int x,int y,int w,int h,DWORD style){auto child=CreateWindowW(cls,label,WS_CHILD|WS_VISIBLE|style,x,y,w,h,dialog,reinterpret_cast<HMENU>(static_cast<INT_PTR>(id)),wc.hInstance,nullptr);SendMessage(child,WM_SETFONT,reinterpret_cast<WPARAM>(font),TRUE);return child;};
    auto edit=add(L"EDIT",wide(name).c_str(),100,12,15,380,26,WS_BORDER|ES_AUTOHSCROLL|WS_TABSTOP);SendMessage(edit,EM_SETLIMITTEXT,200,0);
    for(auto pair:{std::pair{IDOK,L"OK"},std::pair{IDCANCEL,L"Cancel"}}){auto button=add(L"BUTTON",pair.second,pair.first,pair.first==IDOK?212:306,62,86,28,WS_TABSTOP|(pair.first==IDOK?BS_DEFPUSHBUTTON:0));SetWindowSubclass(button,themedButtonProc,10,0);}
    themeTitleBar(dialog);EnableWindow(backgroundWindow,FALSE);ShowWindow(dialog,SW_SHOW);SetFocus(edit);SendMessage(edit,EM_SETSEL,0,-1);
    MSG message{};while(!state.done){int result=GetMessage(&message,nullptr,0,0);if(result<=0){if(result==0)PostQuitMessage(static_cast<int>(message.wParam));break;}if(!IsDialogMessage(dialog,&message)){TranslateMessage(&message);DispatchMessage(&message);}}
    EnableWindow(backgroundWindow,TRUE);DestroyWindow(dialog);SetActiveWindow(backgroundWindow);if(state.accepted)name=utf8(state.name);return state.accepted;
}
LRESULT CALLBACK backgroundColorProc(HWND hwnd,UINT msg,WPARAM wp,LPARAM lp,UINT_PTR,DWORD_PTR){
    if((msg==WM_PAINT || msg==WM_PRINTCLIENT) && backgroundRuleIndex>=0 && backgroundRuleIndex<static_cast<int>(backgroundDraft.size())){
        PAINTSTRUCT ps{};auto dc=msg==WM_PAINT?BeginPaint(hwnd,&ps):reinterpret_cast<HDC>(wp);auto color=backgroundDraft[backgroundRuleIndex].color;RECT r{};GetClientRect(hwnd,&r);
        auto brush=CreateSolidBrush(RGB((color>>16)&255,(color>>8)&255,color&255));FillRect(dc,&r,brush);DeleteObject(brush);auto border=CreateSolidBrush(RGB(110,114,120));FrameRect(dc,&r,border);DeleteObject(border);
        int saved=SaveDC(dc);SetBkMode(dc,TRANSPARENT);SetTextColor(dc,(((color>>16)&255)*299+((color>>8)&255)*587+(color&255)*114)>140000?RGB(0,0,0):RGB(255,255,255));SelectObject(dc,font);wchar_t label[40];swprintf_s(label,L"Color #%06X...",color);DrawTextW(dc,label,-1,&r,DT_CENTER|DT_VCENTER|DT_SINGLELINE);RestoreDC(dc,saved);if(msg==WM_PAINT)EndPaint(hwnd,&ps);return 0;
    }return DefSubclassProc(hwnd,msg,wp,lp);
}
BackgroundNode* selectedBackgroundNode() {
    if(backgroundRuleIndex<0 || backgroundRuleIndex>=static_cast<int>(backgroundDraft.size()) || backgroundNodeIndex<0 || backgroundNodeIndex>=static_cast<int>(backgroundPaths.size()))return nullptr;
    auto* node=&backgroundDraft[backgroundRuleIndex].root;for(size_t i:backgroundPaths[backgroundNodeIndex])node=&node->children.at(i);return node;
}
void fillBackgroundEditor() {
    auto* node=selectedBackgroundNode();bool leaf=node && node->group==-1;
    for(int id:{BgStudy,BgOperator,BgValue,BgValueType,BgValueStudy})EnableWindow(GetDlgItem(backgroundWindow,id),leaf);
    EnableWindow(GetDlgItem(backgroundWindow,BgGroup),node && !leaf);
    if(!node)return;
    SendDlgItemMessage(backgroundWindow,BgGroup,CB_SETCURSEL,std::max(0,node->group),0);
    SendDlgItemMessage(backgroundWindow,BgOperator,CB_SETCURSEL,node->op,0);
    filterBackgroundStudies(BgStudy,wide(node->study));filterBackgroundStudies(BgValueStudy,wide(node->valueStudy));
    SendDlgItemMessage(backgroundWindow,BgValueType,CB_SETCURSEL,node->valueStudy.empty()?0:1,0);
    ShowWindow(GetDlgItem(backgroundWindow,BgValue),node->valueStudy.empty()?SW_SHOW:SW_HIDE);ShowWindow(GetDlgItem(backgroundWindow,BgValueStudy),node->valueStudy.empty()?SW_HIDE:SW_SHOW);
    std::wostringstream v;v<<std::setprecision(12)<<node->value;SetDlgItemTextW(backgroundWindow,BgValue,v.str().c_str());
}
HTREEITEM insertBackgroundNode(const BackgroundNode& node,HTREEITEM parent,std::vector<size_t> path) {
    int index=static_cast<int>(backgroundPaths.size());backgroundPaths.push_back(path);
    std::wstring label;
    if(node.group<0) {std::wostringstream v;v<<std::setprecision(10)<<node.value;label=wide(node.study)+L" "+backgroundOps[node.op]+L" "+(node.valueStudy.empty()?v.str():wide(node.valueStudy));}
    else label=node.group==0?L"AND - all conditions":node.group==1?L"OR - any condition":L"NOT - negate all conditions";
    TVINSERTSTRUCTW item{};item.hParent=parent;item.hInsertAfter=TVI_LAST;item.item.mask=TVIF_TEXT|TVIF_PARAM;item.item.pszText=label.data();item.item.lParam=index;
    auto handle=TreeView_InsertItem(backgroundTree,&item);
    for(size_t i=0;i<node.children.size();++i) {auto childPath=path;childPath.push_back(i);insertBackgroundNode(node.children[i],handle,std::move(childPath));}
    TreeView_Expand(backgroundTree,handle,TVE_EXPAND);return handle;
}
void rebuildBackgroundTree() {
    backgroundSync=true;TreeView_DeleteAllItems(backgroundTree);backgroundPaths.clear();backgroundNodeIndex=-1;
    if(backgroundRuleIndex>=0 && backgroundRuleIndex<static_cast<int>(backgroundDraft.size())) {
        const auto& r=backgroundDraft[backgroundRuleIndex];auto root=insertBackgroundNode(r.root,TVI_ROOT,{});TreeView_SelectItem(backgroundTree,root);backgroundNodeIndex=0;
        SendDlgItemMessage(backgroundWindow,BgEnabled,BM_SETCHECK,r.enabled?BST_CHECKED:BST_UNCHECKED,0);
        wchar_t color[64];swprintf_s(color,L"Color #%06X...",r.color);SetDlgItemTextW(backgroundWindow,BgColor,color);InvalidateRect(GetDlgItem(backgroundWindow,BgColor),nullptr,FALSE);
    }
    backgroundSync=false;fillBackgroundEditor();
}
void rebuildBackgroundList() {
    backgroundSync=true;SendMessage(backgroundList,LB_RESETCONTENT,0,0);
    for(size_t i=0;i<backgroundDraft.size();++i) {auto text=std::to_wstring(i+1)+L". "+wide(backgroundDraft[i].name)+(backgroundDraft[i].enabled?L"":L" (off)");SendMessageW(backgroundList,LB_ADDSTRING,0,reinterpret_cast<LPARAM>(text.c_str()));}
    SendMessage(backgroundList,LB_SETCURSEL,backgroundRuleIndex,0);backgroundSync=false;rebuildBackgroundTree();
}
void saveBackgroundNode() {
    auto* node=selectedBackgroundNode();if(!node)return;
    if(node->group<0) {
        auto study=backgroundStudyText(BgStudy);std::string rhs;double value=node->value;
        if(SendDlgItemMessage(backgroundWindow,BgValueType,CB_GETCURSEL,0,0)==1)rhs=backgroundStudyText(BgValueStudy);
        else {wchar_t text[128]{};GetDlgItemTextW(backgroundWindow,BgValue,text,128);wchar_t* end=nullptr;value=wcstod(text,&end);while(end && iswspace(*end))++end;if(end==text || !end || *end || !std::isfinite(value))throw std::runtime_error("Enter a finite numeric comparison value.");}
        node->study=study;node->valueStudy=rhs;node->op=static_cast<int>(SendDlgItemMessage(backgroundWindow,BgOperator,CB_GETCURSEL,0,0));node->value=value;
    } else node->group=static_cast<int>(SendDlgItemMessage(backgroundWindow,BgGroup,CB_GETCURSEL,0,0));
}
void previewBackground(bool apply) {
    saveBackgroundNode();std::vector<size_t> matches;auto colors=calculateBackground(data,backgroundDraft,&matches);
    size_t painted=static_cast<size_t>(std::count_if(colors.begin(),colors.end(),[](int c){return c>=0;}));
    std::wstring text=(apply?L"Applied: ":L"Preview: ")+std::to_wstring(painted)+L" bars colored.";
    if(backgroundRuleIndex>=0 && backgroundRuleIndex<static_cast<int>(matches.size()))text+=L" Selected rule matches "+std::to_wstring(matches[backgroundRuleIndex])+L" bars before priority.";
    backgroundCache=std::move(colors);backgroundPreview=!apply;
    if(apply){backgroundRules=backgroundDraft;saveStudies();}
    redraw();SetDlgItemTextW(backgroundWindow,BgStatus,text.c_str());rebuildBackgroundTree();
}
void closeBackground() {
    if(backgroundPreview){backgroundCache=calculateBackground(data,backgroundRules);backgroundPreview=false;redraw();}
    ShowWindow(backgroundWindow,SW_HIDE);
}
LRESULT CALLBACK backgroundProc(HWND hwnd,UINT msg,WPARAM wp,LPARAM lp) {
    try {
        if(msg>=WM_CTLCOLORMSGBOX && msg<=WM_CTLCOLORSTATIC)return themedControl(wp);
        if(msg==WM_CLOSE){closeBackground();return 0;}
        if(msg==WM_NOTIFY && reinterpret_cast<NMHDR*>(lp)->hwndFrom==backgroundTree && reinterpret_cast<NMHDR*>(lp)->code==TVN_SELCHANGEDW && !backgroundSync) {
            backgroundNodeIndex=static_cast<int>(reinterpret_cast<NMTREEVIEWW*>(lp)->itemNew.lParam);fillBackgroundEditor();return 0;
        }
        if(msg==WM_COMMAND) {
            int id=LOWORD(wp);
            if((id==BgStudy || id==BgValueStudy) && HIWORD(wp)==CBN_EDITCHANGE && !backgroundSync){filterBackgroundStudies(id,controlText(hwnd,id),true);return 0;}
            if(id==BgValueType && HIWORD(wp)==CBN_SELCHANGE){bool study=SendDlgItemMessage(hwnd,BgValueType,CB_GETCURSEL,0,0)==1;ShowWindow(GetDlgItem(hwnd,BgValue),study?SW_HIDE:SW_SHOW);ShowWindow(GetDlgItem(hwnd,BgValueStudy),study?SW_SHOW:SW_HIDE);return 0;}
            if(id==BgCancel){closeBackground();return 0;}
            if(id==BgRules && HIWORD(wp)==LBN_SELCHANGE && !backgroundSync){backgroundRuleIndex=static_cast<int>(SendMessage(backgroundList,LB_GETCURSEL,0,0));rebuildBackgroundTree();return 0;}
            if(id==BgNew) {
                if(backgroundDraft.size()>=128)throw std::runtime_error("A maximum of 128 background rules is supported.");
                BackgroundRule rule;rule.name="Background "+std::to_string(backgroundDraft.size()+1);if(!askBackgroundName(rule.name))return 0;
                BackgroundNode condition;condition.group=-1;if(!data.studies.empty())condition.study=data.studies.front().name;rule.root.children.push_back(condition);
                backgroundDraft.push_back(rule);backgroundRuleIndex=static_cast<int>(backgroundDraft.size()-1);rebuildBackgroundList();return 0;
            }
            if(id==BgTest || id==BgApply){previewBackground(id==BgApply);return 0;}
            if(backgroundRuleIndex<0 || backgroundRuleIndex>=static_cast<int>(backgroundDraft.size()))return 0;
            if(id==BgRename){auto name=backgroundDraft[backgroundRuleIndex].name;if(askBackgroundName(name)){backgroundDraft[backgroundRuleIndex].name=name;rebuildBackgroundList();}return 0;}
            if(id==BgDelete){backgroundDraft.erase(backgroundDraft.begin()+backgroundRuleIndex);backgroundRuleIndex=backgroundDraft.empty()?-1:std::min(backgroundRuleIndex,static_cast<int>(backgroundDraft.size()-1));rebuildBackgroundList();return 0;}
            if(id==BgUp || id==BgDown){int next=backgroundRuleIndex+(id==BgUp?-1:1);if(next>=0 && next<static_cast<int>(backgroundDraft.size())){std::swap(backgroundDraft[next],backgroundDraft[backgroundRuleIndex]);backgroundRuleIndex=next;rebuildBackgroundList();}return 0;}
            if(id==BgEnabled){backgroundDraft[backgroundRuleIndex].enabled=SendDlgItemMessage(hwnd,BgEnabled,BM_GETCHECK,0,0)==BST_CHECKED;rebuildBackgroundList();return 0;}
            if(id==BgColor){static COLORREF custom[16]{};auto& color=backgroundDraft[backgroundRuleIndex].color;CHOOSECOLORW c{sizeof(c)};c.hwndOwner=hwnd;c.lpCustColors=custom;c.Flags=CC_RGBINIT|CC_FULLOPEN;c.rgbResult=RGB((color>>16)&255,(color>>8)&255,color&255);if(ChooseColorW(&c)){color=(GetRValue(c.rgbResult)<<16)|(GetGValue(c.rgbResult)<<8)|GetBValue(c.rgbResult);rebuildBackgroundTree();}return 0;}
            if(id==BgUpdate){saveBackgroundNode();rebuildBackgroundTree();return 0;}
            auto* node=selectedBackgroundNode();if(!node)return 0;
            if(id==BgRemoveNode){auto path=backgroundPaths[backgroundNodeIndex];if(path.empty())return 0;auto* parent=&backgroundDraft[backgroundRuleIndex].root;for(size_t i=0;i+1<path.size();++i)parent=&parent->children[path[i]];parent->children.erase(parent->children.begin()+path.back());rebuildBackgroundTree();return 0;}
            if(id==BgAddCondition || id==BgAddGroup){
                auto path=backgroundPaths[backgroundNodeIndex];if(path.size()>=16 || backgroundPaths.size()>=512)throw std::runtime_error("Condition nesting/size limit reached.");
                if(node->group<0){path.pop_back();node=&backgroundDraft[backgroundRuleIndex].root;for(size_t i:path)node=&node->children[i];}
                BackgroundNode child;if(id==BgAddCondition){child.group=-1;if(!data.studies.empty())child.study=data.studies.front().name;}
                node->children.push_back(child);rebuildBackgroundTree();return 0;
            }
        }
    }catch(const std::exception& e){MessageBoxA(hwnd,e.what(),"Background rules",MB_ICONERROR);}
    return DefWindowProcW(hwnd,msg,wp,lp);
}
void showBackgroundRules() {
    if(data.time.empty())return;
    if(backgroundWindow && IsWindowVisible(backgroundWindow)){SetForegroundWindow(backgroundWindow);return;}
    if(backgroundPreview){backgroundCache=calculateBackground(data,backgroundRules);backgroundPreview=false;redraw();}
    if(!backgroundWindow) {
        WNDCLASSW wc{};wc.lpfnWndProc=backgroundProc;wc.hInstance=GetModuleHandle(nullptr);wc.lpszClassName=L"FIGChartsBackground";wc.hCursor=LoadCursor(nullptr,IDC_ARROW);wc.hbrBackground=appBackground();RegisterClassW(&wc);
        backgroundWindow=CreateWindowExW(WS_EX_CONTROLPARENT,wc.lpszClassName,L"FIGCharts - Background color rules",WS_OVERLAPPED|WS_CAPTION|WS_SYSMENU|WS_CLIPCHILDREN,CW_USEDEFAULT,CW_USEDEFAULT,940,650,window,nullptr,wc.hInstance,nullptr);
        auto child=[&](const wchar_t* type,const wchar_t* text,int id,int x,int y,int w,int h,DWORD style=0){auto c=CreateWindowW(type,text,WS_CHILD|WS_VISIBLE|style,x,y,w,h,backgroundWindow,reinterpret_cast<HMENU>(static_cast<INT_PTR>(id)),nullptr,nullptr);SendMessage(c,WM_SETFONT,reinterpret_cast<WPARAM>(font),TRUE);return c;};
        child(L"STATIC",L"Rules - first enabled match wins",0,12,12,250,24);
        backgroundList=child(L"LISTBOX",L"",BgRules,12,40,230,420,LBS_NOTIFY|WS_VSCROLL|WS_BORDER|WS_TABSTOP);
        child(L"BUTTON",L"New rule",BgNew,12,470,110,28,WS_TABSTOP);child(L"BUTTON",L"Delete",BgDelete,132,470,110,28,WS_TABSTOP);
        child(L"BUTTON",L"Move up",BgUp,12,508,110,28,WS_TABSTOP);child(L"BUTTON",L"Move down",BgDown,132,508,110,28,WS_TABSTOP);
        SetPropW(child(L"BUTTON",L"Enabled",BgEnabled,260,10,130,26,BS_AUTOCHECKBOX|WS_TABSTOP),L"FIGCharts.NoFocusBorder",reinterpret_cast<HANDLE>(1));child(L"BUTTON",L"Color...",BgColor,410,8,200,28,WS_TABSTOP);
        child(L"BUTTON",L"Rename rule...",BgRename,640,8,140,28,WS_TABSTOP);
        SetPropW(GetDlgItem(backgroundWindow,BgEnabled),L"FIGCharts.GrayCheck",reinterpret_cast<HANDLE>(1));
        backgroundTree=child(WC_TREEVIEWW,L"",BgTree,260,44,650,260,TVS_HASLINES|TVS_HASBUTTONS|TVS_LINESATROOT|TVS_SHOWSELALWAYS|WS_BORDER|WS_TABSTOP);
        child(L"BUTTON",L"Add condition",BgAddCondition,260,314,140,28,WS_TABSTOP);child(L"BUTTON",L"Add group",BgAddGroup,410,314,130,28,WS_TABSTOP);child(L"BUTTON",L"Remove selected",BgRemoveNode,550,314,150,28,WS_TABSTOP);
        child(L"STATIC",L"Selected group",0,260,358,140,24);auto group=child(L"COMBOBOX",L"",BgGroup,410,354,190,130,CBS_DROPDOWNLIST|WS_TABSTOP);for(auto label:{L"AND",L"OR",L"NOT"})SendMessageW(group,CB_ADDSTRING,0,reinterpret_cast<LPARAM>(label));
        child(L"STATIC",L"Study",0,260,394,300,22);child(L"STATIC",L"Comparison",0,550,394,110,22);
        child(L"COMBOBOX",L"",BgStudy,260,418,280,230,CBS_DROPDOWN|WS_VSCROLL|WS_TABSTOP);
        auto op=child(L"COMBOBOX",L"",BgOperator,550,418,90,180,CBS_DROPDOWNLIST|WS_TABSTOP);for(auto label:backgroundOps)SendMessageW(op,CB_ADDSTRING,0,reinterpret_cast<LPARAM>(label));
        child(L"EDIT",L"0",BgValue,660,418,250,26,WS_BORDER|ES_AUTOHSCROLL|WS_TABSTOP);
        auto valueType=child(L"COMBOBOX",L"",BgValueType,660,388,130,100,CBS_DROPDOWNLIST|WS_TABSTOP);for(auto label:{L"Number",L"Study"})SendMessageW(valueType,CB_ADDSTRING,0,reinterpret_cast<LPARAM>(label));
        child(L"COMBOBOX",L"",BgValueStudy,660,418,250,230,CBS_DROPDOWN|WS_VSCROLL|WS_TABSTOP);SendDlgItemMessage(backgroundWindow,BgValueStudy,CB_SETDROPPEDWIDTH,350,0);
        child(L"BUTTON",L"Update selected",BgUpdate,260,458,160,28,WS_TABSTOP);
        child(L"STATIC",L"Select a condition to edit it. Empty groups and missing study values do not match.\nTest previews colors; Close discards an unapplied preview.",0,260,498,650,38);
        child(L"BUTTON",L"Test / Preview",BgTest,260,546,155,28,WS_TABSTOP);child(L"BUTTON",L"Apply",BgApply,430,546,120,28,WS_TABSTOP);child(L"BUTTON",L"Close",BgCancel,565,546,120,28,WS_TABSTOP);
        child(L"STATIC",L"",BgStatus,12,584,895,22);
    }
    backgroundDraft=backgroundRules;backgroundRuleIndex=backgroundDraft.empty()?-1:0;
    filterBackgroundStudies(BgStudy,L"");filterBackgroundStudies(BgValueStudy,L"");
    rebuildBackgroundList();applyTheme();SetWindowSubclass(GetDlgItem(backgroundWindow,BgColor),backgroundColorProc,12,0);ShowWindow(backgroundWindow,testing?SW_HIDE:SW_SHOW);if(!testing)SetForegroundWindow(backgroundWindow);
}
