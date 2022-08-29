using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Server.Services.UserSystem;
using SkiaSharp;
using Server.Db.Entities;
using Server.Services.FilesManagers.Cryptography;

namespace Server.Services.FilesManagers
{
    public class UserFileManager : FileManagerBase<User>
    {
        private const string FolderName = "UsersPictures";
        public static string SaveDirectory { get; private set; }
        public new static void Init(IServiceProvider sp)
        {
            Init(sp, FolderName);
            SaveDirectory = GetSaveDirectory(sp, FolderName);
        }
        public UserFileManager(ICryptoTransformFactory<byte[], byte[]> cryptoTransformFactory) : base(SaveDirectory, cryptoTransformFactory) { }

    }
}