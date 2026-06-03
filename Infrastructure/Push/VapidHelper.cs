using System.Security.Cryptography;

namespace MyCars.Infrastructure.Push;

public static class VapidHelper
{
    /// <summary>
    /// Genera una coppia di chiavi VAPID (curva P-256).
    /// Usare una volta sola e salvare le chiavi via user-secrets.
    /// </summary>
    public static (string PublicKey, string PrivateKey) GenerateKeys()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var p = ecdsa.ExportParameters(includePrivateParameters: true);

        // Chiave privata: base64url del valore d (32 byte)
        var privateKey = Base64UrlEncode(p.D!);

        // Chiave pubblica VAPID: punto non compresso 04 || x (32) || y (32)
        var pub = new byte[65];
        pub[0] = 0x04;
        p.Q.X!.CopyTo(pub, 1);
        p.Q.Y!.CopyTo(pub, 33);
        var publicKey = Base64UrlEncode(pub);

        return (publicKey, privateKey);
    }

    internal static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
