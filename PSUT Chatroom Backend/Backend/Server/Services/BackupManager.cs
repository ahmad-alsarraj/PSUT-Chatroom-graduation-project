using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Server.Db;
using Server.Services.FilesManagers;

namespace Server.Services;
public class BackupManager
{
    public const string FolderName = "Backups";
    public static string SaveDirectory { get; private set; }
    public static void Init(IServiceProvider sp)
    {
        var appOptions = sp.GetRequiredService<IOptions<AppOptions>>().Value;
        SaveDirectory = Path.Combine(appOptions.DataSaveDirectory, FolderName);
        if (!Directory.Exists(SaveDirectory))
        {
            Directory.CreateDirectory(SaveDirectory);
        }
    }
    private static string[] GetFileSystemDirectories() => new string[]
    {
         UserFileManager.SaveDirectory, MessageFileManager.SaveDirectory, GroupFileManager.SaveDirectory
    };
    private readonly AppDbContext _dbContext;
    public BackupManager(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Creates a backup archive in <see cref="SaveDirectory"/> with name equal to DateTime.UtcNow.Ticks, and adds all tables contents to it as json files.
    /// </summary>
    /// <returns>Backup file name.</returns>
    public async Task<string> CreateBackup()
    {
        string backupName = DateTime.UtcNow.Ticks.ToString();
        string backupPath = Path.Combine(SaveDirectory, $"{backupName}.zip");
        using ZipArchive archive = new(new FileStream(backupPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None), ZipArchiveMode.Create);
        async Task AddTable<T>(DbSet<T> table, string entryName) where T : class
        {
            entryName = $"{entryName}.json";
            var entry = archive.CreateEntry(entryName);
            await using var entryStream = entry.Open();
            await JsonSerializer.SerializeAsync(entryStream, table.AsNoTracking().AsEnumerable()).ConfigureAwait(false);
        }
        async Task AddDirectory(string path)
        {
            string directoryName = Path.GetFileName(path);
            foreach (var file in Directory.EnumerateFiles(path))
            {
                var fileEntry = archive.CreateEntry($"{directoryName}/{Path.GetFileName(file)}");
                await using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                await using var entryStream = fileEntry.Open();
                await fileStream.CopyToAsync(entryStream).ConfigureAwait(false);
            }
        }
        await AddTable(_dbContext.Conversations, nameof(AppDbContext.Conversations)).ConfigureAwait(false);
        await AddTable(_dbContext.Courses, nameof(AppDbContext.Courses)).ConfigureAwait(false);
        await AddTable(_dbContext.DirectConversationsMembers, nameof(AppDbContext.DirectConversationsMembers)).ConfigureAwait(false);
        await AddTable(_dbContext.Groups, nameof(AppDbContext.Groups)).ConfigureAwait(false);
        await AddTable(_dbContext.GroupsMembers, nameof(AppDbContext.GroupsMembers)).ConfigureAwait(false);
        await AddTable(_dbContext.Messages, nameof(AppDbContext.Messages)).ConfigureAwait(false);
        await AddTable(_dbContext.MessagesDeliveryInfo, nameof(AppDbContext.MessagesDeliveryInfo)).ConfigureAwait(false);
        await AddTable(_dbContext.Pings, nameof(AppDbContext.Pings)).ConfigureAwait(false);
        await AddTable(_dbContext.Sections, nameof(AppDbContext.Sections)).ConfigureAwait(false);
        await AddTable(_dbContext.Users, nameof(AppDbContext.Users)).ConfigureAwait(false);

        foreach (var d in GetFileSystemDirectories())
        {
            await AddDirectory(d).ConfigureAwait(false);
        }
        return backupName;
    }
    public async Task ClearDb()
    {
        _dbContext.Pings.RemoveRange(_dbContext.Pings);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        _dbContext.MessagesDeliveryInfo.RemoveRange(_dbContext.MessagesDeliveryInfo);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        _dbContext.GroupsMembers.RemoveRange(_dbContext.GroupsMembers);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        _dbContext.DirectConversationsMembers.RemoveRange(_dbContext.DirectConversationsMembers);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        _dbContext.Conversations.RemoveRange(_dbContext.Conversations);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        _dbContext.Messages.RemoveRange(_dbContext.Messages);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        _dbContext.Sections.RemoveRange(_dbContext.Sections);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        _dbContext.Courses.RemoveRange(_dbContext.Courses);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        _dbContext.Users.RemoveRange(_dbContext.Users);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        _dbContext.Groups.RemoveRange(_dbContext.Groups);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        foreach (var d in GetFileSystemDirectories())
        {
            Directory.Delete(d, true);
        }
        foreach (var d in GetFileSystemDirectories())
        {
            Directory.CreateDirectory(d);
        }
    }

    //Might as well provide RestoreDb
}