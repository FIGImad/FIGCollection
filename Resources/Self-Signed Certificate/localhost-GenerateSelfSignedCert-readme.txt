Using Powersshell to generate a self-signed certificate for development purposes
----------------------------------------------------------------------------------

$cert = New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "cert:\LocalMachine\My"
$pwd = ConvertTo-SecureString -String "F!GPa55w0rd" -Force -AsPlainText
Export-PfxCertificate -Cert "cert:\LocalMachine\My\$($cert.Thumbprint)" -FilePath "C:\Roots\Keys\ServiceCert.pfx" -Password $pwd