Use service-manager.bat
----------------------------
Usage: service-manager [command] [service_name] [path]
Commands: install, start, stop, delete
Example: service-manager install MyService C:\Path\To\Service.exe


To install service from powershell
----------------------------------

#$acl = Get-Acl "C:\Roots\Services\FIGSignalSvc\FIGSignalSvc.exe"
#$aclRuleArgs = "ansari\administrator", "Read,Write,ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
#$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($aclRuleArgs)
#Write-Output $accessRule
#$acl.SetAccessRule($accessRule)
#$acl | Set-Acl "C:\Roots\Services\FIGSignalSvc\FIGSignalSvc.exe"
New-Service -Name FIGSignalSvc -BinaryPathName "C:\Roots\Services\FIGSignalSvc\FIGSignalSvc.exe --service --contentRoot C:\Roots\Services\FIGSignalSvc" -StartupType Automatic
#New-Service -Name FIGSignalSvc -BinaryPathName "C:\Roots\Services\FIGSignalSvc\FIGSignalSvc.exe --service" -StartupType Automatic


To install service from Cmd line
----------------------------------
SC CREATE "MyService" binpath= "C:\Roots\Services\MyServiceFolder\MyService.exe --service --contentRoot C:\Roots\Services\MyServiceFolder" start=auto
pause

To start service from cmd line
------------------------------
sc start "MyService" 
pause

To stop service from cmd line
------------------------------
sc stop "MyService" 
pause

To delete service from cmd line
------------------------------
sc delete"MyService" 
pause