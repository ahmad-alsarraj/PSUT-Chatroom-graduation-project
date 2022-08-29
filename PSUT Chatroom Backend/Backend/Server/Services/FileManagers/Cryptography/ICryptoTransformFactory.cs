using System.Security.Cryptography;
namespace Server.Services.FilesManagers.Cryptography;
public interface ICryptoTransformFactory<TKey, TIV>
{
    public ICryptoTransform CreateEncryption(TKey key, TIV iv);
    public ICryptoTransform CreateDecryption(TKey key, TIV iv);
}
