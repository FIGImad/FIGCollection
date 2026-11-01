#pragma once
constexpr int GotoYear=1960;
bool readGotoInputs(SYSTEMTIME& value){
    WORD* fields[]{&value.wYear,&value.wMonth,&value.wDay,&value.wHour,&value.wMinute};
    for(int i=0;i<5;++i){wchar_t text[16]{};GetDlgItemTextW(gotoWindow,GotoYear+i,text,16);int length=lstrlenW(text);if(length==0 || (i==0 && length!=4))return false;
        for(int j=0;j<length;++j)if(text[j]<L'0'||text[j]>L'9')return false;
        *fields[i]=static_cast<WORD>(wcstoul(text,nullptr,10));
    }
    value.wSecond=value.wMilliseconds=0;FILETIME ft{};return SystemTimeToFileTime(&value,&ft)!=FALSE;
}
void setGotoInputs(SYSTEMTIME value){
    value.wSecond=value.wMilliseconds=0;bool wasSyncing=syncingGoto;syncingGoto=true;
    WORD fields[]{value.wYear,value.wMonth,value.wDay,value.wHour,value.wMinute};
    for(int i=0;i<5;++i){wchar_t text[16];swprintf_s(text,i==0?L"%04u":L"%02u",fields[i]);SetDlgItemTextW(gotoWindow,GotoYear+i,text);}
    DateTime_SetSystemtime(datePicker,GDT_VALID,&value);DateTime_SetSystemtime(timePicker,GDT_VALID,&value);
    gotoSelection=value;gotoSelectionSet=true;syncingGoto=wasSyncing;
}
LRESULT CALLBACK gotoInputProc(HWND hwnd,UINT msg,WPARAM wp,LPARAM lp,UINT_PTR,DWORD_PTR field){
    if(msg==WM_GETDLGCODE)return DefSubclassProc(hwnd,msg,wp,lp)|DLGC_WANTTAB;
    if(msg==WM_KEYDOWN && wp==VK_TAB){int next=static_cast<int>(field)+((GetKeyState(VK_SHIFT)&0x8000)?-1:1);
        auto target=next<0?datePicker:next>=5?GetDlgItem(gotoWindow,IDOK):GetDlgItem(gotoWindow,GotoYear+next);SetFocus(target);if(next>=0 && next<5)SendMessage(target,EM_SETSEL,0,-1);return 0;}
    if(msg==WM_CHAR && wp==L'\t')return 0;
    auto result=DefSubclassProc(hwnd,msg,wp,lp);if(msg==WM_SETFOCUS)SendMessage(hwnd,EM_SETSEL,0,-1);return result;
}
void gotoInputChanged(int field){
    if(syncingGoto)return;SYSTEMTIME value{};
    if(readGotoInputs(value)){syncingGoto=true;DateTime_SetSystemtime(datePicker,GDT_VALID,&value);DateTime_SetSystemtime(timePicker,GDT_VALID,&value);syncingGoto=false;gotoSelection=value;gotoSelectionSet=true;}
    auto control=GetDlgItem(gotoWindow,GotoYear+field);
    if(GetFocus()==control && GetWindowTextLengthW(control)==(field==0?4:2)){
        auto next=GetDlgItem(gotoWindow,field==4?IDOK:GotoYear+field+1);SetFocus(next);if(field<4)SendMessage(next,EM_SETSEL,0,-1);
    }
}
