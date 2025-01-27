using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

public static class GoogleSigningKeys
{
    private static readonly HttpClient httpClient = new HttpClient();
    private const string GooglePublicKeysUrl = "https://www.googleapis.com/robot/v1/metadata/x509/securetoken@system.gserviceaccount.com";

    public static IEnumerable<SecurityKey> GetIssuerSigningKeys()
    {
        var response = httpClient.GetStringAsync(GooglePublicKeysUrl).GetAwaiter().GetResult();
        var keys = JsonSerializer.Deserialize<Dictionary<string, string>>(response);

        var signingKeys = new List<SecurityKey>();
        foreach (var key in keys)
        {
            signingKeys.Add(new X509SecurityKey(new System.Security.Cryptography.X509Certificates.X509Certificate2(System.Text.Encoding.UTF8.GetBytes(key.Value))));
        }

        return signingKeys;
    }
}