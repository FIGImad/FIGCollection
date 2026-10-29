#pragma once
// File parsing and derived-data calculations run on a worker. Only the UI thread touches HWNDs.
bool loadingFile=false;
LRESULT CALLBACK loadingProc(HWND h,UINT msg,WPARAM wp,LPARAM lp){
    if(msg==WM_CLOSE)return 0; // A parser cannot safely be interrupted halfway through a file.
    if(msg>=WM_CTLCOLORMSGBOX && msg<=WM_CTLCOLORSTATIC)return themedControl(wp);
    return DefWindowProcW(h,msg,wp,lp);
}
struct LoadingIndicator {
    HWND dialog=nullptr,label=nullptr;std::vector<HWND> disabled;
    LoadingIndicator(const std::wstring& text){
        endDrag();KillTimer(window,LegendTimer);legendRefreshPending=false;crossVisible=false;loadingFile=true;
        EnumThreadWindows(GetCurrentThreadId(),[](HWND h,LPARAM param)->BOOL{
            auto self=reinterpret_cast<LoadingIndicator*>(param);if(IsWindowEnabled(h)){self->disabled.push_back(h);EnableWindow(h,FALSE);}return TRUE;
        },reinterpret_cast<LPARAM>(this));
        WNDCLASSW wc{};wc.lpfnWndProc=loadingProc;wc.hInstance=GetModuleHandle(nullptr);wc.lpszClassName=L"FIGChartsLoading";wc.hbrBackground=appBackground();wc.hCursor=LoadCursor(nullptr,IDC_WAIT);RegisterClassW(&wc);
        RECT owner{};GetWindowRect(window,&owner);
        dialog=CreateWindowExW(WS_EX_DLGMODALFRAME,wc.lpszClassName,L"FIGCharts - Loading",WS_POPUP|WS_CAPTION|WS_CLIPCHILDREN,
            owner.left+std::max(0L,(owner.right-owner.left-460)/2),owner.top+std::max(0L,(owner.bottom-owner.top-150)/2),460,150,window,nullptr,wc.hInstance,nullptr);
        if(!dialog){for(auto h:disabled)if(IsWindow(h))EnableWindow(h,TRUE);loadingFile=false;throw std::runtime_error("Could not create the loading indicator.");}
        SetClassLongPtr(dialog,GCLP_HBRBACKGROUND,reinterpret_cast<LONG_PTR>(appBackground()));
        label=CreateWindowW(L"STATIC",text.c_str(),WS_CHILD|WS_VISIBLE|SS_PATHELLIPSIS,18,14,420,44,dialog,nullptr,wc.hInstance,nullptr);
        SendMessage(label,WM_SETFONT,reinterpret_cast<WPARAM>(font),TRUE);
        auto progress=CreateWindowW(PROGRESS_CLASSW,L"",WS_CHILD|WS_VISIBLE|PBS_MARQUEE,18,67,420,18,dialog,nullptr,wc.hInstance,nullptr);
        if(preferences.dark){SetWindowTheme(progress,L"",L"");SendMessage(progress,PBM_SETBKCOLOR,0,RGB(35,45,58));SendMessage(progress,PBM_SETBARCOLOR,0,RGB(88,158,216));}
        SendMessage(progress,PBM_SETMARQUEE,TRUE,30);themeTitleBar(dialog);
        if(!testing){ShowWindow(dialog,SW_SHOWNORMAL);UpdateWindow(dialog);}
    }
    void status(const std::wstring& text){SetWindowTextW(label,text.c_str());if(!testing)UpdateWindow(dialog);}
    ~LoadingIndicator(){
        for(auto h:disabled)if(IsWindow(h))EnableWindow(h,TRUE);
        if(dialog)DestroyWindow(dialog);loadingFile=false;
        if(!testing && IsWindow(window))SetActiveWindow(window);
    }
};
template<class Work> auto loadingTask(Work work){
    auto task=std::async(std::launch::async,std::move(work));bool quit=false;int quitCode=0;
    while(task.wait_for(std::chrono::milliseconds(0))!=std::future_status::ready){
        MsgWaitForMultipleObjectsEx(0,nullptr,30,QS_ALLINPUT,MWMO_INPUTAVAILABLE);
        MSG message{};while(PeekMessageW(&message,nullptr,0,0,PM_REMOVE)){
            if(message.message==WM_QUIT){quit=true;quitCode=static_cast<int>(message.wParam);continue;}
            TranslateMessage(&message);DispatchMessageW(&message);
        }
    }
    if(quit)PostQuitMessage(quitCode);
    return task.get(); // Exceptions are reported by the existing UI error handler.
}
