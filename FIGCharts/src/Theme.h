#pragma once
#include <uxtheme.h>
COLORREF appBackgroundColor() { return preferences.dark ? RGB(24,32,43) : RGB(255,255,255); }
COLORREF appTextColor() { return preferences.dark ? RGB(220,228,239) : RGB(38,50,68); }
HBRUSH appBackground() {
    static HBRUSH light = CreateSolidBrush(RGB(255,255,255)), dark = CreateSolidBrush(RGB(24,32,43));
    return preferences.dark ? dark : light;
}
LRESULT themedControl(WPARAM wp) {
    auto dc = reinterpret_cast<HDC>(wp); SetTextColor(dc, appTextColor()); SetBkColor(dc, appBackgroundColor());
    return reinterpret_cast<LRESULT>(appBackground());
}
void applyTheme();
void saveAppPreferences();

void themeTitleBar(HWND hwnd) {
    static auto bigIcon = static_cast<HICON>(LoadImageW(GetModuleHandle(nullptr), MAKEINTRESOURCEW(101), IMAGE_ICON, GetSystemMetrics(SM_CXICON), GetSystemMetrics(SM_CYICON), LR_SHARED));
    static auto smallIcon = static_cast<HICON>(LoadImageW(GetModuleHandle(nullptr), MAKEINTRESOURCEW(101), IMAGE_ICON, GetSystemMetrics(SM_CXSMICON), GetSystemMetrics(SM_CYSMICON), LR_SHARED));
    SendMessage(hwnd, WM_SETICON, ICON_BIG, reinterpret_cast<LPARAM>(bigIcon));
    SendMessage(hwnd, WM_SETICON, ICON_SMALL, reinterpret_cast<LPARAM>(smallIcon));
    static auto library = LoadLibraryW(L"dwmapi.dll");
    using Setter = HRESULT(WINAPI*)(HWND,DWORD,LPCVOID,DWORD);
    static auto set = library ? reinterpret_cast<Setter>(GetProcAddress(library, "DwmSetWindowAttribute")) : nullptr;
    if (set) {
        BOOL dark = preferences.dark;
        if (FAILED(set(hwnd, 20, &dark, sizeof(dark)))) set(hwnd, 19, &dark, sizeof(dark));
        COLORREF caption = dark ? RGB(24,32,43) : 0xffffffff, text = dark ? appTextColor() : 0xffffffff;
        set(hwnd, 35, &caption, sizeof(caption)); set(hwnd, 36, &text, sizeof(text));
    }
}
LRESULT CALLBACK themedButtonProc(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp, UINT_PTR, DWORD_PTR) {
    if(auto resource=GetPropW(hwnd,L"FIGCharts.IconResource");resource && (msg==WM_PAINT || msg==WM_PRINTCLIENT)) {
        PAINTSTRUCT ps{};auto dc=msg==WM_PAINT?BeginPaint(hwnd,&ps):reinterpret_cast<HDC>(wp);RECT r{};GetClientRect(hwnd,&r);FillRect(dc,&r,appBackground());
        if(SendMessage(hwnd,BM_GETSTATE,0,0)&BST_PUSHED){auto brush=CreateSolidBrush(RGB(55,69,87));FillRect(dc,&r,brush);DeleteObject(brush);}
        auto module=GetModuleHandle(nullptr);auto res=FindResourceW(module,MAKEINTRESOURCEW(reinterpret_cast<UINT_PTR>(resource)),RT_RCDATA);
        if(res){auto loaded=LoadResource(module,res);auto stream=SHCreateMemStream(static_cast<const BYTE*>(LockResource(loaded)),SizeofResource(module,res));
            if(stream){{Gdiplus::Bitmap icon(stream);Gdiplus::Graphics g(dc);Gdiplus::ColorMatrix tint{};tint.m[3][3]=1;tint.m[4][4]=1;auto color=appTextColor();tint.m[4][0]=GetRValue(color)/255.f;tint.m[4][1]=GetGValue(color)/255.f;tint.m[4][2]=GetBValue(color)/255.f;Gdiplus::ImageAttributes attr;attr.SetColorMatrix(&tint);g.SetInterpolationMode(Gdiplus::InterpolationModeHighQualityBicubic);g.DrawImage(&icon,Gdiplus::Rect((r.right-20)/2,(r.bottom-20)/2,20,20),0,0,icon.GetWidth(),icon.GetHeight(),Gdiplus::UnitPixel,&attr);}stream->Release();}}
        if(GetFocus()==hwnd){InflateRect(&r,-2,-2);DrawFocusRect(dc,&r);}if(msg==WM_PAINT)EndPaint(hwnd,&ps);return 0;
    }
    if(GetPropW(hwnd,L"FIGCharts.ClearSearch") && (msg==WM_PAINT || msg==WM_PRINTCLIENT)) {
        PAINTSTRUCT ps{};auto dc=msg==WM_PAINT?BeginPaint(hwnd,&ps):reinterpret_cast<HDC>(wp);int saved=SaveDC(dc);RECT r;GetClientRect(hwnd,&r);FillRect(dc,&r,appBackground());
        auto pen=CreatePen(PS_SOLID,1,appTextColor());SelectObject(dc,pen);int x=r.right/2,y=r.bottom/2;
        MoveToEx(dc,x-4,y-4,nullptr);LineTo(dc,x+5,y+5);MoveToEx(dc,x+4,y-4,nullptr);LineTo(dc,x-5,y+5);
        if(GetFocus()==hwnd){InflateRect(&r,-2,-2);DrawFocusRect(dc,&r);}RestoreDC(dc,saved);DeleteObject(pen);if(msg==WM_PAINT)EndPaint(hwnd,&ps);return 0;
    }
    if ((preferences.dark || GetPropW(hwnd, L"FIGCharts.NoFocusBorder")) && (msg == WM_PAINT || msg == WM_PRINTCLIENT)) {
        PAINTSTRUCT ps{}; auto dc = msg == WM_PAINT ? BeginPaint(hwnd, &ps) : reinterpret_cast<HDC>(wp);
        int saved = SaveDC(dc); RECT r; GetClientRect(hwnd, &r);
        auto state = SendMessage(hwnd, BM_GETSTATE, 0, 0); auto type = GetWindowLongPtr(hwnd, GWL_STYLE) & BS_TYPEMASK;
        bool check = type == BS_AUTOCHECKBOX || type == BS_CHECKBOX;
        auto brush = CreateSolidBrush(check ? appBackgroundColor() : (state & BST_PUSHED) ? RGB(55,69,87) : RGB(38,50,66));
        FillRect(dc, &r, brush); DeleteObject(brush);
        auto color = IsWindowEnabled(hwnd) ? appTextColor() : RGB(112,123,138);
        auto pen = CreatePen(PS_SOLID, 1, GetPropW(hwnd,L"FIGCharts.GrayCheck")?RGB(110,114,120):RGB(76,90,108)); SelectObject(dc, pen); SelectObject(dc, GetStockObject(NULL_BRUSH));
        RECT text = r;
        if (check) {
            int y = (r.bottom - 14) / 2; Rectangle(dc, 2, y, 16, y + 14);
            if (SendMessage(hwnd, BM_GETCHECK, 0, 0) == BST_CHECKED) { MoveToEx(dc, 5, y + 7, nullptr); LineTo(dc, 8, y + 10); LineTo(dc, 14, y + 3); }
            text.left = 23;
        } else Rectangle(dc, r.left, r.top, r.right, r.bottom);
        wchar_t label[256]{}; GetWindowTextW(hwnd, label, 256); SelectObject(dc, reinterpret_cast<HFONT>(SendMessage(hwnd, WM_GETFONT, 0, 0)));
        SetBkMode(dc, TRANSPARENT); SetTextColor(dc, color);
        DrawTextW(dc, label, -1, &text, DT_SINGLELINE | DT_VCENTER | (check ? DT_LEFT : DT_CENTER));
        if (!GetPropW(hwnd, L"FIGCharts.NoFocusBorder") && GetFocus() == hwnd && !(SendMessage(hwnd, WM_QUERYUISTATE, 0, 0) & UISF_HIDEFOCUS)) { InflateRect(&text, -3, -3); DrawFocusRect(dc, &text); }
        RestoreDC(dc, saved); DeleteObject(pen); if (msg == WM_PAINT) EndPaint(hwnd, &ps); return 0;
    }
    auto result = DefSubclassProc(hwnd, msg, wp, lp);
    if (msg == BM_SETCHECK || msg == BM_SETSTATE || msg == WM_ENABLE || msg == WM_SETFOCUS || msg == WM_KILLFOCUS) InvalidateRect(hwnd, nullptr, FALSE);
    return result;
}
void controlTooltip(HWND control,const wchar_t* text) {
    auto tip=CreateWindowExW(WS_EX_TOPMOST,TOOLTIPS_CLASSW,nullptr,WS_POPUP|TTS_ALWAYSTIP|TTS_NOPREFIX,CW_USEDEFAULT,CW_USEDEFAULT,CW_USEDEFAULT,CW_USEDEFAULT,GetParent(control),nullptr,GetModuleHandle(nullptr),nullptr);
    // The app also runs with common controls v5, which rejects the v6 reserved field.
    TOOLINFOW info{TTTOOLINFOW_V2_SIZE};info.uFlags=TTF_IDISHWND|TTF_SUBCLASS;info.hwnd=GetParent(control);info.uId=reinterpret_cast<UINT_PTR>(control);info.lpszText=const_cast<wchar_t*>(text);
    if(!SendMessage(tip,TTM_ADDTOOLW,0,reinterpret_cast<LPARAM>(&info))){DestroyWindow(tip);return;}
    SendMessage(tip,TTM_SETMAXTIPWIDTH,0,360);
    SendMessage(tip,TTM_SETDELAYTIME,TTDT_INITIAL,400);SendMessage(tip,TTM_SETDELAYTIME,TTDT_RESHOW,100);SendMessage(tip,TTM_SETDELAYTIME,TTDT_AUTOPOP,10000);
    SendMessage(tip,TTM_ACTIVATE,TRUE,0);
    SetPropW(control,L"FIGCharts.Tooltip",tip);
}
LRESULT CALLBACK iconButtonParentProc(HWND hwnd,UINT msg,WPARAM wp,LPARAM lp,UINT_PTR,DWORD_PTR){
    if(msg==WM_DRAWITEM){auto item=reinterpret_cast<DRAWITEMSTRUCT*>(lp);if(item->CtlType==ODT_BUTTON && GetPropW(item->hwndItem,L"FIGCharts.IconResource")){
        SendMessage(item->hwndItem,WM_PRINTCLIENT,reinterpret_cast<WPARAM>(item->hDC),PRF_CLIENT);return TRUE;}}
    return DefSubclassProc(hwnd,msg,wp,lp);
}
void iconButton(HWND control,int resource,const wchar_t* tooltip) {
    // Native pushbuttons can paint their text directly during state changes, bypassing WM_PAINT.
    SetWindowLongPtr(control,GWL_STYLE,(GetWindowLongPtr(control,GWL_STYLE)&~BS_TYPEMASK)|BS_OWNERDRAW);
    SetPropW(control,L"FIGCharts.IconResource",reinterpret_cast<HANDLE>(static_cast<INT_PTR>(resource)));
    SetWindowSubclass(GetParent(control),iconButtonParentProc,1952,0);
    SetWindowSubclass(control,themedButtonProc,10,0);controlTooltip(control,tooltip);
}
void fitSignalReportColumns();
int panelFontSizes[3]{9,9,9};
HFONT panelFonts[3]{};
void applyPanelFont(HWND parent,int kind){
    if(!parent)return;auto dc=GetDC(parent);int height=-MulDiv(panelFontSizes[kind],GetDeviceCaps(dc,LOGPIXELSY),72);ReleaseDC(parent,dc);
    auto next=CreateFontW(height,0,0,0,FW_NORMAL,FALSE,FALSE,FALSE,DEFAULT_CHARSET,OUT_DEFAULT_PRECIS,CLIP_DEFAULT_PRECIS,CLEARTYPE_QUALITY,DEFAULT_PITCH,L"Segoe UI");if(!next)return;
    EnumChildWindows(parent,[](HWND child,LPARAM value)->BOOL{wchar_t name[64]{};GetClassNameW(child,name,64);if(_wcsicmp(name,WC_LISTVIEWW)==0){SendMessage(child,WM_SETFONT,value,TRUE);SendMessage(ListView_GetHeader(child),WM_SETFONT,value,TRUE);}return TRUE;},reinterpret_cast<LPARAM>(next));
    auto old=panelFonts[kind];panelFonts[kind]=next;if(old)DeleteObject(old);
    RECT r{};GetClientRect(parent,&r);SendMessage(parent,WM_SIZE,0,MAKELPARAM(r.right,r.bottom));
}
LRESULT CALLBACK panelFontProc(HWND hwnd,UINT msg,WPARAM wp,LPARAM lp,UINT_PTR,DWORD_PTR kind){
    if(msg==WM_COMMAND && (LOWORD(wp)==1950 || LOWORD(wp)==1951)){int next=std::clamp(panelFontSizes[kind]+(LOWORD(wp)==1950?-1:1),7,24);if(next!=panelFontSizes[kind]){panelFontSizes[kind]=next;applyPanelFont(hwnd,static_cast<int>(kind));if(kind==2)fitSignalReportColumns();}return 0;}
    return DefSubclassProc(hwnd,msg,wp,lp);
}
void addPanelFontControls(HWND parent,int kind,int x,int y){
    SetWindowSubclass(parent,panelFontProc,1950,kind);
    for(int i=0;i<2;++i){auto button=CreateWindowW(L"BUTTON",i?L"A+":L"A-",WS_CHILD|WS_VISIBLE|WS_TABSTOP,x+i*36,y,32,26,parent,reinterpret_cast<HMENU>(static_cast<INT_PTR>(1950+i)),GetModuleHandle(nullptr),nullptr);SendMessage(button,WM_SETFONT,reinterpret_cast<WPARAM>(font),TRUE);SetWindowSubclass(button,themedButtonProc,10,0);controlTooltip(button,i?L"Increase table font size":L"Decrease table font size");}
    applyPanelFont(parent,kind);
}
// Menu labels retain their native strings and command IDs for keyboard navigation.
void drawMenuIcon(HDC,RECT,UINT,bool,bool,bool);
struct MenuLabel { std::wstring text; bool bar=false, separator=false, submenu=false; };
std::vector<std::unique_ptr<MenuLabel>> menuLabels;
void themeMenu(HMENU menu,bool bar=true) {
    if(!menu)return;
    MENUINFO info{sizeof(info)};info.fMask=MIM_BACKGROUND;info.hbrBack=preferences.dark?appBackground():GetSysColorBrush(COLOR_MENU);SetMenuInfo(menu,&info);
    for(int i=0;i<GetMenuItemCount(menu);++i) {
        wchar_t text[256]{};MENUITEMINFOW item{sizeof(item)};item.fMask=MIIM_FTYPE|MIIM_DATA|MIIM_STRING|MIIM_SUBMENU;item.dwTypeData=text;item.cch=256;GetMenuItemInfoW(menu,i,TRUE,&item);
        if(!item.dwItemData) {auto label=std::make_unique<MenuLabel>();label->text=text;label->bar=bar;label->submenu=item.hSubMenu!=nullptr;label->separator=(item.fType&MFT_SEPARATOR)!=0;item.dwItemData=reinterpret_cast<ULONG_PTR>(label.get());menuLabels.push_back(std::move(label));}
        item.fMask=MIIM_FTYPE|MIIM_DATA;item.fType=(preferences.dark || !bar)?(item.fType|MFT_OWNERDRAW):(item.fType&~MFT_OWNERDRAW);SetMenuItemInfoW(menu,i,TRUE,&item);
        if(item.hSubMenu)themeMenu(item.hSubMenu,false);
    }
}
void drawThemedMenu(DRAWITEMSTRUCT* item) {
    auto label=reinterpret_cast<MenuLabel*>(item->itemData);if(!label)return;
    int saved=SaveDC(item->hDC);auto r=item->rcItem;
    bool selected=(item->itemState&(ODS_SELECTED|ODS_HOTLIGHT))!=0;
    auto brush=CreateSolidBrush(selected?(preferences.dark?RGB(55,69,87):RGB(215,226,240)):appBackgroundColor());FillRect(item->hDC,&r,brush);DeleteObject(brush);
    if(label->separator) {auto pen=CreatePen(PS_SOLID,1,preferences.dark?RGB(43,53,66):RGB(210,216,224));SelectObject(item->hDC,pen);MoveToEx(item->hDC,r.left+8,(r.top+r.bottom)/2,nullptr);LineTo(item->hDC,r.right-8,(r.top+r.bottom)/2);RestoreDC(item->hDC,saved);DeleteObject(pen);return;}
    SelectObject(item->hDC,font);SetBkMode(item->hDC,TRANSPARENT);SetTextColor(item->hDC,item->itemState&(ODS_DISABLED|ODS_GRAYED)?RGB(112,123,138):appTextColor());
    if(!label->bar){RECT icon{r.left+25,r.top+(r.bottom-r.top-20)/2,r.left+45,r.top+(r.bottom-r.top-20)/2+20};drawMenuIcon(item->hDC,icon,item->itemID,label->submenu,selected,(item->itemState&(ODS_DISABLED|ODS_GRAYED))!=0);}
    if(!label->bar && item->itemState&ODS_CHECKED) {RECT check=r;check.right=check.left+24;DrawTextW(item->hDC,L"\x2713",1,&check,DT_CENTER|DT_VCENTER|DT_SINGLELINE);}
    if(label->submenu && !label->bar){int x=r.right-10,y=(r.top+r.bottom)/2;auto pen=CreatePen(PS_SOLID,1,appTextColor());auto old=SelectObject(item->hDC,pen);MoveToEx(item->hDC,x-3,y-4,nullptr);LineTo(item->hDC,x+1,y);LineTo(item->hDC,x-3,y+4);SelectObject(item->hDC,old);DeleteObject(pen);}
    r.left+=label->bar?8:54;r.right-=label->submenu?20:8;
    DrawTextW(item->hDC,label->text.c_str(),-1,&r,DT_SINGLELINE|DT_VCENTER|((item->itemState&ODS_NOACCEL)?DT_HIDEPREFIX:0));RestoreDC(item->hDC,saved);
}
void measureThemedMenu(MEASUREITEMSTRUCT* item) {
    auto label=reinterpret_cast<MenuLabel*>(item->itemData);if(!label)return;
    auto dc=GetDC(window);auto old=SelectObject(dc,font);RECT r{};DrawTextW(dc,label->text.c_str(),-1,&r,DT_CALCRECT|DT_SINGLELINE);SelectObject(dc,old);ReleaseDC(window,dc);
    item->itemWidth=r.right+(label->bar?16:70);item->itemHeight=label->separator?9:std::max(24L,r.bottom+8);
}
LRESULT themedMenuChar(WPARAM wp,LPARAM lp) {
    auto menu=reinterpret_cast<HMENU>(lp);int match=-1,count=0,highlight=-1;
    for(int i=0;i<GetMenuItemCount(menu);++i) {MENUITEMINFOW item{sizeof(item)};item.fMask=MIIM_DATA|MIIM_STATE;GetMenuItemInfoW(menu,i,TRUE,&item);if(item.fState&MFS_HILITE)highlight=i;auto label=reinterpret_cast<MenuLabel*>(item.dwItemData);if(!label || item.fState&(MFS_DISABLED|MFS_GRAYED))continue;
        for(size_t j=0;j+1<label->text.size();++j)if(label->text[j]==L'&'){if(label->text[j+1]==L'&'){++j;continue;}if(towupper(label->text[j+1])==towupper(LOWORD(wp))){++count;if(match<0 || (match<=highlight && i>highlight))match=i;}break;}}
    return match<0?MAKELRESULT(0,MNC_IGNORE):MAKELRESULT(match,count==1?MNC_EXECUTE:MNC_SELECT);
}
void paintMenuRemainder(HWND hwnd) {
    if(!preferences.dark)return;
    MENUBARINFO info{sizeof(info)};RECT last{},frame{};auto menu=GetMenu(hwnd);
    if(!menu || !GetMenuBarInfo(hwnd,OBJID_MENU,0,&info) || !GetMenuItemRect(hwnd,menu,GetMenuItemCount(menu)-1,&last))return;
    GetWindowRect(hwnd,&frame);RECT r=info.rcBar;r.left=last.right;OffsetRect(&r,-frame.left,-frame.top);
    auto dc=GetWindowDC(hwnd);FillRect(dc,&r,appBackground());
    // Cover the native menu bottom edge with a subtle one-pixel divider.
    POINT clientOrigin{0,0};ClientToScreen(hwnd,&clientOrigin);
    RECT edge{info.rcBar.left-frame.left,info.rcBar.bottom-frame.top-1,
        info.rcBar.right-frame.left,clientOrigin.y-frame.top};
    if(edge.bottom>edge.top) {auto brush=CreateSolidBrush(RGB(35,43,54));FillRect(dc,&edge,brush);DeleteObject(brush);}
    ReleaseDC(hwnd,dc);
}
