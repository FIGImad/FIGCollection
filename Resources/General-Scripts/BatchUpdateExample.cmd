@echo off
@rem -------------------------
@rem Stop control service
@rem -------------------------
net stop TARGET_SERVICE

@rem wait 6 seconds
ping 127.0.0.1 -n 6 > nul

@make copies and updates
xcopy "DIST_Folder" "c:\Program Files\TARGET_FOLDER" /v /s /y /c
set DeleteFile="c:\Program Files\TARGET_FOLDER\Services\Plugins\PLUGIN_FILE_TO_BE_DELETED.dll"
if Exist %DeleteFile% del %DeleteFile%

@rem -------------------------
@rem Modify Registry settings
@rem -------------------------
Regedit /s "Registry\REG_SECTION.reg"

@rem --------------------------
@rem Restart control service
@rem --------------------------
net start TARGET_SERVICE

@rem --------------------------
@rem Update database script
@rem --------------------------
sqlcmd -S localhost\SQLINSTANCE -U USER_NAME -P "PASSWORD" -i "SCHEMA_OR_UPDATE_FILE.sql" 

