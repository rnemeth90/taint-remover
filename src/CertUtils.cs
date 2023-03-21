using System.IO;
using System.Security.Cryptography.X509Certificates;

internal static class CertUtils
{
    public static X509Certificate2Collection LoadPemFileCert(string file)
    {
        var certCollection = new X509Certificate2Collection();
        using (var stream = File.OpenRead(file))
        {
            certCollection.ImportFromPem(new StreamReader(stream).ReadToEnd());
        }

        return certCollection;
    }
}