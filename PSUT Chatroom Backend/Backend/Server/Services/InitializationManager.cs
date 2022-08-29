using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Server;
using Server.Db;
using Server.Db.Entities;
using Server.Resources;
using Server.Services.FilesManagers;

namespace Server.Services
{
    //works only with postgres
    public class InitializationManager
    {
        private readonly AppDbContext _dbContext;
        private readonly AppOptions _appOptions;
        private readonly RegnewManager _regnewManager;
        private readonly IServiceProvider _serviceProvider;

        public InitializationManager(AppDbContext dbContext, IOptions<AppOptions> appOptions, RegnewManager regnewManager, IServiceProvider serviceProvider)
        {
            _dbContext = dbContext;
            _appOptions = appOptions.Value;
            _regnewManager = regnewManager;
            _serviceProvider = serviceProvider;
        }

        public async Task RecreateDb()
        {
            var dirs = new[]
            {
                UserFileManager.SaveDirectory,
                GroupFileManager.SaveDirectory,
                MessageFileManager.SaveDirectory
            };
            foreach (var dir in dirs)
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }

                Directory.CreateDirectory(dir);
            }

            using (var postgreCon = new NpgsqlConnection(_appOptions.BuildPostgresConnectionString()))
            {
                await postgreCon.OpenAsync().ConfigureAwait(false);
                using (var dropCommand = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_appOptions.DbName}\" WITH (FORCE);", postgreCon))
                {
                    await dropCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
                NpgsqlConnection.ClearAllPools();
                using (var createCommand = new NpgsqlCommand($"CREATE DATABASE \"{_appOptions.DbName}\";", postgreCon))
                {
                    await createCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }

            NpgsqlConnection.ClearAllPools();
            string dbScript = await AppResourcesManager.GetText("DbInitScript.sql").ConfigureAwait(false);

            using (var dbCon = new NpgsqlConnection(_appOptions.BuildAppConnectionString()))
            using (var initCommand = new NpgsqlCommand(dbScript, dbCon))
            {
                await dbCon.OpenAsync().ConfigureAwait(false);
                await initCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        public async Task EnsureDb()
        {
            try
            {
                await using var dbCon = new NpgsqlConnection(_appOptions.BuildAppConnectionString());
                await dbCon.OpenAsync().ConfigureAwait(false);
            }
            catch
            {
                await RecreateDb().ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Depends on regnew mock.
        /// </summary>
        private async Task Seed()
        {
            await _regnewManager.PatchDb().ConfigureAwait(false);

            _dbContext.ChangeTracker.Clear();
            await _dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);
            NpgsqlConnection.ClearAllPools();
            await Task.Delay(500).ConfigureAwait(false);

            SeedingContext seedingContext = new() { ServiceProvider = _serviceProvider };
            seedingContext.Users.AddRange(_dbContext.Users.ToArray());
            seedingContext.Groups.AddRange(_dbContext.Groups.ToArray());
            seedingContext.Conversations.AddRange(_dbContext.Conversations.ToArray());
            seedingContext.GroupsMembers.AddRange(_dbContext.GroupsMembers.ToArray());
            seedingContext.Sections.AddRange(_dbContext.Sections.ToArray());
            seedingContext.Courses.AddRange(_dbContext.Courses.ToArray());
            var existingGroupsIds = seedingContext.Groups.Select(g => g.Id).ToHashSet();
            var existingGroupsMembersIds = seedingContext.GroupsMembers.Select(g => g.Id).ToHashSet();
            var existingConversationsIds = seedingContext.Conversations.Select(g => g.Id).ToHashSet();
            Ping.CreateSeed(seedingContext);
            Group.CreateSeed(seedingContext);
            GroupMember.CreateSeed(seedingContext);
            Conversation.CreateSeed(seedingContext);
            Message.CreateSeed(seedingContext);
            MessageDeliveryInfo.CreateSeed(seedingContext);

            await _dbContext.Pings.AddRangeAsync(seedingContext.Pings).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            await _dbContext.Groups.AddRangeAsync(seedingContext.Groups.Where(g => !existingGroupsIds.Contains(g.Id))).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            await _dbContext.GroupsMembers.AddRangeAsync(seedingContext.GroupsMembers.Where(g => !existingGroupsMembersIds.Contains(g.Id))).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            await _dbContext.Conversations.AddRangeAsync(seedingContext.Conversations.Where(c => !existingConversationsIds.Contains(c.Id))).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            await _dbContext.DirectConversationsMembers.AddRangeAsync(seedingContext.DirectConversationMembers).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            await _dbContext.Messages.AddRangeAsync(seedingContext.Messages).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            await _dbContext.MessagesDeliveryInfo.AddRangeAsync(seedingContext.MessagesDeliveryInfo).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            await User.CreateSeedFiles(seedingContext).ConfigureAwait(false);
            await Group.CreateSeedFiles(seedingContext).ConfigureAwait(false);
            await Message.CreateSeedFiles(seedingContext).ConfigureAwait(false);

            List<string> tablesNames = new();

            await using (var dbCon = new NpgsqlConnection(_appOptions.BuildAppConnectionString()))
            await using (var listCommand =
                new NpgsqlCommand(
                    @"SELECT table_name FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE';",
                    dbCon))
            {
                await dbCon.OpenAsync().ConfigureAwait(false);
                await using var reader = await listCommand.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    tablesNames.Add(reader.GetString(0));
                }
            }

            foreach (var table in tablesNames)
            {
                var conString = _appOptions.BuildAppConnectionString();
                try
                {
                    await using var dbCon = new NpgsqlConnection(_appOptions.BuildAppConnectionString());
                    await using var resetCommand =
                        new NpgsqlCommand(
                            $"SELECT setval(pg_get_serial_sequence('\"{table}\"', 'Id'), coalesce(max(\"Id\"),0) + 1, false) FROM \"{table}\";",
                            dbCon);
                    await dbCon.OpenAsync().ConfigureAwait(false);
                    await resetCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
                catch (NpgsqlException)
                {
                }
            }
        }
        public void InitializeClasses()
        {
            var initializationMethods = Assembly
                            .GetExecutingAssembly()
                            .DefinedTypes
                            .Where(t => !t.IsGenericType)
                            .Select(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                          .FirstOrDefault(m => m.Name == nameof(UserFileManager.Init)))
                            .Where(m => m != null);
            var initializationMethodParams = new object[] { _serviceProvider };
            foreach (var initMethod in initializationMethods)
            {
                initMethod?.Invoke(null, initializationMethodParams);
            }
        }
        public async Task InitializeSystem()
        {
            InitializeClasses();
            await EnsureDb().ConfigureAwait(false);
        }
        public async Task RecreateAndSeedDb()
        {
            await RecreateDb().ConfigureAwait(false);
            await Seed().ConfigureAwait(false);
        }
    }
}