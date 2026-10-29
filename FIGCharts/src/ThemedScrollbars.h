#pragma once
// Custom non-client scrollbars retain the controls' native scroll ranges and wheel/key handling.
namespace themedScroll {
constexpr UINT_PTR subclassId=0x46494753, repeatTimer=0x46494754;
constexpr wchar_t trackProperty[]=L"FIGCharts.ScrollTrack";
struct State {bool painting=false, dragging=false;int bar=SB_VERT,hot=-1,grab=0,command=-1;};
struct Geometry {RECT rect{},thumb{};int start=0,end=0,arrow=0,thumbLength=0;SCROLLINFO info{sizeof(SCROLLINFO),SIF_ALL};};
inline bool geometry(HWND h,int bar,Geometry& g) {
    SCROLLBARINFO b{sizeof(b)};
    if(!(GetWindowLongPtr(h,GWL_STYLE)&(bar==SB_VERT?WS_VSCROLL:WS_HSCROLL)) || !GetScrollBarInfo(h,bar==SB_VERT?OBJID_VSCROLL:OBJID_HSCROLL,&b))return false;
    if(!GetScrollInfo(h,bar,&g.info))return false;
    RECT w{};GetWindowRect(h,&w);g.rect=b.rcScrollBar;OffsetRect(&g.rect,-w.left,-w.top);
    bool vertical=bar==SB_VERT;int length=vertical?g.rect.bottom-g.rect.top:g.rect.right-g.rect.left;
    g.arrow=std::min<int>(vertical?g.rect.right-g.rect.left:g.rect.bottom-g.rect.top,length/2);
    g.start=(vertical?g.rect.top:g.rect.left)+g.arrow;g.end=(vertical?g.rect.bottom:g.rect.right)-g.arrow;
    int track=std::max(0,g.end-g.start);double range=double(g.info.nMax)-g.info.nMin+1;
    g.thumbLength=std::min(track,std::max(18,int(track*g.info.nPage/std::max(1.0,range))));
    int maximum=std::max(g.info.nMin,g.info.nMax-int(g.info.nPage?g.info.nPage-1:0));
    int offset=maximum==g.info.nMin?0:int(double(g.info.nPos-g.info.nMin)*(track-g.thumbLength)/(maximum-g.info.nMin));
    g.thumb=g.rect;if(vertical){g.thumb.top=g.start+offset;g.thumb.bottom=g.thumb.top+g.thumbLength;}else{g.thumb.left=g.start+offset;g.thumb.right=g.thumb.left+g.thumbLength;}
    return true;
}
inline void paint(HWND h,State& state,HDC target=nullptr) {
    if(state.painting || (!target && !IsWindowVisible(h)))return;state.painting=true;
    auto dc=target?target:GetWindowDC(h);int saved=SaveDC(dc);
    auto track=CreateSolidBrush(preferences.dark?RGB(24,32,43):RGB(240,242,245));
    for(int bar:{SB_HORZ,SB_VERT}) {Geometry g;if(!geometry(h,bar,g))continue;FillRect(dc,&g.rect,track);
        bool enabled=IsWindowEnabled(h) && double(g.info.nMax)-g.info.nMin+1>g.info.nPage;
        auto color=!enabled?(preferences.dark?RGB(40,49,61):RGB(217,221,226)):(state.hot==bar || (state.dragging&&state.bar==bar))?(preferences.dark?RGB(111,130,151):RGB(126,141,159)):(preferences.dark?RGB(66,81,99):RGB(175,185,198));
        auto brush=CreateSolidBrush(color);auto oldBrush=SelectObject(dc,brush);auto oldPen=SelectObject(dc,GetStockObject(NULL_PEN));RECT thumb=g.thumb;
        if(bar==SB_VERT)InflateRect(&thumb,-4,-1);else InflateRect(&thumb,-1,-4);
        if(thumb.right>thumb.left&&thumb.bottom>thumb.top)RoundRect(dc,thumb.left,thumb.top,thumb.right,thumb.bottom,6,6);
        SelectObject(dc,oldBrush);SelectObject(dc,oldPen);DeleteObject(brush);
        auto pen=CreatePen(PS_SOLID,1,color);oldPen=SelectObject(dc,pen);
        for(int direction:{-1,1}) {int x=(g.rect.left+g.rect.right)/2,y=(g.rect.top+g.rect.bottom)/2;
            if(bar==SB_VERT)y=direction<0?g.rect.top+g.arrow/2:g.rect.bottom-g.arrow/2;
            else x=direction<0?g.rect.left+g.arrow/2:g.rect.right-g.arrow/2;
            if(bar==SB_VERT){MoveToEx(dc,x-3,y-direction,nullptr);LineTo(dc,x,y+2*direction);LineTo(dc,x+3,y-direction);}
            else{MoveToEx(dc,x-direction,y-3,nullptr);LineTo(dc,x+2*direction,y);LineTo(dc,x-direction,y+3);}
        }SelectObject(dc,oldPen);DeleteObject(pen);
    }
    Geometry hbar,vbar;if(geometry(h,SB_HORZ,hbar)&&geometry(h,SB_VERT,vbar)){RECT corner{vbar.rect.left,hbar.rect.top,vbar.rect.right,hbar.rect.bottom};FillRect(dc,&corner,track);}
    DeleteObject(track);RestoreDC(dc,saved);if(!target)ReleaseDC(h,dc);state.painting=false;
}
inline void position(HWND h,int bar,int target) {
    SCROLLINFO info{sizeof(info),SIF_ALL};GetScrollInfo(h,bar,&info);
    target=std::clamp(target,info.nMin,std::max(info.nMin,info.nMax-int(info.nPage?info.nPage-1:0)));
    if(target==info.nPos)return;
    wchar_t name[64]{};GetClassNameW(h,name,64);
    if(_wcsicmp(name,WC_LISTVIEWW)==0) {
        int delta=target-info.nPos;
        if(bar==SB_VERT){RECT row{};if(ListView_GetItemRect(h,ListView_GetTopIndex(h),&row,LVIR_BOUNDS))delta*=row.bottom-row.top;}
        ListView_Scroll(h,bar==SB_HORZ?delta:0,bar==SB_VERT?delta:0);
    } else if(bar==SB_VERT && (_wcsicmp(name,L"ListBox")==0 || _wcsicmp(name,L"ComboLBox")==0))SendMessage(h,LB_SETTOPINDEX,target,0);
    else {
        // The main chart reads this full-width value rather than the 16-bit scroll message field.
        SetPropW(h,trackProperty,reinterpret_cast<HANDLE>(static_cast<INT_PTR>(target)+1));
        SendMessage(h,bar==SB_VERT?WM_VSCROLL:WM_HSCROLL,MAKEWPARAM(SB_THUMBPOSITION,target),0);
        RemovePropW(h,trackProperty);
    }
}
inline POINT windowPoint(HWND h,LPARAM lp,bool client) {POINT p{GET_X_LPARAM(lp),GET_Y_LPARAM(lp)};if(client)ClientToScreen(h,&p);RECT w{};GetWindowRect(h,&w);p.x-=w.left;p.y-=w.top;return p;}
inline void step(HWND h,State& s) {
    Geometry g;if(!geometry(h,s.bar,g))return;
    POINT p{};GetCursorPos(&p);RECT w{};GetWindowRect(h,&w);p.x-=w.left;p.y-=w.top;if(!PtInRect(&g.rect,p))return;
    int coord=s.bar==SB_VERT?p.y:p.x,thumbStart=s.bar==SB_VERT?g.thumb.top:g.thumb.left;
    if(s.command==SB_PAGEUP && coord>=thumbStart)return;
    if(s.command==SB_PAGEDOWN && coord<thumbStart+g.thumbLength)return;
    SendMessage(h,s.bar==SB_VERT?WM_VSCROLL:WM_HSCROLL,s.command,0);
}
inline LRESULT CALLBACK proc(HWND h,UINT msg,WPARAM wp,LPARAM lp,UINT_PTR,DWORD_PTR refData) {
    auto s=reinterpret_cast<State*>(refData);
    if(msg==WM_NCDESTROY){KillTimer(h,repeatTimer);RemoveWindowSubclass(h,proc,subclassId);auto result=DefSubclassProc(h,msg,wp,lp);delete s;return result;}
    if(msg==WM_NCLBUTTONDOWN || msg==WM_NCLBUTTONDBLCLK) {
        int bar=wp==HTVSCROLL?SB_VERT:wp==HTHSCROLL?SB_HORZ:-1;Geometry g;
        if(bar>=0 && geometry(h,bar,g)) {
            if(!IsWindowEnabled(h)||double(g.info.nMax)-g.info.nMin+1<=g.info.nPage)return 0;
            auto p=windowPoint(h,lp,false);int coord=bar==SB_VERT?p.y:p.x,thumbStart=bar==SB_VERT?g.thumb.top:g.thumb.left;
            s->bar=bar;s->hot=bar;s->dragging=PtInRect(&g.thumb,p)!=FALSE;s->grab=coord-thumbStart;
            s->command=coord<g.start?SB_LINEUP:coord>=g.end?SB_LINEDOWN:coord<thumbStart?SB_PAGEUP:SB_PAGEDOWN;
            SetCapture(h);if(!s->dragging){SendMessage(h,bar==SB_VERT?WM_VSCROLL:WM_HSCROLL,s->command,0);SetTimer(h,repeatTimer,350,nullptr);}paint(h,*s);return 0;
        }
    }
    if(msg==WM_TIMER && wp==repeatTimer){SetTimer(h,repeatTimer,65,nullptr);step(h,*s);paint(h,*s);return 0;}
    if(msg==WM_MOUSEMOVE && GetCapture()==h && (s->dragging||s->command>=0)) {
        if(s->dragging){Geometry g;if(geometry(h,s->bar,g)){auto p=windowPoint(h,lp,true);int distance=g.end-g.start-g.thumbLength;
            if(distance>0){int pixel=std::clamp<int>((s->bar==SB_VERT?p.y:p.x)-s->grab-g.start,0,distance);int maximum=std::max(g.info.nMin,g.info.nMax-int(g.info.nPage?g.info.nPage-1:0));position(h,s->bar,g.info.nMin+int(std::llround(double(pixel)*(maximum-g.info.nMin)/distance)));}}}
        paint(h,*s);return 0;
    }
    if(msg==WM_LBUTTONUP && GetCapture()==h && (s->dragging||s->command>=0)){s->dragging=false;s->command=-1;KillTimer(h,repeatTimer);ReleaseCapture();paint(h,*s);return 0;}
    if(msg==WM_CAPTURECHANGED || msg==WM_CANCELMODE){s->dragging=false;s->command=-1;KillTimer(h,repeatTimer);}
    if(msg==WM_NCMOUSEMOVE){s->hot=wp==HTVSCROLL?SB_VERT:wp==HTHSCROLL?SB_HORZ:-1;TRACKMOUSEEVENT t{sizeof(t),TME_LEAVE|TME_NONCLIENT,h,0};TrackMouseEvent(&t);}
    if(msg==WM_NCMOUSELEAVE)s->hot=-1;
    auto result=DefSubclassProc(h,msg,wp,lp);
    if(msg==WM_PRINT && (lp&PRF_NONCLIENT))paint(h,*s,reinterpret_cast<HDC>(wp));
    if(msg==WM_PAINT || msg==WM_NCPAINT || msg==WM_NCACTIVATE || msg==WM_SIZE || msg==WM_WINDOWPOSCHANGED || msg==WM_MOUSEWHEEL || msg==WM_MOUSEHWHEEL || msg==WM_KEYDOWN || msg==WM_VSCROLL || msg==WM_HSCROLL || msg==WM_NCMOUSEMOVE || msg==WM_NCMOUSELEAVE || msg==WM_THEMECHANGED || msg==WM_STYLECHANGED || msg==WM_CAPTURECHANGED)paint(h,*s);
    return result;
}
inline void attach(HWND h) {if(!h)return;DWORD_PTR refData=0;if(!GetWindowSubclass(h,proc,subclassId,&refData)){auto s=new State;if(!SetWindowSubclass(h,proc,subclassId,reinterpret_cast<DWORD_PTR>(s)))delete s;}else paint(h,*reinterpret_cast<State*>(refData));}
}
