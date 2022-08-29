using System.Security.Cryptography;
namespace Server.Services.FilesManagers.Cryptography;
public class ClearCryptoTransformFactory : ICryptoTransformFactory<byte[], byte[]>
{
    public ICryptoTransform CreateEncryption(byte[] _, byte[] __) => new ClearCryptoTransform();
    public ICryptoTransform CreateDecryption(byte[] _, byte[] __) => new ClearCryptoTransform();
}
