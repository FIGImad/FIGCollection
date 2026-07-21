IIS installation and configuration
----------------------------------
April 3, 2014

Distrib Files
-------------
  * RootsAutoTrade	<-- Published Web Site folder that contains both the back-end and portal
  * Roots		<-- Contains templates, logs, and security keys. Required for "Roots Auto-Trade Site" website

Prerequisite for Web Installation
----------------------------------
  * Install: ASP.NET Core 8.0 Runtime (v8.0.3) - Windows Hosting Bundle Installer!


Installation
-------------
* Copy distrib folder "RootsAutoTrade" into C:\inetpub
* Copy distrib folder "Roots" into C:\

Make IIS app run continuously 
-------------------------------
* Win-R (Run)
* type "optionalfeatures"
* Make sure that "Application Development/Application Initialization" is installed

Using IIS Manager
* Create an Application Pool: RootsAutoTrade
* Create the site "Roots Auto-Trade Site"
	- Use the RootsAutoTrade application Pool 
	- Use the path: C:\inetpub\RootsAutoTrade
	- Ensure it is using https
* Configure the site to be running at all times
	- From RootsAutoTrade application pool advanced settings
	    > Set "Start Mode" to "Always Running"
	    > Set "Idle Time-out" to 0
	- From "Roots Auto-Trade Site" site advanced settings
	    > Set "Preload Enabled" to True
		 

Create self-signed certificate for IIS
--------------------------------------
To create a self-signed SSL certificate for an IIS website in Windows, you can use either the IIS Manager directly or PowerShell. Here’s a guide for both methods:

Using IIS Manager
1- Open IIS Manager:
    * Press Win + R, type inetmgr, and press Enter.
2- Open Server Certificates:
    * In the IIS Manager, click on your server name in the left-hand pane.
    * Double-click on Server Certificates in the middle panel under the “IIS” section.
3- Create a Self-Signed Certificate:
    * In the right-hand Actions pane, select Create Self-Signed Certificate.
    * Enter a friendly name for the certificate (e.g., “MySelfSignedCert”).
    * Select Personal as the certificate store location, then click OK.
4- Bind the Certificate to Your Website:
    * In the IIS Manager, go to Sites and select your website.
    * In the right-hand Actions pane, click Bindings.
    * In the Site Bindings window, click Add.
    * Set Type to https and choose your self-signed certificate from the SSL certificate dropdown.
    * Click OK and close the Site Bindings window.



Web.Config
----------
* Ensure that the web.config file generated looks like:

<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <security>
        <requestFiltering>
          <requestLimits maxAllowedContentLength="524288000" />
          <verbs allowUnlisted="false">
            <add verb="GET" allowed="true" />
            <add verb="POST" allowed="true" />
            <add verb="DELETE" allowed="true" />
            <add verb="PUT" allowed="true" />
          </verbs>
        </requestFiltering>
      </security>
      <modules>
        <remove name="WebDAVModule" />
      </modules>
      <handlers>
        <remove name="WebDAV" />
        <remove name="ExtensionlessUrlHandler-Integrated-4.0" />
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
        <add name="ExtensionlessUrlHandler-Integrated-4.0" path="*." verb="GET,HEAD,POST,DEBUG,DELETE,PUT" type="System.Web.Handlers.TransferRequestHandler" resourceType="Unspecified" requireAccess="Script" preCondition="integratedMode,runtimeVersionv4.0" responseBufferLimit="0" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\ATS.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
<!--ProjectGuid: cb7559dc-8c8a-4fee-819e-181063d02677-->