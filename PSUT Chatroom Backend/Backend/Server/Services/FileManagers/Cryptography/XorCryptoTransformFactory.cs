using System.Security.Cryptography;
namespace Server.Services.FilesManagers.Cryptography;
public class XorCryptoTransformFactory : ICryptoTransformFactory<byte[], byte[]>
{
    public ICryptoTransform CreateEncryption(byte[] key, byte[] _) => new XorCryptoTransform(key[0]);
    public ICryptoTransform CreateDecryption(byte[] key, byte[] _) => new XorCryptoTransform(key[0]);
}
