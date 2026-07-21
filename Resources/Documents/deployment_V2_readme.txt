Router Setup
---------------------------
* Router level, assign the server a fixed local network IP address (e.g. 192.168.1.58)
* Router level, open the SSH port 2278


Server installation:
---------------------------
* Install MSSQL with SQL authentication
* Create the user: sysfig (standard user)
* Add the user to list of RDP Users to connect remotely 

* Open Local Users and Groups (Press Win + R, type lusrmgr.msc) and Goto "Local Users and Groups"->Users. Right-click on user "sysfig", properties
	- Uncheck "User must change password at next logon"
	- Check "User Cannot change password"
	- Check "Password never expires"
	- Uncheck "Account is disabled"
	- Uncheck "Account is locked out"
* Ensure all users disabled except created admin account(s) and figsys


.NET Platform 
-------------------
* Services are dependent on the presence of the Windows .NET Runtime for .NET Platform 9
	- Download "ASP.NET Core 9.0 Runtime (v9.0.6) - Windows x64 Installer" or newer and run it
	- Download "ASP.NET Core 9.0 Runtime (v9.0.6) - Windows Hosting Bundle Installer" or newer and run it. 


Database Setup
---------------------------
* Update database with FIGAutoTrade and FIGUser databases
* Create "roots" user in security with password: <roots' password>
	- map the user "roots" to FIGAutoTrade and FIGUser as "public" and "owner" member
	- map the user "roots" to master as public
	- connection string can be now as 
	"Data Source=localhost;Initial Catalog=FigAutoTrade;Persist Security Info=False;User ID=roots;Password=<roots' password>; Encrypt=False;TrustServerCertificate=True"
	"Data Source=localhost;Initial Catalog=FigUser;Persist Security Info=False;User ID=roots;Password=<roots' password>; Encrypt=False;TrustServerCertificate=True"


Application Setup
---------------------------
* Deploy all services
* login to the server using sysfig
* For each service in C:\Roots\Services update appsettings.vars.json and appsettings.json
	- Use DataProtectionUtil.exe -> Store -> Store Location (CurrentUser) -> Pass Phrase:
	  Pass Phrase: F!gRootLnk1
		Replace "Controller_Password" value in appsettings.vars.json with the Encrypted Passphrase  
		("Controller_Password": <password of the user "SVC" in FIGUser database>)
	  Pass Phrase: Data Source=localhost;Initial Catalog=FigAutoTrade;Persist Security Info=False;User ID=roots;Password=<roots' password>; Encrypt=False;TrustServerCertificate=True
		Replace "DBConnection_Default" value in appsettings.vars.json with the Encrypted Passphrase  
	  Pass Phrase: Data Source=localhost;Initial Catalog=FigUser;Persist Security Info=False;User ID=roots;Password=<roots' password>; Encrypt=False;TrustServerCertificate=True
		Replace "DBConnection_Users" value in appsettings.vars.json with the Encrypted Passphrase  
	  Pass Phrase: VmYq3t6w9z$B&E)H@McQfTjWnZr4u7x!
		Replace "Jwt_Key" value in appsettings.vars.json with the Encrypted Passphrase  
	  Pass Phrase: https://www.FIG.com.jo
		Replace "Jwt_Issuer" value in appsettings.vars.json with the Encrypted Passphrase  
	  Pass Phrase: https://localhost:44337
		Replace "Jwt_Audience" value in appsettings.vars.json with the Encrypted Passphrase  
	  Pass Phrase: <gmail user name>
		Replace "SMTP_Username" value in appsettings.vars.json with the Encrypted Passphrase  
	  Pass Phrase: <gmail application ID password>
		Replace "SMTP_Password" value in appsettings.vars.json with the Encrypted Passphrase  

* Install Services as Windows service 
	Option A: Using powershell 
		New-Service -Name "FIGControllerSvc" `
				-BinaryPathName "C:\Roots\Services\FIGControllerSvc\FIGControllerSvc.exe" `
				-DisplayName "FIGControllerSvc" `
				-Description "FIG Main Service Bus" `
				-StartupType Automatic
	Option B: using command line
		sc create "FIGControllerSvc" binpath= "C:\Roots\Services\FIGControllerSvc\FIGControllerSvc.exe --contentRoot !contentRoot!" start= auto
	Option C: using service-manager.bat Batch file (in deployment package at "C:\Roots\Scripts & Tools")
		C:\Roots\Scripts & Tools> .\service-manager.bat install FIGAutoTradeSvc "C:\Roots\Services\FIGAutoTradeSvc\FIGAutoTradeSvc.exe"
* Edit each service and change logon settings to start as "figsys" user. Provide the correct username and password for "figsys"


Certificate Installation
---------------------------
* Create self-signed certificate using powershell 
	New-SelfSignedCertificate `
	  -DnsName "localhost", "myserver.local", "127.0.0.1", "192.168.1.58" `
	  -CertStoreLocation "cert:\LocalMachine\My" `
	  -FriendlyName "MultiHostCert" `
	  -NotAfter (Get-Date).AddYears(5) `
	  -KeyExportPolicy Exportable

* Open (Press Win + R, type msc) and add Certificate snap-in for (Local Computer)
	- Go to Certificates/Personal/Certificate
	- Import cert.pfx  (enter the password used in creation - i.e. "F!GPa55w0rd@123")
	- double-click on imported certificate and check details.. go the thumpprint and note it down (e.g. d4d74e27c82f53e27487dcb2d79dd0d32d32b483)
	- make sure that this is the same thumpprint used in all aspsettings.vars.json
	- IMPORTANT - rightclick on localhost certificate that was added, All Tasks, Manage Private Key.  Add figsys user to list of users 


Firewall Settings
----------------------
Open firewall to 
 - FIGControllerSvc
 - FIGAdminInterfaceSvc
 - MSSQL database
	