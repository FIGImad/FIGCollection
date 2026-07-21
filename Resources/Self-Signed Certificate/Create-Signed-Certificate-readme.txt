1- Generate Certificate with Exportable Key  (from powershell)
New-SelfSignedCertificate `
  -DnsName "localhost" `
  -CertStoreLocation "Cert:\LocalMachine\My" `
  -FriendlyName "Localhost Dev Cert" `
  -KeyExportPolicy Exportable `
  -KeySpec Signature `
  -KeyLength 2048 `
  -NotAfter (Get-Date).AddYears(5) `
  -KeyUsage KeyEncipherment, DigitalSignature `
  -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1")  # Server Auth





1- Generate a certificate. In PowerShell ISE interface execute the following script

$cert = New-SelfSignedCertificate -DnsName mydemowebapp.net -CertStoreLocation cert:\LocalMachine\My
$pwd = ConvertTo-SecureString -String "MyPassword" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath C:\Roots\Services\cert.pfx -Password $pwd


2- In appsettings.json add the following section with the Certificate Section

  "Kestrel": {
    "EndPoints": {
      "Https": {
        "Url": "https://localhost:5010",
        "Certificate": {
          "Path": "C:\\Roots\\Services\\cert.pfx",
          "Password": "MyPassword",
          "AllowInvalid": "true"
        }
      }
    }
  } 	





  // Another way
  $names = @('localhost','FIGServices', $env:COMPUTERNAME)
$cert = New-SelfSignedCertificate `
  -DnsName $names `
  -CertStoreLocation Cert:\LocalMachine\My `
  -KeyExportPolicy Exportable `
  -FriendlyName 'Dev HTTPS (localhost + AppSvc)' `
  -NotAfter (Get-Date).AddYears(5)

# 2) Export PFX for apps that load from file
$pwd = ConvertTo-SecureString -String 'MyPassword' -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath 'C:\Roots\Services\cert.pfx' -Password $pwd

# 3) Trust the cert by importing the *public* CER into Trusted Root
Export-Certificate -Cert $cert -FilePath 'C:\Roots\Services\cert.cer'
Import-Certificate -FilePath 'C:\Roots\Services\cert.cer' -CertStoreLocation Cert:\LocalMachine\Root