// mfcdemo.h : main header file for the MFCDEMO application
//

#pragma once

#ifndef __AFXWIN_H__
	#error "include 'stdafx.h' before including this file for PCH"
#endif

#include "resource.h"		// main symbols


class CmfcdemoApp : public CWinApp
{
public:
	CmfcdemoApp();

public:
	// Overrides
	virtual BOOL InitInstance();

	// Message Handling
	DECLARE_MESSAGE_MAP()
};

extern CmfcdemoApp theApp;
