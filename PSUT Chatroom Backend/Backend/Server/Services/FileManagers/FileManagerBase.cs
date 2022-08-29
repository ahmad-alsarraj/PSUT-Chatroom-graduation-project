using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Server.Services.UserSystem;
using SkiaSharp;
using Server.Db.Entities;
using Server.Services.FilesManagers.Cryptography;
using System.Security.Cryptography;

namespace Server.Services.FilesManagers
{
    public abstract class FileManagerBase<TFileEntity> where TFileEntity : IFileEntity
    {
        protected static string GetSaveDirectory(IServiceProvider sp, string folderName)
        {
            var appOptions = sp.GetRequiredService<IOptions<AppOptions>>().Value;
            return Path.Combine(appOptions.DataSaveDirectory, folderName);
        }
        private static byte[] s_encryptionKey;
        protected static void Init(IServiceProvider sp, string folderName)
        {
            var saveDirectory = GetSaveDirectory(sp, folderName);
            var appOptions = sp.GetRequiredService<IOptions<AppOptions>>().Value;
            s_encryptionKey = Convert.FromBase64String(appOptions.FilesEncryptionKey);
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }
        }
        private readonly string _saveDirectory;
        private readonly ICryptoTransformFactory<byte[], byte[]> _cryptoTransformFactory;
        protected FileManagerBase(string saveDirectory, ICryptoTransformFactory<byte[], byte[]> cryptoTransformFactory)
        {
            _saveDirectory = saveDirectory;
            _cryptoTransformFactory = cryptoTransformFactory;
        }
        public string GetFilePath(TFileEntity entity) => Path.Combine(_saveDirectory, entity.FileName);

        public async Task SaveFile(TFileEntity entity, Stream content)
        {
            var filePath = GetFilePath(entity);
            using var cryptoTransform = _cryptoTransformFactory.CreateEncryption(s_encryptionKey, entity.EncryptionSalt);

            await using var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            await using var cryptoStream = new CryptoStream(fileStream, cryptoTransform, CryptoStreamMode.Write);
            await content.CopyToAsync(cryptoStream).ConfigureAwait(false);
            await cryptoStream.FlushAsync().ConfigureAwait(false);
            fileStream.SetLength(fileStream.Position);
            await fileStream.FlushAsync().ConfigureAwait(false);
        }
        public async Task SaveBase64File(TFileEntity entity, string contentBase64)
        {
            var filePath = GetFilePath(entity);
            using var cryptoTransform = _cryptoTransformFactory.CreateEncryption(s_encryptionKey, entity.EncryptionSalt);

            await using var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            await using var cryptoStream = new CryptoStream(fileStream, cryptoTransform, CryptoStreamMode.Write);

            await using var content = await Utility.DecodeBase64Async(contentBase64).ConfigureAwait(false);
            await content.CopyToAsync(cryptoStream).ConfigureAwait(false);
            await cryptoStream.FlushAsync().ConfigureAwait(false);
            fileStream.SetLength(content.Position);
            await fileStream.FlushAsync().ConfigureAwait(false);
        }
        public async Task SaveImage(TFileEntity entity, Stream content)
        {
            using var pic = SKImage.FromEncodedData(content);
            using var encodedPic = pic.Encode(SKEncodedImageFormat.Jpeg, 100).AsStream();

            await SaveFile(entity, encodedPic).ConfigureAwait(false);
        }
        public async Task SaveBase64Image(TFileEntity entity, string contentBase64)
        {
            await using var decodedBase64 = await Utility.DecodeBase64Async(contentBase64).ConfigureAwait(false);
            await SaveImage(entity, decodedBase64);
        }

        public Stream? GetFile(TFileEntity entity)
        {
            var filePath = GetFilePath(entity);
            if (!File.Exists(filePath)) { return null; }

            using var cryptoTransform = _cryptoTransformFactory.CreateEncryption(s_encryptionKey, entity.EncryptionSalt);
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var cryptoStream = new CryptoStream(fileStream, cryptoTransform, CryptoStreamMode.Read);
            return cryptoStream;
        }

        public void DeleteFile(TFileEntity entity)
        {
            var filePath = GetFilePath(entity);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}