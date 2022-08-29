namespace Server.Services.FilesManagers;
public interface IFileEntity
{
    public string FileName { get; }
    public byte[] EncryptionSalt { get; }
}