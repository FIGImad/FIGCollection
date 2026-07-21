Accessing Certificates via Service Account
--------------------------------------------------
When working with certificates under a service account, you need to ensure proper certificate installation and permissions. 
Here's how to handle this scenario:

1. Installing Certificates for Service Accounts
--------------------------------------------------
* Open MMC (mmc.exe)
* Add the Certificates snap-in: File > Add/Remove Snap-in
* Select "Certificates" > Add
* Choose "Computer account" > Next > Local computer > Finish

2- Install the certificate:
-----------------------------
* Right-click "Trusted Root Certification Authorities" > All Tasks > Import
* Browse to your certificate file (.pfx or .cer)
* Complete the wizard

3- Locate ThumpPrint for the newly added certificate (e.g. localhost) 
-----------------------------------------------------------------------
* right click on certificate > All Tasks > Open
* in Details tab, find ThumpPrint and copy


or automated through powershell

# For machine-wide access (all service accounts)
Import-Certificate -FilePath "C:\Roots\Keys\ServiceCert.pfx" -CertStoreLocation Cert:\LocalMachine\Root

# If using PFX with password (service account will need access)
$certPassword = ConvertTo-SecureString -String "F!GPa55w0rd" -Force -AsPlainText
Import-PfxCertificate -FilePath "C:\Roots\Keys\ServiceCert.pfx" -CertStoreLocation Cert:\LocalMachine\My -Password $certPassword




4- in C# code, create HttpClient with certificate
-----------------------------------------------------
 
public async Task<string> PostJsonAsync<T>(string url, T content)
{
	var client = CreateHttpClientWithCert();
	if (client == null)
	{
		throw new Exception("Error creating HttpClient with Certificate");
	}
	//var client = _httpClientFactory.CreateClient();

	// Serialize the content to JSON
	string jsonContent = JsonSerializer.Serialize(content);
	var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

	AddTokenHeader(client);

	// Send POST request
	HttpResponseMessage response = await client.PostAsync(url, httpContent);
	response.EnsureSuccessStatusCode();

	// Read and return the response content as a string
	return await response.Content.ReadAsStringAsync();
}



public HttpClient CreateHttpClientWithCert()
{
    const string thumbprint = "95e0c883a270b03fa8da97db1a4d0aef46fcac6d";

    var handler = new HttpClientHandler();
    var cert = GetCertificateFromStore(thumbprint);

    if (cert != null)
    {
        handler.ClientCertificates.Add(cert);
    }

    return new HttpClient(handler);
}


private X509Certificate2? GetCertificateFromStore(string thumbprint)
{
    using (var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
    {
        store.Open(OpenFlags.ReadOnly);
        var certs = store.Certificates.Find(
            X509FindType.FindByThumbprint,
            thumbprint,
            validOnly: false);

        return certs.Count > 0 ? certs[0] : null;
    }
}
