using System;
using System.Text;
using Microsoft.EntityFrameworkCore.DataEncryption;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Options;
namespace Server.Services;
public class DbEncryptionProvider
{
    private static string _encryptionKey, _decryptionKey;
    private static byte _xorKey;
    public static void Init(IServiceProvider sp)
    {
        var appOptions = sp.GetRequiredService<IOptions<AppOptions>>().Value;
        _encryptionKey = appOptions.DbEncryptionKey;
        _decryptionKey = appOptions.DbDecryptionKey;
        //_xorKey = (byte)_encryptionKey[0];
    }
    public string Encrypt(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        for (int i = 0; i < bytes.Length; i++) { bytes[i] ^= _xorKey; }
        return Convert.ToBase64String(bytes);
    }

    public string Decrypt(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        for (int i = 0; i < bytes.Length; i++) { bytes[i] ^= _xorKey; }
        return Convert.ToBase64String(bytes);
    }
}