// mfcdemo.cpp : Defines the class behaviors for the application.
//

#include "stdafx.h"
#include "mfcdemo.h"
#include "mfcdemoDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

BEGIN_MESSAGE_MAP(CmfcdemoApp, CWinApp)
END_MESSAGE_MAP()

//
// Constructor
//
CmfcdemoApp::CmfcdemoApp()
{
}

// The one and only CmfcdemoApp object
CmfcdemoApp theApp;

//
// Initialization
//
BOOL CmfcdemoApp::InitInstance()
{
	// InitCommonControlsEx() is required on Windows XP if an application
	// manifest specifies use of ComCtl32.dll version 6 or later to enable
	// visual styles.  Otherwise, any window creation will fail.
	INITCOMMONCONTROLSEX InitCtrls;
	InitCtrls.dwSize = sizeof(InitCtrls);
	// Set this to include all the common control classes you want to use
	// in your application.
	InitCtrls.dwICC = ICC_WIN95_CLASSES;
	InitCommonControlsEx(&InitCtrls);

	CWinApp::InitInstance();

	CmfcdemoDlg dlg;
	m_pMainWnd = &dlg;
	INT_PTR nResponse = dlg.DoModal();
	
	// Exit application when dialog close
	return FALSE;
}
